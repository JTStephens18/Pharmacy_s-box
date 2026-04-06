using Sandbox;
using System.Collections.Generic;

[System.Serializable]
public class InfoDialogueEntry
{
	[Property] public string Key { get; set; } = "";
	[Property] public string FilePath { get; set; } = "";
}

/// <summary>
/// Per-NPC dialogue trigger. Auto-starts dialogue when NPC is waiting and player is nearby.
/// Replaces Unity's NPCDialogueTrigger.cs.
/// </summary>
public sealed class NPCDialogueTrigger : Component
{
	[Property] public List<string> DialogueFiles { get; set; } = new();
	[Property] public List<InfoDialogueEntry> InfoDialogues { get; set; } = new();
	[Property] public float PlayerRange { get; set; } = 200f;
	[Property] public int MaxQuestions { get; set; } = 5;

	public int QuestionsRemaining { get; private set; }

	private NPCController _controller;
	private int _dialogueIndex;
	private bool _initialDialogueCompleted;

	protected override void OnAwake()
	{
		_controller = GetComponent<NPCController>();
		QuestionsRemaining = MaxQuestions;
	}

	protected override void OnUpdate()
	{
		if ( _controller == null ) return;
		if ( _controller.CurrentState != NPCController.NPCState.WaitingForCheckout ) return;
		if ( _initialDialogueCompleted ) return;
		if ( DialogueManager.Instance == null || DialogueManager.Instance.IsActive ) return;

		// Check player proximity
		var player = GetLocalPlayer();
		if ( player == null ) return;
		if ( WorldPosition.Distance( player.WorldPosition ) > PlayerRange ) return;

		_initialDialogueCompleted = true;
		StartNewConversation();
	}

	public void StartNewConversation()
	{
		if ( DialogueFiles.Count == 0 ) return;
		var file = DialogueFiles[_dialogueIndex % DialogueFiles.Count];
		_dialogueIndex++;
		DialogueManager.Instance?.StartDialogue( file, _controller?.NpcIdentity?.FullName );
	}

	public void StartInfoDialogue( string key )
	{
		if ( QuestionsRemaining <= 0 ) return;

		foreach ( var entry in InfoDialogues )
		{
			if ( entry.Key == key )
			{
				QuestionsRemaining--;
				DialogueManager.Instance?.StartDialogue( entry.FilePath, _controller?.NpcIdentity?.FullName );
				return;
			}
		}

		// Fallback
		StartNewConversation();
	}

	public bool HasInfoDialogue( string key )
	{
		foreach ( var entry in InfoDialogues )
			if ( entry.Key == key ) return true;
		return false;
	}

	public bool IsAvailableForDialogue()
	{
		return _controller?.CurrentState == NPCController.NPCState.WaitingForCheckout
			&& QuestionsRemaining > 0
			&& !(DialogueManager.Instance?.IsActive ?? false);
	}

	private static Component GetLocalPlayer()
	{
		// Find local player via tag
		foreach ( var go in Game.ActiveScene.FindAllWithTag( "player" ) )
		{
			var pc = go.GetComponent<PlayerController>();
			if ( pc != null && !pc.IsProxy ) return pc;
		}
		return null;
	}
}
