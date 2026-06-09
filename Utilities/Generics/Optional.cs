namespace VenueBookingSystem.Utilities.Generics;

// ╔══════════════════════════════════════════════════════════════════════════╗
// ║         📋  Optional<T>  —  NULL-SAFE VALUE CONTAINER (MAYBE TYPE)      ║
// ║  Replaces nullable returns with a descriptive wrapper that forces        ║
// ║  callers to check HasValue before using Data.                           ║
// ╚══════════════════════════════════════════════════════════════════════════╝

/// <summary>
/// A generic container that either holds a value or is empty — a safe
/// alternative to returning null from service/repository methods.
/// Concept: Generics, immutability, null-safety pattern.
/// </summary>
/// <typeparam name="T">The contained value type.</typeparam>
public readonly struct Optional<T>
{
    private readonly T?   _value;
    private readonly bool _hasValue;

    // ── Private constructor ───────────────────────────────────────────────
    private Optional(T value) { _value = value; _hasValue = true; }

    // ── Factories ─────────────────────────────────────────────────────────

    /// <summary>Creates an Optional that contains a value.</summary>
    public static Optional<T> Of(T value)    => new(value);

    /// <summary>Creates an empty Optional (no value).</summary>
    public static Optional<T> Empty()        => default;

    /// <summary>Creates Optional.Of if value is non-null, else Optional.Empty.</summary>
    public static Optional<T> OfNullable(T? value)
        => value is null ? Empty() : Of(value);

    // ── Properties ────────────────────────────────────────────────────────

    /// <summary>True when this Optional contains a value.</summary>
    public bool HasValue => _hasValue;

    /// <summary>True when this Optional is empty.</summary>
    public bool IsEmpty  => !_hasValue;

    /// <summary>
    /// The contained value. Throws InvalidOperationException if empty.
    /// Always check HasValue first.
    /// </summary>
    public T Value => _hasValue
        ? _value!
        : throw new InvalidOperationException("Optional<T> has no value. Check HasValue before accessing Value.");

    // ── Safe access helpers ───────────────────────────────────────────────

    /// <summary>Returns the value if present, otherwise the supplied fallback.</summary>
    public T ValueOr(T fallback) => _hasValue ? _value! : fallback;

    /// <summary>Returns the value if present, otherwise the result of fallbackFactory().</summary>
    public T ValueOrElse(Func<T> fallbackFactory)
    {
        ArgumentNullException.ThrowIfNull(fallbackFactory);
        return _hasValue ? _value! : fallbackFactory();
    }

    /// <summary>Executes action only if a value is present.</summary>
    public void IfPresent(Action<T> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (_hasValue) action(_value!);
    }

    // ── Implicit conversion: value → Optional<T> ─────────────────────────
    public static implicit operator Optional<T>(T value) => OfNullable(value);

    public override string ToString()
        => _hasValue ? $"Optional({_value})" : "Optional.Empty";
}
