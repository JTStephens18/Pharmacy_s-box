using Sandbox;
using System.Threading.Tasks;

/// <summary>
/// Attach to ID card prefabs. Handles focus mode + barcode scanning.
/// Replaces Unity's IDCardInteraction.cs.
/// </summary>
public sealed class IDCardInteraction : Component
{
	[Property] public Collider BarcodeZone { get; set; }
	[Property] public float AutoExitDelay { get; set; } = 1f;
	[Property] public GameObject FocusCameraTarget { get; set; }

	public bool IsActive { get; private set; }

	private NPCIdentity _identity;
	private bool _scanned;

	/// <summary>Initialize after card is placed. Called by IDCardSlot.</summary>
	public void Initialize( NPCIdentity identity, GameObject focusTarget )
	{
		_identity = identity;
		FocusCameraTarget = focusTarget;

		var visuals = GetComponent<IDCardVisuals>();
		visuals?.Initialize( identity );
	}

	/// <summary>Called by PlayerInteraction when player presses E on the card.</summary>
	public void Activate()
	{
		if ( IsActive ) return;
		IsActive = true;

		var focus = GetLocalFocusState();
		focus?.EnterFocus( FocusCameraTarget, () => IsActive = false );
	}

	protected override void OnUpdate()
	{
		if ( !IsActive || _scanned ) return;

		// Detect mouse click on barcode zone
		if ( Input.Pressed( "Attack1" ) && BarcodeZone != null )
		{
			var tr = Scene.Trace
				.Ray( Scene.Camera.WorldPosition, Scene.Camera.WorldPosition + Scene.Camera.WorldRotation.Forward * 200f )
				.Run();

			if ( tr.Hit && tr.GameObject == BarcodeZone.GameObject )
				_ = OnBarcodeScanned();
		}
	}

	private async Task OnBarcodeScanned()
	{
		_scanned = true;
		NPCInfoDisplay.Instance?.ShowNPCInfo( _identity );

		await Task.DelaySeconds( AutoExitDelay );

		var focus = GetLocalFocusState();
		focus?.ExitFocus();
		IsActive = false;
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
}
