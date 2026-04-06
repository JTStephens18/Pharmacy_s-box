using Sandbox;
using System;

/// <summary>
/// Manages transitions between FPS mode and focus mode (computer, pill station, ID card).
/// Attach to the Player root alongside Sandbox.PlayerController.
/// Replaces Unity's FocusStateManager.cs.
/// </summary>
public sealed class FocusStateManager : Component
{
	[Property] public float TransitionDuration { get; set; } = 0.3f;

	/// <summary>True while focused on a station. Blocks player input.</summary>
	public bool IsFocused { get; private set; }

	// Events
	public Action<bool> OnFocusChanged;

	private GameObject _focusTarget;
	private Action _onExit;
	private PlayerController _player;

	// Lerp state
	private bool _transitioning;
	private float _transitionProgress;
	private Vector3 _transitionFromPos;
	private Rotation _transitionFromRot;
	private bool _enteringFocus;

	protected override void OnAwake()
	{
		_player = GetComponent<PlayerController>();
	}

	/// <summary>Enter focus mode, lerping camera to <paramref name="target"/>.</summary>
	public void EnterFocus( GameObject target, Action onExit = null )
	{
		if ( IsFocused ) return;
		if ( target == null || !target.IsValid() ) return;

		IsFocused = true;
		_focusTarget = target;
		_onExit = onExit;

		if ( _player != null )
		{
			_player.UseInputControls = false;
			_player.UseLookControls = false;
			_player.UseCameraControls = false;
		}

		_transitionFromPos = Scene.Camera.WorldPosition;
		_transitionFromRot = Scene.Camera.WorldRotation;
		_transitionProgress = 0f;
		_transitioning = true;
		_enteringFocus = true;

		OnFocusChanged?.Invoke( true );
	}

	/// <summary>Exit focus mode, lerping camera back then re-enabling built-in controls.</summary>
	public void ExitFocus()
	{
		if ( !IsFocused ) return;

		IsFocused = false;

		_transitionFromPos = Scene.Camera.WorldPosition;
		_transitionFromRot = Scene.Camera.WorldRotation;
		_transitionProgress = 0f;
		_transitioning = true;
		_enteringFocus = false;

		_onExit?.Invoke();
		_onExit = null;

		OnFocusChanged?.Invoke( false );
	}

	protected override void OnUpdate()
	{
		if ( IsProxy ) return;

		if ( IsFocused && Input.Pressed( "menu" ) )
		{
			ExitFocus();
			return;
		}

		if ( _transitioning )
			HandleTransition();
	}

	private void HandleTransition()
	{
		_transitionProgress += Time.Delta / TransitionDuration;
		_transitionProgress = _transitionProgress.Clamp( 0f, 1f );
		float t = _transitionProgress * _transitionProgress * (3f - 2f * _transitionProgress);

		if ( _enteringFocus && _focusTarget != null && _focusTarget.IsValid() )
		{
			Scene.Camera.WorldPosition = Vector3.Lerp( _transitionFromPos, _focusTarget.WorldPosition, t );
			Scene.Camera.WorldRotation = Rotation.Lerp( _transitionFromRot, _focusTarget.WorldRotation, t );
		}
		else if ( !_enteringFocus )
		{
			// Lerp back toward the built-in controller's eye position.
			// BodyHeight (72) - EyeDistanceFromTop (8) = 64 units above player root.
			var eyePos = GameObject.WorldPosition + Vector3.Up * 64f;
			Scene.Camera.WorldPosition = Vector3.Lerp( _transitionFromPos, eyePos, t );
		}

		if ( _transitionProgress >= 1f )
		{
			_transitioning = false;

			if ( _enteringFocus && _focusTarget != null && _focusTarget.IsValid() )
			{
				Scene.Camera.WorldPosition = _focusTarget.WorldPosition;
				Scene.Camera.WorldRotation = _focusTarget.WorldRotation;
			}
			else if ( !_enteringFocus && _player != null )
			{
				// Lerp complete — hand camera back to built-in controller
				_player.UseCameraControls = true;
				_player.UseLookControls = true;
				_player.UseInputControls = true;
			}
		}
	}
}
