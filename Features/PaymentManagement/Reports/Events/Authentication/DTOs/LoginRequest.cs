namespace VenueBookingSystem.Features.Authentication.DTOs
{
    public record LoginRequest(string UsernameOrEmail, string Password);
}
