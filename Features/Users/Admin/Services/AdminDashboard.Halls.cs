using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VenueBookingSystem.Features.Halls;
using VenueBookingSystem.Shared;

namespace VenueBookingSystem.Features.Admin
{
    public partial class AdminDashboard
    {
        private async Task ShowHallManagementAsync()
        {
            bool back = false;
            while (!back)
            {
                Console.Clear();
                ConsoleHelper.PrintHeader("Hall Management");

                string[] options = {
                    "Add New Hall",
                    "Edit Hall",
                    "Search Halls",
                    "View Hall Details"
                };

                ConsoleHelper.PrintMenu("Hall Management", options);
                int choice = ValidationHelper.ReadMenuChoice(4);

                switch (choice)
                {
                    case 0: back = true; break;
                    case 1: await AddNewHallAsync(); break;
                    case 2: await EditHallDirectAsync(); break;
                    case 3: await SearchHallsAsync(); break;
                    case 4: await ViewHallDetailsDirectAsync(); break;
                }
            }
        }

        private async Task EditHallDirectAsync()
        {
            var halls = await _halls.GetAllHallsAsync();

          
            if (halls.Count == 0)
            {
                Console.Clear();
                ConsoleHelper.PrintHeader("Edit Hall");
                ConsoleHelper.PrintWarning("No halls found. Please add a hall first.");
                ConsoleHelper.PressAnyKey();
                return;
            }

            int selection = ConsoleHelper.ShowPaginatedTable(halls, HallService.TableColumns, "Select Hall to Edit");
            if (selection == 0) return;

            var hall = halls[selection - 1];
            await PerformEditHallAsync(hall);
        }

        private async Task ViewHallDetailsDirectAsync()
        {
            var halls = await _halls.GetAllHallsAsync();

          
            if (halls.Count == 0)
            {
                Console.Clear();
                ConsoleHelper.PrintHeader("View Hall Details");
                ConsoleHelper.PrintWarning("No halls found in the system.");
                ConsoleHelper.PressAnyKey();
                return;
            }

            int selection = ConsoleHelper.ShowPaginatedTable(halls, HallService.TableColumns, "Select Hall to View Details");
            if (selection == 0) return;

            var hall = halls[selection - 1];
            ShowHallDetails(hall);
            ConsoleHelper.PressAnyKey();
        }

