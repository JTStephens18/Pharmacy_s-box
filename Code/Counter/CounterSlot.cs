using Sandbox;
using System.Collections.Generic;

[System.Serializable]
public class CounterItemPlacement
{
	[Property] public Vector3 PositionOffset { get; set; } = Vector3.Zero;
	[Property] public Angles RotationOffset { get; set; } = Angles.Zero;

	[Hide] public GameObject PlacedItem { get; set; }
}

/// <summary>
/// Counter surface slot where NPCs place items for checkout.
/// Player presses E on items to bag/remove them.
/// Replaces Unity's CounterSlot.cs.
/// </summary>
public sealed class CounterSlot : Component, IPlaceable
{
	[Property] public List<CounterItemPlacement> ItemPlacements { get; set; } = new() { new CounterItemPlacement() };

	// All live instances for fast server-side scan
	private static readonly HashSet<CounterSlot> _all = new();

	public bool IsOccupied => CurrentItemCount >= ItemPlacements.Count;
	public bool HasItems => CurrentItemCount > 0;
	public int MaxItems => ItemPlacements.Count;

	public int CurrentItemCount
	{
		get
		{
			int count = 0;
			foreach ( var p in ItemPlacements )
				if ( p.PlacedItem != null ) count++;
			return count;
		}
	}

	protected override void OnAwake()
	{
		_all.Add( this );
	}

	protected override void OnDestroy()
	{
		_all.Remove( this );
	}

	// ── IPlaceable ────────────────────────────────────────────────────

	public bool CanPlaceItem( GameObject item ) => !IsOccupied;

	public bool TryPlaceItem( GameObject item )
	{
		if ( !CanPlaceItem( item ) ) return false;
		PlaceItem( item );
		return true;
	}

	public string GetPlacementPrompt() =>
		IsOccupied ? "Counter Full" : $"Counter ({CurrentItemCount}/{ItemPlacements.Count})";

	// ── Placement ─────────────────────────────────────────────────────

	/// <summary>Places an item in the first available position. Called by NPCController.</summary>
	public void PlaceItem( GameObject item )
	{
		int emptyIdx = -1;
		for ( int i = 0; i < ItemPlacements.Count; i++ )
		{
			if ( ItemPlacements[i].PlacedItem == null )
			{
				emptyIdx = i;
				break;
			}
		}

		if ( emptyIdx == -1 ) return;

		var placement = ItemPlacements[emptyIdx];
		placement.PlacedItem = item;

		// Unparent from NPC hand (or wherever it came from) before repositioning
		item.SetParent( null, true );

		// Position in world space (s&box NetworkObjects can't be parented to non-NetworkObjects)
		item.WorldPosition = WorldTransform.PointToWorld( placement.PositionOffset );
		item.WorldRotation = WorldRotation * placement.RotationOffset.ToRotation();

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

	/// <summary>Removes a specific item from this slot.</summary>
	public bool RemoveItem( GameObject item )
	{
		for ( int i = 0; i < ItemPlacements.Count; i++ )
		{
			if ( ItemPlacements[i].PlacedItem == item )
			{
				ItemPlacements[i].PlacedItem = null;
				return true;
			}
		}
		return false;
	}

	/// <summary>Returns true if this slot contains the given item.</summary>
	public bool ContainsItem( GameObject item )
	{
		foreach ( var p in ItemPlacements )
			if ( p.PlacedItem == item ) return true;
		return false;
	}

	/// <summary>Clears all placements.</summary>
	public void Clear()
	{
		foreach ( var p in ItemPlacements )
			p.PlacedItem = null;
	}

	/// <summary>Finds which CounterSlot contains a given item.</summary>
	public static CounterSlot GetSlotContaining( GameObject item )
	{
		foreach ( var slot in _all )
			if ( slot.ContainsItem( item ) ) return slot;
		return null;
	}
}
