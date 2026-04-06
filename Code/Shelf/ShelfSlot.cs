using Sandbox;
using System.Collections.Generic;

[System.Serializable]
public class ItemPlacement
{
	[Property] public Vector3 PositionOffset { get; set; } = Vector3.Zero;
	[Property] public Angles RotationOffset { get; set; } = Angles.Zero;

	[Hide] public GameObject PlacedItem { get; set; }
}

/// <summary>
/// A single shelf slot supporting multiple items and category filtering.
/// Implements IPlaceable. Replaces Unity's ShelfSlot.cs.
/// </summary>
public sealed class ShelfSlot : Component, IPlaceable
{
	[Property] public List<ItemPlacement> ItemPlacements { get; set; } = new() { new ItemPlacement() };
	[Property] public ItemCategory AcceptedCategory { get; set; }
	[Property] public bool RequireHeldItem { get; set; } = true;

	[Sync( SyncFlags.FromHost )] public int CurrentItemCount { get; set; } = 0;

	public bool IsOccupied => CurrentItemCount >= ItemPlacements.Count;
	public bool HasItems => CurrentItemCount > 0;
	public int MaxItems => ItemPlacements.Count;
	public Vector3 Position => WorldPosition;

	// ── IPlaceable ────────────────────────────────────────────────────

	public bool CanPlaceItem( GameObject item )
	{
		if ( IsOccupied ) return false;
		if ( AcceptedCategory != null )
		{
			var interactable = item.GetComponent<InteractableItem>();
			if ( interactable == null || interactable.ItemCategory != AcceptedCategory ) return false;
		}
		return true;
	}

	public bool TryPlaceItem( GameObject item )
	{
		if ( !CanPlaceItem( item ) ) return false;
		PlaceItem( item );
		return true;
	}

	public string GetPlacementPrompt()
	{
		if ( IsOccupied ) return "Slot Full";
		return AcceptedCategory != null
			? $"Place [{AcceptedCategory.ResourceName}] ({CurrentItemCount}/{ItemPlacements.Count})"
			: $"Place ({CurrentItemCount}/{ItemPlacements.Count})";
	}

	// ── Placement ─────────────────────────────────────────────────────

	/// <summary>Places an item in the next available position.</summary>
	public void PlaceItem( GameObject item )
	{
		if ( CurrentItemCount >= ItemPlacements.Count ) return;

		var placement = ItemPlacements[CurrentItemCount];
		placement.PlacedItem = item;
		CurrentItemCount++;

		// Calculate rotation (slot offset + category rotation offset)
		var placementRot = placement.RotationOffset.ToRotation();
		var categoryRot = Rotation.Identity;

		var interactable = item.GetComponent<InteractableItem>();
		if ( interactable?.ItemCategory != null )
			categoryRot = interactable.ItemCategory.ShelfRotationOffset.ToRotation();

		item.SetParent( GameObject, true );
		item.LocalPosition = placement.PositionOffset;
		item.LocalRotation = placementRot * categoryRot;

		// Freeze physics
		var rb = item.GetComponent<Rigidbody>();
		if ( rb != null )
		{
			rb.Velocity = Vector3.Zero;
			rb.AngularVelocity = Vector3.Zero;
			rb.Gravity = false;
			rb.MotionEnabled = false;
		}
	}

	/// <summary>Removes and returns the most recently placed item.</summary>
	public GameObject RemoveItem()
	{
		if ( CurrentItemCount <= 0 ) return null;

		CurrentItemCount--;
		var placement = ItemPlacements[CurrentItemCount];
		var item = placement.PlacedItem;
		placement.PlacedItem = null;

		if ( item == null ) return null;

		item.SetParent( null );

		var rb = item.GetComponent<Rigidbody>();
		if ( rb != null )
		{
			rb.MotionEnabled = true;
			rb.Gravity = true;
		}

		return item;
	}

	/// <summary>Returns the top (most recently placed) item without removing it.</summary>
	public InteractableItem PeekTopItem()
	{
		if ( CurrentItemCount <= 0 ) return null;
		var item = ItemPlacements[CurrentItemCount - 1].PlacedItem;
		return item?.GetComponent<InteractableItem>();
	}

	/// <summary>Returns true if this slot contains the given item.</summary>
	public bool ContainsItem( GameObject item )
	{
		for ( int i = 0; i < CurrentItemCount; i++ )
			if ( ItemPlacements[i].PlacedItem == item ) return true;
		return false;
	}

	public void Clear()
	{
		foreach ( var p in ItemPlacements )
			p.PlacedItem = null;
		CurrentItemCount = 0;
	}
}
