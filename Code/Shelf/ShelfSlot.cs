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

	[Property, Group( "Bounds" )] public Vector3 BoundsSize { get; set; } = new Vector3( 10f, 10f, 10f );
	[Property, Group( "Bounds" )] public bool ShowBoundsWireframe { get; set; } = false;

	/// <summary>Set by PlayerInteraction when the player is aiming at this slot.</summary>
	[Hide] public bool IsHighlighted { get; set; }

	private const float EdgeThickness = 0.5f; // world units
	private System.Collections.Generic.List<GameObject> _wireframeEdges;

	[Sync( SyncFlags.FromHost )] public int CurrentItemCount { get; set; } = 0;

	public bool IsOccupied => CurrentItemCount >= ItemPlacements.Count;
	public bool HasItems => CurrentItemCount > 0;
	public int MaxItems => ItemPlacements.Count;
	public Vector3 Position => WorldPosition;

	// ── Bounds ────────────────────────────────────────────────────────

	/// <summary>Returns the local-space bounding box centered on the slot.</summary>
	public BBox GetLocalBounds() => new BBox( -BoundsSize / 2f, BoundsSize / 2f );

	/// <summary>Returns true if a world-space point falls within this slot's bounds.</summary>
	public bool ContainsWorldPoint( Vector3 worldPoint )
	{
		var localPoint = WorldRotation.Inverse * ( worldPoint - WorldPosition );
		return GetLocalBounds().Contains( localPoint );
	}

	/// <summary>Called by PlayerInteraction to show/hide the runtime bounds visual.</summary>
	public void SetBoundsVisualActive( bool active )
	{
		if ( active )
		{
			if ( _wireframeEdges == null || _wireframeEdges.Count == 0 )
				CreateWireframeEdges();

			UpdateWireframeEdges();

			foreach ( var edge in _wireframeEdges )
				if ( edge.IsValid() ) edge.Enabled = true;
		}
		else if ( _wireframeEdges != null )
		{
			foreach ( var edge in _wireframeEdges )
				if ( edge != null && edge.IsValid() ) edge.Enabled = false;
		}
	}

	private void CreateWireframeEdges()
	{
		_wireframeEdges = new System.Collections.Generic.List<GameObject>();
		for ( int i = 0; i < 12; i++ )
		{
			var child = new GameObject( false, $"WireEdge_{i}" );
			child.SetParent( GameObject );
			var mr = child.AddComponent<ModelRenderer>();
			mr.Model = Model.Load( "models/dev/box.vmdl" );
			mr.Tint = Color.Green;
			_wireframeEdges.Add( child );
		}
	}

	private void UpdateWireframeEdges()
	{
		if ( _wireframeEdges == null || _wireframeEdges.Count < 12 ) return;

		float w = BoundsSize.x;
		float h = BoundsSize.y;
		float d = BoundsSize.z;
		float hw = w / 2f, hh = h / 2f, hd = d / 2f;

		// Thickness in model-space (native model is 50x50x50)
		float ts = EdgeThickness / 50f;

		// 12 edges: 4 along each axis
		(Vector3 pos, Vector3 scale)[] edges =
		{
			// X-axis edges (vary Z and Y corners)
			(new Vector3(0,  hh,  hd), new Vector3(w / 50f, ts, ts)),
			(new Vector3(0, -hh,  hd), new Vector3(w / 50f, ts, ts)),
			(new Vector3(0,  hh, -hd), new Vector3(w / 50f, ts, ts)),
			(new Vector3(0, -hh, -hd), new Vector3(w / 50f, ts, ts)),
			// Y-axis edges (vary X and Z corners)
			(new Vector3( hw, 0,  hd), new Vector3(ts, h / 50f, ts)),
			(new Vector3(-hw, 0,  hd), new Vector3(ts, h / 50f, ts)),
			(new Vector3( hw, 0, -hd), new Vector3(ts, h / 50f, ts)),
			(new Vector3(-hw, 0, -hd), new Vector3(ts, h / 50f, ts)),
			// Z-axis edges (vary X and Y corners)
			(new Vector3( hw,  hh, 0), new Vector3(ts, ts, d / 50f)),
			(new Vector3(-hw,  hh, 0), new Vector3(ts, ts, d / 50f)),
			(new Vector3( hw, -hh, 0), new Vector3(ts, ts, d / 50f)),
			(new Vector3(-hw, -hh, 0), new Vector3(ts, ts, d / 50f)),
		};

		for ( int i = 0; i < 12; i++ )
		{
			_wireframeEdges[i].LocalPosition = edges[i].pos;
			_wireframeEdges[i].LocalScale = edges[i].scale;
		}
	}

	protected override void DrawGizmos()
	{
		if ( !ShowBoundsWireframe ) return;

		Gizmo.Draw.Color = IsHighlighted ? Color.Green : Color.Yellow.WithAlpha( 0.5f );
		Gizmo.Draw.LineBBox( GetLocalBounds() );
	}

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
			? $"Place [{AcceptedCategory.Description}] ({CurrentItemCount}/{ItemPlacements.Count})"
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
