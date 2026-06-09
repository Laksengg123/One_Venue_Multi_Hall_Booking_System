using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace VenueBookingSystem.Features.Bookings;

// ╔══════════════════════════════════════════════════════════════════════╗
// ║               📋 IBOOKING — THE BOOKING SERVICE CONTRACT             ║
// ║  This is a written contract (interface).                             ║
// ║  Any class that calls itself a "Booking Service" MUST be able to     ║
// ║  do everything listed here. If even one is missing → code won't run. ║
// ║  Think: like a job description. You MUST have these skills.          ║
// ╚══════════════════════════════════════════════════════════════════════╝
public interface IBooking
{
    // ➕ Skill #1: CREATE a new booking and return its ID number
    //    Takes all booking details, saves to DB, returns the new Booking ID
    Task<int> CreateBookingAsync(Booking booking);

    // 📄 Skill #2: GET all bookings for a specific customer
    //    Returns them page by page (e.g. 10 at a time) to avoid loading too many at once
    Task<List<Booking>> GetBookingsByCustomerAsync(int customerId, int page, int pageSize);

    // ❌ Skill #3: CANCEL a booking by its ID
    //    Returns true if successfully cancelled, throws error if something goes wrong
    Task<bool> CancelBookingAsync(int bookingId);

    // 🔍 Skill #4: CHECK if a hall is free between two date-times
    //    Returns true = Hall is available | false = Hall is already booked
    Task<bool> CheckAvailabilityAsync(int hallId, DateTime start, DateTime end);

    // 🔢 Skill #5: COUNT how many bookings a customer has made in total
    //    Useful for loyalty tracking and reports
    Task<int> GetCustomerBookingCountAsync(int customerId);
}
