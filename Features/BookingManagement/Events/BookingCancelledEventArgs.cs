using System;

namespace VenueBookingSystem.Features.Bookings.Events
{
    /// <summary>
    /// Provides data for the <see cref="BookingService.BookingCancelled"/> domain event.
    /// Raised when a booking transitions to <see cref="BookingStatus.Cancelled"/>.
    /// </summary>
    public sealed class BookingCancelledEventArgs : EventArgs
    {
        /// <summary>Gets the booking that was cancelled.</summary>
        public Booking Booking { get; }

        public BookingCancelledEventArgs(Booking booking)
        {
            Booking = booking ?? throw new ArgumentNullException(nameof(booking));
        }
    }
}
