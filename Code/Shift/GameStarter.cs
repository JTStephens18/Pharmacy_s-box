using Sandbox;
using System.Collections.Generic;

/// <summary>
/// Entry point. Routes to ShiftManager.StartDayShift() on scene load.
/// Replaces Unity's GameStarter.cs.
/// </summary>
public sealed class GameStarter : Component
{
	[Property] public ShiftManager ShiftManagerRef { get; set; }

	// Legacy fallback fields (used when ShiftManager is not assigned)
	[Property] public NPCSpawnManager SpawnManager { get; set; }
	[Property] public RoundConfig RoundConfig { get; set; }

	[Group( "Test" )]
	[Property] public bool TestSpawnOnStart { get; set; } = false;
	[Group( "Test" )]
	[Property] public List<GameObject> TestNpcPrefabs { get; set; } = new();
	[Group( "Test" )]
	[Property] public GameObject TestSpawnPoint { get; set; }

	protected override void OnStart()
	{
		if ( !Networking.IsHost ) return;

		if ( TestSpawnOnStart )
		{
			SpawnTestNpcs();
			return;
		}

		if ( ShiftManagerRef != null )
		{
			ShiftManagerRef.StartDayShift();
		}
		else if ( SpawnManager != null && RoundConfig != null )
		{
			SpawnManager.StartNPCSpawning( RoundConfig );
		}
	}

	private void SpawnTestNpcs()
	{
		var spawnPos = TestSpawnPoint != null ? TestSpawnPoint.WorldPosition : WorldPosition;
		var spawnRot = TestSpawnPoint != null ? TestSpawnPoint.WorldRotation : WorldRotation;

		foreach ( var prefab in TestNpcPrefabs )
		{
			if ( prefab == null || !prefab.IsValid() ) continue;
			var npc = prefab.Clone( new Transform( spawnPos, spawnRot, 1f ) );
			npc.NetworkSpawn();
		}
	}
}
