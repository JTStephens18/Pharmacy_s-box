using Sandbox;

/// <summary>
/// GameResource representing a patient's prescription.
/// Displayed on the computer screen for verification.
/// Replaces Unity's PrescriptionData ScriptableObject.
/// </summary>
[GameResource( "Prescription Data", "prescription", "Defines a patient's prescription details", Icon = "medication" )]
public class PrescriptionData : GameResource
{
	[Group( "Prescription" )]
	[Property] public string MedicationName { get; set; } = "";

	[Group( "Prescription" )]
	[Property] public int Quantity { get; set; } = 30;

	[Group( "Prescription" )]
	[Property] public string Dosage { get; set; } = "";

	[Group( "Prescriber" )]
	[Property] public string PrescriberName { get; set; } = "";

	[Group( "Prescriber" )]
	[Property] public string PrescriberNPI { get; set; } = "";

	[Group( "Prescriber" )]
	[Property] public string PrescriberSpecialty { get; set; } = "";

	[Group( "Prescriber" )]
	[Property] public string PrescriberAddress { get; set; } = "";

	[Group( "Fill History" )]
	[Property] public string[] PreviousFills { get; set; } = System.Array.Empty<string>();
}
