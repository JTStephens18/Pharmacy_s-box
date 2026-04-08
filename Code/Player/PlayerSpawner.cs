using Sandbox;
using System.Collections.Generic;

/// <summary>
/// Spawns a player prefab for every client that connects (including the host).
/// Add this component to a persistent scene GameObject (e.g. GameManagers).
/// Remove any hardcoded Player prefab instances from the scene — this replaces them.
/// Supports up to 4 players. Each connection is assigned the next available spawn point.
/// </summary>
public sealed class PlayerSpawner : Component, Component.INetworkListener
{
	[Property] public GameObject PlayerPrefab { get; set; }
	[Property] public List<GameObject> SpawnPoints { get; set; } = new();

	private int _nextSpawnIndex = 0;

	protected override void OnStart()
	{
		// The host's connection is already active before INetworkListener.OnActive fires,
		// so the host must spawn their own player here.
		if ( !Networking.IsHost ) return;

		SpawnPlayerFor( Connection.Local );
	}

	void INetworkListener.OnActive( Connection conn )
	{
		if ( !Networking.IsHost ) return;

		// Host already spawned in OnStart — skip their connection here.
		if ( conn == Connection.Local ) return;

		SpawnPlayerFor( conn );
	}

	private void SpawnPlayerFor( Connection conn )
	{
		if ( PlayerPrefab == null )
		{
			Log.Warning( "PlayerSpawner: PlayerPrefab is not assigned." );
			return;
		}

		Transform spawnTransform;
		if ( SpawnPoints.Count > 0 && _nextSpawnIndex < SpawnPoints.Count )
		{
			var point = SpawnPoints[_nextSpawnIndex++];
			spawnTransform = new Transform( point.WorldPosition, point.WorldRotation );
		}
		else
		{
			spawnTransform = new Transform( Vector3.Zero, Rotation.Identity );
		}

		var player = PlayerPrefab.Clone( spawnTransform );
		player.NetworkSpawn( conn );

		Log.Info( $"PlayerSpawner: Spawned player for {conn.DisplayName} ({conn.SteamId}) at spawn point {_nextSpawnIndex - 1}" );
	}
}
