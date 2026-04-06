using Sandbox;
using System;
using System.Collections.Generic;

/// <summary>
/// Singleton that manages the dialogue overlay panel.
/// Drives DialoguePanel.razor via reactive state properties.
/// Replaces Unity's DialogueManager.cs.
/// </summary>
public sealed class DialogueManager : Component
{
	public static DialogueManager Instance { get; private set; }

	[Property] public string CloseButtonText { get; set; } = "[Continue]";
	[Property] public float NpcHeadOffset { get; set; } = 70f;

	// ── Events ─────────────────────────────────────────────────────────
	public event Action OnDialogueStarted;
	public event Action OnDialogueEnded;

	// ── Reactive state read by DialoguePanel.razor ─────────────────────
	public bool IsActive { get; private set; }
	public string SpeakerName { get; private set; } = "";
	public string BodyText { get; private set; } = "";
	public List<DialogueResponse> CurrentResponses { get; private set; } = new();
	public bool IsTerminal { get; private set; }

	// ── Private ────────────────────────────────────────────────────────
	private DialogueData _data;
	private Dictionary<string, DialogueNode> _lookup;
	private DialogueNode _currentNode;
	private string _speakerOverride;
	private bool _suppressEndReset;

	protected override void OnAwake()
	{
		Instance = this;
	}

	protected override void OnDestroy()
	{
		if ( Instance == this ) Instance = null;
	}

	// ── Public API ─────────────────────────────────────────────────────

	/// <summary>Start dialogue from a JSON file path (e.g. "data/dialogues/npc_01.json").</summary>
	public void StartDialogue( string filePath, string speakerNameOverride = null )
	{
		var (data, lookup) = DialogueLoader.Load( filePath );
		if ( data == null || lookup == null ) return;
		StartDialogue( data, lookup, speakerNameOverride );
	}

	/// <summary>Start dialogue from pre-loaded data.</summary>
	public void StartDialogue( DialogueData data, Dictionary<string, DialogueNode> lookup, string speakerNameOverride = null )
	{
		_data = data;
		_lookup = lookup;
		_speakerOverride = speakerNameOverride;
		_suppressEndReset = false;

		IsActive = true;
		// Cursor visibility managed by active Razor UI panels

		var startNode = string.IsNullOrEmpty( data.StartNodeId )
			? (data.Nodes?.Length > 0 ? data.Nodes[0] : null)
			: lookup.GetValueOrDefault( data.StartNodeId );

		if ( startNode == null )
		{
			EndDialogue();
			return;
		}

		ShowNode( startNode );
		OnDialogueStarted?.Invoke();
	}

	/// <summary>Called when the player picks a response (by index).</summary>
	public void SelectResponse( int index )
	{
		if ( _currentNode == null || _currentNode.Responses == null ) return;
		if ( index < 0 || index >= _currentNode.Responses.Length ) return;

		var response = _currentNode.Responses[index];
		if ( string.IsNullOrEmpty( response.NextNodeId ) )
		{
			EndDialogue();
			return;
		}

		if ( _lookup.TryGetValue( response.NextNodeId, out var nextNode ) )
			ShowNode( nextNode );
		else
			EndDialogue();
	}

	/// <summary>Close button / Continue button pressed (terminal nodes).</summary>
	public void Continue()
	{
		EndDialogue();
	}

	/// <summary>
	/// Suppresses cursor relock + control re-enable on EndDialogue.
	/// Used by NPCInfoTalkButton when focus will immediately re-enter.
	/// </summary>
	public void SetSuppressEndReset( bool suppress )
	{
		_suppressEndReset = suppress;
	}

	public void EndDialogue()
	{
		IsActive = false;
		// Cursor visibility managed by active Razor UI panels

		_currentNode = null;
		SpeakerName = "";
		BodyText = "";
		CurrentResponses.Clear();
		IsTerminal = false;

		OnDialogueEnded?.Invoke();
	}

	// ── Private ────────────────────────────────────────────────────────

	private void ShowNode( DialogueNode node )
	{
		_currentNode = node;

		// Speaker name priority: node → override → data root
		SpeakerName = !string.IsNullOrEmpty( node.SpeakerName ) ? node.SpeakerName
			: !string.IsNullOrEmpty( _speakerOverride ) ? _speakerOverride
			: _data.SpeakerName ?? "";

		BodyText = node.Text ?? "";
		IsTerminal = node.IsTerminal;

		CurrentResponses.Clear();
		if ( !IsTerminal && node.Responses != null )
			CurrentResponses.AddRange( node.Responses );
	}
}
