using Sandbox;

/// <summary>NPC-pickable object interface. Attach to shelf items.</summary>
public interface IInteractable
{
	Vector3 GetInteractionPoint();
	void OnPickedUp( GameObject hand );
}
