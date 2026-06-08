using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VenueBookingSystem.Features.Authentication;
using VenueBookingSystem.Features.Bookings;
using VenueBookingSystem.Shared;

namespace VenueBookingSystem.Features.Customers
{
    public partial class CustomerDashboard
    {
        private async Task BookHallAsync()
        {
            await BookHallDirectAsync();
        }

        private async Task BookHallDirectAsync()
        {
            var halls = await _hallService.GetAllHallsAsync();

            if (halls.Count == 0)
            {
                ConsoleHelper.PrintWarning("No halls are currently available for booking.");
                ConsoleHelper.PressAnyKey();
                return;
            }

            int selection = ConsoleHelper.ShowPaginatedTable(halls, Features.Halls.HallService.TableColumns, "Select Hall to Book");
            if (selection == 0) return;

            var hall = halls[selection - 1];

            try
            {
                Console.Clear();
                ConsoleHelper.PrintHeader("Book a Hall");
                ConsoleHelper.PrintNonEditableField("Selected Hall", hall.HallName);
                ConsoleHelper.PrintNonEditableField("Capacity", $"{hall.Capacity} persons");
                ConsoleHelper.PrintNonEditableField("Price", ConsoleHelper.FormatCurrency(hall.PricePerHour));
                Console.WriteLine();

                string purpose;
                while (true)
                {
                    purpose = ConsoleHelper.ReadInput("Purpose of Booking", "e.g. Conference, Wedding, Training (5-200 chars)", required: true, allowBack: true);
                    if (purpose == ConsoleHelper.BACK_COMMAND) return;
                    if (!ValidationHelper.ValidatePurpose(purpose))
                    {
                        ConsoleHelper.PrintError("Purpose must be between 5 and 200 characters. Please try again.");
                        continue;
                    }
                    ConsoleHelper.PrintSuccess("  Purpose accepted.");
                    break;
                }

                int guestCount;
                while (true)
                {
                    string gcStr = ConsoleHelper.ReadInput("Expected Guest Count", $"1 - {hall.Capacity}", required: true, allowBack: true);
                    if (gcStr == ConsoleHelper.BACK_COMMAND) return;
                    var (valid, errMsg) = ValidationHelper.ValidateGuestCapacity(gcStr, hall.Capacity);
                    if (!valid)
                    {
                        ConsoleHelper.PrintError(errMsg + " Please try again.");
                        continue;
                    }
                    guestCount = int.Parse(gcStr);
                    ConsoleHelper.PrintSuccess($"  Guest count accepted: {guestCount}");
                    break;
                }

                Console.WriteLine();

                DateTime start = ValidationHelper.ReadFutureDate("Start Date & Time");
                if (start == DateTime.MinValue) return;
                ConsoleHelper.PrintSuccess($"  Start date accepted: {ConsoleHelper.FormatDateTime(start)}");

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

                if (!await _bookingService.CheckAvailabilityAsync(hall.HallId, start, end))
                {
                    ConsoleHelper.PrintError("This hall is not available for the selected dates.");
                    ConsoleHelper.PrintInfo("Please try a different date range or choose another hall.");
                    ConsoleHelper.PressAnyKey();
                    return;
                }

                double hours = (end - start).TotalHours;
                decimal totalAmount = (decimal)hours * hall.PricePerHour;

                Console.Clear();
                ConsoleHelper.PrintHeader("Booking Summary");
                ConsoleHelper.PrintNonEditableField("Hall", hall.HallName);
                ConsoleHelper.PrintNonEditableField("From", ConsoleHelper.FormatDateTime(start));
                ConsoleHelper.PrintNonEditableField("To", ConsoleHelper.FormatDateTime(end));
                ConsoleHelper.PrintNonEditableField("Duration", $"{hours:F1} hours");
                ConsoleHelper.PrintNonEditableField("Total Amount", ConsoleHelper.FormatCurrency(totalAmount));
                Console.WriteLine();

                if (!ConsoleHelper.Confirm("Confirm and place booking?")) return;

                var newBooking = new Booking(
                    BookingId: 0,
                    CustomerId: _currentUser!.UserId,
                    CustomerName: _currentUser.FullName,
                    HallId: hall.HallId,
                    HallName: hall.HallName,
                    StartDateTime: start,
                    EndDateTime: end,
                    TotalHours: (decimal)hours,
                    TotalAmount: totalAmount,
                    Status: BookingStatus.Pending,
                    Purpose: purpose,
                    GuestCount: guestCount,
                    CreatedAt: DateTime.Now,
                    UpdatedAt: DateTime.Now
                );

                int newBookingId = await _bookingService.CreateBookingAsync(newBooking);

                if (newBookingId > 0)
                {
                    ConsoleHelper.PrintSuccess("Booking placed successfully!");
                    Console.WriteLine();
                    if (ConsoleHelper.Confirm("Would you like to make a payment for this booking now?"))
                    {
                        await ProcessPaymentForBookingIdAsync(newBookingId, totalAmount, hall.HallName);
                    }
                    else
                    {
                        ConsoleHelper.PrintInfo("Status: Pending — please wait for admin confirmation, or proceed to payment later.");
                    }
                }
                else
                {
                    ConsoleHelper.PrintError("Failed to create booking. Please try again.");
                }
            }
            catch (Exception ex)
            {
                ConsoleHelper.PrintError($"An error occurred during booking: {ex.Message}");
            }

            ConsoleHelper.PressAnyKey();
        }

