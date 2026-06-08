namespace VenueBookingSystem.Features.Users.Customers.DTOs
{
    public record UpdateMembershipTierRequest(int CustomerId, string NewTier);
}
