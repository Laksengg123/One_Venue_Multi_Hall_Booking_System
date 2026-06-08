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
                    "View My Payments"
                };

                ConsoleHelper.PrintMenu("Payment Options", options);
                int choice = ValidationHelper.ReadMenuChoice(1);

                switch (choice)
                {
                    case 0:
                        back = true;
                        break;
                    case 1:
                        await ViewMyPaymentsAsync();
                        break;
                }
            }

            _navigationHistory.Pop();
        }

        private async Task ViewMyPaymentsAsync()
        {
            var payments = await _paymentService.GetPaymentsByCustomerAsync(_currentUser.UserId);
            ConsoleHelper.ShowPaginatedTable(payments, PaymentService.TableColumns, "My Payments");
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



