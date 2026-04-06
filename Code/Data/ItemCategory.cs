using Sandbox;

/// <summary>
/// GameResource representing an item category (e.g. Medication, Supplement).
/// Used by shelf slots to filter items and by NPCs to choose what to pick up.
/// Replaces Unity's ItemCategory ScriptableObject.
/// </summary>
[GameResource( "Item Category", "itemcategory", "Defines an item type for shelf placement", Icon = "inventory_2" )]
public class ItemCategory : GameResource
{
	[Property] public string Description { get; set; } = "";

	[Group( "Prefab" )]
	[Property] public PrefabFile Prefab { get; set; }

	[Group( "Placement Settings" )]
	[Title( "Shelf Rotation Offset (Euler)" )]
	[Property] public Angles ShelfRotationOffset { get; set; } = Angles.Zero;
}
