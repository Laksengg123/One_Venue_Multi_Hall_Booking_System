using System;

namespace VenueBookingSystem.Features.Bookings.DTOs
{
    // ╔══════════════════════════════════════════════════════════════════════╗
    // ║         📋 CREATE BOOKING REQUEST — THE BOOKING FORM (INPUT ONLY)   ║
    // ║  This is a simple DATA TRANSFER OBJECT (DTO).                       ║
    // ║  It carries the customer's booking request from the UI to the        ║
    // ║  Service layer — like handing over a filled form at the counter.    ║
    // ║                                                                      ║
    // ║  Note: NO logic here — just data fields. Pure information carrier.  ║
    // ╚══════════════════════════════════════════════════════════════════════╝
    public record CreateBookingRequest(
        // 👤 Who is making this booking? (customer's account ID + their name)
        int CustomerId,
        string CustomerName,

        // 🏛️ Which hall do they want? (hall's ID number + hall's display name)
        int HallId,
        string HallName,

        // 📅 When does the event start and end? (exact date + time)
        DateTime StartDateTime,
        DateTime EndDateTime,

        // ⏱️ How many hours total? And what is the total price?
        decimal TotalHours,
        decimal TotalAmount,

        // 📝 What is this hall being booked for? (company event, wedding, training...)
        string Purpose,

        // 👥 How many guests are expected to attend?
        int GuestCount
    );
}
