using Sandbox;

/// <summary>
/// Checkout trigger. Player presses E to process the nearest waiting NPC.
/// Replaces Unity's CashRegister.cs.
/// </summary>
public sealed class CashRegister : Component
{
	[Property] public float NpcDetectionRadius { get; set; } = 300f;
	[Property] public float InteractionRange { get; set; } = 150f;

	// Injected / found at runtime
	[Property] public ShiftManager ShiftManagerRef { get; set; }
	[Property] public ShiftScoreManager ScoreManagerRef { get; set; }

	/// <summary>Called by PlayerInteraction when player presses E on this register.</summary>
	public void Activate()
	{
		ProcessCheckout();
	}

	[Rpc.Host]
	public void ProcessCheckout()
	{
		var npc = FindClosestWaitingNPC();
		if ( npc == null ) return;

		if ( npc.IsDoppelganger )
		{
			ShiftManagerRef?.ReportEscape();
			ScoreManagerRef?.RecordWrongApproval();
		}
		else
		{
			ScoreManagerRef?.RecordCorrectApproval();
		}

		npc.TriggerCheckout();
	}

	private NPCController FindClosestWaitingNPC()
	{
		NPCController closest = null;
		float nearestDist = NpcDetectionRadius;

		foreach ( var npc in Scene.GetAllComponents<NPCController>() )
		{
			if ( npc.CurrentState != NPCController.NPCState.WaitingForCheckout ) continue;
			float dist = WorldPosition.Distance( npc.WorldPosition );
			if ( dist < nearestDist )
			{
				nearestDist = dist;
				closest = npc;
			}
		}

		return closest;
	}
}
