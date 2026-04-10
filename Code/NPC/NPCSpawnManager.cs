using Sandbox;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Manages sequential NPC spawning from a RoundConfig.
/// Runs on host only. Replaces Unity's NPCSpawnManager.cs.
/// </summary>
public sealed class NPCSpawnManager : Component
{
	[Group( "Spawn Settings" )]
	[Property] public GameObject SpawnPoint { get; set; }
	[Group( "Spawn Settings" )]
	[Property] public float InitialDelay { get; set; } = 2f;
	[Group( "Spawn Settings" )]
	[Property] public float DelayAfterExit { get; set; } = 3f;

	[Group( "Shared Scene References" )]
	[Property] public List<CounterSlot> CounterSlots { get; set; } = new();
	[Group( "Shared Scene References" )]
	[Property] public GameObject CounterTarget { get; set; }
	[Group( "Shared Scene References" )]
	[Property] public GameObject ExitPoint { get; set; }
	[Group( "Shared Scene References" )]
	[Property] public IDCardSlot IdCardSlot { get; set; }
	[Group( "Shared Scene References" )]
	[Property] public List<ShelfSlot> AllowedShelfSlots { get; set; } = new();

	public event Action<NPCController> OnNPCSpawned;
	public event Action OnAllNPCsFinished;

	public bool IsSpawning { get; private set; }

	private struct SpawnEntry
	{
		public GameObject Prefab;
		public DoppelgangerProfile DoppelgangerProfile;
	}

	private Queue<SpawnEntry> _spawnQueue = new();
	private NPCController _activeNPC;

	public void StartNPCSpawning( RoundConfig config )
	{
		if ( Networking.IsActive && !Networking.IsHost ) return;
		if ( config == null ) { Log.Error( "[NPCSpawnManager] RoundConfig is null." ); return; }
		if ( SpawnPoint == null ) { Log.Error( "[NPCSpawnManager] SpawnPoint not assigned." ); return; }

		StopNPCSpawning();

		var resolved = ResolveQueue( config );
		_spawnQueue = new Queue<SpawnEntry>( resolved );

		NPCController.OnNPCExited += HandleNPCExited;
		IsSpawning = true;
		_ = SpawnLoop();
	}

	public void StopNPCSpawning()
	{
		NPCController.OnNPCExited -= HandleNPCExited;
		_spawnQueue.Clear();
		_activeNPC = null;
		IsSpawning = false;
	}

	private async Task SpawnLoop()
	{
		await Task.DelaySeconds( InitialDelay );

		while ( _spawnQueue.Count > 0 )
		{
			SpawnNextNPC();
			while ( _activeNPC.IsValid() )
				await Task.Yield();

			if ( _spawnQueue.Count > 0 )
				await Task.DelaySeconds( DelayAfterExit );
		}

		IsSpawning = false;
		NPCController.OnNPCExited -= HandleNPCExited;
		OnAllNPCsFinished?.Invoke();
	}

	private void SpawnNextNPC()
	{
		var entry = _spawnQueue.Dequeue();
		if ( !entry.Prefab.IsValid() ) return;

		var npcGo = entry.Prefab.Clone( new Transform( SpawnPoint.WorldPosition, SpawnPoint.WorldRotation, 1f ) );
		npcGo.NetworkSpawn();

		_activeNPC = npcGo.GetComponent<NPCController>();
		if ( _activeNPC == null )
		{
			Log.Error( $"[NPCSpawnManager] Spawned prefab '{entry.Prefab.Name}' has no NPCController." );
			return;
		}

		_activeNPC.AssignSceneReferences( CounterSlots, CounterTarget, ExitPoint, IdCardSlot, AllowedShelfSlots );

		if ( entry.DoppelgangerProfile != null )
			_activeNPC.AssignDoppelgangerProfile( entry.DoppelgangerProfile );

		OnNPCSpawned?.Invoke( _activeNPC );
	}

	private void HandleNPCExited( NPCController npc )
	{
		if ( npc == _activeNPC )
			_activeNPC = null;
	}

	protected override void OnDestroy()
	{
		NPCController.OnNPCExited -= HandleNPCExited;
	}

	// ── Queue resolution ──────────────────────────────────────────────

	private List<SpawnEntry> ResolveQueue( RoundConfig config )
	{
		var available = new List<GameObject>( config.NpcPool );
		var resolved = new List<SpawnEntry>();

		foreach ( var qe in config.QueueEntries )
		{
			var se = new SpawnEntry();

			if ( qe.IsFixed )
			{
				if ( qe.FixedNpcPrefab == null ) continue;
				se.Prefab = qe.FixedNpcPrefab;
				available.Remove( qe.FixedNpcPrefab );
			}
			else
			{
				if ( available.Count == 0 ) break;
				int idx = Game.Random.Int( 0, available.Count - 1 );
				se.Prefab = available[idx];
				available.RemoveAt( idx );
			}

			if ( qe.ForceDoppelganger && qe.FixedProfile != null )
				se.DoppelgangerProfile = qe.FixedProfile;

			resolved.Add( se );
		}

		AssignRandomDoppelgangers( resolved, config );
		return resolved;
	}

	private void AssignRandomDoppelgangers( List<SpawnEntry> resolved, RoundConfig config )
	{
		if ( config.RandomDoppelgangerCount <= 0 || config.DoppelgangerPool == null || config.DoppelgangerPool.Count == 0 ) return;

		var candidates = new List<int>();
		for ( int i = 0; i < resolved.Count; i++ )
			if ( resolved[i].DoppelgangerProfile == null )
				candidates.Add( i );

		var profiles = new List<DoppelgangerProfile>( config.DoppelgangerPool );
		int toAssign = Math.Min( config.RandomDoppelgangerCount, Math.Min( candidates.Count, profiles.Count ) );

		for ( int i = 0; i < toAssign; i++ )
		{
			int ci = Game.Random.Int( 0, candidates.Count - 1 );
			int pi = Game.Random.Int( 0, profiles.Count - 1 );

			var entry = resolved[candidates[ci]];
			entry.DoppelgangerProfile = profiles[pi];
			resolved[candidates[ci]] = entry;

			candidates.RemoveAt( ci );
			profiles.RemoveAt( pi );
		}
	}
}
