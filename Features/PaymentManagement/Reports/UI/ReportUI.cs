using System;
using System.Collections.Generic;
using VenueBookingSystem.Features.Reports.DTOs;
using VenueBookingSystem.Shared;

namespace VenueBookingSystem.Features.Reports.UI
{
    /// <summary>
    /// DESIGN SPECIFICATIONS: Reports & Business Intelligence Interface
    /// ─────────────────────────────────────────────────────────────────────────────
    /// 1. LAYOUT & VISUAL HIERARCHY
    ///    - Key Performance Indicators (KPIs) are displayed as boxed summary cards at the top.
    ///    - Data is organized in structured, readable grid tables with aligned headers.
    ///    - Text charts (horizontal progress bars) visualize percentages.
    /// 
    /// 2. COLOR PALETTE & SIGNALS
    ///    - Cyan / Dark Cyan: Headers, boundaries, and label markers.
    ///    - White / Gray: Standard rows and metadata details.
    ///    - Yellow: Highlighting booking numbers and average metrics.
    ///    - Green: Highlight high performance and revenue totals.
    /// 
    /// 3. DATA VISUALIZATION
    ///    - Bar charts use standard character blocks (█/░) on a 10-step scale.
    ///    - Proportional contributions are calculated dynamically against grand totals.
    /// ─────────────────────────────────────────────────────────────────────────────
    /// </summary>
    public static class ReportUI
    {
        public static void DisplayRevenue(List<RevenueReportItem> items, DateTime start, DateTime end)
        {
            ConsoleHelper.PrintHeader(" REVENUE PERFORMANCE DASHBOARD ");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"  Reporting Period: {ConsoleHelper.FormatDate(start)} to {ConsoleHelper.FormatDate(end)}");
            Console.ResetColor();

            if (items == null || items.Count == 0)
            {
                ConsoleHelper.PrintWarning("No revenue data found for this period.");
                return;
            }

            decimal grandTotal = 0m;
            int totalBookings = 0;
            foreach (var i in items) { grandTotal += i.TotalRevenue; totalBookings += i.BookingCount; }
            decimal averageRevenue = items.Count > 0 ? grandTotal / items.Count : 0m;

            // Render KPI cards
            RenderKpis(new[]
            {
                ("GRAND REVENUE", ConsoleHelper.FormatCurrency(grandTotal), ConsoleColor.Green),
                ("TOTAL RESERVATIONS", totalBookings.ToString(), ConsoleColor.Yellow),
                ("AVG / HALL VENUE", ConsoleHelper.FormatCurrency(averageRevenue), ConsoleColor.Cyan)
            });

            Console.WriteLine();
            ConsoleHelper.PrintSubHeader("Revenue Contribution Breakdown");

            // Print custom table with contribution progress bar
            string indent = ConsoleHelper.GetIndent();
            string border = "+" + new string('-', 34) + "+" + new string('-', 10) + "+" + new string('-', 16) + "+" + new string('-', 16) + "+" + new string('-', 18) + "+";

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(indent + border);
            Console.Write(indent + "| ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{"Hall Venue",-32}");
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write(" | ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{"Bookings",-8}");
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write(" | ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{"Revenue",-14}");
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write(" | ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{"Contribution",-14}");
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write(" | ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{"Share Chart",-16}");
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(" |");
            Console.WriteLine(indent + border);