        private async Task ViewMyBookingsAsync()
        {
            _navigationHistory.Push("My Bookings");
            bool back = false;
            while (!back)
            {
                Console.Clear();
                ConsoleHelper.PrintHeader("My Bookings");

                string[] options = {
                    "Book a Hall",
                    "View Hall Details & Bookings",
                    "View All My Bookings",
                    "Cancel a Booking"
                };

                ConsoleHelper.PrintMenu("My Bookings", options);
                int choice = ValidationHelper.ReadMenuChoice(4);

                switch (choice)
                {
                    case 0:
                        back = true;
                        break;
                    case 1:
                        await BookHallDirectAsync();
                        break;
                    case 2:
                        await ViewHallDetailsWithBookingsAsync();
                        break;
                    case 3:
                        var bookings = await _bookingService.GetBookingsByCustomerAsync(_currentUser.UserId, 1, 1000);
                        if (bookings.Count == 0)
                        {
                            Console.Clear();
                            ConsoleHelper.PrintHeader("My Bookings");
                            ConsoleHelper.PrintWarning("You have no bookings yet.");
                            ConsoleHelper.PressAnyKey();
                        }
                        else
                        {
                            ConsoleHelper.ShowPaginatedTable(bookings, BookingService.TableColumns, "My Bookings");
                        }
                        break;
                    case 4:
                        await CancelBookingDirectAsync();
                        break;
                }
            }
            _navigationHistory.Pop();
        }

        private async Task CancelBookingDirectAsync()
        {
            var bookings = await _bookingService.GetBookingsByCustomerAsync(_currentUser.UserId, 1, 1000);
            var activeBookings = new List<Booking>();
            foreach (var b in bookings)
            {
                if (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed)
                    activeBookings.Add(b);
            }

            if (activeBookings.Count == 0)
            {
                Console.Clear();
                ConsoleHelper.PrintHeader("Cancel a Booking");
                ConsoleHelper.PrintWarning("You have no pending or confirmed bookings available to cancel.");
                ConsoleHelper.PressAnyKey();
                return;
            }

            int selection = ConsoleHelper.ShowPaginatedTable(activeBookings, BookingService.TableColumns, "Select Booking to Cancel");
            if (selection == 0) return;

            var booking = activeBookings[selection - 1];

            Console.Clear();
            ConsoleHelper.PrintWarning($"You are about to cancel booking #{booking.BookingId} for Hall '{booking.HallName}'.");

            if (!ConsoleHelper.Confirm("Are you sure you want to cancel this booking?")) return;

            bool success = await _bookingService.CancelBookingAsync(booking.BookingId);

            if (success)
            {
                ConsoleHelper.PrintSuccess("Booking cancelled successfully.");
                ConsoleHelper.PrintInfo("A refund request has been added to the admin payment dashboard.");
            }
            else
                ConsoleHelper.PrintError("Failed to cancel booking. Please try again.");

            ConsoleHelper.PressAnyKey();
        }
    }
}
