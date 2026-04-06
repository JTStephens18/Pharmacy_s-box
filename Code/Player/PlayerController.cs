using Sandbox;

/// <summary>
/// FPS player controller. Combines movement (walk/sprint/jump) and camera look.
/// Attach to the Player root GameObject alongside CharacterController.
/// Replaces Unity's PlayerMovement.cs + MouseLook.cs.
/// </summary>
public sealed class PlayerController : Component
{
	[Group( "Movement" )]
	[Property] public float WalkSpeed { get; set; } = 200f;
	[Group( "Movement" )]
	[Property] public float SprintSpeed { get; set; } = 350f;
	[Group( "Movement" )]
	[Property] public float Acceleration { get; set; } = 10f;
	[Group( "Movement" )]
	[Property] public float Deceleration { get; set; } = 12f;

	[Group( "Jumping" )]
	[Property] public float JumpStrength { get; set; } = 300f;
	[Group( "Jumping" )]
	[Property] public float CoyoteTime { get; set; } = 0.15f;
	[Group( "Jumping" )]
	[Property] public float JumpBufferTime { get; set; } = 0.1f;

	[Group( "Look" )]
	[Property] public float MouseSensitivity { get; set; } = 0.15f;
	[Group( "Look" )]
	[Property] public bool InvertY { get; set; } = false;

	[Group( "Shake" )]
	[Property] public float DefaultShakeIntensity { get; set; } = 5f;
	[Group( "Shake" )]
	[Property] public float DefaultShakeDuration { get; set; } = 0.2f;

	[Group( "References" )]
	[Property] public CameraComponent Camera { get; set; }

	// ── Internal state ───────────────────────────────────────────────
	private CharacterController _cc;
	private float _currentSpeed;
	private float _lastGroundedTime = float.NegativeInfinity;
	private float _lastJumpPressedTime = float.NegativeInfinity;

	// Screen shake
	private float _shakeTimeRemaining;
	private float _shakeIntensity;

	// ── Public accessors ─────────────────────────────────────────────
	public bool IsGrounded => _cc != null && _cc.IsOnGround;
	public float CurrentSpeed => _currentSpeed;

	/// <summary>Current eye/look angles. Read/write by FocusStateManager for save/restore.</summary>
	public Angles EyeAngles { get; set; }

	protected override void OnAwake()
	{
		_cc = GetComponent<CharacterController>();
	}

	protected override void OnUpdate()
	{
		if ( IsProxy ) return;

		HandleShake();

		// Block look + jump input while focused on a station
		var focus = GetComponent<FocusStateManager>();
		if ( focus != null && focus.IsFocused ) return;

		HandleLook();
		HandleJumpBuffer();
	}

	protected override void OnFixedUpdate()
	{
		if ( IsProxy ) return;

		var focus = GetComponent<FocusStateManager>();
		bool isFocused = focus != null && focus.IsFocused;

		HandleMovement( isFocused );
		if ( !isFocused ) HandleJump();
	}

	// ── Look ─────────────────────────────────────────────────────────

	private void HandleLook()
	{
		var look = Input.AnalogLook;
		EyeAngles += new Angles( look.pitch * (InvertY ? 1f : -1f), look.yaw, 0f );
		EyeAngles = EyeAngles.WithPitch( EyeAngles.pitch.Clamp( -89f, 89f ) );

		if ( _shakeTimeRemaining > 0f )
		{
			float progress = _shakeTimeRemaining / DefaultShakeDuration;
			float intensity = _shakeIntensity * progress;
			EyeAngles += new Angles(
				Game.Random.Float( -intensity, intensity ),
				Game.Random.Float( -intensity, intensity ),
				0f
			);
		}

		// Body rotates on yaw — this is what makes WASD camera-relative
		WorldRotation = Rotation.FromYaw( EyeAngles.yaw );

		// Camera rotates on pitch only (world rotation = body yaw + camera pitch)
		var cam = Camera ?? Scene.Camera;
		if ( cam != null )
			cam.WorldRotation = EyeAngles.ToRotation();
	}

	private void HandleShake()
	{
		if ( _shakeTimeRemaining > 0f )
			_shakeTimeRemaining -= Time.Delta;
	}

	/// <summary>Triggers a camera shake effect.</summary>
	public void Shake( float intensity = -1f, float duration = -1f )
	{
		_shakeIntensity = intensity > 0f ? intensity : DefaultShakeIntensity;
		_shakeTimeRemaining = duration > 0f ? duration : DefaultShakeDuration;
	}

	// ── Movement ─────────────────────────────────────────────────────

	private void HandleMovement( bool isFocused )
	{
		if ( _cc == null ) return;

		// Always apply gravity regardless of focus state
		if ( !_cc.IsOnGround )
			_cc.Velocity += Scene.PhysicsWorld.Gravity * Time.Delta;
		else if ( _cc.Velocity.z < 0f )
			_cc.Velocity = _cc.Velocity.WithZ( 0f );

		if ( isFocused )
		{
			// Decelerate to stop but keep gravity; no movement input
			_currentSpeed = MathX.Approach( _currentSpeed, 0f, Deceleration * 100f * Time.Delta );
			_cc.ApplyFriction( 6f );
			_cc.Move();
			return;
		}

		var inputDir = Input.AnalogMove;
		bool hasInput = inputDir.LengthSquared > 0.01f;

		bool isSprinting = Input.Down( "Run" ) && inputDir.x > 0f;
		float targetSpeed = isSprinting ? SprintSpeed : WalkSpeed;

		if ( hasInput )
			_currentSpeed = MathX.Approach( _currentSpeed, targetSpeed, Acceleration * 100f * Time.Delta );
		else
			_currentSpeed = MathX.Approach( _currentSpeed, 0f, Deceleration * 100f * Time.Delta );

		var wishDir = WorldRotation * new Vector3( inputDir.y, inputDir.x, 0f );

		_cc.Accelerate( wishDir.Normal * _currentSpeed );
		_cc.ApplyFriction( isSprinting ? 3f : 4f );
		_cc.Move();
	}

	// ── Jumping ───────────────────────────────────────────────────────

	private void HandleJumpBuffer()
	{
		if ( Input.Pressed( "Jump" ) )
			_lastJumpPressedTime = Time.Now;

		if ( _cc != null && _cc.IsOnGround )
			_lastGroundedTime = Time.Now;
	}

	private void HandleJump()
	{
		if ( _cc == null ) return;

		bool coyoteOk = (Time.Now - _lastGroundedTime) <= CoyoteTime;
		bool bufferOk = (Time.Now - _lastJumpPressedTime) <= JumpBufferTime;

		if ( coyoteOk && bufferOk )
		{
			_cc.Punch( Vector3.Up * JumpStrength );
			_lastJumpPressedTime = float.NegativeInfinity;
			_lastGroundedTime = float.NegativeInfinity;
		}
	}
}
