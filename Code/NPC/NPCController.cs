using Sandbox;
using System;
using System.Collections.Generic;

/// <summary>
/// Controls NPC behavior: item collection, counter delivery, checkout, exit.
/// 8-state machine running on the host only.
/// Replaces Unity's NPCInteractionController.cs.
/// </summary>
public sealed class NPCController : Component
{
	// ── Static event ─────────────────────────────────────────────────
	/// <summary>Fired just before this NPC is destroyed. Used by NPCSpawnManager.</summary>
	public static event Action<NPCController> OnNPCExited;

	// ── Inspector fields ─────────────────────────────────────────────
	[Group( "Detection" )]
	[Property] public float DetectionRadius { get; set; } = 500f;

	[Group( "Interaction" )]
	[Property] public float ReachDistance { get; set; } = 40f;

	[Group( "Interaction" )]
	[Property] public GameObject HandBone { get; set; }

	[Group( "Item Preferences" )]
	[Property] public List<ItemCategory> WantedCategories { get; set; } = new();

	[Group( "Behavior" )]
	[Property] public bool AutoScan { get; set; } = true;
	[Group( "Behavior" )]
	[Property] public float ScanInterval { get; set; } = 1f;
	[Group( "Behavior" )]
	[Property] public float PickupPauseTime { get; set; } = 0.5f;
	[Group( "Behavior" )]
	[Property] public int BatchSize { get; set; } = 4;
	[Group( "Behavior" )]
	[Property] public bool IsCollecting { get; set; } = true;
	[Group( "Behavior" )]
	[Property] public bool DebugLogging { get; set; } = false;

	[Group( "ID Card" )]
	[Property] public NPCIdentity NpcIdentity { get; set; }
	[Group( "ID Card" )]
	[Property] public GameObject IdCardPrefab { get; set; }

	[Group( "Prescription" )]
	[Property] public PrescriptionData Prescription { get; set; }

	// ── State ─────────────────────────────────────────────────────────
	public enum NPCState
	{
		Idle,
		MovingToItem,
		WaitingAtItem,
		PickingUp,
		MovingToCounter,
		PlacingItem,
		WaitingForCheckout,
		MovingToExit
	}

	[Sync( SyncFlags.FromHost )] public int NetworkState { get; set; } = 0;

	public NPCState CurrentState => (NPCState)NetworkState;

	private NPCState _state
	{
		get => (NPCState)NetworkState;
		set => NetworkState = (int)value;
	}

	// Scene references (injected by NPCSpawnManager)
	private List<CounterSlot> _counterSlots = new();
	private GameObject _counterTarget;   // stand position in front of counter
	private GameObject _exitPoint;
	private IDCardSlot _idCardSlot;
	private List<ShelfSlot> _allowedShelfSlots = new();
	private bool _useShelfSlots;

	// Runtime state
	private NavMeshAgent _agent;
	private InteractableItem _currentTargetItem;
	private List<InteractableItem> _heldItems = new();
	private List<GameObject> _placedItems = new();
	private float _scanTimer;
	private float _pauseTimer;
	private float _moveTimer;        // seconds since last MoveTo — guards against velocity=0 on frame 1
	private bool _hasCheckedOut;
	private bool _hasPlacedIDCard;
	private GameObject _spawnedIdCard;

	// Doppelganger
	public DoppelgangerProfile DoppelgangerData { get; private set; }
	public bool IsDoppelganger => DoppelgangerData != null;

	// Animation events
	public event Action OnPickupStart;
	public event Action OnPlaceStart;

	// ── Lifecycle ─────────────────────────────────────────────────────

	protected override void OnAwake()
	{
		_agent = GetComponent<NavMeshAgent>();
		if ( _agent == null )
		{
			Log.Warning( $"[NPCController] {GameObject.Name}: NavMeshAgent component not found!" );
			return;
		}

		if ( DebugLogging ) Log.Info( $"[NPCController] Agent speed={_agent.MaxSpeed}" );

		if ( _agent.MaxSpeed <= 0f )
			_agent.MaxSpeed = 150f;

		// Check if the navmesh exists at our position
		var closest = Scene.NavMesh.GetClosestPoint( WorldPosition );
		if ( DebugLogging ) Log.Info( $"[NPCController] NavMesh closest point to spawn: {closest}" );
		if ( closest == null )
			Log.Warning( "[NPCController] No NavMesh found! Enable NavMesh in the scene editor header." );
	}

