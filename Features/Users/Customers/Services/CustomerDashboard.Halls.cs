using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VenueBookingSystem.Features.Halls;
using VenueBookingSystem.Features.Bookings;
using VenueBookingSystem.Shared;

namespace VenueBookingSystem.Features.Customers
{
    public partial class CustomerDashboard
    {
        private async Task BrowseHallsAsync()
        {
            _navigationHistory.Push("Browse Halls");
            bool back = false;
            while (!back)
            {
                Console.Clear();
                ConsoleHelper.PrintHeader("Browse Available Halls");

            
                var allHalls = await _hallService.GetAllHallsAsync(1, 1000);
                if (allHalls.Count == 0)
                {
                    ConsoleHelper.PrintWarning("No halls are currently available.");
                    ConsoleHelper.PressAnyKey();
                    back = true;
                    break;
                }

   
                ConsoleHelper.PrintSubHeader("Available Halls");
                ConsoleHelper.PrintTable(allHalls, HallService.TableColumns);

                string[] options = {
                    "Book a Hall",
                    "View Hall Details & Bookings"
                };

                ConsoleHelper.PrintMenu("Options", options);
                int choice = ValidationHelper.ReadMenuChoice(2);

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
                }
            }
            _navigationHistory.Pop();
        }

        private async Task ViewHallDetailsWithBookingsAsync()
        {
            var halls = await _hallService.GetAllHallsAsync(1, 1000);
            if (halls.Count == 0)
            {
                ConsoleHelper.PrintWarning("No halls available.");
                ConsoleHelper.PressAnyKey();
                return;
            }

            int selection = ConsoleHelper.ShowPaginatedTable(halls, HallService.TableColumns, "Select Hall to View Details");
            if (selection == 0) return;

            var hall = halls[selection - 1];

          
            ShowHallDetails(hall, clearScreen: true);

            Console.WriteLine();
            ConsoleHelper.PrintSubHeader($"Booking Records for: {hall.HallName}");

            var allBookings = await _bookingService.GetAllBookingsForHallAsync(hall.HallId);
            if (allBookings == null || allBookings.Count == 0)
            {
                ConsoleHelper.PrintWarning("No bookings available for this hall.");
            }
            else
            {
                ConsoleHelper.PrintTable(allBookings, BookingService.TableColumns);
            }

            ConsoleHelper.PressAnyKey();
        }

        private async Task ViewHallDetailsDirectAsync()
        {
            var halls = await _hallService.GetAllHallsAsync(1, 1000);
            int selection = ConsoleHelper.ShowPaginatedTable(halls, HallService.TableColumns, "Select Hall to View Details");
            if (selection == 0) return;

            var hall = halls[selection - 1];
            ShowHallDetails(hall, clearScreen: true);
            ConsoleHelper.PressAnyKey();
        }

        private static void ShowHallDetails(Hall hall, bool clearScreen = true)
        {
            if (clearScreen) Console.Clear();
            else Console.WriteLine();

            ConsoleHelper.PrintHeader($"Hall Details — {hall.HallName}");
            ConsoleHelper.PrintReadOnlyId("Hall ID", hall.HallId.ToString());
            ConsoleHelper.PrintNonEditableField("Hall Name", hall.HallName);
            ConsoleHelper.PrintNonEditableField("Type", hall.GetHallTypeDisplay());
            ConsoleHelper.PrintNonEditableField("Capacity", $"{hall.Capacity} persons");
            ConsoleHelper.PrintNonEditableField("Price", ConsoleHelper.FormatCurrency(hall.PricePerHour));
            ConsoleHelper.PrintNonEditableField("Description", hall.Description);
            ConsoleHelper.PrintNonEditableField("Location", hall.Location);
            ConsoleHelper.PrintNonEditableField("Air Conditioning", ConsoleHelper.FormatBool(hall.HasAC));
            ConsoleHelper.PrintNonEditableField("Projector", ConsoleHelper.FormatBool(hall.HasProjector));
            ConsoleHelper.PrintNonEditableField("WiFi", ConsoleHelper.FormatBool(hall.HasWifi));
        }
    }
}
