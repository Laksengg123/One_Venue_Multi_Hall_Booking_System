using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace VenueBookingSystem.Features.Bookings;

public interface IBooking
{
    
    Task<int> CreateBookingAsync(Booking booking);

    Task<List<Booking>> GetBookingsByCustomerAsync(int customerId, int page, int pageSize);

    Task<bool> CancelBookingAsync(int bookingId);


    Task<bool> CheckAvailabilityAsync(int hallId, DateTime start, DateTime end);


    Task<int> GetCustomerBookingCountAsync(int customerId);
}
