using Sandbox;

/// <summary>
/// Shelf item that NPCs can pick up. Implements IInteractable.
/// Replaces Unity's InteractableItem.cs.
/// </summary>
public sealed class InteractableItem : Component, IInteractable
{
	[Property] public ItemCategory ItemCategory { get; set; }
	[Property] public GameObject GrabTarget { get; set; }

	public bool IsDelivered { get; private set; }

	private Rigidbody _rb;
	private Collider _col;

	protected override void OnAwake()
	{
		_rb = GetComponent<Rigidbody>();
		_col = GetComponent<Collider>();
	}

	// ── IInteractable ─────────────────────────────────────────────────

	public Vector3 GetInteractionPoint() =>
		GrabTarget != null ? GrabTarget.WorldPosition : WorldPosition;

	public void OnPickedUp( GameObject hand )
	{
		// Disable physics + collider, parent to hand
		if ( _rb != null )
			_rb.MotionEnabled = false;
		if ( _col != null )
			_col.Enabled = false;

		GameObject.SetParent( hand, true );
		GameObject.Enabled = false;
	}

	/// <summary>Marks this item as delivered so NPCs won't pick it up again.</summary>
	public void MarkAsDelivered()
	{
		IsDelivered = true;
	}

	/// <summary>Re-enables physics (e.g. when item is dropped).</summary>
	public void Release()
	{
		if ( _rb != null )
			_rb.MotionEnabled = true;
		if ( _col != null )
			_col.Enabled = true;
	}
}
