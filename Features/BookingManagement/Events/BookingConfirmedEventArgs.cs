using System;

namespace VenueBookingSystem.Features.Bookings.Events
{
    // ╔══════════════════════════════════════════════════════════════════════╗
    // ║       📢 BOOKING CONFIRMED EVENT ARGS — THE APPROVAL ENVELOPE       ║
    // ║  When admin CONFIRMS a booking, this envelope carries the details   ║
    // ║  of which booking was confirmed to all listeners.                   ║
    // ╚══════════════════════════════════════════════════════════════════════╝

    // 'sealed' = final class, no inheritance allowed
    public sealed class BookingConfirmedEventArgs : EventArgs
    {
        // 📦 The confirmed booking — listeners read this to know which one was approved
        public Booking Booking { get; }

        // 🏗️ Must provide a booking when creating this envelope — null is not allowed
        public BookingConfirmedEventArgs(Booking booking)
        {
            Booking = booking ?? throw new ArgumentNullException(nameof(booking));
        }
    }
}
