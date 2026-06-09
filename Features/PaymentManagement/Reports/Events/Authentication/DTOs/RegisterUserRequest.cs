using VenueBookingSystem.Features.Authentication.Models;

namespace VenueBookingSystem.Features.Authentication.DTOs
{
    public record RegisterUserRequest(string FullName, string Username, string Email, string Phone, string Password, UserRole Role);
}
