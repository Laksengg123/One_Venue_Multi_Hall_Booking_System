using VenueBookingSystem.Features.Authentication;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VenueBookingSystem.Features.Bookings;
using VenueBookingSystem.Features.Payments;
using VenueBookingSystem.Shared;

namespace VenueBookingSystem.Features.Customers
{
    public partial class CustomerDashboard
    {
        private async Task ShowPaymentMenuAsync()
        {
            _navigationHistory.Push("Payments");
            bool back = false;

            while (!back)
            {
                Console.Clear();
                ConsoleHelper.PrintHeader("Payments");

                string[] options = {
                    "View My Payments",
                    "View My Refunds / Cancellations"
                };

                ConsoleHelper.PrintMenu("Payment Options", options);
                int choice = ValidationHelper.ReadMenuChoice(2);

                switch (choice)
                {
                    case 0:
                        back = true;
                        break;
                    case 1:
                        await ViewMyPaymentsAsync();
                        break;
                    case 2:
                        await ViewMyRefundsAsync();
                        break;
                }
            }

            _navigationHistory.Pop();
        }

        private async Task ViewMyPaymentsAsync()
        {
            var payments = await _paymentService.GetPaymentsByCustomerAsync(_currentUser.UserId);
            if (payments.Count == 0)
            {
                Console.Clear();
                ConsoleHelper.PrintHeader("My Payments");
                ConsoleHelper.PrintWarning("You have no payments yet.");
                ConsoleHelper.PressAnyKey();
                return;
            }
            int selection = ConsoleHelper.ShowPaginatedTable(payments, PaymentService.TableColumns, "My Payments");
            if (selection > 0)
            {
                var payment = payments[selection - 1];
                var booking = await _bookingService.GetBookingByIdAsync(payment.BookingId);
                if (booking != null)
                {
                    Console.Clear();
                    ConsoleHelper.PrintHeader("Payment Receipt Details");
                    VenueBookingSystem.Features.Payments.UI.PaymentUI.DisplayReceipt(payment, booking);
                }
                else
                {
                    ConsoleHelper.PrintError("Could not retrieve booking details for this payment.");
                }
                ConsoleHelper.PressAnyKey();
            }
        }

        private async Task ViewMyRefundsAsync()
        {
            var refunds = await _paymentService.GetCancellationsByCustomerAsync(_currentUser.UserId);
            if (refunds.Count == 0)
            {
                Console.Clear();
                ConsoleHelper.PrintHeader("My Refunds & Cancellations");
                ConsoleHelper.PrintWarning("You have no cancellation or refund requests.");
                ConsoleHelper.PressAnyKey();
                return;
            }
            int selection = ConsoleHelper.ShowPaginatedTable(refunds, PaymentService.RefundColumns, "My Refunds & Cancellations");
            if (selection == 0) return;

            var req = refunds[selection - 1];
            Console.Clear();
            ConsoleHelper.PrintHeader($"Cancellation Request Details — #{req.CancellationId}");
            ShowCustomerCancellationDetailCard(req);
            ConsoleHelper.PressAnyKey();
        }