        private async Task AddNewHallAsync()
        {
            Console.Clear();
            ConsoleHelper.PrintHeader("Add New Hall");

            try
            {
                string hallName;
                while (true)
                {
                    hallName = ConsoleHelper.ReadInput("Hall Name", "letters and spaces, 2-50 chars", true, true);
                    if (hallName == ConsoleHelper.BACK_COMMAND) return;

                    if (!ValidationHelper.ValidateName(hallName))
                    {
                        ConsoleHelper.PrintError("Hall name must be 2-50 characters containing only letters and spaces.");
                        continue;
                    }

                    var existingHalls = await _halls.GetAllHallsAsync();
                    bool hallNameExists = false;
                    foreach (var h in existingHalls)
                    {
                        if (h.HallName.Equals(hallName, StringComparison.OrdinalIgnoreCase))
                        {
                            hallNameExists = true;
                            break;
                        }
                    }

                    if (hallNameExists)
                    {
                      
                        ConsoleHelper.PrintError("A hall with this name already exists. Please choose another name.");
                        continue;
                    }

                    ConsoleHelper.PrintSuccess("  Hall name accepted.");
                    break;
                }

                Console.WriteLine();
                string[] hallTypes = { "Conference", "Banquet", "Exhibition", "Training", "Auditorium" };
                ConsoleHelper.PrintMenu("Hall Types", hallTypes, showBack: true);

                int typeChoice = ValidationHelper.ReadMenuChoice(5);
                if (typeChoice == 0) return;

                string hallType = hallTypes[typeChoice - 1];
                ConsoleHelper.PrintSuccess($"  Hall type selected: {hallType}");


                int capacity;
                while (true)
                {
                    string capStr = ConsoleHelper.ReadInput("Capacity", "1-10000", true, true);
                    if (capStr == ConsoleHelper.BACK_COMMAND) return;
                    if (!ValidationHelper.ValidateCapacity(capStr))
                    {
                        ConsoleHelper.PrintError("Capacity must be a whole number between 1 and 10,000.");
                        continue;
                    }
                    capacity = int.Parse(capStr);
                    ConsoleHelper.PrintSuccess($"  Capacity accepted: {capacity} persons");
                    break;
                }


                decimal pricePerHour;
                while (true)
                {
                    string priceStr = ConsoleHelper.ReadInput("Price (Rs.)", "100-100000", true, true);
                    if (priceStr == ConsoleHelper.BACK_COMMAND) return;
                    if (!decimal.TryParse(priceStr, out pricePerHour) || pricePerHour < 100 || pricePerHour > 100000)
                    {
                        ConsoleHelper.PrintError("Price must be between Rs.100 and Rs.100,000.");
                        continue;
                    }
                    ConsoleHelper.PrintSuccess($"  Price accepted: {ConsoleHelper.FormatCurrency(pricePerHour)}");
                    break;
                }

                string description = ConsoleHelper.ReadInput("Description", "Modern conference hall with advanced AV", false, true);
                if (description == ConsoleHelper.BACK_COMMAND) return;

                string location;
                while (true)
                {
                    location = ConsoleHelper.ReadInput("Location", "Building A, Ground Floor", true, true);
                    if (location == ConsoleHelper.BACK_COMMAND) return;
                    if (string.IsNullOrWhiteSpace(location))
                    {
                        ConsoleHelper.PrintError("Location is required.");
                        continue;
                    }
                    ConsoleHelper.PrintSuccess("  Location accepted.");
                    break;
                }

                bool hasAC = ConsoleHelper.Confirm("Has Air Conditioning?");
                bool hasProjector = ConsoleHelper.Confirm("Has Projector?");
                bool hasWifi = ConsoleHelper.Confirm("Has WiFi?");

                Console.Clear();
                ConsoleHelper.PrintHeader("Confirm New Hall Details");
                ConsoleHelper.PrintNonEditableField("Hall Name", hallName);
                ConsoleHelper.PrintNonEditableField("Hall Type", hallType);
                ConsoleHelper.PrintNonEditableField("Capacity", $"{capacity} persons");
                ConsoleHelper.PrintNonEditableField("Price", ConsoleHelper.FormatCurrency(pricePerHour));
                ConsoleHelper.PrintNonEditableField("Description", description);
                ConsoleHelper.PrintNonEditableField("Location", location);
                ConsoleHelper.PrintNonEditableField("Air Conditioning", ConsoleHelper.FormatBool(hasAC));
                ConsoleHelper.PrintNonEditableField("Projector", ConsoleHelper.FormatBool(hasProjector));
                ConsoleHelper.PrintNonEditableField("WiFi", ConsoleHelper.FormatBool(hasWifi));

                if (!ConsoleHelper.Confirm("Save this hall?")) return;

                bool success = await _halls.CreateHallAsync(
                    hallName, hallType, capacity, pricePerHour,
                    description, location, hasAC, hasProjector, hasWifi);

                if (success)
                    ConsoleHelper.PrintSuccess("Hall created successfully!");
                else
                    ConsoleHelper.PrintError("Failed to create hall. Please try again.");
            }
            catch (Exception ex)
            {
                ConsoleHelper.PrintError($"Error creating hall: {ex.Message}");
            }

            ConsoleHelper.PressAnyKey();
        }

