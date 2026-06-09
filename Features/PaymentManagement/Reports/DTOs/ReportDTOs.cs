namespace VenueBookingSystem.Features.Reports.DTOs
{
    public record RevenueReportItem(string HallName, int BookingCount, decimal TotalRevenue);
    public record OccupancyReportItem(string HallName, int TotalBookings, decimal TotalHours);
    public record StatusSummaryItem(string Status, int BookingCount);
}
