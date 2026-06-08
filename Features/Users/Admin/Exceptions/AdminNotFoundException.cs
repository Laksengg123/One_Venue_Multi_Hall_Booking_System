using System;

namespace VenueBookingSystem.Features.Users.Admin.Exceptions
{
    public class AdminNotFoundException : Exception
    {
        public AdminNotFoundException(string message) : base(message) { }
    }
}
