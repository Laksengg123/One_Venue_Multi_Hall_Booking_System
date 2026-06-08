namespace VenueBookingSystem.Features.Payments.Exceptions;

// Concept: Custom Exception — specific to payment processing errors
public class PaymentException : Exception
{
    public PaymentException() : base() { }

    public PaymentException(string message) : base(message) { }

    public PaymentException(string message, Exception innerException)
        : base(message, innerException) { }
}
