using System.Collections.Generic;
using System.Threading.Tasks;
using VenueBookingSystem.Features.Authentication;

namespace VenueBookingSystem.Features.Payments.Interfaces
{
    public interface IPaymentRepository
    {
        Task<bool> CreatePaymentAsync(Payment payment);
        Task<Payment?> GetPaymentByBookingAsync(int bookingId);
        Task<List<Payment>> GetPaymentsByCustomerAsync(int customerId);
        Task<List<Payment>> GetAllPaymentsAsync(int page, int pageSize);
    }
}
