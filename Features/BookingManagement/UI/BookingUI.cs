using System;
using System.Collections.Generic;
using VenueBookingSystem.Features.Authentication;
using VenueBookingSystem.Shared;

namespace VenueBookingSystem.Features.Bookings.UI
{
    /// <summary>
    /// DESIGN SPECIFICATIONS: Reservation & Booking Interface
    /// ─────────────────────────────────────────────────────────────────────────────
    /// 1. LAYOUT & VISUAL HIERARCHY
    ///    - Tabular lists are rendered using dynamic console-width scaling tables.
    ///    - Details cards are framed with double-border borders (═/║) of width 76.
    ///    - Sections are separated by single-line horizontal dividers (─).
    /// 
    /// 2. COLOR PALETTE & SIGNALS
    ///    - Confirmed / Completed Status: Green (indicating verified/finished reservations).
    ///    - Pending Status: Yellow (indicating action/review needed).
    ///    - Cancelled / Rejected Status: Red (indicating cancelled or blocked states).
    ///    - Labels & Titles: Cyan and White.
    ///    - Price / Currency Values: Green.
    /// 
    /// 3. METADATA DISPLAY
    ///    - Shows start/end times and calculate total duration displays.
    ///    - Dynamic alignment for labels to ensure neat grid positioning.
    /// ─────────────────────────────────────────────────────────────────────────────
    /// </summary>
    public static class BookingUI
    {
        public static Dictionary<string, Func<Booking, string>> TableColumns => new()
        {
            ["ID"] = b => b.BookingId.ToString(),
            ["Hall Name"] = b => b.HallName,
            ["Customer Name"] = b => b.CustomerName,
            ["Date"] = b => b.StartDateTime.ToString("dd MMM yyyy"),
            ["Time"] = b => $"{b.StartDateTime:HH:mm} - {b.EndDateTime:HH:mm}",
            ["Total Amt"] = b => ConsoleHelper.FormatCurrency(b.TotalAmount),
            ["Status"] = b => b.GetStatusDisplay()
        };

        public static void DisplayBookings(List<Booking> bookings, string title = "BOOKINGS")
        {
            if (bookings.Count == 0)
            {
                ConsoleHelper.PrintWarning("No bookings found.");
                return;
            }

            ConsoleHelper.PrintTable(bookings, TableColumns, title);
        }

        public static void DisplayBookingDetails(Booking booking)
        {
            if (booking == null)
            {
                ConsoleHelper.PrintError("No booking data available to display.");
                return;
            }

            string indent = ConsoleHelper.GetIndent();
            const int W = 76;

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(indent + "╔" + new string('═', W) + "╗");
            
            Console.Write(indent + "║");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(ConsoleHelper.CenterPad($"RESERVATION DETAILS: #{booking.BookingId}", W));
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("║");
            
            Console.WriteLine(indent + "╠" + new string('═', W) + "╣");

            // Customer and Hall relations
            PrintDetailRow("Booking ID", $"{booking.BookingId}  (System Generated)", ConsoleColor.DarkGray, W);
            PrintDetailRow("Customer", $"#{booking.CustomerId} — {booking.CustomerName}", ConsoleColor.White, W);
            PrintDetailRow("Hall Venue", $"#{booking.HallId} — {booking.HallName}", ConsoleColor.White, W);

            Console.WriteLine(indent + "╟" + new string('─', W) + "╢");

            // Schedule and purpose
            PrintDetailRow("Start Time", ConsoleHelper.FormatDateTime(booking.StartDateTime), ConsoleColor.Yellow, W);
            PrintDetailRow("End Time", ConsoleHelper.FormatDateTime(booking.EndDateTime), ConsoleColor.Yellow, W);
            PrintDetailRow("Duration", booking.GetDurationDisplay(), ConsoleColor.White, W);
            PrintDetailRow("Guest Count", $"{booking.GuestCount} Guests", ConsoleColor.White, W);
            PrintDetailRow("Purpose", booking.Purpose, ConsoleColor.Gray, W);

            Console.WriteLine(indent + "╟" + new string('─', W) + "╢");

            // Totals
            PrintDetailRow("Hourly Rates", $"Total hours: {booking.TotalHours} hrs", ConsoleColor.White, W);
            PrintDetailRow("Total Amount", ConsoleHelper.FormatCurrency(booking.TotalAmount), ConsoleColor.Green, W);

            // Colored Status Badge
            string statusStr = booking.GetStatusDisplay().ToUpper();
            ConsoleColor statusColor = booking.Status switch
            {
                BookingStatus.Confirmed => ConsoleColor.Green,
                BookingStatus.Completed => ConsoleColor.Green,
                BookingStatus.Pending => ConsoleColor.Yellow,
                BookingStatus.Cancelled => ConsoleColor.Red,
                BookingStatus.Rejected => ConsoleColor.Red,
                _ => ConsoleColor.White
            };
            PrintDetailRow("Booking Status", statusStr, statusColor, W);

            Console.WriteLine(indent + "╟" + new string('─', W) + "╢");
            PrintDetailRow("Created Date", ConsoleHelper.FormatDateTime(booking.CreatedAt), ConsoleColor.DarkGray, W);
            PrintDetailRow("Last Updated", ConsoleHelper.FormatDateTime(booking.UpdatedAt), ConsoleColor.DarkGray, W);

            Console.WriteLine(indent + "╚" + new string('═', W) + "╝");
            Console.ResetColor();
            Console.WriteLine();
        }

        private static void PrintDetailRow(string label, string value, ConsoleColor valColor, int width)
        {
            string indent = ConsoleHelper.GetIndent();
            string labelPart = $" {label,-15} : ";
            int remaining = width - labelPart.Length - 2;

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write(indent + "║");
            
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(labelPart);

            Console.ForegroundColor = valColor;
            if (value.Length > remaining)
            {
                Console.Write(value.Substring(0, remaining));
            }
            else
            {
                Console.Write(value.PadRight(remaining));
            }

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("║");
        }
    }
}
