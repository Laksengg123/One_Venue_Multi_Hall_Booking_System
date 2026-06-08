using System;

namespace VenueBookingSystem.Features.Users.Customers.Exceptions
{
    public class CustomerOperationException : Exception
    {
        public CustomerOperationException(string message) : base(message)
        {
        }

        public CustomerOperationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
