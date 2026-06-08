using System;

namespace VenueBookingSystem.Features.Users.Admin.Exceptions
{
    public class AdminPrivilegeException : Exception
    {
        public AdminPrivilegeException(string message) : base(message)
        {
        }

        public AdminPrivilegeException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
