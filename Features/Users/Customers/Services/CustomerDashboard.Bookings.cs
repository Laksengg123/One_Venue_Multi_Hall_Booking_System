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
            // ── Policy Gate (BOOKING FLOW) ─────────────────────────────────────
            // Show ONLY the Booking Policy before the hall list is shown.
            Console.Clear();
            ConsoleHelper.PrintHeader("Booking Policy");
            if (!PolicyService.ShowBookingPolicyOnly()) return;

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
                ConsoleHelper.PrintNonEditableField("Capacity",      $"{hall.Capacity} persons");
                ConsoleHelper.PrintNonEditableField("Price",         ConsoleHelper.FormatCurrency(hall.PricePerHour) + " / hr");
                Console.WriteLine();

                // ── Purpose ────────────────────────────────────────────────────
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

                // ── Guest Count ────────────────────────────────────────────────
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

                // ── Dates ──────────────────────────────────────────────────────
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

                // ── Availability ───────────────────────────────────────────────
                if (!await _bookingService.CheckAvailabilityAsync(hall.HallId, start, end))
                {
                    ConsoleHelper.PrintError("This hall is not available for the selected dates.");
                    ConsoleHelper.PrintInfo("Please try a different date range or choose another hall.");
                    ConsoleHelper.PressAnyKey();
                    return;
                }

                double  hours       = (end - start).TotalHours;
                decimal baseTotal   = (decimal)hours * hall.PricePerHour;

                // ── 💰 Pricing Breakdown + Add-ons ─────────────────────────────
                Console.Clear();
                ConsoleHelper.PrintHeader("💰  Pricing Breakdown");
                ShowPricingBreakdown(hall.HallName, hall.PricePerHour, hours, baseTotal, guestCount);

                decimal addOnTotal  = 0m;
                bool    hasCatering = false;
                bool    hasDecor    = false;

                // Optional: Catering
                decimal cateringCost = 500m * guestCount;
                Console.WriteLine();
                ConsoleHelper.PrintInfo($"🍽️  Catering add-on: {ConsoleHelper.FormatCurrency(500m)} × {guestCount} guests = {ConsoleHelper.FormatCurrency(cateringCost)}");
                if (ConsoleHelper.Confirm("Add Catering?"))
                {
                    hasCatering  = true;
                    addOnTotal  += cateringCost;
                    ConsoleHelper.PrintSuccess($"  Catering added: +{ConsoleHelper.FormatCurrency(cateringCost)}");
                }

                // Optional: Decoration
                const decimal decorCost = 5000m;
                ConsoleHelper.PrintInfo($"🎊  Decoration add-on: {ConsoleHelper.FormatCurrency(decorCost)} flat fee");
                if (ConsoleHelper.Confirm("Add Decoration?"))
                {
                    hasDecor    = true;
                    addOnTotal += decorCost;
                    ConsoleHelper.PrintSuccess($"  Decoration added: +{ConsoleHelper.FormatCurrency(decorCost)}");
                }

                decimal totalAmount = baseTotal + addOnTotal;

                // ── Final Booking Summary ──────────────────────────────────────
                Console.Clear();
                ConsoleHelper.PrintHeader("Booking Summary");
                ConsoleHelper.PrintNonEditableField("Hall",          hall.HallName);
                ConsoleHelper.PrintNonEditableField("From",          ConsoleHelper.FormatDateTime(start));
                ConsoleHelper.PrintNonEditableField("To",            ConsoleHelper.FormatDateTime(end));
                ConsoleHelper.PrintNonEditableField("Duration",      $"{hours:F1} hours");
                ConsoleHelper.PrintNonEditableField("Base Amount",   ConsoleHelper.FormatCurrency(baseTotal));
                if (hasCatering)
                    ConsoleHelper.PrintNonEditableField("Catering",  ConsoleHelper.FormatCurrency(cateringCost));
                if (hasDecor)
                    ConsoleHelper.PrintNonEditableField("Decoration", ConsoleHelper.FormatCurrency(decorCost));
                ConsoleHelper.PrintNonEditableField("TOTAL AMOUNT",  ConsoleHelper.FormatCurrency(totalAmount));
                Console.WriteLine();

                if (!ConsoleHelper.Confirm("Confirm and place booking?")) return;

                var newBooking = new Booking(
                    BookingId:     0,
                    CustomerId:    _currentUser!.UserId,
                    CustomerName:  _currentUser.FullName,
                    HallId:        hall.HallId,
                    HallName:      hall.HallName,
                    StartDateTime: start,
                    EndDateTime:   end,
                    TotalHours:    (decimal)hours,
                    TotalAmount:   totalAmount,
                    Status:        BookingStatus.Pending,
                    Purpose:       purpose,
                    GuestCount:    guestCount,
                    CreatedAt:     DateTime.Now,
                    UpdatedAt:     DateTime.Now
                );

                int newBookingId = await _bookingService.CreateBookingAsync(newBooking);

                if (newBookingId > 0)
                {
                    ConsoleHelper.PrintSuccess("Booking placed successfully!");
                    Console.WriteLine();
                    if (ConsoleHelper.Confirm("Would you like to make a payment for this booking now?"))
                        await ProcessPaymentForBookingIdAsync(newBookingId, totalAmount, hall.HallName);
                    else
                        ConsoleHelper.PrintInfo("Status: Pending — please wait for admin confirmation, or proceed to payment later.");
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

        // ─────────────────────────────────────────────────────────────────────
        // 💰 PRICING BREAKDOWN TABLE
        // ─────────────────────────────────────────────────────────────────────
        private static void ShowPricingBreakdown(
            string  hallName,
            decimal pricePerHour,
            double  hours,
            decimal baseTotal,
            int     guestCount)
        {
            string indent   = ConsoleHelper.GetIndent();
            const int W     = 60;

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(indent + "╔" + new string('═', W) + "╗");

            Console.Write(indent + "║");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(ConsoleHelper.CenterPad("💰  PRICING BREAKDOWN", W));
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("║");

            Console.WriteLine(indent + "╠" + new string('═', W) + "╣");

            PrintBreakdownRow("Hall",         hallName,                                         ConsoleColor.Cyan,   W);
            PrintBreakdownRow("Rate",         ConsoleHelper.FormatCurrency(pricePerHour) + " / hr", ConsoleColor.White, W);
            PrintBreakdownRow("Duration",     $"{hours:F1} hours",                               ConsoleColor.White,  W);
            PrintBreakdownRow("Guests",       $"{guestCount} persons",                           ConsoleColor.White,  W);

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(indent + "╟" + new string('─', W) + "╢");

            PrintBreakdownRow("Base Total",   ConsoleHelper.FormatCurrency(baseTotal),           ConsoleColor.Green,  W);

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(indent + "╟" + new string('─', W) + "╢");

            PrintBreakdownRow("🍽️  Catering", "+Rs.500 per guest  (optional)",                  ConsoleColor.Yellow, W);
            PrintBreakdownRow("🎊  Decoration", "+Rs.5,000 flat fee  (optional)",               ConsoleColor.Yellow, W);

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(indent + "╚" + new string('═', W) + "╝");
            Console.ResetColor();
        }

        private static void PrintBreakdownRow(string label, string value, ConsoleColor valColor, int width)
        {
            string indent    = ConsoleHelper.GetIndent();
            string labelPart = $"  {label,-15} : ";
            int remaining    = width - labelPart.Length - 2;

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

        // ─────────────────────────────────────────────────────────────────────
        // 📅 MY BOOKINGS MENU
        // ─────────────────────────────────────────────────────────────────────
        private async Task ViewMyBookingsAsync()
        {
            _navigationHistory.Push("My Bookings");
            bool back = false;
            while (!back)
            {
                Console.Clear();
                ConsoleHelper.PrintHeader("My Bookings");

                string[] options =
                {
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
                            int selection = ConsoleHelper.ShowPaginatedTable(bookings, BookingService.TableColumns, "My Bookings");
                            if (selection > 0)
                            {
                                VenueBookingSystem.Features.Bookings.UI.BookingUI.DisplayBookingDetails(bookings[selection - 1]);
                                ConsoleHelper.PressAnyKey();
                            }
                        }
                        break;
                    case 4:
                        await CancelBookingDirectAsync();
                        break;
                }
            }
            _navigationHistory.Pop();
        }

        // ─────────────────────────────────────────────────────────────────────
        // ❌ CANCEL BOOKING — with detail card + refund/cancel policy gate
        // ─────────────────────────────────────────────────────────────────────
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

            // ── Show Cancellation Detail Card ─────────────────────────────────
            Console.Clear();
            ConsoleHelper.PrintHeader("Cancellation Detail");
            ShowCancellationDetailCard(booking);

            // ── Show Cancellation Policy only ─────────────────────────────────
            if (!PolicyService.ShowCancellationPolicyOnly()) return;

            if (!ConsoleHelper.Confirm($"Confirm cancellation of booking #{booking.BookingId}?")) return;

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

        // ─────────────────────────────────────────────────────────────────────
        // 🃏 CANCELLATION DETAIL CARD — shows booking + estimated refund
        // ─────────────────────────────────────────────────────────────────────
        private static void ShowCancellationDetailCard(Booking booking)
        {
            string indent = ConsoleHelper.GetIndent();
            const int W   = 76;

            double daysLeft = (booking.StartDateTime - DateTime.Now).TotalDays;
            decimal estimatedRefund;
            string  tierLabel;
            ConsoleColor tierColor;

            if (daysLeft >= 7)
            {
                estimatedRefund = Math.Round(booking.TotalAmount * 0.80m, 2);
                tierLabel       = "🟢 80% refund (≥ 7 days notice)";
                tierColor       = ConsoleColor.Green;
            }
            else if (daysLeft >= 3)
            {
                estimatedRefund = Math.Round(booking.TotalAmount * 0.50m, 2);
                tierLabel       = "🟡 50% refund (3–6 days notice)";
                tierColor       = ConsoleColor.Yellow;
            }
            else
            {
                estimatedRefund = 0m;
                tierLabel       = "🔴 No refund (< 3 days notice)";
                tierColor       = ConsoleColor.Red;
            }

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(indent + "╔" + new string('═', W) + "╗");

            Console.Write(indent + "║");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(ConsoleHelper.CenterPad($"CANCELLATION REQUEST — Booking #{booking.BookingId}", W));
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("║");

            Console.WriteLine(indent + "╠" + new string('═', W) + "╣");

            // Customer & Hall
            PrintCancelDetailRow("Customer",      booking.CustomerName,                            ConsoleColor.White,   W);
            PrintCancelDetailRow("Hall",          booking.HallName,                               ConsoleColor.White,   W);
            PrintCancelDetailRow("Event Start",   ConsoleHelper.FormatDateTime(booking.StartDateTime), ConsoleColor.Yellow, W);
            PrintCancelDetailRow("Event End",     ConsoleHelper.FormatDateTime(booking.EndDateTime),   ConsoleColor.Yellow, W);
            PrintCancelDetailRow("Duration",      booking.GetDurationDisplay(),                   ConsoleColor.White,   W);
            PrintCancelDetailRow("Guests",        $"{booking.GuestCount} persons",               ConsoleColor.White,   W);
            PrintCancelDetailRow("Purpose",       booking.Purpose,                               ConsoleColor.Gray,    W);

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(indent + "╟" + new string('─', W) + "╢");

            // Financial summary
            PrintCancelDetailRow("Total Paid",     ConsoleHelper.FormatCurrency(booking.TotalAmount), ConsoleColor.White,  W);
            PrintCancelDetailRow("Days Remaining", $"{daysLeft:F0} days until event",                 ConsoleColor.White,  W);
            PrintCancelDetailRow("Refund Tier",    tierLabel,                                         tierColor,           W);
            PrintCancelDetailRow("Est. Refund",    ConsoleHelper.FormatCurrency(estimatedRefund),     tierColor,           W);

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(indent + "╚" + new string('═', W) + "╝");
            Console.ResetColor();
            Console.WriteLine();
        }

        private static void PrintCancelDetailRow(string label, string value, ConsoleColor valColor, int width)
        {
            string indent    = ConsoleHelper.GetIndent();
            string labelPart = $"  {label,-15} : ";
            int remaining    = width - labelPart.Length - 2;

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
    }
}
