using Sandbox;

/// <summary>
/// Singleton that holds the currently-scanned NPC's data.
/// The ComputerPanel.razor reads from this to populate identity/prescription fields.
/// Replaces Unity's NPCInfoDisplay.cs + NPCIdentityField.cs (no separate TMP binding needed).
/// </summary>
public sealed class NPCInfoDisplay : Component
{
	public static NPCInfoDisplay Instance { get; private set; }

	/// <summary>The NPC whose info is currently being displayed. Null if panel is hidden.</summary>
	public NPCController CurrentNPC { get; private set; }

	/// <summary>Whether the NPC info panel should be visible.</summary>
	public bool IsShowing => CurrentNPC != null;

	public event System.Action OnInfoChanged;

	protected override void OnAwake()
	{
		Instance = this;
	}

	protected override void OnDestroy()
	{
		if ( Instance == this ) Instance = null;
	}

	/// <summary>Show the NPC info panel for the given identity. Finds the matching NPCController.</summary>
	public void ShowNPCInfo( NPCIdentity identity )
	{
		if ( identity == null ) { ClearNPCInfo(); return; }

		foreach ( var npc in Scene.GetAllComponents<NPCController>() )
		{
			if ( npc.NpcIdentity == identity )
			{
				CurrentNPC = npc;
				OnInfoChanged?.Invoke();
				return;
			}
		}

		// If no matching NPC found (e.g. NPC already left), clear
		ClearNPCInfo();
	}

	/// <summary>Hide the NPC info panel.</summary>
	public void ClearNPCInfo()
	{
		CurrentNPC = null;
		OnInfoChanged?.Invoke();
	}
}
