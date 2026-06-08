using System;

namespace VenueBookingSystem.Features.Bookings.Events
{
    /// <summary>
    /// Provides data for the <see cref="BookingService.BookingConfirmed"/> domain event.
    /// Raised when a booking transitions to <see cref="BookingStatus.Confirmed"/>.
    /// </summary>
    public sealed class BookingConfirmedEventArgs : EventArgs
    {
        /// <summary>Gets the booking that was confirmed.</summary>
        public Booking Booking { get; }

        public BookingConfirmedEventArgs(Booking booking)
        {
            Booking = booking ?? throw new ArgumentNullException(nameof(booking));
        }
    }
}