        private async Task PerformEditHallAsync(Hall hall)
        {
            Console.Clear();
            ConsoleHelper.PrintHeader($"Editing Hall: {hall.HallName}");
            ConsoleHelper.PrintInfo("Press Enter (leave blank) or type 0 to keep the current value.");
            Console.WriteLine();

            ConsoleHelper.PrintNonEditableField("Hall ID", hall.HallId.ToString());
            ConsoleHelper.PrintNonEditableField("Created At", ConsoleHelper.FormatDateTime(hall.CreatedAt));
            ConsoleHelper.PrintNonEditableField("Hall Type", hall.GetHallTypeDisplay());
            Console.WriteLine();

            ConsoleHelper.PrintNonEditableField("Current Hall Name", hall.HallName);
            string newName;
            while (true)
            {
                string input = ConsoleHelper.ReadInput("New Hall Name (0 = keep)", "", false, true);
                if (input == ConsoleHelper.BACK_COMMAND) return;
                if (string.IsNullOrWhiteSpace(input) || input == "0") { newName = hall.HallName; break; }
                if (!ValidationHelper.ValidateName(input))
                {
                    ConsoleHelper.PrintError("Name must be 2-50 characters containing only letters and spaces.");
                    continue;
                }
                newName = input;
                ConsoleHelper.PrintSuccess("  Hall name accepted.");
                break;
            }

            ConsoleHelper.PrintNonEditableField("Current Capacity", $"{hall.Capacity} persons");
            int finalCapacity;
            while (true)
            {
                string capStr = ConsoleHelper.ReadInput("New Capacity (0 = keep)", "", false, true);
                if (capStr == ConsoleHelper.BACK_COMMAND) return;
                if (string.IsNullOrWhiteSpace(capStr) || capStr == "0") { finalCapacity = hall.Capacity; break; }
                if (!ValidationHelper.ValidateCapacity(capStr))
                {
                    ConsoleHelper.PrintError("Capacity must be a whole number between 1 and 10,000.");
                    continue;
                }
                finalCapacity = int.Parse(capStr);
                ConsoleHelper.PrintSuccess($"  Capacity accepted: {finalCapacity} persons");
                break;
            }

     
            ConsoleHelper.PrintNonEditableField("Current Price", ConsoleHelper.FormatCurrency(hall.PricePerHour));
            decimal finalPrice;
            while (true)
            {
                string priceStr = ConsoleHelper.ReadInput("New Price (0 = keep)", "", false, true);
                if (priceStr == ConsoleHelper.BACK_COMMAND) return;
                if (string.IsNullOrWhiteSpace(priceStr) || priceStr == "0") { finalPrice = hall.PricePerHour; break; }
                if (!decimal.TryParse(priceStr, out finalPrice) || finalPrice < 100 || finalPrice > 100000)
                {
                    ConsoleHelper.PrintError("Price must be between Rs.100 and Rs.100,000.");
                    continue;
                }
                ConsoleHelper.PrintSuccess($"  Price accepted: {ConsoleHelper.FormatCurrency(finalPrice)}");
                break;
            }

           
            ConsoleHelper.PrintNonEditableField("Current Description", hall.Description);
            string newDesc = ConsoleHelper.ReadInput("New Description (0 = keep)", "", false, true);
            if (newDesc == ConsoleHelper.BACK_COMMAND) return;
            string finalDesc = (!string.IsNullOrWhiteSpace(newDesc) && newDesc != "0") ? newDesc : hall.Description;

        
            ConsoleHelper.PrintNonEditableField("Current Location", hall.Location);
            string newLoc = ConsoleHelper.ReadInput("New Location (0 = keep)", "", false, true);
            if (newLoc == ConsoleHelper.BACK_COMMAND) return;
            string finalLoc = (!string.IsNullOrWhiteSpace(newLoc) && newLoc != "0") ? newLoc : hall.Location;

            bool finalAC = ConsoleHelper.Confirm($"Change AC status? (currently: {ConsoleHelper.FormatBool(hall.HasAC)})")
                ? !hall.HasAC : hall.HasAC;

            bool finalProjector = ConsoleHelper.Confirm($"Change Projector status? (currently: {ConsoleHelper.FormatBool(hall.HasProjector)})")
                ? !hall.HasProjector : hall.HasProjector;

            bool finalWifi = ConsoleHelper.Confirm($"Change WiFi status? (currently: {ConsoleHelper.FormatBool(hall.HasWifi)})")
                ? !hall.HasWifi : hall.HasWifi;

            if (!ConsoleHelper.Confirm("Save changes?")) return;

            var updatedHall = new Hall(
                hallId: hall.HallId,
                hallName: newName,
                hallType: hall.HallType,
                capacity: finalCapacity,
                pricePerHour: finalPrice,
                description: finalDesc,
                location: finalLoc,
                hasAC: finalAC,
                hasProjector: finalProjector,
                hasWifi: finalWifi,
                isActive: hall.IsActive,
                createdAt: hall.CreatedAt
            );

            bool success = await _halls.UpdateHallAsync(updatedHall);
            if (success)
                ConsoleHelper.PrintSuccess("Hall updated successfully!");
            else
                ConsoleHelper.PrintError("Failed to update hall.");

            ConsoleHelper.PressAnyKey();
        }

        private async Task SearchHallsAsync()
        {
            Console.Clear();
            ConsoleHelper.PrintHeader("Search Halls");

            try
            {
 
                string keyword;
                while (true)
                {
                    keyword = ConsoleHelper.ReadInput("Search keyword", "Conference / Banquet", true, true);
                    if (keyword == ConsoleHelper.BACK_COMMAND) return;
                    if (string.IsNullOrWhiteSpace(keyword))
                    {
                        ConsoleHelper.PrintError("Please enter a search keyword.");
                        continue;
                    }
                    ConsoleHelper.PrintSuccess("  Searching...");
                    break;
                }

                var results = await _halls.SearchHallsAsync(keyword, 1, ConsoleHelper.PAGE_SIZE);

                // FIX 2: Proper message when no results
                if (results.Count == 0)
                {
                    ConsoleHelper.PrintWarning($"No halls found matching '{keyword}'. Try a different keyword.");
                    ConsoleHelper.PressAnyKey();
                    return;
                }

                int selected = ConsoleHelper.ShowPaginatedTable(results, HallService.TableColumns, $"Search Results: '{keyword}'", ConsoleHelper.PAGE_SIZE);
                if (selected > 0)
                {
                    ShowHallDetails(results[selected - 1]);
                    ConsoleHelper.PressAnyKey();
                }
            }
            catch (Exception ex)
            {
                ConsoleHelper.PrintError($"Error searching halls: {ex.Message}");
                ConsoleHelper.PressAnyKey();
            }
        }

        private static void ShowHallDetails(Hall hall)
        {
            Console.Clear();
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
            ConsoleHelper.PrintNonEditableField("Created At", ConsoleHelper.FormatDateTime(hall.CreatedAt));
        }
    }
}
