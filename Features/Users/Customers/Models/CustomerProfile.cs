using System;

namespace VenueBookingSystem.Features.Users.Customers.Models
{
    // ╔══════════════════════════════════════════════════════════════════════╗
    // ║           🪙 CUSTOMER PROFILE — THE LOYALTY CARD DATA               ║
    // ║  Stores the loyalty and membership details for a customer.          ║
    // ║  Links to their User login account via UserId.                      ║
    // ║  Used to display the "Customer Membership & Loyalty Card" on screen.║
    // ╚══════════════════════════════════════════════════════════════════════╝
    public class CustomerProfile
    {
        // 🔢 Unique ID for this customer record
        //    e.g. shown as "CUST-LN-0007" on the loyalty card
        public int CustomerId { get; set; }

        // 🔗 Links this customer profile to their user login account
        //    CustomerId ≠ UserId — they are separate IDs
        public int UserId { get; set; }

        // 🏅 Which membership tier does this customer belong to?
        //    Standard → Silver → Gold → Platinum (highest)
        //    Starts as "Standard" for all new customers
        public string MembershipTier { get; set; } = "Standard";

        // ⭐ How many loyalty points does this customer have?
        //    Points increase with each booking/payment
        //    500 pts → Silver | 1500 pts → Gold | 5000 pts → Platinum
        //    Starts at 0 for new customers
        public decimal LoyaltyPoints { get; set; } = 0;

        // 📅 When did this customer first join / register?
        //    Shown as "Anniversary" on the loyalty card
        //    DateTime.UtcNow = "right now" as default
        public DateTime MemberSince { get; set; } = DateTime.UtcNow;
    }
}
