using Sandbox;
using System;
using System.Threading.Tasks;

/// <summary>
/// Host-authoritative state machine driving the day/night cycle.
/// Dawn → DayShift → (Transition →) NightShift → Dawn → ...
/// Replaces Unity's ShiftManager.cs.
/// </summary>
public sealed class ShiftManager : Component
{
	public enum ShiftPhase { Dawn = 0, DayShift = 1, Transition = 2, NightShift = 3 }

	// ── Inspector fields ──────────────────────────────────────────────
	[Property] public NPCSpawnManager SpawnManager { get; set; }
	[Property] public RoundConfig DefaultRoundConfig { get; set; }
	[Property] public ShiftScoreManager ScoreManager { get; set; }

	[Group( "Timing" )]
	[Property] public float DawnDuration { get; set; } = 5f;
	[Group( "Timing" )]
	[Property] public float TransitionDuration { get; set; } = 4f;
	[Group( "Timing" )]
	[Property] public float NightDuration { get; set; } = 120f;

	// ── Synced state ──────────────────────────────────────────────────
	[Sync( SyncFlags.FromHost )] public int CurrentPhase { get; set; } = (int)ShiftPhase.Dawn;
	[Sync( SyncFlags.FromHost )] public int EscapedDoppelgangers { get; set; } = 0;
	[Sync( SyncFlags.FromHost )] public int CurrentNight { get; set; } = 1;

	/// <summary>Current phase as enum (convenience).</summary>
	public ShiftPhase Phase => (ShiftPhase)CurrentPhase;

	// ── Events (host-side) ────────────────────────────────────────────
	public event Action<ShiftPhase> OnPhaseChanged;
	public event Action OnDayShiftStarted;
	public event Action OnNightShiftStarted;
	public event Action OnShiftCycleCompleted;

	// ── Static singleton ──────────────────────────────────────────────
	public static ShiftManager Instance { get; private set; }

	// ── Private ───────────────────────────────────────────────────────
	private int _lastPhase = -1;
	private int _monsterCount;
	private bool _skipPhaseTimer;

	// ── Lifecycle ─────────────────────────────────────────────────────

	protected override void OnAwake()
	{
		Instance = this;
	}

	protected override void OnStart()
	{
		if ( Networking.IsHost && SpawnManager != null )
			SpawnManager.OnAllNPCsFinished += OnAllNPCsFinished;
	}

	protected override void OnUpdate()
	{
		// Clients react to phase changes via OnRefresh
	}

	protected override void OnRefresh()
	{
		if ( CurrentPhase != _lastPhase )
		{
			_lastPhase = CurrentPhase;
			OnPhaseChanged?.Invoke( Phase );
		}
	}

	protected override void OnDestroy()
	{
		if ( Instance == this ) Instance = null;
		if ( SpawnManager != null )
			SpawnManager.OnAllNPCsFinished -= OnAllNPCsFinished;
	}

	// ── Public API (host-only) ────────────────────────────────────────

	/// <summary>Begin a day shift. Called by GameStarter.</summary>
	public void StartDayShift()
	{
		if ( !Networking.IsHost ) return;
		ScoreManager?.ResetForNewShift();
		SetPhase( ShiftPhase.DayShift );
		OnDayShiftStarted?.Invoke();
		SpawnManager?.StartNPCSpawning( DefaultRoundConfig );
	}

	/// <summary>Increment escaped doppelgangers. Called by CashRegister.</summary>
	public void ReportEscape()
	{
		if ( !Networking.IsHost ) return;
		EscapedDoppelgangers++;
	}

	/// <summary>Report a monster kill. Host-only.</summary>
	public void OnMonsterKilled()
	{
		if ( !Networking.IsHost ) return;
		_monsterCount--;
		if ( _monsterCount <= 0 )
			OnDawnReached();
	}

	/// <summary>End night shift and go to dawn. Called by monster system or timer.</summary>
	public void OnDawnReached()
	{
		if ( !Networking.IsHost ) return;
		_ = DawnPhaseAsync();
	}

	/// <summary>Debug: force-jump to any phase.</summary>
	public void ForcePhase( ShiftPhase phase )
	{
		if ( !Networking.IsHost ) return;
		_skipPhaseTimer = true;
		SetPhase( phase );
	}

	// ── Phase transitions ─────────────────────────────────────────────

	private void OnAllNPCsFinished()
	{
		if ( !Networking.IsHost ) return;

		if ( EscapedDoppelgangers > 0 )
			_ = TransitionToNightAsync();
		else
			_ = DawnPhaseAsync();
	}

	private async Task DawnPhaseAsync()
	{
		SetPhase( ShiftPhase.Dawn );
		OnShiftCycleCompleted?.Invoke();

		_skipPhaseTimer = false;
		float elapsed = 0f;
		while ( elapsed < DawnDuration && !_skipPhaseTimer )
		{
			elapsed += Time.Delta;
			await Task.Yield();
		}

		CurrentNight++;
		StartDayShift();
	}

	private async Task TransitionToNightAsync()
	{
		SetPhase( ShiftPhase.Transition );
		TriggerLightsFlicker();

		_skipPhaseTimer = false;
		float elapsed = 0f;
		while ( elapsed < TransitionDuration && !_skipPhaseTimer )
		{
			elapsed += Time.Delta;
			await Task.Yield();
		}

		await NightShiftAsync();
	}

	private async Task NightShiftAsync()
	{
		SetPhase( ShiftPhase.NightShift );
		EscapedDoppelgangers = 0;
		_monsterCount = 1; // Placeholder — real monster spawning is Phase 3
		OnNightShiftStarted?.Invoke();

		if ( NightDuration <= 0f ) return; // Infinite — killed by monsters only

		_skipPhaseTimer = false;
		float elapsed = 0f;
		while ( elapsed < NightDuration && !_skipPhaseTimer && _monsterCount > 0 )
		{
			elapsed += Time.Delta;
			await Task.Yield();
		}

		OnDawnReached();
	}

	private void SetPhase( ShiftPhase phase )
	{
		CurrentPhase = (int)phase;
		OnPhaseChanged?.Invoke( phase );
	}

	[Rpc.Broadcast]
	public void TriggerLightsFlicker()
	{
		// Lighting system reacts here (Phase 3: ShiftLighting)
	}
}
