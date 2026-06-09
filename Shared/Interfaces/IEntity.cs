namespace VenueBookingSystem.Shared.Models
{
    // ╔══════════════════════════════════════════════════════════════════════╗
    // ║                🪪 IENTITY — THE "MUST HAVE AN ID" RULE              ║
    // ║  This is the simplest contract in the system.                       ║
    // ║  Rule: ANYTHING stored in the database MUST have an integer ID.     ║
    // ║  Think: every record in a filing cabinet must have a file number.   ║
    // ╚══════════════════════════════════════════════════════════════════════╝

    /// <summary>
    /// Marker interface for all domain entities with an integer primary key.
    /// </summary>
    public interface IEntity
    {
        // 🔢 Every entity (Hall, Booking, User, Payment...) must expose an Id
        //    This is the unique identifier — like a unique reference number
        //    'get' only → can be read but never set from outside
        int Id { get; }
    }
}
