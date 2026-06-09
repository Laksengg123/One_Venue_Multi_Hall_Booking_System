namespace VenueBookingSystem.Utilities.DTOs;

// ╔══════════════════════════════════════════════════════════════════════════╗
// ║           📄  PagedResult<T>  —  PAGINATION RESPONSE WRAPPER            ║
// ║  Used whenever a list of items is returned from a service/query.        ║
// ║  Carries: current page, page size, total count, and the data slice.     ║
// ╚══════════════════════════════════════════════════════════════════════════╝

/// <summary>
/// Wraps a paginated list of items with full pagination metadata.
/// </summary>
/// <typeparam name="T">The item type in the page (e.g. Hall, Booking …)</typeparam>
public sealed class PagedResult<T>
{
    /// <summary>The current page slice (may be empty but never null).</summary>
    public List<T>  Items       { get; init; } = new();

    /// <summary>Total number of records across ALL pages.</summary>
    public int      TotalCount  { get; init; }

    /// <summary>Current page number (1-based).</summary>
    public int      Page        { get; init; }

    /// <summary>Number of items per page.</summary>
    public int      PageSize    { get; init; }

    // ── Derived ──────────────────────────────────────────────────────────

    /// <summary>Total number of pages.</summary>
    public int  TotalPages  => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;

    /// <summary>True when there is a next page after this one.</summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>True when there is a previous page before this one.</summary>
    public bool HasPrevPage => Page > 1;

    /// <summary>True when this page contains at least one item.</summary>
    public bool HasItems    => Items.Count > 0;

    // ── Factory ──────────────────────────────────────────────────────────

    /// <summary>Creates a PagedResult from a full in-memory list, slicing it automatically.</summary>
    public static PagedResult<T> From(IEnumerable<T> source, int page, int pageSize)
    {
        var list  = new System.Collections.Generic.List<T>(source);
        var total = list.Count;

        int skip = (page - 1) * pageSize;
        if (skip < 0) skip = 0;

        var slice = new System.Collections.Generic.List<T>();
        if (skip < total)
        {
            int count = Math.Min(pageSize, total - skip);
            slice = list.GetRange(skip, count);
        }

        return new PagedResult<T> { Items = slice, TotalCount = total, Page = page, PageSize = pageSize };
    }

    /// <summary>Creates an empty PagedResult (no records).</summary>
    public static PagedResult<T> Empty(int page = 1, int pageSize = 7)
        => new() { Items = new(), TotalCount = 0, Page = page, PageSize = pageSize };

    public override string ToString()
        => $"Page {Page}/{TotalPages}  ({Items.Count} of {TotalCount} items)";
}
