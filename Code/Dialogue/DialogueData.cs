using Sandbox;
using System.Collections.Generic;
using System.Text.Json.Serialization;

/// <summary>Data classes matching the dialogue JSON format. Unchanged from Unity.</summary>

[System.Serializable]
public class DialogueResponse
{
	[JsonPropertyName( "text" )] public string Text { get; set; }
	[JsonPropertyName( "nextNodeId" )] public string NextNodeId { get; set; }
}

[System.Serializable]
public class DialogueNode
{
	[JsonPropertyName( "id" )] public string Id { get; set; }
	[JsonPropertyName( "text" )] public string Text { get; set; }
	[JsonPropertyName( "speakerName" )] public string SpeakerName { get; set; }
	[JsonPropertyName( "responses" )] public DialogueResponse[] Responses { get; set; }

	public bool IsTerminal => Responses == null || Responses.Length == 0;
}

[System.Serializable]
public class DialogueData
{
	[JsonPropertyName( "dialogueId" )] public string DialogueId { get; set; }
	[JsonPropertyName( "speakerName" )] public string SpeakerName { get; set; }
	[JsonPropertyName( "startNodeId" )] public string StartNodeId { get; set; }
	[JsonPropertyName( "nodes" )] public DialogueNode[] Nodes { get; set; }
}

public static class DialogueLoader
{
	/// <summary>
	/// Loads dialogue JSON from a file path (relative to mounted filesystem).
	/// s&box equivalent of Unity's TextAsset + JsonUtility.
	/// </summary>
	public static (DialogueData data, Dictionary<string, DialogueNode> lookup) Load( string filePath )
	{
		if ( string.IsNullOrEmpty( filePath ) )
		{
			Log.Error( "[DialogueLoader] filePath is null or empty." );
			return (null, null);
		}

		string json;
		try
		{
			json = FileSystem.Mounted.ReadAllText( filePath );
		}
		catch
		{
			Log.Error( $"[DialogueLoader] Failed to read file: {filePath}" );
			return (null, null);
		}

		var data = Json.Deserialize<DialogueData>( json );
		if ( data == null )
		{
			Log.Error( $"[DialogueLoader] Failed to parse JSON from '{filePath}'." );
			return (null, null);
		}

		var lookup = new Dictionary<string, DialogueNode>();
		if ( data.Nodes != null )
		{
			foreach ( var node in data.Nodes )
			{
				if ( string.IsNullOrEmpty( node.Id ) ) continue;
				if ( lookup.ContainsKey( node.Id ) ) continue;
				lookup[node.Id] = node;
			}
		}

		return (data, lookup);
	}
}
