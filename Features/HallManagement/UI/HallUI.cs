using System;
using System.Collections.Generic;
using VenueBookingSystem.Shared;

namespace VenueBookingSystem.Features.Halls.UI
{
    /// <summary>
    /// DESIGN SPECIFICATIONS: Hall Directory & Resource Interface
    /// ─────────────────────────────────────────────────────────────────────────────
    /// 1. LAYOUT & VISUAL HIERARCHY
    ///    - Tabular records display a structured summary of name, location, and rates.
    ///    - Detail cards use a double-line boundary frame (Width = 76 characters) for clarity.
    ///    - Left-aligned padded labels are used to organize details without clutter.
    /// 
    /// 2. COLOR PALETTE & SIGNALS
    ///    - Cyan / White: primary title borders and labels.
    ///    - Yellow: Pricing details and status values.
    ///    - Green / Red: Status signals for availability (Active/Inactive) and amenities.
    ///    - Dark Cyan: Decorative headers and section lines.
    ///    - Dark Gray: System generated metadata hints.
    /// 
    /// 3. RESOURCE INDICATORS
    ///    - Capacity is displayed visually using a 10-block text progress bar (e.g., [████░░░░░░]).
    ///    - Amenities (AC, Wifi, Projector) are rendered using color-coded badge tags.
    /// ─────────────────────────────────────────────────────────────────────────────
    /// </summary>
    public static class HallUI
    {
        public static Dictionary<string, Func<Hall, string>> TableColumns => new()
        {
            ["ID"] = h => h.HallId.ToString(),
            ["Name"] = h => h.HallName,
            ["Type"] = h => h.GetHallTypeDisplay(),
            ["Capacity"] = h => h.Capacity.ToString(),
            ["Price"] = h => ConsoleHelper.FormatCurrency(h.PricePerHour),
            ["Location"] = h => h.Location,
            ["Status"] = h => h.IsActive ? "Active" : "Inactive"
        };

        public static void DisplayHalls(List<Hall> halls)
        {
            ConsoleHelper.PrintHeader("Registered Halls");
            if (halls.Count == 0)
            {
                ConsoleHelper.PrintWarning("No halls found in the system.");
                return;
            }

            ConsoleHelper.PrintTable(halls, TableColumns, "HALL DIRECTORY");
        }

        public static void DisplayHallDetails(Hall hall)
        {
            if (hall == null)
            {
                ConsoleHelper.PrintError("No hall data available to display.");
                return;
            }

            string indent = ConsoleHelper.GetIndent();
            const int W = 76;

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(indent + "╔" + new string('═', W) + "╗");
            
            Console.Write(indent + "║");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(ConsoleHelper.CenterPad($"HALL DETAILS: {hall.HallName.ToUpper()}", W));
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("║");
            
            Console.WriteLine(indent + "╠" + new string('═', W) + "╣");

            // Type and Location
            PrintDetailRow("Hall ID", $"{hall.HallId}  (System Generated)", ConsoleColor.DarkGray, W);
            PrintDetailRow("Type", hall.GetHallTypeDisplay(), ConsoleColor.White, W);
            PrintDetailRow("Location", hall.Location, ConsoleColor.White, W);
            
            // Description block (wrapped if too long)
            string desc = string.IsNullOrWhiteSpace(hall.Description) ? "No description provided." : hall.Description;
            PrintDetailRow("Description", desc, ConsoleColor.Gray, W);

            Console.WriteLine(indent + "╟" + new string('─', W) + "╢");

            // Capacity & Progress Bar
            string capBar = GetCapacityIndicator(hall.Capacity);
            PrintDetailRow("Capacity", capBar, ConsoleColor.Yellow, W);

            // Pricing
            PrintDetailRow("Price", ConsoleHelper.FormatCurrency(hall.PricePerHour), ConsoleColor.Green, W);

            // Status
            string statusStr = hall.IsActive ? "Active & Bookable" : "Inactive / Under Maintenance";
            ConsoleColor statusCol = hall.IsActive ? ConsoleColor.Green : ConsoleColor.Red;
            PrintDetailRow("Status", statusStr, statusCol, W);

            Console.WriteLine(indent + "╟" + new string('─', W) + "╢");

            // Amenities
            Console.Write(indent + "║ ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(" Amenities:    ");

            // AC badge
            Console.BackgroundColor = hall.HasAC ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(" AC ");
            Console.ResetColor();
            Console.Write("  ");

            // Projector badge
            Console.BackgroundColor = hall.HasProjector ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(" Projector ");
            Console.ResetColor();
            Console.Write("  ");

            // Wifi badge
            Console.BackgroundColor = hall.HasWifi ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(" WiFi ");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(new string(' ', W - 37) + "║");

            Console.WriteLine(indent + "╚" + new string('═', W) + "╝");
            Console.ResetColor();
            Console.WriteLine();
        }

        private static void PrintDetailRow(string label, string value, ConsoleColor valColor, int width)
        {
            string indent = ConsoleHelper.GetIndent();
            string labelPart = $" {label,-15} : ";
            int remaining = width - labelPart.Length - 2; // Subtract left and right borders

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

        private static string GetCapacityIndicator(int capacity)
        {
            const int totalBlocks = 10;
            // Normalize capacity using a logical scale (e.g. 1 block per 50 guests, max 500)
            int score = Math.Min(capacity / 50, totalBlocks);
            if (score == 0 && capacity > 0) score = 1;
            
            string filled = new string('█', score);
            string empty = new string('░', totalBlocks - score);
            return $"{filled}{empty} ({capacity} Guests max)";
        }
    }
}
