namespace VenueBookingSystem.Utilities.Events;

// ╔══════════════════════════════════════════════════════════════════════════╗
// ║            📢  DOMAIN EVENT BASE + EVENT BUS                            ║
// ║  Provides a lightweight in-process publish/subscribe event system.      ║
// ║  Concept: Observer Pattern, Generics, Delegates, Decoupled Events       ║
// ╚══════════════════════════════════════════════════════════════════════════╝

// ── Base domain event ─────────────────────────────────────────────────────

/// <summary>
/// Base class for all domain events raised within the VenueBookingSystem.
/// Every event carries the time it was raised.
/// </summary>
public abstract class DomainEvent
{
    /// <summary>UTC timestamp when this event was created.</summary>
    public DateTime OccurredAt { get; } = DateTime.UtcNow;

    /// <summary>Optional correlation ID to trace a chain of related events.</summary>
    public string CorrelationId { get; init; } = Guid.NewGuid().ToString("N")[..8];
}

// ── Concrete domain events ────────────────────────────────────────────────

/// <summary>Raised when a customer successfully submits a new booking.</summary>
public sealed class BookingCreatedEvent : DomainEvent
{
    public int    BookingId    { get; init; }
    public int    CustomerId   { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public int    HallId       { get; init; }
    public string HallName     { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
}

/// <summary>Raised when admin confirms a pending booking.</summary>
public sealed class BookingConfirmedEvent : DomainEvent
{
    public int    BookingId   { get; init; }
    public string HallName    { get; init; } = string.Empty;
    public string CustomerName{ get; init; } = string.Empty;
    public DateTime StartDate { get; init; }
}

/// <summary>Raised when a customer cancels a confirmed/pending booking.</summary>
public sealed class BookingCancelledEvent : DomainEvent
{
    public int     BookingId    { get; init; }
    public int     CustomerId   { get; init; }
    public string  CustomerName { get; init; } = string.Empty;
    public string  HallName     { get; init; } = string.Empty;
    public decimal RefundAmount { get; init; }
    public string  Reason       { get; init; } = string.Empty;
}

/// <summary>Raised when admin approves a cancellation and processes the refund.</summary>
public sealed class RefundProcessedEvent : DomainEvent
{
    public int     CancellationId { get; init; }
    public int     BookingId      { get; init; }
    public string  CustomerName   { get; init; } = string.Empty;
    public decimal RefundAmount   { get; init; }
    public string  ProcessedBy    { get; init; } = string.Empty;
}

/// <summary>Raised when admin rejects a cancellation request.</summary>
public sealed class CancellationRejectedEvent : DomainEvent
{
    public int    CancellationId { get; init; }
    public int    BookingId      { get; init; }
    public string CustomerName   { get; init; } = string.Empty;
    public string Reason         { get; init; } = string.Empty;
}

/// <summary>Raised when a payment is completed for a booking.</summary>
public sealed class PaymentCompletedEvent : DomainEvent
{
    public int     PaymentId    { get; init; }
    public int     BookingId    { get; init; }
    public decimal Amount       { get; init; }
    public string  Method       { get; init; } = string.Empty;
    public string  TransactionRef{ get; init; }= string.Empty;
}

/// <summary>Raised when a new hall is added by admin.</summary>
public sealed class HallCreatedEvent : DomainEvent
{
    public int    HallId     { get; init; }
    public string HallName   { get; init; } = string.Empty;
    public string CreatedBy  { get; init; } = string.Empty;
}

// ── Lightweight in-process Event Bus ─────────────────────────────────────

/// <summary>
/// A simple synchronous in-process publish/subscribe event bus.
/// Handlers registered for a type are called when that event is published.
/// Concept: Observer Pattern, Dictionary of delegates, Generics.
/// </summary>
public sealed class EventBus
{
    // Singleton instance
    public static readonly EventBus Instance = new();
    private EventBus() { }

    // Map: event type → list of handler delegates
    private readonly Dictionary<Type, List<Action<DomainEvent>>> _handlers = new();

    // ── Subscribe ─────────────────────────────────────────────────────────

    /// <summary>
    /// Subscribes a strongly-typed handler to events of type TEvent.
    /// </summary>
    public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : DomainEvent
    {
        var type = typeof(TEvent);
        if (!_handlers.ContainsKey(type))
            _handlers[type] = new List<Action<DomainEvent>>();

        // Wrap the typed handler in an untyped Action<DomainEvent>
        _handlers[type].Add(e => handler((TEvent)e));
    }

    // ── Unsubscribe all for a type ────────────────────────────────────────

    /// <summary>Removes all handlers registered for TEvent.</summary>
    public void ClearHandlers<TEvent>() where TEvent : DomainEvent
    {
        _handlers.Remove(typeof(TEvent));
    }

    // ── Publish ───────────────────────────────────────────────────────────

    /// <summary>
    /// Publishes an event, invoking all registered handlers synchronously.
    /// Exceptions in individual handlers are caught and printed as warnings.
    /// </summary>
    public void Publish<TEvent>(TEvent domainEvent) where TEvent : DomainEvent
    {
        if (!_handlers.TryGetValue(typeof(TEvent), out var handlers)) return;

        foreach (var handler in handlers)
        {
            try   { handler(domainEvent); }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"[EventBus] Handler error for {typeof(TEvent).Name}: {ex.Message}");
                Console.ResetColor();
            }
        }
    }

    /// <summary>Returns the count of registered handlers for TEvent.</summary>
    public int HandlerCount<TEvent>() where TEvent : DomainEvent
        => _handlers.TryGetValue(typeof(TEvent), out var h) ? h.Count : 0;
}
