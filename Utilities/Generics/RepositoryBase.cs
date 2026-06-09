namespace VenueBookingSystem.Utilities.Generics;

// ╔══════════════════════════════════════════════════════════════════════════╗
// ║           🔄  Repository<T>  —  GENERIC REPOSITORY BASE CLASS           ║
// ║  Provides a common in-memory store with CRUD helpers.                   ║
// ║  Concrete repositories extend this and add DB-specific logic.           ║
// ╚══════════════════════════════════════════════════════════════════════════╝

/// <summary>
/// A generic in-memory repository base class for any entity type T.
/// Useful for unit tests, prototyping, or lightweight caching.
/// Concept: Generics, Constraints, CRUD pattern.
/// </summary>
/// <typeparam name="T">Entity type — must have an integer Id.</typeparam>
public abstract class RepositoryBase<T> where T : class
{
    // ── In-memory store ───────────────────────────────────────────────────
    protected readonly List<T> _store = new();

    // ── Abstract: subclass must tell us the Id of any item ───────────────
    protected abstract int GetId(T item);

    // ── CREATE ────────────────────────────────────────────────────────────

    /// <summary>Adds an item to the store.</summary>
    public virtual void Add(T item)
    {
        ArgumentNullException.ThrowIfNull(item);
        _store.Add(item);
    }

    // ── READ ──────────────────────────────────────────────────────────────

    /// <summary>Returns ALL items in the store.</summary>
    public virtual IReadOnlyList<T> GetAll() => _store.AsReadOnly();

    /// <summary>Finds the first item matching the predicate, or null.</summary>
    public virtual T? Find(Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        foreach (var item in _store)
            if (predicate(item)) return item;
        return null;
    }

    /// <summary>Returns all items matching the predicate.</summary>
    public virtual List<T> FindAll(Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        var result = new List<T>();
        foreach (var item in _store)
            if (predicate(item)) result.Add(item);
        return result;
    }

    /// <summary>Finds an item by its integer Id.</summary>
    public virtual T? GetById(int id) => Find(x => GetId(x) == id);

    // ── UPDATE ────────────────────────────────────────────────────────────

    /// <summary>
    /// Replaces the first item with a matching Id with the supplied replacement.
    /// Returns true if found and replaced; false if no match.
    /// </summary>
    public virtual bool Update(T updated)
    {
        ArgumentNullException.ThrowIfNull(updated);
        for (int i = 0; i < _store.Count; i++)
        {
            if (GetId(_store[i]) == GetId(updated))
            {
                _store[i] = updated;
                return true;
            }
        }
        return false;
    }

    // ── DELETE ────────────────────────────────────────────────────────────

    /// <summary>Removes the item with the given Id. Returns true if removed.</summary>
    public virtual bool Remove(int id)
    {
        var item = GetById(id);
        return item is not null && _store.Remove(item);
    }

    // ── COUNT / EXISTS ────────────────────────────────────────────────────

    public int  Count               => _store.Count;
    public bool Exists(int id)      => GetById(id) is not null;
    public bool Any(Func<T, bool> p) { foreach (var i in _store) if (p(i)) return true; return false; }

    // ── CLEAR (useful in tests) ────────────────────────────────────────────
    public void Clear() => _store.Clear();
}
