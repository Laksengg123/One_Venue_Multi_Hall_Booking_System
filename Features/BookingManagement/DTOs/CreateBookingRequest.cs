using System;

namespace VenueBookingSystem.Features.Bookings.DTOs
{
    public record CreateBookingRequest(int CustomerId, string CustomerName, int HallId, string HallName, DateTime StartDateTime, DateTime EndDateTime, decimal TotalHours, decimal TotalAmount, string Purpose, int GuestCount);
}
