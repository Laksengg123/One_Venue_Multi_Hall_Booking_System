using System.Collections.Generic;
using System.Threading.Tasks;
using VenueBookingSystem.Features.Authentication;

namespace VenueBookingSystem.Features.Bookings.Interfaces
{
    // ╔══════════════════════════════════════════════════════════════════════╗
    // ║           🗄️ IBOOKING REPOSITORY — THE DATABASE WORKER CONTRACT      ║
    // ║  This is the contract for whoever directly talks to the DATABASE.    ║
    // ║  Different from IBooking — this is about raw data (no business rules)║
    // ║  Think: IBooking = what the booking desk can do                      ║
    // ║         IBookingRepository = what the filing cabinet worker can do   ║
    // ╚══════════════════════════════════════════════════════════════════════╝
    public interface IBookingRepository
    {
        // 📦 Get ALL bookings ever stored in the database — return as a list
        Task<List<Booking>> GetAllBookingsAsync();

        // 🔍 Find ONE specific booking by its ID number
        //    The '?' after Booking means: might return nothing (null) if not found
        Task<Booking?> GetBookingByIdAsync(int bookingId);

        // 👤 Get all bookings made by ONE specific customer using their ID
        Task<List<Booking>> GetBookingsByCustomerAsync(int customerId);

        // ➕ Save a brand new booking to the database — return the new Booking ID
        Task<int> CreateBookingAsync(Booking booking);

        // 🔄 Change the status of an existing booking (e.g. Pending → Confirmed)
        //    Also save a note (remarks) explaining WHY the status was changed
        Task<bool> UpdateBookingStatusAsync(int bookingId, BookingStatus newStatus, string remarks);

        // ❌ Cancel a booking and record the reason why it was cancelled
        Task<bool> CancelBookingAsync(int bookingId, string remarks);
    }
}
