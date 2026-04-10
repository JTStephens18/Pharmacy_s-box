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
	[Group( "Test" )]
	[Property] public NPCSpawnManager TestSpawnManager { get; set; }

	protected override void OnStart()
	{
		if ( Networking.IsActive && !Networking.IsHost ) return;

		LogServerInfo();

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

	private static void LogServerInfo()
	{
		var connections = Connection.All;

		Log.Info( "╔══════════════════════════════════╗" );
		Log.Info( "║       SERVER STARTED             ║" );
		Log.Info( "╚══════════════════════════════════╝" );
		Log.Info( $"  Host SteamId: {Connection.Local?.SteamId}" );
		Log.Info( $"  Players     : {connections.Count()}" );

		foreach ( var conn in connections )
		{
			Log.Info( $"  Connected   : [{conn.SteamId}] {conn.DisplayName}" );
		}

		Log.Info( "──────────────────────────────────" );
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

			// Inject scene references so NPC can navigate the full loop
			if ( TestSpawnManager != null )
			{
				var controller = npc.GetComponent<NPCController>();
				controller?.AssignSceneReferences(
					TestSpawnManager.CounterSlots,
					TestSpawnManager.CounterTarget,
					TestSpawnManager.ExitPoint,
					TestSpawnManager.IdCardSlot,
					TestSpawnManager.AllowedShelfSlots
				);
			}
		}
	}
}
