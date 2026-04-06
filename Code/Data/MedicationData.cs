using Sandbox;

/// <summary>
/// GameResource defining a medication type used in the pill-filling station.
/// Replaces Unity's MedicationData ScriptableObject.
/// </summary>
[GameResource( "Medication Data", "medication", "Defines a medication type and pill appearance", Icon = "science" )]
public class MedicationData : GameResource
{
	[Property] public string MedicationName { get; set; } = "";
	[Property] public Color PillColor { get; set; } = Color.White;
	[Property] public Model PillModel { get; set; }
}
