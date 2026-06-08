using System;
using System.Collections.Generic;
using VenueBookingSystem.Features.Authentication;
using VenueBookingSystem.Features.Bookings;
using VenueBookingSystem.Shared;

namespace VenueBookingSystem.Features.Payments.UI
{
    /// <summary>
    /// DESIGN SPECIFICATIONS: Financial Transactions & Invoicing Interface
    /// ─────────────────────────────────────────────────────────────────────────────
    /// 1. LAYOUT & VISUAL HIERARCHY
    ///    - Tabular transaction lists dynamically adjust to terminal widths.
    ///    - Customer receipts model a physical invoice layout using a double-line boundary 
    ///      frame (width = 76) and dotted/single dividers.
    ///    - Financial amounts are strictly aligned to the right-side values for tabular grids.
    /// 
    /// 2. COLOR PALETTE & SIGNALS
    ///    - Paid / Completed: Green (financial transaction succeeded).
    ///    - Pending Payment: Yellow (payment due/in process).
    ///    - Failed / Refused: Red (transaction error).
    ///    - Refunded / Cancelled: Dark Cyan or Magenta.
    ///    - Primary text / values: White and Cyan.
    /// 
    /// 3. INTERACTIVE FEEDBACK
    ///    - Receipt includes reference ID, transaction tracking reference, and dates.
    /// ─────────────────────────────────────────────────────────────────────────────
    /// </summary>
    public static class PaymentUI
    {
        public static Dictionary<string, Func<Payment, string>> TableColumns => new()
        {
            ["ID"] = p => p.PaymentId.ToString(),
            ["Booking ID"] = p => p.BookingId.ToString(),
            ["Amount"] = p => ConsoleHelper.FormatCurrency(p.Amount),
            ["Method"] = p => p.GetMethodDisplay(),
            ["Status"] = p => p.GetStatusDisplay(),
            ["Transaction Ref"] = p => p.TransactionRef,
            ["Paid At"] = p => p.PaidAt.HasValue
                                   ? ConsoleHelper.FormatDateTime(p.PaidAt.Value)
                                   : "Not paid"
        };

        public static void DisplayPayments(List<Payment> payments, string title = "PAYMENTS")
        {
            if (payments.Count == 0)
            {
                ConsoleHelper.PrintWarning("No payments found.");
                return;
            }

            ConsoleHelper.PrintTable(payments, TableColumns, title);
        }

        public static void DisplayReceipt(Payment payment, Booking booking)
        {
            if (payment == null)
            {
                ConsoleHelper.PrintError("No payment data available to print receipt.");
                return;
            }

            string indent = ConsoleHelper.GetIndent();
            const int W = 76;

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(indent + "╔" + new string('═', W) + "╗");
            
            Console.Write(indent + "║");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(ConsoleHelper.CenterPad("OFFICIAL PAYMENT RECEIPT", W));
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("║");
            
            Console.WriteLine(indent + "╠" + new string('═', W) + "╣");

            // Receipt metadata
            PrintDetailRow("Receipt ID", $"PAY-REC-{payment.PaymentId:D5}", ConsoleColor.DarkGray, W);
            PrintDetailRow("Transaction Ref", payment.TransactionRef, ConsoleColor.Cyan, W);
            PrintDetailRow("Processed Date", payment.PaidAt.HasValue ? ConsoleHelper.FormatDateTime(payment.PaidAt.Value) : "Unpaid", ConsoleColor.White, W);
            PrintDetailRow("Payment Method", payment.GetMethodDisplay(), ConsoleColor.White, W);

            Console.WriteLine(indent + "╟" + new string('─', W) + "╢");

            // Booking relations
            PrintDetailRow("Booking ID", $"#{booking.BookingId}", ConsoleColor.DarkGray, W);
            PrintDetailRow("Customer", booking.CustomerName, ConsoleColor.White, W);
            PrintDetailRow("Hall Name", booking.HallName, ConsoleColor.White, W);
            PrintDetailRow("Event Purpose", booking.Purpose, ConsoleColor.Gray, W);
            PrintDetailRow("Event Date", booking.StartDateTime.ToString("dd MMMM yyyy"), ConsoleColor.White, W);
            PrintDetailRow("Duration", $"{booking.TotalHours} hours ({booking.StartDateTime:HH:mm} to {booking.EndDateTime:HH:mm})", ConsoleColor.White, W);

            Console.WriteLine(indent + "╟" + new string('─', W) + "╢");

            // Totals
            PrintDetailRow("Base Rate", $"{ConsoleHelper.FormatCurrency(booking.TotalAmount / booking.TotalHours)} / hour", ConsoleColor.White, W);
            PrintDetailRow("Total Hours", $"{booking.TotalHours} hrs", ConsoleColor.White, W);
            PrintDetailRow("Subtotal", ConsoleHelper.FormatCurrency(booking.TotalAmount), ConsoleColor.White, W);
            
            Console.WriteLine(indent + "╟" + new string('─', W) + "╢");

            PrintDetailRow("Total Paid", ConsoleHelper.FormatCurrency(payment.Amount), ConsoleColor.Green, W);

            // Colored Status
            string statusStr = payment.GetStatusDisplay().ToUpper();
            ConsoleColor statusColor = payment.Status switch
            {
                PaymentStatus.Completed => ConsoleColor.Green,
                PaymentStatus.Pending => ConsoleColor.Yellow,
                PaymentStatus.Failed => ConsoleColor.Red,
                PaymentStatus.Refunded => ConsoleColor.Magenta,
                _ => ConsoleColor.White
            };
            PrintDetailRow("Payment Status", statusStr, statusColor, W);

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
