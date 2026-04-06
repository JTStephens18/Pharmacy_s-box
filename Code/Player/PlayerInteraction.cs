using Sandbox;

/// <summary>
/// Central interaction hub: pickup, throw, drop, and station routing.
/// Runs on the owning client only. Replaces Unity's ObjectPickup.cs.
/// Attach to the Player root (same hierarchy as PlayerController).
/// </summary>
public sealed class PlayerInteraction : Component
{
	[Group( "Pickup" )]
	[Property] public float PickupRange { get; set; } = 200f;
	[Group( "Pickup" )]
	[Property] public float PickupSmoothSpeed { get; set; } = 12f;

	[Group( "Hold Position" )]
	[Property] public Vector3 DefaultHoldOffset { get; set; } = new Vector3( 20f, -15f, 50f );
	[Group( "Hold Position" )]
	[Property] public Angles DefaultHoldRotation { get; set; } = new Angles( 10f, -15f, 0f );

	[Group( "Throw" )]
	[Property] public float ThrowForce { get; set; } = 800f;

	// ── Held object state ─────────────────────────────────────────────
	private GameObject _heldObject;
	private Rigidbody _heldRb;
	private Collider _heldCol;

	// ── Detected interactables ────────────────────────────────────────
	private IPlaceable _currentPlaceable;
	private DeliveryStation _currentDelivery;
	private InteractableItem _currentCounterItem;
	private CounterSlot _currentCounterSlot;
	private CashRegister _currentCashRegister;
	private IDCardInteraction _currentIdCard;
	private ComputerScreen _currentComputer;
	private bool _holdingBox;

	public bool IsHolding => _heldObject != null && _heldObject.IsValid();

	// ── Lifecycle ─────────────────────────────────────────────────────

	protected override void OnUpdate()
	{
		if ( IsProxy ) return;

		var focus = GetComponent<FocusStateManager>();
		if ( focus != null && focus.IsFocused ) return;

		// Detect every frame
		Detect();

		// E: interact
		if ( Input.Pressed( "use" ) )
			HandleInteract();

		// Attack1: throw
		if ( Input.Pressed( "Attack1" ) && IsHolding )
			ThrowObject();

		// Attack2 / drop: gentle drop
		if ( Input.Pressed( "Attack2" ) && IsHolding )
			DropObject();

		// Keep held object glued to camera
		if ( IsHolding )
			UpdateHeldPosition();
	}

	// ── Detection ────────────────────────────────────────────────────

	private void Detect()
	{
		_currentPlaceable = null;
		_currentDelivery = null;
		_currentCounterItem = null;
		_currentCounterSlot = null;
		_currentCashRegister = null;
		_currentIdCard = null;
		_currentComputer = null;

		var tr = CastRay();
		if ( !tr.Hit ) return;

		var go = tr.GameObject;
		if ( go == null ) return;

		// Walk up hierarchy for components
		_currentPlaceable = go.GetComponentInParent<IPlaceable>() as Component as IPlaceable
			?? go.GetComponent<IPlaceable>() as Component as IPlaceable;
		_currentDelivery = go.GetComponentInParent<DeliveryStation>() ?? go.GetComponent<DeliveryStation>();
		_currentCashRegister = go.GetComponentInParent<CashRegister>() ?? go.GetComponent<CashRegister>();
		_currentIdCard = go.GetComponentInParent<IDCardInteraction>() ?? go.GetComponent<IDCardInteraction>();
		_currentComputer = go.GetComponentInParent<ComputerScreen>() ?? go.GetComponent<ComputerScreen>();

		// Counter item detection
		var item = go.GetComponentInParent<InteractableItem>() ?? go.GetComponent<InteractableItem>();
		if ( item != null && item.IsDelivered )
		{
			_currentCounterItem = item;
			_currentCounterSlot = CounterSlot.GetSlotContaining( item.GameObject );
		}

		_holdingBox = IsHolding && _heldObject.GetComponent<InventoryBox>() != null;
	}

	private SceneTraceResult CastRay()
	{
		return Scene.Trace
			.Ray( Scene.Camera.WorldPosition, Scene.Camera.WorldPosition + Scene.Camera.WorldRotation.Forward * PickupRange )
			.Run();
	}

	// ── Interaction dispatch ──────────────────────────────────────────

