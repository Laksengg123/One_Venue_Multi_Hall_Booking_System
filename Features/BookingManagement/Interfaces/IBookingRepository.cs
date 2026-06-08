using System.Collections.Generic;
using System.Threading.Tasks;
using VenueBookingSystem.Features.Authentication;

namespace VenueBookingSystem.Features.Bookings.Interfaces
{
    public interface IBookingRepository
    {
        Task<List<Booking>> GetAllBookingsAsync();
        Task<Booking?> GetBookingByIdAsync(int bookingId);
        Task<List<Booking>> GetBookingsByCustomerAsync(int customerId);
        Task<int> CreateBookingAsync(Booking booking);
        Task<bool> UpdateBookingStatusAsync(int bookingId, BookingStatus newStatus, string remarks);
        Task<bool> CancelBookingAsync(int bookingId, string remarks);
    }
}
