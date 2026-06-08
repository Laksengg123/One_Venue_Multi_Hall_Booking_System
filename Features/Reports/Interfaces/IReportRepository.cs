using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VenueBookingSystem.Features.Reports.DTOs;

namespace VenueBookingSystem.Features.Reports.Interfaces
{
    public interface IReportRepository
    {
        Task<List<RevenueReportItem>> GetRevenueReportAsync(DateTime startDate, DateTime endDate);
        Task<List<OccupancyReportItem>> GetOccupancyReportAsync(DateTime startDate, DateTime endDate);
        Task<List<StatusSummaryItem>> GetBookingStatisticsAsync();
        Task<List<dynamic>> GetDashboardSummaryAsync(int topN);
    }
}
