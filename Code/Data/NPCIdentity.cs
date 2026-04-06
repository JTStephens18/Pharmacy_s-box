using Sandbox;

/// <summary>
/// GameResource holding NPC identity data for ID card and computer screen.
/// Create via: Right-click → Add → NPC Identity (.npcid)
/// Replaces Unity's NPCIdentity ScriptableObject.
/// </summary>
[GameResource( "NPC Identity", "npcid", "Defines an NPC's personal identity data", Icon = "person" )]
public class NPCIdentity : GameResource
{
	[Group( "Personal Information" )]
	[Property] public string FullName { get; set; } = "";

	[Group( "Personal Information" )]
	[Property] public string DateOfBirth { get; set; } = "";

	[Group( "Personal Information" )]
	[Property] public string Address { get; set; } = "";

	[Group( "Personal Information" )]
	[Property] public string IdNumber { get; set; } = "";

	[Group( "Computer Screen Visuals" )]
	[Property] public Texture PhotoSprite { get; set; }

	[Group( "ID Card Overrides" )]
	[Title( "Card Photo Override" )]
	[Property] public Texture IdCardPhotoSprite { get; set; }

	[Group( "ID Card Overrides" )]
	[Title( "Card Name Override" )]
	[Property] public string IdCardName { get; set; } = "";

	// ── Convenience properties ───────────────────────────────────────

	/// <summary>Photo to display on the physical ID card (falls back to PhotoSprite).</summary>
	public Texture IdCardPhoto => IdCardPhotoSprite ?? PhotoSprite;

	/// <summary>Name to print on the physical ID card (falls back to FullName).</summary>
	public string IdCardDisplayName => !string.IsNullOrEmpty( IdCardName ) ? IdCardName : FullName;
}
