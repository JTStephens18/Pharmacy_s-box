using Sandbox;

/// <summary>
/// Activates focus mode (camera lerp to monitor) and makes the computer UI visible.
/// Player presses E → computer activates; Escape → exits.
/// Replaces Unity's ComputerScreen.cs + ComputerScreenController.cs.
/// </summary>
public sealed class ComputerScreen : Component
{
	[Property] public GameObject FocusCameraTarget { get; set; }

	/// <summary>Exclusive user lock: SteamId of current user, or -1 if free.</summary>
	[Sync( SyncFlags.FromHost )] public long CurrentUserId { get; set; } = -1;

	public bool IsActive { get; private set; }
	public bool IsInUse => Networking.IsActive && CurrentUserId != -1;

	// ── Events ─────────────────────────────────────────────────────────
	public System.Action<bool> OnActivationChanged;

	// ── Active view state (read by ComputerPanel.razor) ───────────────
	public int ActiveViewIndex { get; private set; } = 0;

	protected override void OnStart()
	{
		// Register focus exit callback
		var focus = GetLocalFocusState();
		if ( focus != null )
			focus.OnFocusChanged += OnFocusChanged;
	}

	protected override void OnDestroy()
	{
		var focus = GetLocalFocusState();
		if ( focus != null )
			focus.OnFocusChanged -= OnFocusChanged;
	}

	/// <summary>Called by PlayerInteraction when player presses E on the monitor.</summary>
	public void Activate()
	{
		if ( IsActive || IsInUse ) return;
		RequestActivation();
	}

	[Rpc.Host]
	private void RequestActivation()
	{
		if ( CurrentUserId != -1 ) return;
		CurrentUserId = (long)Rpc.Caller.SteamId;
		ActivateForUser( Rpc.Caller );
	}

	[Rpc.Owner]
	private void ActivateForUser( Connection conn )
	{
		IsActive = true;
		ActiveViewIndex = 0;

		var focus = GetLocalFocusState();
		focus?.EnterFocus( FocusCameraTarget, () => Deactivate() );

		OnActivationChanged?.Invoke( true );
	}

	private void Deactivate()
	{
		IsActive = false;
		OnActivationChanged?.Invoke( false );
		ReleaseActivation();
	}

	[Rpc.Host]
	private void ReleaseActivation()
	{
		CurrentUserId = -1;
	}

	/// <summary>Switch to a different computer view tab.</summary>
	public void ShowView( int index )
	{
		ActiveViewIndex = index;
	}

	/// <summary>
	/// Temporarily exit focus for dialogue without fully deactivating the screen.
	/// Called by NPCInfoTalkButton.
	/// </summary>
	public void TemporaryExitForDialogue( System.Action onComplete )
	{
		var focus = GetLocalFocusState();
		if ( focus == null ) return;

		// Exit focus without triggering Deactivate callback
		focus.OnFocusChanged -= OnFocusChanged;
		focus.ExitFocus();
		focus.OnFocusChanged += OnFocusChanged;

		onComplete?.Invoke();
	}

	/// <summary>Re-enter computer focus after dialogue ends.</summary>
	public void ReactivateAfterDialogue()
	{
		var focus = GetLocalFocusState();
		focus?.EnterFocus( FocusCameraTarget );
	}

	private void OnFocusChanged( bool entering )
	{
		if ( !entering && IsActive )
			Deactivate();
	}

	private FocusStateManager GetLocalFocusState()
	{
		foreach ( var go in Game.ActiveScene.FindAllWithTag( "player" ) )
		{
			var pc = go.GetComponent<PlayerController>();
			if ( pc != null && !pc.IsProxy )
				return go.GetComponent<FocusStateManager>();
		}
		return null;
	}

	/// <summary>Force-release lock for a disconnected user. Called by disconnect handler.</summary>
	public void ForceReleaseLock( long userId )
	{
		if ( CurrentUserId == userId )
			CurrentUserId = -1;
	}
}
