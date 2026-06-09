namespace VenueBookingSystem.Utilities.Helpers;

// ╔══════════════════════════════════════════════════════════════════════════╗
// ║            🚦  StateHelper  —  STATE MACHINE UTILITY                    ║
// ║  Tracks valid state transitions for domain entities.                    ║
// ║  Prevents illegal transitions (e.g. Completed → Pending is invalid).   ║
// ║  Concept: State Machine pattern, Generics, Dictionary lookup            ║
// ╚══════════════════════════════════════════════════════════════════════════╝

/// <summary>
/// A lightweight generic state machine.
/// Define which transitions are allowed; then call CanTransition / Transition.
/// </summary>
/// <typeparam name="TState">Any enum type representing states.</typeparam>
public class StateMachine<TState> where TState : struct, Enum
{
    // Map: from-state → set of valid to-states
    private readonly Dictionary<TState, HashSet<TState>> _transitions = new();

    // ── Builder ───────────────────────────────────────────────────────────

    /// <summary>
    /// Registers a valid transition: from → to.
    /// Supports chaining: machine.Allow(A, B).Allow(B, C)
    /// </summary>
    public StateMachine<TState> Allow(TState from, TState to)
    {
        if (!_transitions.ContainsKey(from))
            _transitions[from] = new HashSet<TState>();
        _transitions[from].Add(to);
        return this;
    }

    /// <summary>Registers multiple valid targets for one source state.</summary>
    public StateMachine<TState> Allow(TState from, params TState[] toStates)
    {
        foreach (var to in toStates)
            Allow(from, to);
        return this;
    }

    // ── Query ─────────────────────────────────────────────────────────────

    /// <summary>Returns true if moving from → to is a registered valid transition.</summary>
    public bool CanTransition(TState from, TState to)
        => _transitions.TryGetValue(from, out var targets) && targets.Contains(to);

    /// <summary>Returns all valid next states from the current state.</summary>
    public IReadOnlySet<TState> GetAllowedTransitions(TState from)
        => _transitions.TryGetValue(from, out var targets)
            ? targets
            : (IReadOnlySet<TState>)new HashSet<TState>();

    // ── Transition ────────────────────────────────────────────────────────

    /// <summary>
    /// Attempts the transition from → to.
    /// Returns true and sets current to 'to' on success.
    /// Returns false and leaves current unchanged on failure.
    /// </summary>
    public bool TryTransition(ref TState current, TState to)
    {
        if (!CanTransition(current, to)) return false;
        current = to;
        return true;
    }

    /// <summary>
    /// Forces the transition from → to.
    /// Throws InvalidOperationException if the transition is not allowed.
    /// </summary>
    public void Transition(ref TState current, TState to)
    {
        if (!CanTransition(current, to))
            throw new InvalidOperationException(
                $"Transition from '{current}' to '{to}' is not permitted.");
        current = to;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Pre-built state machines for VenueBookingSystem domain entities
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Ready-to-use state machines for the system's core domain entities.
/// Import this class to get pre-configured transition rules.
/// </summary>
public static class DomainStateMachines
{
    // ── Booking state machine ─────────────────────────────────────────────

    /// <summary>
    /// Valid booking lifecycle transitions:
    ///   Pending  → Confirmed, Cancelled, Rejected
    ///   Confirmed → Completed, Cancelled
    ///   (Completed, Cancelled, Rejected are terminal — no further transitions)
    /// </summary>
    public static StateMachine<BookingStateEnum> Booking { get; } =
        new StateMachine<BookingStateEnum>()
            .Allow(BookingStateEnum.Pending,   BookingStateEnum.Confirmed, BookingStateEnum.Cancelled, BookingStateEnum.Rejected)
            .Allow(BookingStateEnum.Confirmed, BookingStateEnum.Completed, BookingStateEnum.Cancelled);

    // ── Cancellation state machine ────────────────────────────────────────

    /// <summary>
    /// Valid cancellation request transitions:
    ///   Pending → Approved, Rejected
    ///   (Approved and Rejected are terminal)
    /// </summary>
    public static StateMachine<CancellationStateEnum> Cancellation { get; } =
        new StateMachine<CancellationStateEnum>()
            .Allow(CancellationStateEnum.Pending, CancellationStateEnum.Approved, CancellationStateEnum.Rejected);

    // ── Refund state machine ──────────────────────────────────────────────

    /// <summary>
    /// Valid refund status transitions:
    ///   Initiated → Processed, Failed
    ///   Failed    → Initiated  (retry)
    /// </summary>
    public static StateMachine<RefundStateEnum> Refund { get; } =
        new StateMachine<RefundStateEnum>()
            .Allow(RefundStateEnum.Initiated, RefundStateEnum.Processed, RefundStateEnum.Failed)
            .Allow(RefundStateEnum.Failed,    RefundStateEnum.Initiated);
}

// ── Local enum aliases (mirrors DB string values) ────────────────────────

public enum BookingStateEnum      { Pending = 1, Confirmed, Cancelled, Completed, Rejected }
public enum CancellationStateEnum { Pending = 1, Approved, Rejected }
public enum RefundStateEnum       { Initiated = 1, Processed, Failed }
