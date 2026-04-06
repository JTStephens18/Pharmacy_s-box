using Sandbox;

public enum DiscrepancyType
{
	PhotoMismatch,
	InvalidNPI,
	NoFillHistory,
	WrongPrescriberSpecialty,
	DoseJump,
	NonStandardQuantity,
	PrescriberOutsideArea,
	WrongDOB,
	WrongAddress
}

/// <summary>
/// GameResource defining a doppelganger's fake data and discrepancies.
/// Replaces Unity's DoppelgangerProfile ScriptableObject.
/// </summary>
[GameResource( "Doppelganger Profile", "doppelganger", "Defines fake data overrides for a doppelganger NPC", Icon = "masks" )]
public class DoppelgangerProfile : GameResource
{
	[Group( "Discrepancies" )]
	[Property] public DiscrepancyType[] Discrepancies { get; set; } = System.Array.Empty<DiscrepancyType>();

	[Group( "Identity Overrides" )]
	[Property] public Texture FakePhoto { get; set; }

	[Group( "Identity Overrides" )]
	[Property] public string FakeDOB { get; set; } = "";

	[Group( "Identity Overrides" )]
	[Property] public string FakeAddress { get; set; } = "";

	[Group( "Prescription Overrides" )]
	[Property] public string FakePrescriberNPI { get; set; } = "";

	[Group( "Prescription Overrides" )]
	[Property] public string FakePrescriberSpecialty { get; set; } = "";

	[Group( "Prescription Overrides" )]
	[Property] public string FakeDosage { get; set; } = "";

	[Group( "Prescription Overrides" )]
	[Title( "Fake Quantity (0 = use real)" )]
	[Property] public int FakeQuantity { get; set; } = 0;

	// ── Convenience methods ──────────────────────────────────────────

	public bool HasOverride( DiscrepancyType type )
	{
		if ( Discrepancies == null ) return false;
		foreach ( var d in Discrepancies )
			if ( d == type ) return true;
		return false;
	}

	public string GetDOB( string real ) => !string.IsNullOrEmpty( FakeDOB ) ? FakeDOB : real;
	public string GetAddress( string real ) => !string.IsNullOrEmpty( FakeAddress ) ? FakeAddress : real;
	public Texture GetPhoto( Texture real ) => FakePhoto != null ? FakePhoto : real;
	public string GetPrescriberNPI( string real ) => !string.IsNullOrEmpty( FakePrescriberNPI ) ? FakePrescriberNPI : real;
	public string GetPrescriberSpecialty( string real ) => !string.IsNullOrEmpty( FakePrescriberSpecialty ) ? FakePrescriberSpecialty : real;
	public string GetDosage( string real ) => !string.IsNullOrEmpty( FakeDosage ) ? FakeDosage : real;
	public int GetQuantity( int real ) => FakeQuantity > 0 ? FakeQuantity : real;
}
