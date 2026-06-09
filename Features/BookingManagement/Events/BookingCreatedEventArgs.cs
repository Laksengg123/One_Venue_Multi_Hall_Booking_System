using System;

namespace VenueBookingSystem.Features.Bookings.Events
{
    // ╔══════════════════════════════════════════════════════════════════════╗
    // ║         📢 BOOKING CREATED EVENT ARGS — THE ANNOUNCEMENT ENVELOPE   ║
    // ║  When a new booking is created, the system makes an announcement.   ║
    // ║  This class is the ENVELOPE that carries the booking details        ║
    // ║  along with that announcement — so listeners know WHICH booking.    ║
    // ╚══════════════════════════════════════════════════════════════════════╝

    /// <summary>
    /// Provides data for the <see cref="BookingService.BookingCreated"/> domain event.
    /// Carries the newly persisted <see cref="Booking"/> that was just inserted.
    /// </summary>

    // 'sealed'    = nobody can inherit from this class — it is final, complete as-is
    // ': EventArgs' = this is a standard C# event data carrier (required for events)
    public sealed class BookingCreatedEventArgs : EventArgs
    {
        /// <summary>Gets the booking that was created.</summary>
        // 📦 The actual booking that was just saved — read-only once set
        //    Listeners open this envelope to find out WHICH booking was created
        public Booking Booking { get; }

        // 🏗️ CONSTRUCTOR — You MUST put a booking inside this envelope when creating it
        //    If you try to create this envelope with NO booking (null) → throw an error
        //    Because an announcement about "a booking" with no booking data makes no sense
        public BookingCreatedEventArgs(Booking booking)
        {
            Booking = booking ?? throw new ArgumentNullException(nameof(booking));
            //                ↑ If booking is null → crash immediately with a clear error
        }
    }
}
