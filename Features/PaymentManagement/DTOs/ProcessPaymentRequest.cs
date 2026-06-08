using VenueBookingSystem.Features.Authentication;

namespace VenueBookingSystem.Features.Payments.DTOs
{
    public record ProcessPaymentRequest(int BookingId, PaymentMethod Method);
}
