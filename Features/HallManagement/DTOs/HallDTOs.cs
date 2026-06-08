namespace VenueBookingSystem.Features.Halls.DTOs
{
    public record CreateHallRequest(string Name, int Capacity, decimal PricePerHour, string Facilities);
    public record UpdateHallRequest(int HallId, string Name, int Capacity, decimal PricePerHour, string Facilities, bool IsActive);
}
