using System;

namespace VenueBookingSystem.Features.Bookings.Events
{
    /// <summary>
    /// Provides data for the <see cref="BookingService.BookingCreated"/> domain event.
    /// Carries the newly persisted <see cref="Booking"/> that was just inserted.
    /// </summary>
    public sealed class BookingCreatedEventArgs : EventArgs
    {
        /// <summary>Gets the booking that was created.</summary>
        public Booking Booking { get; }

        public BookingCreatedEventArgs(Booking booking)
        {
            Booking = booking ?? throw new ArgumentNullException(nameof(booking));
        }
    }
}
