using System;
using VenueBookingSystem.Features.Users.Customers.Models;
using VenueBookingSystem.Features.Authentication;
using VenueBookingSystem.Shared;

namespace VenueBookingSystem.Features.Users.Customers.UI
{
    /// <summary>
    /// DESIGN SPECIFICATIONS: Customer Profile & Loyalty System Interface
    /// ─────────────────────────────────────────────────────────────────────────────
    /// 1. LAYOUT & VISUAL HIERARCHY
    ///    - Displayed as a premium loyalty card using a double-line boundary card (width = 76).
    ///    - Single-line dividers isolate user details from loyalty points tracking.
    ///    - Explicit grid alignment is used for labels to maintain structure.
    /// 
    /// 2. COLOR PALETTE & SIGNALS
    ///    - Membership Tiers: 
    ///      * Platinum: Cyan / White (highest privilege tier)
    ///      * Gold: Yellow (high-value tier)
    ///      * Silver: White / Gray (mid tier)
    ///      * Standard / Bronze: Dark Yellow
    ///    - Progress Bars: Green (filling up as points grow).
    ///    - Labels: Cyan.
    /// 
    /// 3. LOYALTY GAMIFICATION
    ///    - Renders points progress bar (e.g. [████░░░░░░]) indicating threshold to next membership tier.
    ///    - Displays membership anniversary date.
    /// ─────────────────────────────────────────────────────────────────────────────
    /// </summary>
    public static class CustomerUI
    {
        public static void DisplayCustomerProfile(CustomerProfile profile)
        {
            DisplayCustomerProfile(profile, null);
        }

        public static void DisplayCustomerProfile(CustomerProfile profile, User? user)
        {
            if (profile == null)
            {
                ConsoleHelper.PrintError("No customer profile data available to display.");
                return;
            }

            string indent = ConsoleHelper.GetIndent();
            const int W = 76;

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(indent + "╔" + new string('═', W) + "╗");
            
            Console.Write(indent + "║");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(ConsoleHelper.CenterPad("CUSTOMER MEMBERSHIP & LOYALTY CARD", W));
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("║");
            
            Console.WriteLine(indent + "╠" + new string('═', W) + "╣");

            // Customer IDs
            PrintDetailRow("Customer ID", $"CUST-LN-{profile.CustomerId:D4}", ConsoleColor.DarkGray, W);
            PrintDetailRow("User ID", $"USR-{profile.UserId:D4}", ConsoleColor.DarkGray, W);

            // Membership Anniversary
            PrintDetailRow("Anniversary", ConsoleHelper.FormatDate(profile.MemberSince), ConsoleColor.White, W);

            if (user != null)
            {
                PrintDetailRow("Full Name", user.FullName, ConsoleColor.White, W);
                PrintDetailRow("Username", user.Username, ConsoleColor.Cyan, W);
                PrintDetailRow("Email Address", user.Email, ConsoleColor.White, W);
                PrintDetailRow("Contact Phone", user.Phone, ConsoleColor.White, W);
            }

            Console.WriteLine(indent + "╟" + new string('─', W) + "╢");

            // Membership Tier Badge
            string tierStr = (profile.MembershipTier ?? "Standard").ToUpper();
            ConsoleColor tierColor = tierStr switch
            {
                "PLATINUM" => ConsoleColor.Cyan,
                "GOLD" => ConsoleColor.Yellow,
                "SILVER" => ConsoleColor.White,
                _ => ConsoleColor.DarkYellow
            };
            PrintDetailRow("Current Tier", tierStr, tierColor, W);
            PrintDetailRow("Loyalty Points", $"{profile.LoyaltyPoints:F0} Points", ConsoleColor.Green, W);

            // Points progress tracker bar
            var progress = GetTierProgress(tierStr, profile.LoyaltyPoints);
            if (progress.Threshold > 0)
            {
                string progressBar = GetProgressBar(progress.ProgressPct);
                string progressText = $"{progressBar}  {progress.Current:F0} / {progress.Threshold:F0} to {progress.NextTier}";
                PrintDetailRow("Tier Up Progress", progressText, ConsoleColor.Green, W);
            }
            else
            {
                PrintDetailRow("Tier Up Progress", "MAXIMUM MEMBERSHIP STATUS REACHED!", ConsoleColor.Cyan, W);
            }

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

        private static (string NextTier, decimal Threshold, decimal Current, double ProgressPct) GetTierProgress(string currentTier, decimal loyaltyPoints)
        {
            currentTier = currentTier?.Trim().ToUpper() ?? "STANDARD";
            
            decimal threshold = 0;
            string nextTier = "NONE";
            
            if (currentTier == "STANDARD" || currentTier == "BRONZE")
            {
                threshold = 500;
                nextTier = "SILVER";
            }
            else if (currentTier == "SILVER")
            {
                threshold = 1500;
                nextTier = "GOLD";
            }
            else if (currentTier == "GOLD")
            {
                threshold = 5000;
                nextTier = "PLATINUM";
            }
            else
            {
                return (nextTier, 0, loyaltyPoints, 100);
            }

            double progress = threshold > 0 ? (double)(loyaltyPoints / threshold) * 100 : 100;
            if (progress > 100) progress = 100;
            return (nextTier, threshold, loyaltyPoints, progress);
        }

        private static string GetProgressBar(double percentage)
        {
            const int scale = 10;
            int filled = (int)Math.Round((percentage / 100) * scale);
            filled = Math.Min(Math.Max(filled, 0), scale);
            
            string bars = new string('█', filled);
            string dots = new string('░', scale - filled);
            return $"[{bars}{dots}]";
        }
    }
}
