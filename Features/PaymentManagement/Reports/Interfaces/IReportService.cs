using System;
using System.Threading.Tasks;

namespace VenueBookingSystem.Features.Reports.Interfaces
{
    public interface IReportService
    {
        Task DisplayRevenueReportAsync(DateTime startDate, DateTime endDate);
        Task<string> ExportRevenueReportAsync(DateTime startDate, DateTime endDate);
        Task DisplayOccupancyReportAsync(DateTime startDate, DateTime endDate);
        Task DisplayTopHallsAsync(int topN);
        Task<string> ExportTopHallsReportAsync(int topN);
        Task DisplayMonthlyRevenueAsync(int year);
        Task DisplayStatusSummaryAsync();
        Task<string> ExportBookingSummaryAsync();
    }
}
