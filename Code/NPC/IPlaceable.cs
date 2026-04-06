using Sandbox;

/// <summary>Player-placeable target interface. Implemented by ShelfSlot, ShelfSection, CounterSlot.</summary>
public interface IPlaceable
{
	bool CanPlaceItem( GameObject item );
	bool TryPlaceItem( GameObject item );
	string GetPlacementPrompt();
}
