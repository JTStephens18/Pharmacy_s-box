using Sandbox;
using System.Collections.Generic;

[System.Serializable]
public class PrescriberEntry
{
	[Property] public string PrescriberName { get; set; } = "";
	[Property] public string NPI { get; set; } = "";
	[Property] public string Specialty { get; set; } = "";
	[Property] public string Address { get; set; } = "";
}

/// <summary>
/// GameResource containing valid prescriber records for NPI lookup on the computer screen.
/// Replaces Unity's PrescriberDatabase ScriptableObject.
/// </summary>
[GameResource( "Prescriber Database", "npidb", "List of valid prescribers for NPI verification", Icon = "local_hospital" )]
public class PrescriberDatabase : GameResource
{
	[Property] public List<PrescriberEntry> Prescribers { get; set; } = new();

	private Dictionary<string, PrescriberEntry> _npiLookup;

	/// <summary>Look up a prescriber by NPI number. Returns null if not found.</summary>
	public PrescriberEntry LookupByNPI( string npi )
	{
		if ( string.IsNullOrEmpty( npi ) ) return null;

		_npiLookup ??= BuildLookup();
		_npiLookup.TryGetValue( npi.Trim(), out var entry );
		return entry;
	}

	/// <summary>Returns true if the NPI exists in the database.</summary>
	public bool IsValidNPI( string npi ) => LookupByNPI( npi ) != null;

	private Dictionary<string, PrescriberEntry> BuildLookup()
	{
		var dict = new Dictionary<string, PrescriberEntry>();
		foreach ( var entry in Prescribers )
		{
			if ( !string.IsNullOrEmpty( entry.NPI ) )
				dict[entry.NPI.Trim()] = entry;
		}
		return dict;
	}
}
