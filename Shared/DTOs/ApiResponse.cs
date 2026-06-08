namespace VenueBookingSystem.Shared.DTOs
{
    public record ApiResponse<T>(bool Success, string Message, T? Data);
}
