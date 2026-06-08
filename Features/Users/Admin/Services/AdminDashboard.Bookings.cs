using VenueBookingSystem.Features.Authentication;
using VenueBookingSystem.Features.Bookings;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VenueBookingSystem.Shared;

namespace VenueBookingSystem.Features.Admin
{
    public partial class AdminDashboard
    {
        private async Task ShowBookingManagementAsync()
        {
            bool back = false;
            while (!back)
            {
                Console.Clear();
                ConsoleHelper.PrintHeader("Booking Management");

                string[] options = {
                    "Confirm Booking",
                    "Reject Booking",
                    "View Booking Details",
                    "View Booking History (Audit Trail)",
                    "Filter Bookings by Date Range"
                };

                ConsoleHelper.PrintMenu("Booking Management", options);
                int choice = ValidationHelper.ReadMenuChoice(5);

                switch (choice)
                {
                    case 0: back = true; break;
                    case 1: await ConfirmBookingDirectAsync(); break;
                    case 2: await RejectBookingDirectAsync(); break;
                    case 3: await ViewBookingDetailsDirectAsync(); break;
                    case 4: await ViewBookingHistoryDirectAsync(); break;
                    case 5: await ViewBookingsByDateRangeAsync(); break;
                }
            }
        }

        private async Task ConfirmBookingDirectAsync()
        {
            var bookings = await _bookings.GetPendingBookingsAsync();


            if (bookings.Count == 0)
            {
                Console.Clear();
                ConsoleHelper.PrintHeader("Confirm Booking");
                ConsoleHelper.PrintWarning("No pending bookings available to confirm.");
                ConsoleHelper.PressAnyKey();
                return;
            }

            int selection = ConsoleHelper.ShowPaginatedTable(bookings, BookingService.TableColumns, "Select Booking to Confirm");
            if (selection == 0) return;

            var booking = bookings[selection - 1];

            Console.Clear();
            ConsoleHelper.PrintWarning($"You are about to confirm booking #{booking.BookingId} for {booking.CustomerName}");

            if (!ConsoleHelper.Confirm("Confirm this booking?")) return;

            bool success = await _bookings.UpdateBookingStatusAsync(booking.BookingId, BookingStatus.Confirmed, _auth.CurrentUser!.Username);
            if (success)
                ConsoleHelper.PrintSuccess("Booking confirmed successfully.");
            else
                ConsoleHelper.PrintError("Failed to confirm booking.");

            ConsoleHelper.PressAnyKey();
        }

        private async Task RejectBookingDirectAsync()
        {
            var bookings = await _bookings.GetPendingBookingsAsync();

            if (bookings.Count == 0)
            {
                Console.Clear();
                ConsoleHelper.PrintHeader("Reject Booking");
                ConsoleHelper.PrintWarning("No pending bookings available to reject.");
                ConsoleHelper.PressAnyKey();
                return;
            }

            int selection = ConsoleHelper.ShowPaginatedTable(bookings, BookingService.TableColumns, "Select Booking to Reject");
            if (selection == 0) return;

            var booking = bookings[selection - 1];

            Console.Clear();
            ConsoleHelper.PrintWarning($"You are about to reject booking #{booking.BookingId} for {booking.CustomerName}");

            if (!ConsoleHelper.Confirm("Reject this booking?")) return;

            bool success = await _bookings.UpdateBookingStatusAsync(booking.BookingId, BookingStatus.Rejected, _auth.CurrentUser!.Username);
            if (success)
                ConsoleHelper.PrintSuccess("Booking rejected successfully.");
            else
                ConsoleHelper.PrintError("Failed to reject booking.");

            ConsoleHelper.PressAnyKey();
        }

        private async Task ViewBookingDetailsDirectAsync()
        {
            var bookings = await _bookings.GetAllBookingsAsync(1, 10000);

        
            if (bookings.Count == 0)
            {
                Console.Clear();
                ConsoleHelper.PrintHeader("View Booking Details");
                ConsoleHelper.PrintWarning("No bookings found in the system.");
                ConsoleHelper.PressAnyKey();
                return;
            }

            int selection = ConsoleHelper.ShowPaginatedTable(bookings, BookingService.TableColumns, "Select Booking to View Details");
            if (selection == 0) return;

            var booking = bookings[selection - 1];
            ShowBookingDetails(booking);
            ConsoleHelper.PressAnyKey();
        }

