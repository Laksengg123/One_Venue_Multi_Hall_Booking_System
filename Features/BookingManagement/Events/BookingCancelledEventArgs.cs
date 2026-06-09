using System;

namespace VenueBookingSystem.Features.Bookings.Events
{
    // ╔══════════════════════════════════════════════════════════════════════╗
    // ║       📢 BOOKING CANCELLED EVENT ARGS — THE CANCELLATION ENVELOPE   ║
    // ║  When a booking is CANCELLED, this envelope carries the details     ║
    // ║  of which booking was cancelled to all listeners.                   ║
    // ╚══════════════════════════════════════════════════════════════════════╝

    // 'sealed' = this class is final — nobody can extend or inherit from it
    public sealed class BookingCancelledEventArgs : EventArgs
    {
        // 📦 The cancelled booking — listeners use this to know which booking was cancelled
        public Booking Booking { get; }

        // 🏗️ Must provide the cancelled booking when creating this envelope
        //    If booking is null (empty) → throw an error right away
        public BookingCancelledEventArgs(Booking booking)
        {
            Booking = booking ?? throw new ArgumentNullException(nameof(booking));
        }
    }
}
