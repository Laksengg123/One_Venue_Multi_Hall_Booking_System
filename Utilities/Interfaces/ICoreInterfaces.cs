using VenueBookingSystem.Utilities.DTOs;

namespace VenueBookingSystem.Utilities.Interfaces;

// ╔══════════════════════════════════════════════════════════════════════════╗
// ║             🔌  CORE SERVICE & REPOSITORY INTERFACES                    ║
// ║  These contracts decouple callers from concrete implementations.        ║
// ║  Any class implementing these can be swapped without touching callers.  ║
// ╚══════════════════════════════════════════════════════════════════════════╝

// ── Generic CRUD Repository ────────────────────────────────────────────────

/// <summary>
/// Standard CRUD contract for any data repository.
/// Concept: Interface, Generics, Dependency Inversion Principle.
/// </summary>
/// <typeparam name="T">Entity type stored in the repository.</typeparam>
/// <typeparam name="TKey">Primary key type (usually int).</typeparam>
public interface IRepository<T, TKey>
{
    /// <summary>Returns all entities.</summary>
    Task<List<T>> GetAllAsync();

    /// <summary>Returns the entity with the given key, or null if not found.</summary>
    Task<T?> GetByIdAsync(TKey id);

    /// <summary>Persists a new entity. Returns the generated key.</summary>
    Task<TKey> CreateAsync(T entity);

    /// <summary>Updates an existing entity. Returns true if found and updated.</summary>
    Task<bool> UpdateAsync(T entity);

    /// <summary>Deletes an entity by key. Returns true if found and deleted.</summary>
    Task<bool> DeleteAsync(TKey id);
}

// ── Auditable Repository ───────────────────────────────────────────────────

/// <summary>
/// Extended repository interface for entities that track created/updated timestamps.
/// </summary>
public interface IAuditableRepository<T> : IRepository<T, int>
{
    /// <summary>Returns entities modified after the given date.</summary>
    Task<List<T>> GetModifiedAfterAsync(DateTime since);
}

// ── Generic Service ────────────────────────────────────────────────────────

/// <summary>
/// Base service contract wrapping a repository with business logic.
/// Returns ApiResponse so the caller always knows success/failure without try-catch.
/// </summary>
/// <typeparam name="T">Domain model type (e.g. Hall, Booking).</typeparam>
/// <typeparam name="TCreateDto">DTO used to create a new entity.</typeparam>
/// <typeparam name="TUpdateDto">DTO used to update an existing entity.</typeparam>
public interface IService<T, TCreateDto, TUpdateDto>
{
    Task<ApiResponse<List<T>>>  GetAllAsync();
    Task<ApiResponse<T>>        GetByIdAsync(int id);
    Task<ApiResponse<int>>      CreateAsync(TCreateDto dto);
    Task<ApiResponse<bool>>     UpdateAsync(int id, TUpdateDto dto);
    Task<ApiResponse<bool>>     DeleteAsync(int id);
}

// ── Searchable ────────────────────────────────────────────────────────────

/// <summary>Marks a service as supporting keyword search.</summary>
public interface ISearchable<T>
{
    Task<List<T>> SearchAsync(string keyword);
}

// ── Pageable ──────────────────────────────────────────────────────────────

/// <summary>Marks a service as supporting server-side pagination.</summary>
public interface IPageable<T>
{
    Task<PagedResult<T>> GetPageAsync(int page, int pageSize);
}

// ── Exportable ────────────────────────────────────────────────────────────

/// <summary>
/// Contract for any service that can export data to a file format.
/// </summary>
public interface IExportable
{
    /// <summary>Exports to the given file path in the specified format.</summary>
    Task<bool> ExportAsync(string filePath, ExportFormat format);
}

/// <summary>Supported export file formats.</summary>
public enum ExportFormat { Txt, Csv, Xlsx, Docx }

// ── Auditable (entities) ──────────────────────────────────────────────────

/// <summary>
/// Marks an entity as having automatic created/updated timestamps.
/// </summary>
public interface IAuditable
{
    DateTime CreatedAt { get; }
    DateTime UpdatedAt { get; }
}

// ── Soft-Deletable ────────────────────────────────────────────────────────

/// <summary>
/// Marks an entity as supporting soft-delete (IsDeleted flag instead of
/// physically removing the record from the database).
/// </summary>
public interface ISoftDeletable
{
    bool      IsDeleted  { get; }
    DateTime? DeletedAt  { get; }
}
