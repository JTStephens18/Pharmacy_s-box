using Sandbox;

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

	protected override void OnStart()
	{
		if ( !Networking.IsHost ) return;

		if ( ShiftManagerRef != null )
		{
			ShiftManagerRef.StartDayShift();
		}
		else if ( SpawnManager != null && RoundConfig != null )
		{
			SpawnManager.StartNPCSpawning( RoundConfig );
		}
	}
}
