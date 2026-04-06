using Sandbox;

/// <summary>
/// Drives the physical ID card's photo and name visuals.
/// Replaces Unity's IDCardVisuals.cs.
/// </summary>
public sealed class IDCardVisuals : Component
{
	[Property] public ModelRenderer PhotoRenderer { get; set; }
	[Property] public TextRenderer NameText { get; set; }

	public void Initialize( NPCIdentity identity )
	{
		if ( identity == null ) return;

		if ( PhotoRenderer != null && identity.IdCardPhoto != null )
		{
			// Apply photo texture directly to the scene object's render attributes
			PhotoRenderer.SceneObject?.Attributes.Set( "Texture", identity.IdCardPhoto );
		}

		if ( NameText != null )
			NameText.Text = identity.IdCardDisplayName;
	}
}
