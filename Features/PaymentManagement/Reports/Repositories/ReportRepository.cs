using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using VenueBookingSystem.Features.Reports.DTOs;
using VenueBookingSystem.Features.Reports.Interfaces;
using VenueBookingSystem.Storage;

namespace VenueBookingSystem.Features.Reports.Repositories
{
    public class ReportRepository : IReportRepository
    {
        private readonly DatabaseContext _dbContext;

        public ReportRepository(DatabaseContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<List<RevenueReportItem>> GetRevenueReportAsync(DateTime startDate, DateTime endDate)
        {
            var items = new List<RevenueReportItem>();
            await using var conn = await _dbContext.CreateConnectionAsync();
            await using var cmd = new SqlCommand("sp_GetRevenueReport", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@StartDate", startDate);
            cmd.Parameters.AddWithValue("@EndDate", endDate);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(new RevenueReportItem(
                    reader.GetString(reader.GetOrdinal("HallName")),
                    reader.GetInt32(reader.GetOrdinal("BookingCount")),
                    reader.GetDecimal(reader.GetOrdinal("TotalRevenue"))
                ));
            }
            return items;
        }

        public async Task<List<OccupancyReportItem>> GetOccupancyReportAsync(DateTime startDate, DateTime endDate)
        {
            var items = new List<OccupancyReportItem>();
            await using var conn = await _dbContext.CreateConnectionAsync();
            await using var cmd = new SqlCommand("sp_GetHallOccupancy", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@StartDate", startDate);
            cmd.Parameters.AddWithValue("@EndDate", endDate);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(new OccupancyReportItem(
                    reader.GetString(reader.GetOrdinal("HallName")),
                    reader.GetInt32(reader.GetOrdinal("TotalBookings")),
                    reader.GetDecimal(reader.GetOrdinal("TotalHours"))
                ));
            }
            return items;
        }

        public async Task<List<StatusSummaryItem>> GetBookingStatisticsAsync()
        {
            var items = new List<StatusSummaryItem>();
            await using var conn = await _dbContext.CreateConnectionAsync();
            await using var cmd = new SqlCommand("sp_GetBookingStatistics", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                string status = reader.IsDBNull(reader.GetOrdinal("Status")) ? "Unknown" : reader.GetString(reader.GetOrdinal("Status"));
                int count = reader.IsDBNull(reader.GetOrdinal("BookingCount")) ? 0 : reader.GetInt32(reader.GetOrdinal("BookingCount"));
                items.Add(new StatusSummaryItem(status, count));
            }
            return items;
        }

        public async Task<List<dynamic>> GetDashboardSummaryAsync(int topN)
        {
            var items = new List<dynamic>();
            await using var conn = await _dbContext.CreateConnectionAsync();
            await using var cmd = new SqlCommand("sp_GetDashboardSummary", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                string hallName = reader.IsDBNull(reader.GetOrdinal("HallName")) ? "N/A" : reader.GetString(reader.GetOrdinal("HallName"));
                int count = reader.IsDBNull(reader.GetOrdinal("BookingCount")) ? 0 : reader.GetInt32(reader.GetOrdinal("BookingCount"));
                items.Add(new { HallName = hallName, BookingCount = count });
            }
            return items;
        }
    }
}
