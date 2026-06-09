namespace VenueBookingSystem.Utilities.DTOs;

// ╔══════════════════════════════════════════════════════════════════════════╗
// ║                 GENERIC RESULT WRAPPER                                 ║
// ║  Every service operation returns this so callers always know:           ║
// ║    • Whether it succeeded  (IsSuccess)                                  ║
// ║    • What data came back   (Data)                                       ║
// ║    • What went wrong       (Message / Errors)                           ║
// ╚══════════════════════════════════════════════════════════════════════════╝

/// <summary>
/// Generic response wrapper that wraps any service/repository result.
/// Eliminates the need to throw exceptions for expected business failures.
/// </summary>
/// <typeparam name="T">The payload type (e.g. Booking, Hall, int …)</typeparam>
public class ApiResponse<T>
{
    // ── Core fields ────────────────────────────────────────────────────────
    public bool         IsSuccess  { get; protected set; }
    public T?           Data       { get; protected set; }
    public string       Message    { get; protected set; } = string.Empty;
    public List<string> Errors     { get; protected set; } = new();

    // ── Protected constructor — use factory methods ───────────────────────
    protected ApiResponse() { }

    // ── Factory: SUCCESS ─────────────────────────────────────────────────

    /// <summary>Creates a successful response with a data payload.</summary>
    public static ApiResponse<T> Success(T data, string message = "Operation completed successfully.")
        => new() { IsSuccess = true, Data = data, Message = message };

    /// <summary>Creates a successful response without a payload (void operations).</summary>
    public static ApiResponse<T> Ok(string message = "Operation completed successfully.")
        => new() { IsSuccess = true, Message = message };

    // ── Factory: FAILURE ─────────────────────────────────────────────────

    /// <summary>Creates a failed response with a single error message.</summary>
    public static ApiResponse<T> Fail(string error)
        => new() { IsSuccess = false, Message = error, Errors = new List<string> { error } };

    /// <summary>Creates a failed response with multiple validation errors.</summary>
    public static ApiResponse<T> Fail(IEnumerable<string> errors)
    {
        var list = new List<string>(errors);
        return new() { IsSuccess = false, Message = "One or more errors occurred.", Errors = list };
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    /// <summary>True when the response has a non-null data payload.</summary>
    public bool HasData   => Data is not null;

    /// <summary>True when there are validation errors attached.</summary>
    public bool HasErrors => Errors.Count > 0;

    public override string ToString()
        => IsSuccess
            ? $"[OK] {Message}"
            : $"[FAIL] {Message} | Errors: {string.Join("; ", Errors)}";
}

// ── Non-generic static helper for void operations ─────────────────────────

/// <summary>
/// Static convenience helper for service operations that return no data.
/// Usage: ApiResponse.Ok("Done") or ApiResponse.Fail("Bad input")
/// </summary>
public static class ApiResponse
{
    /// <summary>Returns a successful void response.</summary>
    public static ApiResponse<object> Ok(string message = "Done.")
        => ApiResponse<object>.Ok(message);

    /// <summary>Returns a failed void response with an error message.</summary>
    public static ApiResponse<object> Fail(string error)
        => ApiResponse<object>.Fail(error);

    /// <summary>Returns a failed void response with multiple errors.</summary>
    public static ApiResponse<object> Fail(IEnumerable<string> errors)
        => ApiResponse<object>.Fail(errors);
}
