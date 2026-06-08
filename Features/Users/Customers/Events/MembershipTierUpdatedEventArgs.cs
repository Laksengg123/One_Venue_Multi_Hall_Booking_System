using System;

namespace VenueBookingSystem.Features.Users.Customers.Events
{
    public class MembershipTierUpdatedEventArgs : EventArgs
    {
        public int CustomerId { get; }
        public string NewTier { get; }

        public MembershipTierUpdatedEventArgs(int customerId, string newTier)
        {
            CustomerId = customerId;
            NewTier = newTier;
        }
    }
}