        private async Task ViewBookingHistoryDirectAsync()
        {
            var bookings = await _bookings.GetAllBookingsAsync(1, 10000);

      
            if (bookings.Count == 0)
            {
                Console.Clear();
                ConsoleHelper.PrintHeader("Booking History");
                ConsoleHelper.PrintWarning("No bookings found in the system.");
                ConsoleHelper.PressAnyKey();
                return;
            }

            int selection = ConsoleHelper.ShowPaginatedTable(bookings, BookingService.TableColumns, "Select Booking to View History");
            if (selection == 0) return;

            var booking = bookings[selection - 1];

            var history = await _history.GetHistoryByBookingAsync(booking.BookingId);

            Console.Clear();
            ConsoleHelper.PrintHeader($"Audit Trail — Booking #{booking.BookingId}");

            if (history.Count == 0)
            {
                ConsoleHelper.PrintWarning("No audit history records found for this booking.");
            }
            else
            {
               
                foreach (var h in history)
                {
                    ConsoleHelper.PrintNonEditableField("Changed At", ConsoleHelper.FormatDateTime(h.ChangedAt));
                    ConsoleHelper.PrintNonEditableField("Status", h.NewStatus);
                    ConsoleHelper.PrintNonEditableField("Changed By", h.ChangedBy);
                    if (!string.IsNullOrWhiteSpace(h.Remarks))
                        ConsoleHelper.PrintNonEditableField("Remarks", h.Remarks);
                    Console.WriteLine();
                }
            }

            ConsoleHelper.PressAnyKey();
        }

        private async Task ViewBookingsByDateRangeAsync()
        {
            Console.Clear();
            ConsoleHelper.PrintHeader("Bookings by Date Range");

      
            DateTime start;
            while (true)
            {
                start = ValidationHelper.ReadFutureDate("Start Date & Time");
                if (start == DateTime.MinValue) return;
                ConsoleHelper.PrintSuccess($"  Start date accepted: {ConsoleHelper.FormatDateTime(start)}");
                break;
            }

            DateTime end;
            while (true)
            {
                end = ValidationHelper.ReadFutureDate("End Date & Time");
                if (end == DateTime.MinValue) return;
                if (end <= start)
                {
                    ConsoleHelper.PrintError("End time must be after the start time. Please re-enter.");
                    continue;
                }
                ConsoleHelper.PrintSuccess($"  End date accepted: {ConsoleHelper.FormatDateTime(end)}");
                break;
            }

            var rangeBookings = await _bookings.GetBookingsByDateRangeAsync(start, end);

   
            if (rangeBookings.Count == 0)
            {
                ConsoleHelper.PrintWarning($"No bookings found between {ConsoleHelper.FormatDateTime(start)} and {ConsoleHelper.FormatDateTime(end)}.");
                ConsoleHelper.PressAnyKey();
                return;
            }

            ConsoleHelper.ShowPaginatedTable(
                rangeBookings,
                BookingService.TableColumns,
                $"Bookings: {ConsoleHelper.FormatDateTime(start)} to {ConsoleHelper.FormatDateTime(end)}");
        }

        private static void ShowBookingDetails(Booking booking)
        {
            Console.Clear();
            ConsoleHelper.PrintHeader($"Booking Details — #{booking.BookingId}");
            ConsoleHelper.PrintReadOnlyId("Booking ID", booking.BookingId.ToString());
            ConsoleHelper.PrintNonEditableField("Hall", booking.HallName);
            ConsoleHelper.PrintNonEditableField("Customer", booking.CustomerName);
            ConsoleHelper.PrintNonEditableField("Start Time", ConsoleHelper.FormatDateTime(booking.StartDateTime));
            ConsoleHelper.PrintNonEditableField("End Time", ConsoleHelper.FormatDateTime(booking.EndDateTime));
            ConsoleHelper.PrintNonEditableField("Duration", $"{booking.TotalHours} hrs");
            ConsoleHelper.PrintNonEditableField("Total Amount", ConsoleHelper.FormatCurrency(booking.TotalAmount));
            ConsoleHelper.PrintNonEditableField("Status", booking.GetStatusDisplay());
            ConsoleHelper.PrintNonEditableField("Created At", ConsoleHelper.FormatDateTime(booking.CreatedAt));
        }
    }
}
