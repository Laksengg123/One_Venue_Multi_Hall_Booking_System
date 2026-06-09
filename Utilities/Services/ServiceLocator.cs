namespace VenueBookingSystem.Utilities.Services;

// ╔══════════════════════════════════════════════════════════════════════════╗
// ║            🧩  SERVICE LOCATOR — SIMPLE DI CONTAINER                    ║
// ║  A minimal manual dependency-injection registry.                        ║
// ║  Register singleton/transient services; resolve them anywhere.          ║
// ║  Concept: Dependency Inversion Principle, Generic Constraints           ║
// ╚══════════════════════════════════════════════════════════════════════════╝

/// <summary>
/// Lightweight manual service container (Service Locator pattern).
/// Supports singleton and transient registrations.
/// Usage:
///   ServiceLocator.Register&lt;IHallService&gt;(() => new HallService(db));
///   var svc = ServiceLocator.Resolve&lt;IHallService&gt;();
/// </summary>
public static class ServiceLocator
{
    // Stores: interface type → factory delegate
    private static readonly Dictionary<Type, Func<object>> _factories   = new();
    // Stores singletons: type → single instance
    private static readonly Dictionary<Type, object>       _singletons  = new();

    // ── Register Singleton ────────────────────────────────────────────────

    /// <summary>
    /// Registers a pre-created singleton instance.
    /// The same instance is returned every time Resolve is called.
    /// </summary>
    public static void RegisterSingleton<TService>(TService instance)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(instance);
        _singletons[typeof(TService)] = instance;
        _factories[typeof(TService)] = () => _singletons[typeof(TService)];
    }

    /// <summary>
    /// Registers a singleton factory — created on first resolve, reused after.
    /// </summary>
    public static void RegisterSingleton<TService>(Func<TService> factory)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factories[typeof(TService)] = () =>
        {
            if (!_singletons.ContainsKey(typeof(TService)))
                _singletons[typeof(TService)] = factory();
            return _singletons[typeof(TService)];
        };
    }

    // ── Register Transient ────────────────────────────────────────────────

    /// <summary>
    /// Registers a transient factory — a NEW instance is created on every Resolve.
    /// </summary>
    public static void RegisterTransient<TService>(Func<TService> factory)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factories[typeof(TService)] = () => factory();
    }

    // ── Resolve ───────────────────────────────────────────────────────────

    /// <summary>
    /// Resolves a registered service by its type.
    /// Throws InvalidOperationException if the type was not registered.
    /// </summary>
    public static TService Resolve<TService>() where TService : class
    {
        var type = typeof(TService);
        if (!_factories.TryGetValue(type, out var factory))
            throw new InvalidOperationException(
                $"Service '{type.Name}' is not registered. Call Register first.");

        return (TService)factory();
    }

    /// <summary>
    /// Tries to resolve a service. Returns null if not registered.
    /// </summary>
    public static TService? TryResolve<TService>() where TService : class
    {
        if (!_factories.TryGetValue(typeof(TService), out var factory)) return null;
        return factory() as TService;
    }

    // ── Utilities ─────────────────────────────────────────────────────────

    /// <summary>Returns true if a service of TService type has been registered.</summary>
    public static bool IsRegistered<TService>() => _factories.ContainsKey(typeof(TService));

    /// <summary>Removes all registrations. Useful for testing.</summary>
    public static void Reset()
    {
        _factories.Clear();
        _singletons.Clear();
    }
}
