using Sandbox;

/// <summary>
/// Interactable delivery station that spawns InventoryBox prefabs.
/// Replaces Unity's DeliveryStation.cs.
/// </summary>
public sealed class DeliveryStation : Component
{
	[Property] public GameObject InventoryBoxPrefab { get; set; }
	[Property] public GameObject SpawnPoint { get; set; }
	[Property] public GameObject HighlightObject { get; set; }

	protected override void OnAwake()
	{
		HideHighlight();
	}

	/// <summary>Called by PlayerInteraction when player presses E on this station.</summary>
	public void SpawnBox()
	{
		SpawnBoxOnHost();
	}

	[Rpc.Host]
	private void SpawnBoxOnHost()
	{
		if ( InventoryBoxPrefab == null )
		{
			Log.Warning( "[DeliveryStation] No inventory box prefab assigned!" );
			return;
		}

		var spawnPos = SpawnPoint != null ? SpawnPoint.WorldPosition : WorldPosition;
		var spawnRot = SpawnPoint != null ? SpawnPoint.WorldRotation : WorldRotation;

		var box = InventoryBoxPrefab.Clone( new Transform( spawnPos, spawnRot, 1f ) );
		box.NetworkSpawn();
	}

	public void ShowHighlight()
	{
		if ( HighlightObject != null ) HighlightObject.Enabled = true;
	}

	public void HideHighlight()
	{
		if ( HighlightObject != null ) HighlightObject.Enabled = false;
	}
}
