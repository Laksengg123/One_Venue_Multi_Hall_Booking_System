namespace VenueBookingSystem.Features.Payments;

public record Invoice(
    int InvoiceId,
    int BookingId,
    string InvoiceNumber,
    decimal TotalAmount,
    DateTime GeneratedDate,
    string IssuedTo,
    string Notes
);
