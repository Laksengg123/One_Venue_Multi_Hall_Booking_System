using System.Collections.Generic;
using System.Threading.Tasks;
using VenueBookingSystem.Features.Authentication;
using VenueBookingSystem.Features.Bookings;

namespace VenueBookingSystem.Features.Payments.Interfaces
{
    public interface IPaymentService
    {
        Task<bool> ProcessPaymentAsync(Booking booking, PaymentMethod method);
        Task<bool> ProcessPaymentAsync(int bookingId, PaymentMethod method);
        Task<Payment?> GetPaymentByBookingAsync(int bookingId);
        Task<List<Payment>> GetPaymentsByCustomerAsync(int customerId);
        Task<List<Payment>> GetAllPaymentsAsync(int page, int pageSize);
        Task<List<RefundCandidate>> GetPendingRefundsAsync();
        Task<RefundResult?> ProcessRefundAsync(int cancellationId, decimal refundAmount, string processedBy, string remarks);
        Task<Invoice?> GenerateInvoiceAsync(int bookingId);
    }
}
