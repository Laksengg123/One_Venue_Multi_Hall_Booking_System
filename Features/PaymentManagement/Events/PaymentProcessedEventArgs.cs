using System;

namespace VenueBookingSystem.Features.Payments.Events
{
    /// <summary>
    /// Provides data for the <see cref="PaymentService.PaymentProcessed"/> domain event.
    /// Carries the completed <see cref="Payment"/> record after a successful transaction.
    /// </summary>
    public sealed class PaymentProcessedEventArgs : EventArgs
    {
        /// <summary>Gets the payment that was processed.</summary>
        public Payment Payment { get; }

        public PaymentProcessedEventArgs(Payment payment)
        {
            Payment = payment ?? throw new ArgumentNullException(nameof(payment));
        }
    }
}
