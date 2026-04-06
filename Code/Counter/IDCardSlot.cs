using Sandbox;

/// <summary>
/// Counter spot where the NPC's ID card is placed.
/// Replaces Unity's IDCardSlot.cs.
/// </summary>
public sealed class IDCardSlot : Component
{
	[Property] public GameObject FocusCameraTarget { get; set; }

	private GameObject _card;

	/// <summary>Spawns the ID card prefab and initializes its visuals/interaction.</summary>
	public void PlaceIDCard( GameObject prefab, NPCIdentity identity )
	{
		RemoveIDCard();
		if ( !prefab.IsValid() || identity == null ) return;

		_card = SceneUtility.Instantiate( prefab, WorldTransform );
		_card.NetworkSpawn();

		var interaction = _card.GetComponent<IDCardInteraction>();
		interaction?.Initialize( identity, FocusCameraTarget );
	}

	/// <summary>Destroys the current ID card.</summary>
	public void RemoveIDCard()
	{
		if ( _card != null && _card.IsValid() )
			_card.Destroy();
		_card = null;
	}
}
