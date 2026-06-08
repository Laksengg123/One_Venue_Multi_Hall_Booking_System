using VenueBookingSystem.Features.Authentication;

namespace VenueBookingSystem.Features.Payments;

// Enums PaymentMethod and PaymentStatus are imported from VenueBookingSystem.Features.Authentication (defined in User.cs) to match the database values.

// Concept: Record (Value Semantics / Immutable Data)
public record Payment(
    int PaymentId,
    int BookingId,
    decimal Amount,
    PaymentMethod Method,
    PaymentStatus Status,
    string TransactionRef,
    DateTime? PaidAt,
    DateTime CreatedAt);

// Concept: Extension Methods — behaviour added to Payment without modifying the record
public static class PaymentExtensions
{
    public static string GetMethodDisplay(this Payment payment) => payment.Method switch
    {
        PaymentMethod.Cash => "Cash",
        PaymentMethod.Card => "Card",
        PaymentMethod.UPI => "UPI",
        PaymentMethod.BankTransfer => "Bank Transfer",
        _ => payment.Method.ToString()
    };

    public static string GetStatusDisplay(this Payment payment) => payment.Status switch
    {
        PaymentStatus.Pending => "Pending",
        PaymentStatus.Completed => "Completed",
        PaymentStatus.Failed => "Failed",
        PaymentStatus.Refunded => "Refunded",
        _ => payment.Status.ToString()
    };
}