	private void HandleInteract()
	{
		if ( IsHolding )
		{
			// Holding item: try to place it
			if ( _currentPlaceable != null && !_holdingBox && _currentPlaceable.CanPlaceItem( _heldObject ) )
				PlaceOnTarget();
			else
				DropObject();
		}
		else
		{
			// Not holding: priority order matches Unity's ObjectPickup
			if ( _currentComputer != null && !_currentComputer.IsActive )
				_currentComputer.Activate();
			else if ( _currentIdCard != null && !_currentIdCard.IsActive )
				_currentIdCard.Activate();
			else if ( _currentCounterItem != null )
				DeleteCounterItem();
			else if ( _currentCashRegister != null )
				_currentCashRegister.Activate();
			else if ( _currentDelivery != null )
				_currentDelivery.SpawnBox();
			else
				TryPickup();
		}
	}

	// ── Pickup ────────────────────────────────────────────────────────

	private void TryPickup()
	{
		var tr = CastRay();
		if ( !tr.Hit ) return;

		var go = tr.GameObject;
		if ( go == null ) return;

		// Must have a Rigidbody and not be a static/kinematic station
		var rb = go.GetComponent<Rigidbody>() ?? go.GetComponentInParent<Rigidbody>();
		if ( rb == null ) return;

		// Skip items that are on shelves (kinematic + parented) unless they're free
		if ( go.GetComponentInParent<ShelfSlot>() != null ) return;

		PickupObject( go );
	}

	private void PickupObject( GameObject go )
	{
		if ( IsHolding ) return;

		_heldObject = go;
		_heldRb = go.GetComponent<Rigidbody>();
		_heldCol = go.GetComponent<Collider>();

		// Disable physics while held
		if ( _heldRb != null )
		{
			_heldRb.Velocity = Vector3.Zero;
			_heldRb.AngularVelocity = Vector3.Zero;
			_heldRb.MotionEnabled = false;
		}
		if ( _heldCol != null )
			_heldCol.Enabled = false;

		// Request host to transfer ownership
		RequestPickup( go );
	}

	[Rpc.Host]
	private void RequestPickup( GameObject go )
	{
		if ( !go.IsValid() ) return;
		go.Network.AssignOwnership( Rpc.Caller );
	}

	private void UpdateHeldPosition()
	{
		if ( !IsHolding ) return;

		var holdableItem = _heldObject.GetComponent<HoldableItem>();
		var offset = holdableItem != null ? holdableItem.HoldOffset : DefaultHoldOffset;
		var rot = holdableItem != null ? holdableItem.HoldRotation : DefaultHoldRotation;

		_heldObject.WorldPosition = Vector3.Lerp(
			_heldObject.WorldPosition,
			Scene.Camera.WorldPosition + Scene.Camera.WorldRotation * offset,
			PickupSmoothSpeed * Time.Delta
		);
		_heldObject.WorldRotation = Scene.Camera.WorldRotation * rot.ToRotation();
	}

	// ── Release ───────────────────────────────────────────────────────

	private void ThrowObject()
	{
		if ( !IsHolding ) return;

		ReleaseObject();
		_heldRb?.ApplyImpulse( Scene.Camera.WorldRotation.Forward * ThrowForce );
	}

	private void DropObject()
	{
		if ( !IsHolding ) return;
		ReleaseObject();
	}

	private void ReleaseObject()
	{
		if ( _heldCol != null )
			_heldCol.Enabled = true;
		if ( _heldRb != null )
			_heldRb.MotionEnabled = true;

		_heldObject = null;
		_heldRb = null;
		_heldCol = null;
	}

	/// <summary>Destroys the held object (used by PillFillingStation to consume a bottle).</summary>
	public void ConsumeHeldObject()
	{
		if ( !IsHolding ) return;
		var obj = _heldObject;
		_heldObject = null;
		_heldRb = null;
		_heldCol = null;
		obj.Destroy();
	}

	// ── Counter item deletion ─────────────────────────────────────────

	private void DeleteCounterItem()
	{
		if ( _currentCounterItem == null ) return;
		DeleteCounterItemOnHost( _currentCounterItem.GameObject );
	}

	[Rpc.Host]
	private void DeleteCounterItemOnHost( GameObject item )
	{
		if ( !item.IsValid() ) return;

		var slot = CounterSlot.GetSlotContaining( item );
		slot?.RemoveItem( item );
		item.Destroy();
	}

	// ── Shelf placement ───────────────────────────────────────────────

	private void PlaceOnTarget()
	{
		if ( !IsHolding || _currentPlaceable == null ) return;

		var item = _heldObject;
		ReleaseObject();
		_currentPlaceable.TryPlaceItem( item );
	}
}
