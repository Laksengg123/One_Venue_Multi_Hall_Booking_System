namespace VenueBookingSystem.Features.Authentication.DTOs
{
    public record UpdateUserRequest(int UserId, string Email, string Phone, string FullName);
}