        private static void ShowCustomerCancellationDetailCard(RefundCandidate c)
        {
            string indent = ConsoleHelper.GetIndent();
            const int W   = 76;

            decimal      approxRefund = c.RefundAmount;
            string       refundTierLabel;
            ConsoleColor refundColor;

            if (approxRefund > 0 && c.RefundStatus == "Processed")
            {
                refundTierLabel = "Processed / Transferred";
                refundColor     = ConsoleColor.Green;
            }
            else if (approxRefund > 0)
            {
                refundTierLabel = $"Pending Approval ({ConsoleHelper.FormatCurrency(approxRefund)})";
                refundColor     = ConsoleColor.Yellow;
            }
            else
            {
                refundTierLabel = "No Refund (per Policy)";
                refundColor     = ConsoleColor.Red;
            }

            string statusText = c.CancellationStatus;
            ConsoleColor statusColor = statusText switch
            {
                "Approved" => ConsoleColor.Green,
                "Rejected" => ConsoleColor.Red,
                _          => ConsoleColor.Yellow
            };

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(indent + "╔" + new string('═', W) + "╗");

            Console.Write(indent + "║");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(ConsoleHelper.CenterPad($"CANCELLATION REQUEST #{c.CancellationId}", W));
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("║");
            Console.WriteLine(indent + "╠" + new string('═', W) + "╣");

            PrintCustomerCancelRow("Cancellation #", c.CancellationId.ToString(),       ConsoleColor.DarkGray, W);
            PrintCustomerCancelRow("Booking ID",     c.BookingId.ToString(),            ConsoleColor.White,    W);
            PrintCustomerCancelRow("Hall Name",      c.HallName,                         ConsoleColor.Cyan,     W);
            PrintCustomerCancelRow("Cancelled On",   ConsoleHelper.FormatDateTime(c.CancellationDate), ConsoleColor.Yellow, W);
            PrintCustomerCancelRow("Reason",         c.Reason,                          ConsoleColor.Gray,     W);
            PrintCustomerCancelRow("Request Status", statusText,                        statusColor,           W);
            PrintCustomerCancelRow("Admin Remarks",  string.IsNullOrWhiteSpace(c.AdminRemarks) ? "—" : c.AdminRemarks, ConsoleColor.Gray, W);

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(indent + "╟" + new string('─', W) + "╢");

            PrintCustomerCancelRow("Refund Amount",  ConsoleHelper.FormatCurrency(approxRefund), ConsoleColor.Green, W);
            PrintCustomerCancelRow("Refund Status",  refundTierLabel,                            refundColor,    W);

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(indent + "╚" + new string('═', W) + "╝");
            Console.ResetColor();
            Console.WriteLine();
        }

        private static void PrintCustomerCancelRow(string label, string value, ConsoleColor valColor, int width)
        {
            string indent = ConsoleHelper.GetIndent();
            string labelPart = $"  {label,-16} : ";
            int remaining = width - labelPart.Length - 2;

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write(indent + "║");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(labelPart);
            Console.ForegroundColor = valColor;
            string valPart = value.Length > remaining ? value[..remaining] : value.PadRight(remaining);
            Console.Write(valPart);
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("║");
        }

        private async Task MakePaymentDirectAsync()
        {
            var bookings = await _bookingService.GetBookingsByCustomerAsync(_currentUser.UserId, 1, 1000);
            var unpaidBookings = new List<Booking>();
            foreach(var b in bookings) if(b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed) unpaidBookings.Add(b);

            int selection = ConsoleHelper.ShowPaginatedTable(unpaidBookings, BookingService.TableColumns, "Select Booking to Pay");
            if (selection == 0) return;

            var booking = unpaidBookings[selection - 1];

            await ProcessPaymentForBookingIdAsync(booking.BookingId, booking.TotalAmount, booking.HallName);
            ConsoleHelper.PressAnyKey();
        }

        private async Task<bool> ProcessPaymentForBookingIdAsync(int bookingId, decimal amount, string hallName)
        {
            Console.Clear();
            ConsoleHelper.PrintHeader($"Payment for Booking #{bookingId}");
            ConsoleHelper.PrintInfo($"Hall: {hallName}");
            ConsoleHelper.PrintInfo($"Total Amount: {ConsoleHelper.FormatCurrency(amount)}");
            Console.WriteLine();

            ConsoleHelper.PrintInfo("Select Payment Method:");
            string[] methods = { "Cash", "Card", "UPI", "Bank Transfer" };
            ConsoleHelper.PrintMenu("Payment Method", methods, showBack: true);

            int methodChoice = ValidationHelper.ReadMenuChoice(4);
            if (methodChoice == 0) return false;

            PaymentMethod method = (PaymentMethod)methodChoice;

            if (!ConsoleHelper.Confirm($"Proceed with {method} payment of {ConsoleHelper.FormatCurrency(amount)}?")) return false;

            bool success = await _paymentService.ProcessPaymentAsync(bookingId, method);
            
            if (success)
                ConsoleHelper.PrintSuccess("Payment successful!");
            else
                ConsoleHelper.PrintError("Payment failed. Please try again later.");
            
            return success;
        }
    }
}



