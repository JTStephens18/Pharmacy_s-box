using Sandbox;
using System;

/// <summary>
/// Tracks shift outcomes: money, customers served, kills, escapes.
/// Host-authoritative, synced to all clients.
/// Replaces Unity's ShiftScoreManager.cs.
/// </summary>
public sealed class ShiftScoreManager : Component
{
	// ── Reward/penalty amounts ────────────────────────────────────────
	[Group( "Rewards" )]
	[Property] public int CorrectApprovalReward { get; set; } = 50;
	[Group( "Rewards" )]
	[Property] public int CorrectKillReward { get; set; } = 25;
	[Group( "Penalties" )]
	[Property] public int WrongKillPenalty { get; set; } = 100;

	// ── Synced state ──────────────────────────────────────────────────
	[Sync( SyncFlags.FromHost )] public int Money { get; set; } = 0;
	[Sync( SyncFlags.FromHost )] public int CustomersServed { get; set; } = 0;
	[Sync( SyncFlags.FromHost )] public int DoppelgangersCaught { get; set; } = 0;
	[Sync( SyncFlags.FromHost )] public int DoppelgangersEscaped { get; set; } = 0;
	[Sync( SyncFlags.FromHost )] public int InnocentsKilled { get; set; } = 0;

	public static ShiftScoreManager Instance { get; private set; }

	public event Action<ShiftScoreManager> OnScoreChanged;

	protected override void OnAwake()
	{
		Instance = this;
	}

	protected override void OnDestroy()
	{
		if ( Instance == this ) Instance = null;
	}

	// ── Score methods (host-only) ─────────────────────────────────────

	public void RecordCorrectApproval()
	{
		if ( !Networking.IsHost ) return;
		Money += CorrectApprovalReward;
		CustomersServed++;
		OnScoreChanged?.Invoke( this );
	}

	public void RecordWrongApproval()
	{
		if ( !Networking.IsHost ) return;
		DoppelgangersEscaped++;
		OnScoreChanged?.Invoke( this );
	}

	public void RecordCorrectKill()
	{
		if ( !Networking.IsHost ) return;
		Money += CorrectKillReward;
		DoppelgangersCaught++;
		OnScoreChanged?.Invoke( this );
	}

	public void RecordWrongKill()
	{
		if ( !Networking.IsHost ) return;
		Money -= WrongKillPenalty;
		InnocentsKilled++;
		OnScoreChanged?.Invoke( this );
	}

	/// <summary>Resets per-shift counters (Money persists).</summary>
	public void ResetForNewShift()
	{
		if ( !Networking.IsHost ) return;
		CustomersServed = 0;
		DoppelgangersCaught = 0;
		DoppelgangersEscaped = 0;
		InnocentsKilled = 0;
		OnScoreChanged?.Invoke( this );
	}
}
