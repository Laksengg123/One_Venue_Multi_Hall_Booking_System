using System;

namespace VenueBookingSystem.Features.Bookings.Exceptions
{
    // ╔══════════════════════════════════════════════════════════════════════╗
    // ║              🚨 BOOKING EXCEPTION — THE BOOKING ALARM               ║
    // ║  This is a CUSTOM error type — specifically for booking problems.   ║
    // ║  Instead of a generic "Error", we throw a named "BookingException". ║
    // ║  This makes it easy to know WHERE the problem came from.            ║
    // ║                                                                      ║
    // ║  ': Exception' means: we INHERIT from C#'s built-in Exception.     ║
    // ║  We ARE an Exception — just a more specific, labeled one.           ║
    // ╚══════════════════════════════════════════════════════════════════════╝
    public class BookingException : Exception
    {
        // 🔕 Empty alarm — just says "something went wrong in bookings" with no details
        //    Used when the error is obvious from context
        public BookingException() { }

        // 🔔 Alarm WITH a message — explains what went wrong
        //    ': base(message)' = pass the message to the parent Exception class
        //    so it is properly stored and can be read later (ex.Message)
        public BookingException(string message) : base(message) { }

        // 🚨 Full alarm — includes TWO things:
        //    1. message        = what went wrong in bookings (human-readable)
        //    2. innerException = the original root cause (e.g. the raw SQL error)
        //    This preserves the full error chain — like a trail of breadcrumbs
        //    e.g. "Failed to create booking: Cannot insert NULL into column 'HallId'"
        public BookingException(string message, Exception innerException) : base(message, innerException) { }
    }
}
