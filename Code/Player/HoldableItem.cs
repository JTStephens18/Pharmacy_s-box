using Sandbox;

/// <summary>
/// Per-item hold position/rotation overrides.
/// Attach to pickupable item prefabs to customize their held position.
/// Replaces Unity's HoldableItem.cs.
/// </summary>
public sealed class HoldableItem : Component
{
	[Property] public Vector3 HoldOffset { get; set; } = new Vector3( 20f, -15f, 50f );
	[Property] public Angles HoldRotation { get; set; } = Angles.Zero;
}
