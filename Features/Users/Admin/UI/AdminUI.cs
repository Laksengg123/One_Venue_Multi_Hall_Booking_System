using System;
using VenueBookingSystem.Features.Users.Admin.Models;
using VenueBookingSystem.Features.Authentication;
using VenueBookingSystem.Shared;

namespace VenueBookingSystem.Features.Users.Admin.UI
{
    /// <summary>
    /// DESIGN SPECIFICATIONS: Administration Profile Interface
    /// ─────────────────────────────────────────────────────────────────────────────
    /// 1. LAYOUT & VISUAL HIERARCHY
    ///    - Displayed as a premium identity card using a double-line boundary card (width = 76).
    ///    - Dotted and single-line divisions structure section fields.
    ///    - Uniform label spacing ensures perfect alignment of values.
    /// 
    /// 2. COLOR PALETTE & SIGNALS
    ///    - Privilege Levels: Red / Magenta for "SuperAdmin" (denoting full rights), 
    ///      Yellow for "Standard" (denoting general management access).
    ///    - Primary Labels: Cyan.
    ///    - Data values: White or Green.
    ///    - System Metadata: Dark Gray.
    /// 
    /// 3. METADATA PRESENTATION
    ///    - Combines physical profile parameters (Department, Privilege Level) 
    ///      with account authentication metadata (Name, Email, Account Status).
    /// ─────────────────────────────────────────────────────────────────────────────
    /// </summary>
    public static class AdminUI
    {
        public static void DisplayAdminProfile(AdminProfile profile)
        {
            DisplayAdminProfile(profile, null);
        }

        public static void DisplayAdminProfile(AdminProfile profile, User? user)
        {
            if (profile == null)
            {
                ConsoleHelper.PrintError("No admin profile data available to display.");
                return;
            }

            string indent = ConsoleHelper.GetIndent();
            const int W = 76;

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(indent + "╔" + new string('═', W) + "╗");
            
            Console.Write(indent + "║");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(ConsoleHelper.CenterPad("ADMINISTRATOR IDENTITY BADGE", W));
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("║");
            
            Console.WriteLine(indent + "╠" + new string('═', W) + "╣");

            // Admin IDs and Department
            PrintDetailRow("Admin ID", $"ADM-SYS-{profile.AdminId:D4}", ConsoleColor.DarkGray, W);
            PrintDetailRow("User ID", $"USR-{profile.UserId:D4}", ConsoleColor.DarkGray, W);
            PrintDetailRow("Department", profile.Department, ConsoleColor.White, W);

            // Privilege Level
            string privStr = (profile.PrivilegeLevel ?? "Standard").ToUpper();
            ConsoleColor privColor = privStr == "SUPERADMIN" || privStr == "OWNER" ? ConsoleColor.Red : ConsoleColor.Yellow;
            PrintDetailRow("Privilege Level", privStr, privColor, W);

            // Assigned Date
            PrintDetailRow("Assigned Since", ConsoleHelper.FormatDate(profile.AssignedDate), ConsoleColor.White, W);

            if (user != null)
            {
                Console.WriteLine(indent + "╟" + new string('─', W) + "╢");
                PrintDetailRow("Full Name", user.FullName, ConsoleColor.White, W);
                PrintDetailRow("Username", user.Username, ConsoleColor.Cyan, W);
                PrintDetailRow("Email Address", user.Email, ConsoleColor.White, W);
                PrintDetailRow("Contact Phone", user.Phone, ConsoleColor.White, W);
                
                string actStr = user.IsActive ? "ACTIVE ACCOUNT" : "SUSPENDED ACCOUNT";
                ConsoleColor actCol = user.IsActive ? ConsoleColor.Green : ConsoleColor.Red;
                PrintDetailRow("System Status", actStr, actCol, W);
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
    }
}
