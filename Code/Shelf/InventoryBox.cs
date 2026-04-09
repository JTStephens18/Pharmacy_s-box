using Sandbox;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Portable inventory box that holds items for shelf restocking.
/// Decrements as items are placed; destroys when empty.
/// Replaces Unity's InventoryBox.cs.
/// </summary>
public sealed class InventoryBox : Component
{
	[Property] public int TotalItems { get; set; } = 10;
	[Property] public List<ItemCategory> ItemTypes { get; set; } = new();

	[Group( "Visuals" )]
	[Property] public GameObject ClosedModel { get; set; }
	[Group( "Visuals" )]
	[Property] public GameObject OpenModel { get; set; }

	public int RemainingItems { get; private set; }

	// Queue of items still to be placed (built from ItemTypes)
	private Queue<ItemCategory> _queue = new();

	protected override void OnAwake()
	{
		RemainingItems = TotalItems;
		BuildQueue();

		if ( ClosedModel != null ) ClosedModel.Enabled = true;
		if ( OpenModel != null ) OpenModel.Enabled = false;
	}

	private void BuildQueue()
	{
		_queue.Clear();
		if ( ItemTypes.Count == 0 ) return;
		for ( int i = 0; i < TotalItems; i++ )
			_queue.Enqueue( ItemTypes[i % ItemTypes.Count] );
	}

	/// <summary>Returns the next item category in the queue, without removing it.</summary>
	public ItemCategory PeekNextCategory() =>
		_queue.Count > 0 ? _queue.Peek() : null;

	/// <summary>Decrements the box. Destroys it when empty.</summary>
	public void Decrement()
	{
		if ( _queue.Count > 0 ) _queue.Dequeue();
		RemainingItems--;

		if ( RemainingItems <= 0 )
		{
			_ = ShrinkAndDestroy();
		}
		else
		{
			// Switch to open model once first item is removed
			if ( ClosedModel != null ) ClosedModel.Enabled = false;
			if ( OpenModel != null ) OpenModel.Enabled = true;
		}
	}

	private async Task ShrinkAndDestroy()
	{
		// Shrink to zero over 0.5 seconds then destroy
		float t = 0f;
		var startScale = LocalScale;
		while ( t < 0.5f )
		{
			t += Time.Delta;
			LocalScale = Vector3.Lerp( startScale, Vector3.Zero, t / 0.5f );
			await Task.Yield();
		}
		GameObject.Destroy();
	}
}
