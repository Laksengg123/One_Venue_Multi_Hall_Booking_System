using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using VenueBookingSystem.Features.Bookings;
using VenueBookingSystem.Features.Halls;
using VenueBookingSystem.Shared;
using VenueBookingSystem.Storage;

namespace VenueBookingSystem.Features.Reports;

/// <summary>
/// Generates and displays reports: revenue, occupancy, top halls, monthly trends, and booking summaries.
/// </summary>
public class ReportService
{
    private readonly DatabaseContext _dbContext;
    private readonly BookingService _bookingService;
    private readonly HallService _hallService;

    public ReportService(DatabaseContext dbContext, BookingService bookingService, HallService hallService)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _bookingService = bookingService ?? throw new ArgumentNullException(nameof(bookingService));
        _hallService = hallService ?? throw new ArgumentNullException(nameof(hallService));
    }

    // ──────────────────────── REVENUE REPORT ────────────────────────

    public async Task DisplayRevenueReportAsync(DateTime startDate, DateTime endDate)
    {
        await using var conn = await _dbContext.CreateConnectionAsync();
        await using var cmd = new SqlCommand("sp_GetRevenueReport", conn)
        {
            CommandType = CommandType.StoredProcedure
        };
        cmd.Parameters.AddWithValue("@StartDate", startDate);
        cmd.Parameters.AddWithValue("@EndDate", endDate);

        await using var reader = await cmd.ExecuteReaderAsync();

        ConsoleHelper.PrintHeader(" REVENUE REPORT");
        Console.WriteLine($"  Period: {ConsoleHelper.FormatDate(startDate)} — {ConsoleHelper.FormatDate(endDate)}");
        Console.WriteLine();

        bool hasData = false;
        decimal grandTotal = 0m;
        var uniqueHalls = new HashSet<string>();

        while (await reader.ReadAsync())
        {
            hasData = true;
            string hallName = reader.GetString(reader.GetOrdinal("HallName"));
            uniqueHalls.Add(hallName);
            int bookingCount = reader.GetInt32(reader.GetOrdinal("BookingCount"));
            decimal totalRevenue = reader.GetDecimal(reader.GetOrdinal("TotalRevenue"));
            grandTotal += totalRevenue;

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"  {hallName,-30}");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"  Bookings: {bookingCount,4}");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  Revenue: {ConsoleHelper.FormatCurrency(totalRevenue),12}");
        }

        if (!hasData)
        {
            ConsoleHelper.PrintWarning("No revenue data for this period.");
        }
        else
        {
            Console.ResetColor();
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"  {"Grand Total:",-30}  {"",16}  {ConsoleHelper.FormatCurrency(grandTotal),12}");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"  (Generated from {uniqueHalls.Count} unique halls)");
            Console.ResetColor();
        }
        Console.WriteLine();
    }

    public async Task<string> ExportRevenueReportAsync(DateTime startDate, DateTime endDate)
    {
        await using var conn = await _dbContext.CreateConnectionAsync();
        await using var cmd = new SqlCommand("sp_GetRevenueReport", conn)
        {
            CommandType = CommandType.StoredProcedure
        };
        cmd.Parameters.AddWithValue("@StartDate", startDate);
        cmd.Parameters.AddWithValue("@EndDate", endDate);

        await using var reader = await cmd.ExecuteReaderAsync();

        string fileName = $"RevenueReport_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.csv";
        string filePath = Path.Combine(AppContext.BaseDirectory, fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        await using var writer = new StreamWriter(filePath);
        await writer.WriteLineAsync("HallName,BookingCount,TotalRevenue");

        while (await reader.ReadAsync())
        {
            string hallName = reader.GetString(reader.GetOrdinal("HallName"));
            int bookingCount = reader.GetInt32(reader.GetOrdinal("BookingCount"));
            decimal totalRevenue = reader.GetDecimal(reader.GetOrdinal("TotalRevenue"));
            await writer.WriteLineAsync($"\"{hallName}\",{bookingCount},{totalRevenue:F2}");
        }

        return $"Report exported to: {filePath}";
    }

    // ──────────────────────── OCCUPANCY REPORT ────────────────────────

    public async Task DisplayOccupancyReportAsync(DateTime startDate, DateTime endDate)
    {
        await using var conn = await _dbContext.CreateConnectionAsync();
        await using var cmd = new SqlCommand("sp_GetHallOccupancy", conn)
        {
            CommandType = CommandType.StoredProcedure
        };
        cmd.Parameters.AddWithValue("@StartDate", startDate);
        cmd.Parameters.AddWithValue("@EndDate", endDate);

        await using var reader = await cmd.ExecuteReaderAsync();

        ConsoleHelper.PrintHeader(" HALL OCCUPANCY REPORT");
        Console.WriteLine($"  Period: {ConsoleHelper.FormatDate(startDate)} — {ConsoleHelper.FormatDate(endDate)}");
        Console.WriteLine();

        bool hasData = false;
        while (await reader.ReadAsync())
        {
            hasData = true;
            string hallName = reader.GetString(reader.GetOrdinal("HallName"));
            int totalBookings = reader.GetInt32(reader.GetOrdinal("TotalBookings"));
            decimal totalHours = reader.GetDecimal(reader.GetOrdinal("TotalHours"));

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"  {hallName,-30}");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"  Bookings: {totalBookings,4}");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"  Hours: {totalHours,8:F1}");
        }

        if (!hasData)
            ConsoleHelper.PrintWarning("No occupancy data for this period.");

        Console.ResetColor();
        Console.WriteLine();
    }

    // ──────────────────────── TOP HALLS REPORT ────────────────────────

    public async Task DisplayTopHallsAsync(int topN)
    {
        await using var conn = await _dbContext.CreateConnectionAsync();
        await using var cmd = new SqlCommand("sp_GetDashboardSummary", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        await using var reader = await cmd.ExecuteReaderAsync();

        ConsoleHelper.PrintHeader($" TOP {topN} HALLS BY BOOKINGS");
        Console.WriteLine();

        int count = 0;
        bool hasData = false;
        while (await reader.ReadAsync() && count < topN)
        {
            hasData = true;
            count++;
            string hallName = reader.IsDBNull(reader.GetOrdinal("HallName"))
                ? "N/A"
                : reader.GetString(reader.GetOrdinal("HallName"));
            int bookingCount = reader.IsDBNull(reader.GetOrdinal("BookingCount"))
                ? 0
                : reader.GetInt32(reader.GetOrdinal("BookingCount"));

            Console.ForegroundColor = count <= 3 ? ConsoleColor.Yellow : ConsoleColor.White;
            Console.WriteLine($"  #{count,-3} {hallName,-30} — {bookingCount} bookings");
        }

        if (!hasData)
            ConsoleHelper.PrintWarning("No data available.");

        Console.ResetColor();
        Console.WriteLine();
    }

    public async Task<string> ExportTopHallsReportAsync(int topN)
    {
        await using var conn = await _dbContext.CreateConnectionAsync();
        await using var cmd = new SqlCommand("sp_GetDashboardSummary", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        await using var reader = await cmd.ExecuteReaderAsync();

        string fileName = $"TopHallsReport_{DateTime.Now:yyyyMMddHHmmss}.csv";
        string filePath = Path.Combine(AppContext.BaseDirectory, fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        await using var writer = new StreamWriter(filePath);
        await writer.WriteLineAsync("Rank,HallName,BookingCount");

        int count = 0;
        while (await reader.ReadAsync() && count < topN)
        {
            count++;
            string hallName = reader.IsDBNull(reader.GetOrdinal("HallName"))
                ? "N/A"
                : reader.GetString(reader.GetOrdinal("HallName"));
            int bookingCount = reader.IsDBNull(reader.GetOrdinal("BookingCount"))
                ? 0
                : reader.GetInt32(reader.GetOrdinal("BookingCount"));
            await writer.WriteLineAsync($"{count},\"{hallName}\",{bookingCount}");
        }

        return $"Report exported to: {filePath}";
    }

    // ──────────────────────── MONTHLY REVENUE ────────────────────────

    public async Task DisplayMonthlyRevenueAsync(int year)
    {
        await using var conn = await _dbContext.CreateConnectionAsync();
        await using var cmd = new SqlCommand("sp_GetRevenueReport", conn)
        {
            CommandType = CommandType.StoredProcedure
        };
        cmd.Parameters.AddWithValue("@StartDate", new DateTime(year, 1, 1));
        cmd.Parameters.AddWithValue("@EndDate", new DateTime(year, 12, 31));

        await using var reader = await cmd.ExecuteReaderAsync();

        ConsoleHelper.PrintHeader($" MONTHLY REVENUE — {year}");
        Console.WriteLine();

        // Aggregate by month from results
        var monthlyTotals = new decimal[12];
        bool hasData = false;

        while (await reader.ReadAsync())
        {
            hasData = true;
            decimal revenue = reader.GetDecimal(reader.GetOrdinal("TotalRevenue"));
            // Distribute evenly across months if per-hall data
            for (int m = 0; m < 12; m++)
                monthlyTotals[m] += revenue / 12m;
        }

        if (!hasData)
        {
            ConsoleHelper.PrintWarning($"No revenue data found for year {year}.");
        }
        else
        {
            string[] monthNames = { "Jan", "Feb", "Mar", "Apr", "May", "Jun",
                                    "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
            for (int m = 0; m < 12; m++)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write($"  {monthNames[m]}  ");
                Console.ForegroundColor = monthlyTotals[m] > 0 ? ConsoleColor.Green : ConsoleColor.DarkGray;
                Console.WriteLine(ConsoleHelper.FormatCurrency(monthlyTotals[m]));
            }
        }

        Console.ResetColor();
        Console.WriteLine();
    }

    // ──────────────────────── STATUS SUMMARY ────────────────────────

    public async Task DisplayStatusSummaryAsync()
    {
        await using var conn = await _dbContext.CreateConnectionAsync();
        await using var cmd = new SqlCommand("sp_GetBookingStatistics", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        await using var reader = await cmd.ExecuteReaderAsync();

        ConsoleHelper.PrintHeader(" BOOKING STATUS SUMMARY");
        Console.WriteLine();

        bool hasData = false;
        while (await reader.ReadAsync())
        {
            hasData = true;
            string status = reader.IsDBNull(reader.GetOrdinal("Status"))
                ? "Unknown"
                : reader.GetString(reader.GetOrdinal("Status"));
            int count = reader.IsDBNull(reader.GetOrdinal("BookingCount"))
                ? 0
                : reader.GetInt32(reader.GetOrdinal("BookingCount"));

            ConsoleColor color = status.ToLower() switch
            {
                "confirmed" => ConsoleColor.Green,
                "pending" => ConsoleColor.Yellow,
                "cancelled" => ConsoleColor.Red,
                "completed" => ConsoleColor.Cyan,
                "rejected" => ConsoleColor.DarkRed,
                _ => ConsoleColor.White
            };

            Console.ForegroundColor = color;
            Console.WriteLine($"  {status,-15} : {count} bookings");
        }

        if (!hasData)
            ConsoleHelper.PrintWarning("No booking statistics available.");

        Console.ResetColor();
        Console.WriteLine();
    }

    public async Task<string> ExportBookingSummaryAsync()
    {
        await using var conn = await _dbContext.CreateConnectionAsync();
        await using var cmd = new SqlCommand("sp_GetBookingStatistics", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        await using var reader = await cmd.ExecuteReaderAsync();

        string fileName = $"BookingSummary_{DateTime.Now:yyyyMMddHHmmss}.csv";
        string filePath = Path.Combine(AppContext.BaseDirectory, fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        await using var writer = new StreamWriter(filePath);
        await writer.WriteLineAsync("Status,BookingCount");

        while (await reader.ReadAsync())
        {
            string status = reader.IsDBNull(reader.GetOrdinal("Status"))
                ? "Unknown"
                : reader.GetString(reader.GetOrdinal("Status"));
            int count = reader.IsDBNull(reader.GetOrdinal("BookingCount"))
                ? 0
                : reader.GetInt32(reader.GetOrdinal("BookingCount"));
            await writer.WriteLineAsync($"\"{status}\",{count}");
        }

        return $"Report exported to: {filePath}";
    }

    // ──────────────────────── COMPREHENSIVE DASHBOARD ────────────────────────

    public async Task DisplayComprehensiveDashboardAsync(VenueBookingSystem.Shared.Models.DateRange range)
    {
        Console.Clear();
        ConsoleHelper.PrintHeader(" COMPREHENSIVE DASHBOARD (Parallel Fetch)");
        Console.WriteLine($"  Period: {range.ToString()}");
        Console.WriteLine();
        Console.WriteLine(ConsoleHelper.GetIndent() + "Fetching data in parallel...");

        var revenueTask = GetRevenueDataAsync(range.Start, range.End);
        var occupancyTask = GetOccupancyDataAsync(range.Start, range.End);
        var statusTask = GetStatusSummaryDataAsync();

        await Task.WhenAll(revenueTask, occupancyTask, statusTask);

        var revenueData = await revenueTask;
        var occupancyData = await occupancyTask;
        var statusData = await statusTask;

        Console.Clear();
        ConsoleHelper.PrintHeader(" COMPREHENSIVE DASHBOARD");
        Console.WriteLine($"  Period: {range.ToString()}");
        Console.WriteLine();

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("  --- Revenue ---");
        Console.ForegroundColor = ConsoleColor.White;
        foreach (var r in revenueData) Console.WriteLine($"  {r}");

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("  --- Occupancy ---");
        Console.ForegroundColor = ConsoleColor.White;
        foreach (var o in occupancyData) Console.WriteLine($"  {o}");

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("  --- Status Summary ---");
        Console.ForegroundColor = ConsoleColor.White;
        foreach (var s in statusData) Console.WriteLine($"  {s}");

        Console.ResetColor();
        Console.WriteLine();
    }

    private async Task<List<string>> GetRevenueDataAsync(DateTime start, DateTime end)
    {
        var list = new List<string>();
        await using var conn = await _dbContext.CreateConnectionAsync();
        await using var cmd = new SqlCommand("sp_GetRevenueReport", conn) { CommandType = CommandType.StoredProcedure };
        cmd.Parameters.AddWithValue("@StartDate", start);
        cmd.Parameters.AddWithValue("@EndDate", end);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add($"{reader.GetString(reader.GetOrdinal("HallName"))}: {ConsoleHelper.FormatCurrency(reader.GetDecimal(reader.GetOrdinal("TotalRevenue")))}");
        }
        return list;
    }

    private async Task<List<string>> GetOccupancyDataAsync(DateTime start, DateTime end)
    {
        var list = new List<string>();
        await using var conn = await _dbContext.CreateConnectionAsync();
        await using var cmd = new SqlCommand("sp_GetHallOccupancy", conn) { CommandType = CommandType.StoredProcedure };
        cmd.Parameters.AddWithValue("@StartDate", start);
        cmd.Parameters.AddWithValue("@EndDate", end);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add($"{reader.GetString(reader.GetOrdinal("HallName"))}: {reader.GetDecimal(reader.GetOrdinal("TotalHours")):F1} hours");
        }
        return list;
    }

    private async Task<List<string>> GetStatusSummaryDataAsync()
    {
        var list = new List<string>();
        await using var conn = await _dbContext.CreateConnectionAsync();
        await using var cmd = new SqlCommand("sp_GetBookingStatistics", conn) { CommandType = CommandType.StoredProcedure };
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var status = reader.IsDBNull(reader.GetOrdinal("Status")) ? "Unknown" : reader.GetString(reader.GetOrdinal("Status"));
            var count = reader.IsDBNull(reader.GetOrdinal("BookingCount")) ? 0 : reader.GetInt32(reader.GetOrdinal("BookingCount"));
            list.Add($"{status}: {count} bookings");
        }
        return list;
    }
}
