namespace VenueBookingSystem.Utilities.Exceptions;

// ╔══════════════════════════════════════════════════════════════════════════╗
// ║               ⚠️  APP EXCEPTION HIERARCHY                               ║
// ║  All custom exceptions in VenueBookingSystem inherit from               ║
// ║  AppException → gives a consistent base for catch clauses.             ║
// ╚══════════════════════════════════════════════════════════════════════════╝

// ── Base ──────────────────────────────────────────────────────────────────

/// <summary>
/// Root exception for all VenueBookingSystem application errors.
/// Catching AppException catches any domain error in one clause.
/// </summary>
public class AppException : Exception
{
    /// <summary>Machine-readable error code (e.g. "HALL_NOT_FOUND").</summary>
    public string ErrorCode { get; }

    public AppException(string message, string errorCode = "APP_ERROR")
        : base(message) => ErrorCode = errorCode;

    public AppException(string message, Exception inner, string errorCode = "APP_ERROR")
        : base(message, inner) => ErrorCode = errorCode;
}

// ── Validation ────────────────────────────────────────────────────────────

/// <summary>
/// Thrown when user-supplied input fails validation rules.
/// Example: guest count exceeds hall capacity.
/// </summary>
public class ValidationException : AppException
{
    /// <summary>The name of the field that failed validation.</summary>
    public string Field { get; }

    public ValidationException(string field, string message)
        : base(message, $"VALIDATION_{field.ToUpperInvariant()}_ERROR")
        => Field = field;
}

// ── Not Found ─────────────────────────────────────────────────────────────

/// <summary>
/// Thrown when a requested entity does not exist in the database.
/// Example: Booking #99 does not exist.
/// </summary>
public class NotFoundException : AppException
{
    /// <summary>The type of entity not found (e.g. "Booking", "Hall").</summary>
    public string EntityType { get; }

    /// <summary>The identifier that was searched for.</summary>
    public object EntityId { get; }

    public NotFoundException(string entityType, object id)
        : base($"{entityType} with ID '{id}' was not found.", "NOT_FOUND")
    {
        EntityType = entityType;
        EntityId   = id;
    }
}

// ── Conflict ──────────────────────────────────────────────────────────────

/// <summary>
/// Thrown when an operation conflicts with existing data.
/// Example: hall is already booked for the requested time slot.
/// </summary>
public class ConflictException : AppException
{
    public ConflictException(string message)
        : base(message, "CONFLICT") { }
}

// ── Authorisation ─────────────────────────────────────────────────────────

/// <summary>
/// Thrown when an action is attempted by a user without permission.
/// Example: customer tries to access admin dashboard.
/// </summary>
public class UnauthorisedException : AppException
{
    public UnauthorisedException(string message = "You do not have permission to perform this action.")
        : base(message, "UNAUTHORISED") { }
}

// ── Business Rule ─────────────────────────────────────────────────────────

/// <summary>
/// Thrown when a domain business rule is violated.
/// Example: cancelling a booking less than 1 hour before the event.
/// </summary>
public class BusinessRuleException : AppException
{
    /// <summary>Short rule reference code (e.g. "BR-019").</summary>
    public string RuleCode { get; }

    public BusinessRuleException(string ruleCode, string message)
        : base(message, $"BUSINESS_RULE_{ruleCode}")
        => RuleCode = ruleCode;
}

// ── Database ──────────────────────────────────────────────────────────────

/// <summary>
/// Thrown when a database operation fails (wraps SqlException).
/// </summary>
public class DatabaseException : AppException
{
    public DatabaseException(string message, Exception? inner = null)
        : base(message, inner!, "DB_ERROR") { }
}

// ── Configuration ─────────────────────────────────────────────────────────

/// <summary>
/// Thrown when the application configuration (appsettings.json) is missing
/// a required value.
/// </summary>
public class ConfigurationException : AppException
{
    public string Key { get; }

    public ConfigurationException(string key)
        : base($"Required configuration key '{key}' is missing or empty.", "CONFIG_ERROR")
        => Key = key;
}
