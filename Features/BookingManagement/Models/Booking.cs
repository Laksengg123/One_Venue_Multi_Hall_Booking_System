using System;
using VenueBookingSystem.Features.Authentication;

namespace VenueBookingSystem.Features.Bookings;


public record Booking(
    int BookingId,
    int CustomerId,
    string CustomerName,
    int HallId,
    string HallName,
    DateTime StartDateTime,
    DateTime EndDateTime,
    decimal TotalHours,
    decimal TotalAmount,
    BookingStatus Status,
    string Purpose,
    int GuestCount,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public static class BookingExtensions
{
    public static string GetStatusDisplay(this Booking b) => b.Status switch
    {
        BookingStatus.Pending   => "Pending",
        BookingStatus.Confirmed => "Confirmed",
        BookingStatus.Cancelled => "Cancelled",
        BookingStatus.Completed => "Completed",
        BookingStatus.Rejected  => "Rejected",
        _                       => "Unknown"
    };

    public static bool CanBeCancelled(this Booking b) =>
        b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed;

    public static bool IsPaid(this Booking b) =>
        b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Completed;

 
    public static string GetDurationDisplay(this Booking b)
    {
        var duration = b.EndDateTime - b.StartDateTime;
        return $"{(int)duration.TotalHours} hrs {duration.Minutes} mins";
    }
}
