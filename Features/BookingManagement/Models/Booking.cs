using System;
using VenueBookingSystem.Features.Authentication;

namespace VenueBookingSystem.Features.Bookings;

// ╔══════════════════════════════════════════════════════════════════════╗
// ║                     📋 BOOKING — THE RECEIPT                        ║
// ║  This is a digital booking slip — like a printed hotel receipt.     ║
// ║  Once created, it CANNOT be changed (record = immutable/read-only). ║
// ╚══════════════════════════════════════════════════════════════════════╝

public record Booking(
    // 🎫 Every booking gets a unique token number — like a serial number on a receipt
    int BookingId,

    // 👤 Who made this booking? Store their account ID and their full name
    int CustomerId,
    string CustomerName,

    // 🏛️ Which hall was booked? Store the hall's ID number and its display name
    int HallId,
    string HallName,

    // 📅 When does the booking start and end? We store EXACT date + time (not just date)
    DateTime StartDateTime,
    DateTime EndDateTime,

    // ⏱️ How many hours is this booking? Use decimal because it can be 2.5 hours
    decimal TotalHours,

    // 💰 How much does the customer owe? Use decimal for rupees (e.g. ₹40,000.50)
    decimal TotalAmount,

    // 🚦 Current state of this booking: Pending / Confirmed / Cancelled / Completed / Rejected
    BookingStatus Status,

    // 📝 Why is this hall being booked? (Annual dinner, wedding, conference, training...)
    string Purpose,

    // 👥 How many guests are expected to attend?
    int GuestCount,

    // 🕐 When was this booking first created in the system?
    DateTime CreatedAt,

    // 🔄 When was this booking last updated/modified?
    DateTime UpdatedAt
);

// ════════════════════════════════════════════════════════════════════════
// 🛠️ BOOKING EXTENSIONS — Extra helper methods added to the Booking type
//    These are like sticky notes attached to the booking slip
// ════════════════════════════════════════════════════════════════════════
public static class BookingExtensions
{
    // 🏷️ Converts the status number into a readable English word
    //    e.g. Status = 1 → shows "Confirmed" instead of just "1"
    public static string GetStatusDisplay(this Booking b) => b.Status switch
    {
        BookingStatus.Pending   => "Pending",    // Waiting for admin approval
        BookingStatus.Confirmed => "Confirmed",  // Admin approved it
        BookingStatus.Cancelled => "Cancelled",  // Customer or admin cancelled it
        BookingStatus.Completed => "Completed",  // The event happened and it's done
        BookingStatus.Rejected  => "Rejected",   // Admin rejected the request
        _                       => "Unknown"     // Safety fallback — should never happen
    };

    // ❌ Can this booking still be cancelled?
    //    YES → if it is still Pending or Confirmed (event hasn't happened yet)
    //    NO  → if it's already Cancelled, Completed, or Rejected (too late to cancel)
    public static bool CanBeCancelled(this Booking b) =>
        b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed;

    // ✅ Has this booking been paid for?
    //    Confirmed = paid and approved | Completed = paid and event is done
    //    Pending/Cancelled/Rejected = NOT paid
    public static bool IsPaid(this Booking b) =>
        b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Completed;

    // ⏳ Show the duration in human-friendly format: "5 hrs 30 mins"
    //    Calculate: EndTime minus StartTime = duration
    public static string GetDurationDisplay(this Booking b)
    {
        var duration = b.EndDateTime - b.StartDateTime; // Subtract end - start to get total time
        return $"{(int)duration.TotalHours} hrs {duration.Minutes} mins"; // Format nicely
    }
}
