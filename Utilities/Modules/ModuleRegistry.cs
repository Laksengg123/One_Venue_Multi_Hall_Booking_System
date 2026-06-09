namespace VenueBookingSystem.Utilities.Modules;

// ╔══════════════════════════════════════════════════════════════════════════╗
// ║            📦  MODULE SYSTEM — Feature module bootstrapping             ║
// ║  Each feature (Hall, Booking, Payment…) is a self-contained Module.    ║
// ║  Modules register their own services and are loaded at startup.         ║
// ║  Concept: Module Pattern, Interface segregation, Startup bootstrapping  ║
// ╚══════════════════════════════════════════════════════════════════════════╝

// ── Module interface ──────────────────────────────────────────────────────

/// <summary>
/// Contract that every feature module must implement.
/// A module is a self-contained unit that knows how to register itself.
/// </summary>
public interface IModule
{
    /// <summary>Human-readable name of this module (e.g. "HallManagement").</summary>
    string Name { get; }

    /// <summary>Module version — useful for logging at startup.</summary>
    string Version { get; }

    /// <summary>
    /// Called at application startup.
    /// The module uses this to register its services, event handlers, etc.
    /// </summary>
    void Register();
}

// ── Module registry ───────────────────────────────────────────────────────

/// <summary>
/// Holds all registered feature modules and boots them at startup.
/// Usage:
///   ModuleRegistry.Add(new HallModule(db));
///   ModuleRegistry.Add(new BookingModule(db));
///   ModuleRegistry.BootAll();
/// </summary>
public static class ModuleRegistry
{
    private static readonly List<IModule> _modules = new();

    // ── Registration ──────────────────────────────────────────────────────

    /// <summary>Adds a module to the registry.</summary>
    public static void Add(IModule module)
    {
        ArgumentNullException.ThrowIfNull(module);
        _modules.Add(module);
    }

    /// <summary>Adds multiple modules at once.</summary>
    public static void AddRange(IEnumerable<IModule> modules)
    {
        foreach (var m in modules) Add(m);
    }

    // ── Boot ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Calls Register() on every module in registration order.
    /// Exceptions from one module are caught so others still boot.
    /// </summary>
    public static void BootAll()
    {
        foreach (var module in _modules)
        {
            try
            {
                module.Register();
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine($"  ✔  [{module.Name} v{module.Version}] loaded.");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine($"  ✘  [{module.Name}] failed to load: {ex.Message}");
                Console.ResetColor();
            }
        }
    }

    // ── Query ─────────────────────────────────────────────────────────────

    /// <summary>Returns all registered modules (read-only).</summary>
    public static IReadOnlyList<IModule> All => _modules.AsReadOnly();

    /// <summary>Returns the module with the given name, or null if not found.</summary>
    public static IModule? Find(string name)
    {
        foreach (var m in _modules)
            if (string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase))
                return m;
        return null;
    }

    /// <summary>Clears all modules (useful for testing).</summary>
    public static void Reset() => _modules.Clear();
}

// ── Example feature module stubs ─────────────────────────────────────────
// Each feature folder can define its own concrete IModule subclass.
// These stubs show the pattern — extend them in the actual feature code.

/// <summary>Bootstrap module for Hall Management feature.</summary>
public sealed class HallModule : IModule
{
    public string Name    => "HallManagement";
    public string Version => "1.0";

    public void Register()
    {
        // Example: ServiceLocator.RegisterSingleton<IHallService>(() => new HallService(...));
        // Wire up event handlers, validators, etc.
    }
}

/// <summary>Bootstrap module for Booking Management feature.</summary>
public sealed class BookingModule : IModule
{
    public string Name    => "BookingManagement";
    public string Version => "1.0";

    public void Register()
    {
        // Example: ServiceLocator.RegisterSingleton<IBookingService>(() => new BookingService(...));
    }
}

/// <summary>Bootstrap module for Payment Management feature.</summary>
public sealed class PaymentModule : IModule
{
    public string Name    => "PaymentManagement";
    public string Version => "1.0";

    public void Register()
    {
        // Example: ServiceLocator.RegisterSingleton<IPaymentService>(() => new PaymentService(...));
    }
}
