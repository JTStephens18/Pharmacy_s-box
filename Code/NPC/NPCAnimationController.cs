using Sandbox;

/// <summary>
/// Drives NPC animations from NPCController state.
/// Attach alongside NPCController. Replaces Unity's NPCAnimationController.cs.
/// </summary>
public sealed class NPCAnimationController : Component
{
	private NPCController _controller;
	private SkinnedModelRenderer _model;
	private int _lastState = -1;

	protected override void OnAwake()
	{
		_controller = GetComponent<NPCController>();
		_model = GetComponentInChildren<SkinnedModelRenderer>();
	}

	protected override void OnUpdate()
	{
		if ( _model == null || _controller == null ) return;

		int currentState = _controller.NetworkState;
		bool isMoving = currentState == (int)NPCController.NPCState.MovingToItem
			|| currentState == (int)NPCController.NPCState.MovingToCounter
			|| currentState == (int)NPCController.NPCState.MovingToExit;

		_model.Set( "b_walking", isMoving );

		var agent = GetComponent<NavMeshAgent>();
		if ( agent != null )
			_model.Set( "f_speed", agent.Velocity.LengthSquared > 1f ? 1f : 0f );
	}

	/// <summary>Trigger pickup animation — called via RPC from NPCController.</summary>
	public void TriggerPickup() => _model?.Set( "b_pickup", true );

	/// <summary>Trigger place animation — called via RPC from NPCController.</summary>
	public void TriggerPlace() => _model?.Set( "b_place", true );
}