	protected override void OnFixedUpdate()
	{
		if ( Networking.IsActive && !Networking.IsHost ) return;

		switch ( _state )
		{
			case NPCState.Idle: HandleIdle(); break;
			case NPCState.MovingToItem: HandleMovingToItem(); break;
			case NPCState.WaitingAtItem: HandleWaitingAtItem(); break;
			case NPCState.PickingUp: HandlePickingUp(); break;
			case NPCState.MovingToCounter: HandleMovingToCounter(); break;
			case NPCState.PlacingItem: HandlePlacingItem(); break;
			case NPCState.WaitingForCheckout: HandleWaitingForCheckout(); break;
			case NPCState.MovingToExit: HandleMovingToExit(); break;
		}
	}

	// ── Scene reference injection ─────────────────────────────────────

	/// <summary>Called by NPCSpawnManager after instantiation to inject shared scene refs.</summary>
	public void AssignSceneReferences( List<CounterSlot> counters, GameObject counterTarget, GameObject exit, IDCardSlot cardSlot, List<ShelfSlot> shelfSlots )
	{
		_counterSlots = counters ?? new();
		_counterTarget = counterTarget;
		_exitPoint = exit;
		_idCardSlot = cardSlot;
		_allowedShelfSlots = shelfSlots ?? new();
		_useShelfSlots = _allowedShelfSlots.Count > 0;
	}

	/// <summary>Assigns a doppelganger profile (null = real patient).</summary>
	public void AssignDoppelgangerProfile( DoppelgangerProfile profile )
	{
		DoppelgangerData = profile;
	}

	// ── Public API ────────────────────────────────────────────────────

	/// <summary>Called by CashRegister to send this NPC to exit.</summary>
	public void TriggerCheckout()
	{
		if ( Networking.IsActive && !Networking.IsHost ) return;
		_hasCheckedOut = true;
	}

	/// <summary>Called by GunCase to kill this NPC.</summary>
	public void Kill()
	{
		if ( Networking.IsActive && !Networking.IsHost ) return;
		FireExitEvent();
		GameObject.Destroy();
	}

	// ── State handlers ────────────────────────────────────────────────

	private void HandleIdle()
	{
		if ( _hasCheckedOut )
		{
			GoToExit();
			return;
		}

		if ( !AutoScan || !IsCollecting ) return;

		_scanTimer += Time.Delta;
		if ( _scanTimer >= ScanInterval )
		{
			_scanTimer = 0f;
			ScanForItems();
		}
	}

	private bool _diagLogged;

	private void HandleMovingToItem()
	{
		if ( _currentTargetItem == null || !_currentTargetItem.IsValid() )
		{
			_currentTargetItem = null;
			_state = NPCState.Idle;
			return;
		}

		if ( _agent == null ) return;

		_moveTimer += Time.Delta;
		_agent.MoveTo( _currentTargetItem.WorldPosition );

		// One-time deep diagnostic after 1 second
		if ( DebugLogging && !_diagLogged && _moveTimer > 1f )
		{
			_diagLogged = true;
			var dest = _currentTargetItem.WorldPosition;
			float dist = WorldPosition.Distance( dest );

			Log.Info( $"[NPC DIAG] ===== NavMeshAgent Deep Diagnostic =====" );
			Log.Info( $"[NPC DIAG] NPC pos         : {WorldPosition}" );
			Log.Info( $"[NPC DIAG] Target pos      : {dest}" );
			Log.Info( $"[NPC DIAG] Distance         : {dist:F1}" );
			Log.Info( $"[NPC DIAG] Agent.Enabled    : {_agent.Enabled}" );
			Log.Info( $"[NPC DIAG] Agent.MaxSpeed   : {_agent.MaxSpeed}" );
			Log.Info( $"[NPC DIAG] Agent.Velocity   : {_agent.Velocity}" );
			Log.Info( $"[NPC DIAG] Agent.WishVelocity: {_agent.WishVelocity}" );
			Log.Info( $"[NPC DIAG] Agent.TargetPosition: {_agent.TargetPosition}" );
			Log.Info( $"[NPC DIAG] Agent.GameObject : {_agent.GameObject.Name}" );

			// Check for conflicting components
			var rb = GetComponent<Rigidbody>();
			Log.Info( $"[NPC DIAG] Has Rigidbody    : {rb != null}" );
			if ( rb != null )
				Log.Info( $"[NPC DIAG]   MotionEnabled={rb.MotionEnabled} Gravity={rb.Gravity}" );

			var cc = GetComponent<CharacterController>();
			Log.Info( $"[NPC DIAG] Has CharController: {cc != null}" );

			// Try to manually nudge position to verify transform isn't locked
			var before = WorldPosition;
			WorldPosition += Vector3.Up * 0.01f;
			var after = WorldPosition;
			WorldPosition = before; // restore
			Log.Info( $"[NPC DIAG] Transform moveable: {before != after}" );
			Log.Info( $"[NPC DIAG] ===== End Diagnostic =====" );
		}

		if ( _moveTimer < 0.25f ) return;

		float d = WorldPosition.Distance( _currentTargetItem.WorldPosition );
		if ( d <= ReachDistance )
		{
			_pauseTimer = 0f;
			_state = NPCState.WaitingAtItem;
		}
	}