            foreach (var item in items)
            {
                double sharePct = grandTotal > 0 ? (double)(item.TotalRevenue / grandTotal) * 100 : 0;
                string progressBar = GetProgressBar(sharePct);

                Console.Write(indent + "| ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write($"{item.HallName,-32}");
                
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.Write(" | ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write($"{item.BookingCount,8}");
                
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.Write(" | ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"{ConsoleHelper.FormatCurrency(item.TotalRevenue),14}");
                
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.Write(" | ");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($"{sharePct,12:F1}%");
                
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.Write(" | ");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write($"{progressBar,-16}");
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine(" |");
            }

            Console.WriteLine(indent + border);
            Console.ResetColor();
            Console.WriteLine();
        }

        public static void DisplayOccupancy(List<OccupancyReportItem> items, DateTime start, DateTime end)
        {
            ConsoleHelper.PrintHeader(" HALL OCCUPANCY ANALYSIS ");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"  Reporting Period: {ConsoleHelper.FormatDate(start)} to {ConsoleHelper.FormatDate(end)}");
            Console.ResetColor();

            if (items == null || items.Count == 0)
            {
                ConsoleHelper.PrintWarning("No occupancy data found for this period.");
                return;
            }

            int totalBookings = 0;
            decimal totalHours = 0m;
            decimal maxHours = 0m;
            foreach (var i in items)
            {
                totalBookings += i.TotalBookings;
                totalHours += i.TotalHours;
                if (i.TotalHours > maxHours) maxHours = i.TotalHours;
            }

            // Render KPI cards
            RenderKpis(new[]
            {
                ("TOTAL OCCUPIED", $"{totalHours:F1} Hrs", ConsoleColor.Green),
                ("TOTAL RESERVATIONS", totalBookings.ToString(), ConsoleColor.Yellow),
                ("MAX ACTIVE HALL", items.Count > 0 ? GetHallWithMaxHours(items) : "N/A", ConsoleColor.Cyan)
            });

            Console.WriteLine();
            ConsoleHelper.PrintSubHeader("Relative Occupancy Distribution");

            // Print custom table with occupancy progress bar
            string indent = ConsoleHelper.GetIndent();
            string border = "+" + new string('-', 34) + "+" + new string('-', 10) + "+" + new string('-', 14) + "+" + new string('-', 16) + "+" + new string('-', 20) + "+";

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(indent + border);
            Console.Write(indent + "| ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{"Hall Venue",-32}");
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write(" | ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{"Bookings",-8}");
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write(" | ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{"Hours Used",-12}");
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write(" | ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{"Rel Occupancy",-14}");
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write(" | ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{"Relative Scale",-18}");
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(" |");
            Console.WriteLine(indent + border);

            foreach (var item in items)
            {
                double relPct = maxHours > 0m ? (double)(item.TotalHours / maxHours * 100m) : 0.0;
                string progressBar = GetProgressBar(relPct);

                Console.Write(indent + "| ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write($"{item.HallName,-32}");
                
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.Write(" | ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write($"{item.TotalBookings,8}");
                
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.Write(" | ");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($"{item.TotalHours,12:F1}");
                
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.Write(" | ");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"{relPct,12:F1}%");
                
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.Write(" | ");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write($"{progressBar,-18}");
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine(" |");
            }

            Console.WriteLine(indent + border);
            Console.ResetColor();
            Console.WriteLine();
        }

        private static string GetHallWithMaxHours(List<OccupancyReportItem> items)
        {
            string best = items[0].HallName;
            decimal max = items[0].TotalHours;
            for (int i = 1; i < items.Count; i++)
            {
                if (items[i].TotalHours > max)
                {
                    max = items[i].TotalHours;
                    best = items[i].HallName;
                }
            }
            return best;
        }

        private static void RenderKpis((string Label, string Value, ConsoleColor Color)[] kpis)
        {
            string indent = ConsoleHelper.GetIndent();
            const int cardWidth = 26;

            // Draw Top Border
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write(indent);
            foreach (var kpi in kpis)
                Console.Write("┌" + new string('─', cardWidth) + "┐ ");
            Console.WriteLine();

            // Draw Label Row
            Console.Write(indent);
            foreach (var kpi in kpis)
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.Write("│");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write(ConsoleHelper.CenterPad(kpi.Label, cardWidth));
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.Write("│ ");
            }
            Console.WriteLine();

            // Draw Separator Row
            Console.Write(indent);
            foreach (var kpi in kpis)
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.Write("├" + new string('─', cardWidth) + "┤ ");
            }
            Console.WriteLine();

            // Draw Value Row
            Console.Write(indent);
            foreach (var kpi in kpis)
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.Write("│");
                Console.ForegroundColor = kpi.Color;
                Console.Write(ConsoleHelper.CenterPad(kpi.Value, cardWidth));
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.Write("│ ");
            }
            Console.WriteLine();

            // Draw Bottom Border
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write(indent);
            foreach (var kpi in kpis)
                Console.Write("└" + new string('─', cardWidth) + "┘ ");
            Console.WriteLine();
            Console.ResetColor();
        }

        private static string GetProgressBar(double percentage)
        {
            const int scale = 10;
            int filled = (int)Math.Round((percentage / 100) * scale);
            filled = Math.Min(Math.Max(filled, 0), scale);
            
            string bars = new string('█', filled);
            string dots = new string('░', scale - filled);
            return $"{bars}{dots}";
        }
    }
}
