using Sandbox;

/// <summary>
/// Per-item hold position/rotation/scale overrides.
/// Attach to pickupable item prefabs to customize their held appearance.
/// Replaces Unity's HoldableItem.cs.
/// </summary>
public sealed class HoldableItem : Component
{
	[Property] public Vector3 HoldOffset { get; set; } = new Vector3( 20f, -15f, 50f );
	[Property] public Angles HoldRotation { get; set; } = Angles.Zero;

	/// <summary>
	/// Uniform scale applied while the item is held. Set to 1 to keep original size.
	/// Values below 1 shrink the item when picked up (e.g. 0.5 = half size).
	/// </summary>
	[Property, Range( 0.01f, 5f )]
	public float HoldScale { get; set; } = 0.6f;
}
