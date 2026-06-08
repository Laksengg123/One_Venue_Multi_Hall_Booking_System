namespace VenueBookingSystem.Shared.Models
{
    /// <summary>
    /// Marker interface for all domain entities with an integer primary key.
    /// </summary>
    public interface IEntity
    {
        int Id { get; }
    }
}
