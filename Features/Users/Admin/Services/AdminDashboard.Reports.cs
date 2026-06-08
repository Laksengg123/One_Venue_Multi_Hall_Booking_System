using VenueBookingSystem.Features.Bookings;
using VenueBookingSystem.Features.Reports;
using VenueBookingSystem.Shared;

namespace VenueBookingSystem.Features.Admin;

public partial class AdminDashboard
{
    private async Task ShowReportsAsync()
    {
        bool back = false;
        while (!back)
        {
            Console.Clear();
            ConsoleHelper.PrintHeader("Reports");
            ConsoleHelper.PrintMenu("Reports", new[]
            {
                "Revenue Report (by date range)",
                "Occupancy Report (by date range)",
                "Monthly Revenue (by year)",
                "Booking Summary (with export)"
            });

            int choice = ConsoleHelper.ReadMenuChoice(1, 4);
            switch (choice)
            {
                case 1: await RevenueReportAsync(); break;
                case 2: await OccupancyReportAsync(); break;
                case 3: await MonthlyRevenueAsync(); break;
                case 4: await BookingSummaryReportAsync(); break;
                case 0: back = true; break;
            }
        }
    }

    private async Task RevenueReportAsync()
    {
        Console.Clear();
        ConsoleHelper.PrintHeader("Revenue Report");
        try
        {
            var (startDate, endDate) = ReadDateRange();
            if (startDate == DateTime.MinValue) return;

            await _reports.DisplayRevenueReportAsync(startDate, endDate);

            Console.WriteLine();
            if (ConsoleHelper.Confirm("Do you want to export this revenue report (CSV/Excel)?"))
            {
                var resultMsg = await _reports.ExportRevenueReportAsync(startDate, endDate);
                ConsoleHelper.PrintSuccess(resultMsg);
            }
        }
        catch (Exception ex)
        {
            ConsoleHelper.PrintError($"Error generating revenue report: {ex.Message}");
        }
        ConsoleHelper.PressAnyKey();
    }

    private async Task OccupancyReportAsync()
    {
        Console.Clear();
        ConsoleHelper.PrintHeader("Occupancy Report");
        try
        {
            var (startDate, endDate) = ReadDateRange();
            if (startDate == DateTime.MinValue) return;

            await _reports.DisplayOccupancyReportAsync(startDate, endDate);
        }
        catch (Exception ex)
        {
            ConsoleHelper.PrintError($"Error generating occupancy report: {ex.Message}");
        }
        ConsoleHelper.PressAnyKey();
    }

    private async Task TopHallsAsync()
    {
        Console.Clear();
        ConsoleHelper.PrintHeader("Top Halls Report");
        try
        {
            await _reports.DisplayTopHallsAsync(5);

            Console.WriteLine();
            if (ConsoleHelper.Confirm("Do you want to export this top halls report (CSV/Excel)?"))
            {
                var resultMsg = await _reports.ExportTopHallsReportAsync(5);
                ConsoleHelper.PrintSuccess(resultMsg);
            }
        }
        catch (Exception ex)
        {
            ConsoleHelper.PrintError($"Error generating top halls report: {ex.Message}");
        }
        ConsoleHelper.PressAnyKey();
    }

    private async Task MonthlyRevenueAsync()
    {
        Console.Clear();
        ConsoleHelper.PrintHeader("Monthly Revenue Report");
        try
        {
            int year = ValidationHelper.ReadInt("Year", 2020, 2030, DateTime.Now.Year.ToString());
            if (year == -1) return;

            await _reports.DisplayMonthlyRevenueAsync(year);
        }
        catch (Exception ex)
        {
            ConsoleHelper.PrintError($"Error generating monthly revenue: {ex.Message}");
        }
        ConsoleHelper.PressAnyKey();
    }

    private async Task StatusSummaryAsync()
    {
        Console.Clear();
        ConsoleHelper.PrintHeader("Booking Status Summary");
        try
        {
            await _reports.DisplayStatusSummaryAsync();
        }
        catch (Exception ex)
        {
            ConsoleHelper.PrintError($"Error generating status summary: {ex.Message}");
        }
        ConsoleHelper.PressAnyKey();
    }

    private async Task BookingSummaryReportAsync()
    {
        Console.Clear();
        ConsoleHelper.PrintHeader("Booking Summary Report");
        try
        {
            var bookings = await _bookings.GetAllBookingsAsync();
            var columns = new Dictionary<string, Func<VenueBookingSystem.Features.Bookings.Booking, string>>
            {
                ["Ref"] = b => b.BookingId.ToString(),
                ["Hall"] = b => b.HallName,
                ["Customer"] = b => b.CustomerName,
                ["Start"] = b => b.StartDateTime.ToString("dd-MM-yyyy HH:mm"),
                ["End"] = b => b.EndDateTime.ToString("dd-MM-yyyy HH:mm"),
                ["Amount"] = b => ConsoleHelper.FormatCurrency(b.TotalAmount),
                ["Status"] = b => b.GetStatusDisplay()
            };

            ConsoleHelper.PrintTable(bookings, columns, "All Bookings in Database");

            Console.WriteLine();
            if (ConsoleHelper.Confirm("Do you want to export this booking summary (CSV/Excel)?"))
            {
                var resultMsg = await _reports.ExportBookingSummaryAsync();
                ConsoleHelper.PrintSuccess(resultMsg);
            }
        }
        catch (Exception ex)
        {
            ConsoleHelper.PrintError($"Error generating booking summary: {ex.Message}");
        }
        ConsoleHelper.PressAnyKey();
    }

    private static (DateTime start, DateTime end) ReadDateRange()
    {
        DateTime start;
        while (true)
        {
            string startStr = ConsoleHelper.ReadInput("Start Date", "dd-MM-yyyy", true, true);
            if (startStr == ConsoleHelper.BACK_COMMAND) return (DateTime.MinValue, DateTime.MinValue);

            if (DateTime.TryParseExact(startStr, "dd-MM-yyyy",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out start))
                break;

            ConsoleHelper.PrintError("Invalid start date. Use dd-MM-yyyy.");
        }

        DateTime end;
        while (true)
        {
            string endStr = ConsoleHelper.ReadInput("End Date", "dd-MM-yyyy", true, true);
            if (endStr == ConsoleHelper.BACK_COMMAND) return (DateTime.MinValue, DateTime.MinValue);

            if (!DateTime.TryParseExact(endStr, "dd-MM-yyyy",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out end))
            {
                ConsoleHelper.PrintError("Invalid end date. Use dd-MM-yyyy.");
                continue;
            }

            end = end.AddDays(1).AddSeconds(-1);
            if (end > start)
                break;

            ConsoleHelper.PrintError("End date must be after start date.");
        }

        return (start, end);
    }
}
