using System.ComponentModel;
using System.Reflection;

namespace VenueBookingSystem.Utilities.Enums;

// ╔══════════════════════════════════════════════════════════════════════════╗
// ║              🔢  EnumHelper  —  UTILITY METHODS FOR ENUMS               ║
// ║  Works with ANY enum type using generics + constraints.                 ║
// ║  Concept: Generics (where TEnum : Enum), Reflection, Extension Methods  ║
// ╚══════════════════════════════════════════════════════════════════════════╝

/// <summary>
/// Static helper class for common enum operations.
/// All methods are generic and work with any enum type.
/// </summary>
public static class EnumHelper
{
    // ── Display Name ──────────────────────────────────────────────────────

    /// <summary>
    /// Returns the [Description] attribute text for an enum value,
    /// or the enum name itself if no attribute is set.
    /// Example: BookingStatus.Confirmed → "Confirmed Booking"
    /// </summary>
    public static string GetDescription<TEnum>(TEnum value) where TEnum : Enum
    {
        var field = typeof(TEnum).GetField(value.ToString());
        if (field is null) return value.ToString();

        var attr = field.GetCustomAttribute<DescriptionAttribute>();
        return attr?.Description ?? value.ToString();
    }

    // ── Parse ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Safely parses a string to an enum value.
    /// Returns true + the parsed value on success; false + default on failure.
    /// Case-insensitive by default.
    /// </summary>
    public static bool TryParse<TEnum>(string input, out TEnum result, bool ignoreCase = true)
        where TEnum : struct, Enum
        => Enum.TryParse(input, ignoreCase, out result);

    /// <summary>
    /// Parses a string to an enum value or returns the fallback if parsing fails.
    /// </summary>
    public static TEnum ParseOrDefault<TEnum>(string input, TEnum fallback, bool ignoreCase = true)
        where TEnum : struct, Enum
        => Enum.TryParse(input, ignoreCase, out TEnum result) ? result : fallback;

    // ── From Int ──────────────────────────────────────────────────────────

    /// <summary>
    /// Converts an integer to an enum value.
    /// Throws ArgumentOutOfRangeException if the integer is not defined in the enum.
    /// </summary>
    public static TEnum FromInt<TEnum>(int value) where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(typeof(TEnum), value))
            throw new ArgumentOutOfRangeException(nameof(value),
                $"Value {value} is not defined in enum {typeof(TEnum).Name}.");
        return (TEnum)(object)value;
    }

    /// <summary>
    /// Safely converts an integer to an enum value, returning fallback if not defined.
    /// </summary>
    public static TEnum FromIntOrDefault<TEnum>(int value, TEnum fallback)
        where TEnum : struct, Enum
        => Enum.IsDefined(typeof(TEnum), value) ? (TEnum)(object)value : fallback;

    // ── List / Range ──────────────────────────────────────────────────────

    /// <summary>Returns all values of the enum as a list.</summary>
    public static List<TEnum> GetValues<TEnum>() where TEnum : struct, Enum
        => new List<TEnum>(Enum.GetValues<TEnum>());

    /// <summary>Returns all enum names as a list of strings.</summary>
    public static List<string> GetNames<TEnum>() where TEnum : Enum
        => new List<string>(Enum.GetNames(typeof(TEnum)));

    /// <summary>
    /// Returns all enum values paired with their display names.
    /// Useful for rendering selection menus in the console UI.
    /// </summary>
    public static Dictionary<TEnum, string> GetDisplayMap<TEnum>() where TEnum : struct, Enum
    {
        var map = new Dictionary<TEnum, string>();
        foreach (var value in Enum.GetValues<TEnum>())
            map[value] = GetDescription(value);
        return map;
    }

    // ── Validation ────────────────────────────────────────────────────────

    /// <summary>Returns true if the integer is a valid value for the enum.</summary>
    public static bool IsDefined<TEnum>(int value) where TEnum : struct, Enum
        => Enum.IsDefined(typeof(TEnum), value);

    /// <summary>Returns true if the string is a valid name for the enum (case-insensitive).</summary>
    public static bool IsDefinedName<TEnum>(string name) where TEnum : struct, Enum
        => Enum.TryParse<TEnum>(name, ignoreCase: true, out _);
}
