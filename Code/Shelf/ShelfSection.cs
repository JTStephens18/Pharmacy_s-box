using Sandbox;
using System.Collections.Generic;

/// <summary>
/// Groups multiple ShelfSlot children into a named section.
/// Implements IPlaceable so PlayerInteraction can detect it.
/// Replaces Unity's ShelfSection.cs.
/// </summary>
public sealed class ShelfSection : Component, IPlaceable
{
	[Property] public bool AutoFindSlots { get; set; } = true;
	[Property] public List<ShelfSlot> Slots { get; set; } = new();
	[Property] public ItemCategory AcceptedCategory { get; set; }
	[Property] public string PlacementPrompt { get; set; } = "Press E to Place";

	public int MaxCapacity => Slots.Count;
	public int AvailableSlots => GetAvailableCount();

	protected override void OnAwake()
	{
		if ( AutoFindSlots )
		{
			Slots.Clear();
			Slots.AddRange( GetComponentsInChildren<ShelfSlot>() );
		}
	}

	// ── IPlaceable ────────────────────────────────────────────────────

	public bool CanPlaceItem( GameObject item )
	{
		if ( AcceptedCategory != null )
		{
			var interactable = item.GetComponent<InteractableItem>();
			if ( interactable == null || interactable.ItemCategory != AcceptedCategory ) return false;
		}
		return GetFirstAvailableSlot() != null;
	}

	public bool TryPlaceItem( GameObject item )
	{
		if ( !CanPlaceItem( item ) ) return false;
		GetFirstAvailableSlot()?.PlaceItem( item );
		return true;
	}

	public string GetPlacementPrompt() =>
		AvailableSlots > 0 ? PlacementPrompt : "Shelf Full";

	// ── Queries ───────────────────────────────────────────────────────

	/// <summary>Returns all ItemCategories currently missing from this section's slots.</summary>
	public List<ItemCategory> GetMissingItems()
	{
		var missing = new List<ItemCategory>();
		foreach ( var slot in Slots )
		{
			if ( slot.AcceptedCategory == null ) continue;
			int empty = slot.MaxItems - slot.CurrentItemCount;
			for ( int i = 0; i < empty; i++ )
				missing.Add( slot.AcceptedCategory );
		}
		return missing;
	}

	public List<ShelfSlot> GetSlots() => Slots;

	private ShelfSlot GetFirstAvailableSlot()
	{
		foreach ( var slot in Slots )
			if ( !slot.IsOccupied ) return slot;
		return null;
	}

	private int GetAvailableCount()
	{
		int count = 0;
		foreach ( var slot in Slots )
			if ( !slot.IsOccupied ) count++;
		return count;
	}
}