	private void HandleWaitingAtItem()
	{
		_pauseTimer += Time.Delta;
		if ( _pauseTimer >= PickupPauseTime )
			_state = NPCState.PickingUp;
	}

	private void HandlePickingUp()
	{
		OnPickupStart?.Invoke();
		TriggerPickupAnimation();

		if ( _currentTargetItem != null && _currentTargetItem.IsValid() && HandBone != null )
		{
			_currentTargetItem.OnPickedUp( HandBone );
			_heldItems.Add( _currentTargetItem );
		}

		_currentTargetItem = null;

		bool batchFull = _heldItems.Count >= BatchSize;
		bool shouldCheckout = batchFull || !IsCollecting;

		if ( shouldCheckout && _counterSlots.Count > 0 )
		{
			MoveToCounter();
		}
		else if ( !shouldCheckout && IsCollecting )
		{
			_state = NPCState.Idle;
		}
		else
		{
			_state = NPCState.Idle;
		}
	}

	private void HandleMovingToCounter()
	{
		if ( _agent == null ) return;

		_moveTimer += Time.Delta;
		if ( _moveTimer < 0.25f ) return;

		// Stand at the counter target if assigned, otherwise aim for the first slot
		var dest = GetCounterStandPosition();
		float dist = WorldPosition.Distance( dest );
		if ( dist <= ReachDistance )
		{
			_state = NPCState.PlacingItem;
		}
	}

	private void HandlePlacingItem()
	{
		OnPlaceStart?.Invoke();
		TriggerPlaceAnimation();

		if ( _counterSlots.Count == 0 )
		{
			PlaceIdCard();
			_state = NPCState.WaitingForCheckout;
			return;
		}

		var toPlace = new List<InteractableItem>( _heldItems );
		var placed = new List<InteractableItem>();

		foreach ( var item in toPlace )
		{
			if ( item == null || !item.IsValid() ) continue;

			CounterSlot slot = GetAvailableCounterSlot();
			if ( slot == null ) break;

			// Unparent from NPC hand before repositioning
			item.GameObject.SetParent( null, true );
			item.GameObject.Enabled = true;

			var col = item.GetComponent<Collider>();
			if ( col != null ) col.Enabled = true;

			item.MarkAsDelivered();
			slot.PlaceItem( item.GameObject );
			_placedItems.Add( item.GameObject );
			placed.Add( item );
		}

		foreach ( var p in placed )
			_heldItems.Remove( p );

		if ( _heldItems.Count > 0 )
			return; // All counter slots full — wait for player to bag an item

		PlaceIdCard();
		_state = NPCState.WaitingForCheckout;
	}

	private void HandleWaitingForCheckout()
	{
		if ( _hasCheckedOut )
			GoToExit();
	}

	private void HandleMovingToExit()
	{
		if ( _exitPoint == null )
		{
			DespawnNPC();
			return;
		}

		_moveTimer += Time.Delta;
		if ( _moveTimer < 0.25f ) return;

		float dist = WorldPosition.Distance( _exitPoint.WorldPosition );
		if ( dist <= ReachDistance * 2f )
			DespawnNPC();
	}

	// ── Helpers ───────────────────────────────────────────────────────

	private void MoveToCounter()
	{
		var dest = GetCounterStandPosition();
		_moveTimer = 0f;
		_agent?.MoveTo( dest );
		_state = NPCState.MovingToCounter;
	}

