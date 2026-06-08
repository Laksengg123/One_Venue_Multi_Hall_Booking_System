using System;

namespace VenueBookingSystem.Features.Users.Customers.Models
{
    public class CustomerProfile
    {
        public int CustomerId { get; set; }
        public int UserId { get; set; }
        public string MembershipTier { get; set; } = "Standard";
        public decimal LoyaltyPoints { get; set; } = 0;
        public DateTime MemberSince { get; set; } = DateTime.UtcNow;
    }
}
