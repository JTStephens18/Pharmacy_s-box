using Sandbox;
using System.Collections.Generic;

[System.Serializable]
public class QueueEntry
{
	[Property] public bool IsFixed { get; set; } = false;
	[Property] public GameObject FixedNpcPrefab { get; set; }

	[Group( "Doppelganger" )]
	[Property] public bool ForceDoppelganger { get; set; } = false;

	[Group( "Doppelganger" )]
	[Property] public DoppelgangerProfile FixedProfile { get; set; }
}

/// <summary>
/// GameResource defining the NPC queue for a single round/shift.
/// Replaces Unity's RoundConfig ScriptableObject.
/// </summary>
[GameResource( "Round Config", "rndcfg", "Defines the NPC spawn queue and doppelganger settings for a shift", Icon = "settings" )]
public class RoundConfig : GameResource
{
	[Group( "NPC Pool" )]
	[Property] public List<GameObject> NpcPool { get; set; } = new();

	[Group( "Queue" )]
	[Property] public List<QueueEntry> QueueEntries { get; set; } = new();

	[Group( "Doppelganger Settings" )]
	[Property] public List<DoppelgangerProfile> DoppelgangerPool { get; set; } = new();

	[Group( "Doppelganger Settings" )]
	[Title( "Random Doppelganger Count" )]
	[Property] public int RandomDoppelgangerCount { get; set; } = 1;
}