	private void GoToExit()
	{
		CleanupIdCard();
		if ( _exitPoint != null )
		{
			_moveTimer = 0f;
			_agent?.MoveTo( _exitPoint.WorldPosition );
			_state = NPCState.MovingToExit;
		}
		else
		{
			DespawnNPC();
		}
	}

	/// <summary>Returns the world position the NPC should walk to when going to the counter.</summary>
	private Vector3 GetCounterStandPosition()
	{
		if ( _counterTarget != null && _counterTarget.IsValid() )
			return _counterTarget.WorldPosition;

		// Fallback: first counter slot position
		if ( _counterSlots.Count > 0 && _counterSlots[0] != null )
			return _counterSlots[0].WorldPosition;

		return WorldPosition;
	}

	private void ScanForItems()
	{
		if ( _heldItems.Count >= BatchSize ) return;

		if ( _useShelfSlots )
		{
			ScanShelfSlots();
			return;
		}

		float nearest = float.MaxValue;
		InteractableItem nearestItem = null;
		int total = 0, skippedDelivered = 0, skippedCategory = 0, skippedRange = 0;

		foreach ( var item in Scene.GetAllComponents<InteractableItem>() )
		{
			total++;
			if ( item.IsDelivered ) { skippedDelivered++; continue; }
			if ( WantedCategories.Count > 0 && !WantedCategories.Contains( item.ItemCategory ) ) { skippedCategory++; continue; }

			float dist = WorldPosition.Distance( item.WorldPosition );
			if ( dist > DetectionRadius ) { skippedRange++; continue; }
			if ( dist < nearest )
			{
				nearest = dist;
				nearestItem = item;
			}
		}

		if ( DebugLogging ) Log.Info( $"[NPCController] Scan: {total} items | {skippedDelivered} delivered | {skippedCategory} wrong category | {skippedRange} out of range ({DetectionRadius}u) | WantedCategories={WantedCategories.Count}" );

		if ( nearestItem != null )
			NavigateTo( nearestItem );
	}

	private void ScanShelfSlots()
	{
		foreach ( var slot in _allowedShelfSlots )
		{
			if ( slot == null || !slot.IsValid() || !slot.HasItems ) continue;
			if ( WantedCategories.Count > 0 && !WantedCategories.Contains( slot.AcceptedCategory ) ) continue;

			var item = slot.PeekTopItem();
			if ( item == null || item.IsDelivered ) continue;

			NavigateTo( item );
			return;
		}
	}

	private void NavigateTo( InteractableItem item )
	{
		_currentTargetItem = item;
		_moveTimer = 0f;
		if ( DebugLogging ) Log.Info( $"[NPCController] NavigateTo: {item.GameObject.Name} at {item.WorldPosition}, agent={_agent != null}" );
		_agent?.MoveTo( item.WorldPosition );
		_state = NPCState.MovingToItem;
	}

	private CounterSlot GetAvailableCounterSlot()
	{
		foreach ( var slot in _counterSlots )
			if ( slot != null && slot.IsValid() && !slot.IsOccupied )
				return slot;
		return null;
	}

	private void PlaceIdCard()
	{
		if ( _hasPlacedIDCard || _idCardSlot == null || IdCardPrefab == null || NpcIdentity == null ) return;
		_hasPlacedIDCard = true;
		_idCardSlot.PlaceIDCard( IdCardPrefab, NpcIdentity );
	}

	private void CleanupIdCard()
	{
		if ( _idCardSlot != null )
			_idCardSlot.RemoveIDCard();

		if ( _spawnedIdCard != null && _spawnedIdCard.IsValid() )
			_spawnedIdCard.Destroy();

		_spawnedIdCard = null;
	}

	private void DespawnNPC()
	{
		FireExitEvent();
		GameObject.Destroy();
	}

	private void FireExitEvent()
	{
		OnNPCExited?.Invoke( this );
	}

	protected override void OnDestroy()
	{
		CleanupIdCard();
	}

	// ── Animation RPCs ─────────────────────────────────────────────────

	[Rpc.Broadcast]
	private void TriggerPickupAnimation()
	{
		var anim = GetComponent<NPCAnimationController>();
		anim?.TriggerPickup();
	}

	[Rpc.Broadcast]
	private void TriggerPlaceAnimation()
	{
		var anim = GetComponent<NPCAnimationController>();
		anim?.TriggerPlace();
	}
}
