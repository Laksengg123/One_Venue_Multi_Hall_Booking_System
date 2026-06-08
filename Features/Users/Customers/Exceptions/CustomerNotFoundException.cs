using System;

namespace VenueBookingSystem.Features.Users.Customers.Exceptions
{
    public class CustomerNotFoundException : Exception
    {
        public CustomerNotFoundException(string message) : base(message) { }
    }
}
