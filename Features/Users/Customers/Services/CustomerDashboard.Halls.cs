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

            var allHalls = await _hallService.GetAllHallsAsync(1, 1000);

            if (allHalls.Count == 0)
            {
                Console.Clear();
                ConsoleHelper.PrintHeader("Browse Available Halls");
                ConsoleHelper.PrintWarning("No halls are currently available.");
                ConsoleHelper.PressAnyKey();
                _navigationHistory.Pop();
                return;
            }

            const int PageSize = 10;
            int totalPages     = (int)Math.Ceiling(allHalls.Count / (double)PageSize);
            int currentPage    = 1;

            while (true)
            {
                Console.Clear();
                ConsoleHelper.PrintHeader("Available Halls");

                // ── Render the current page of halls ──────────────────────────
                RenderHallsPage(allHalls, currentPage, PageSize);

                // ── Page info line ────────────────────────────────────────────
                string indent = ConsoleHelper.GetIndent();
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"{indent}  Page {currentPage} of {totalPages}  |  Total: {allHalls.Count} record(s)");
                Console.ResetColor();
                Console.WriteLine();

                // ── Navigation hint ───────────────────────────────────────────
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write($"{indent}    Navigation:");
                if (currentPage > 1)  { Console.ForegroundColor = ConsoleColor.Cyan; Console.Write("  [P] Prev"); }
                if (currentPage < totalPages) { Console.ForegroundColor = ConsoleColor.Cyan; Console.Write("  [N] Next"); }
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("  [0] Back");
                Console.ResetColor();

                // ── Options menu (always visible below the table) ─────────────
                Console.WriteLine();
                ConsoleHelper.PrintMenu("Options", new[]
                {
                    "Book a Hall",
                    "View Hall Details & Bookings"
                });

                // ── Unified prompt ────────────────────────────────────────────
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"{indent}  ➤ Choice (1-2, P/N, or 0=Back) : ");
                Console.ResetColor();

                string? raw = Console.ReadLine()?.Trim().ToUpperInvariant();

                // Page navigation
                if (raw == "N" || raw == "NEXT")
                {
                    if (currentPage < totalPages) currentPage++;
                    continue;
                }
                if (raw == "P" || raw == "PREV")
                {
                    if (currentPage > 1) currentPage--;
                    continue;
                }

                // Back / exit
                if (raw == "0" || raw == "")
                    break;

                // Menu choices
                if (raw == "1")
                {
                    await BookHallDirectAsync();
                    continue;
                }
                if (raw == "2")
                {
                    await ViewHallDetailsWithBookingsAsync();
                    continue;
                }

                // Invalid — just re-render
            }

            _navigationHistory.Pop();
        }

        // ── Page renderer ─────────────────────────────────────────────────────
        private static void RenderHallsPage(List<Hall> halls, int page, int pageSize)
        {
            string indent = ConsoleHelper.GetIndent();

            int start = (page - 1) * pageSize;
            int end   = Math.Min(start + pageSize, halls.Count);
            var slice = halls.GetRange(start, end - start);

            // Column widths
            const int wNo   = 5;
            const int wName = 22;
            const int wType = 14;
            const int wCap  = 10;
            const int wPrc  = 11;
            const int wAC   = 7;
            const int wProj = 11;
            const int wWifi = 8;
            const int wLoc  = 22;

            // ── Border helpers ────────────────────────────────────────────────
            string topBorder = "╔" + Bar('═', wNo) + "╦" + Bar('═', wName) + "╦" + Bar('═', wType) +
                               "╦" + Bar('═', wCap) + "╦" + Bar('═', wPrc) + "╦" + Bar('═', wAC) +
                               "╦" + Bar('═', wProj) + "╦" + Bar('═', wWifi) + "╦" + Bar('═', wLoc) + "╗";

            string headSep  = "╠" + Bar('═', wNo) + "╬" + Bar('═', wName) + "╬" + Bar('═', wType) +
                              "╬" + Bar('═', wCap) + "╬" + Bar('═', wPrc) + "╬" + Bar('═', wAC) +
                              "╬" + Bar('═', wProj) + "╬" + Bar('═', wWifi) + "╬" + Bar('═', wLoc) + "╣";

            string rowSep   = "╟" + Bar('─', wNo) + "╫" + Bar('─', wName) + "╫" + Bar('─', wType) +
                              "╫" + Bar('─', wCap) + "╫" + Bar('─', wPrc) + "╫" + Bar('─', wAC) +
                              "╫" + Bar('─', wProj) + "╫" + Bar('─', wWifi) + "╫" + Bar('─', wLoc) + "╢";

            string botBorder = "╚" + Bar('═', wNo) + "╩" + Bar('═', wName) + "╩" + Bar('═', wType) +
                               "╩" + Bar('═', wCap) + "╩" + Bar('═', wPrc) + "╩" + Bar('═', wAC) +
                               "╩" + Bar('═', wProj) + "╩" + Bar('═', wWifi) + "╩" + Bar('═', wLoc) + "╝";

            Console.ForegroundColor = ConsoleColor.DarkCyan;

            // Top border
            Console.WriteLine(indent + topBorder);

            // Header row
            Console.Write(indent + "║");
            WriteHdr(" # ", wNo);
            WriteHdr(" Hall Name ", wName);
            WriteHdr(" Type ", wType);
            WriteHdr(" Capacity ", wCap);
            WriteHdr(" Price ", wPrc);
            WriteHdr(" AC ", wAC);
            WriteHdr(" Projector ", wProj);
            WriteHdr(" WiFi ", wWifi);
            WriteHdr(" Location ", wLoc);
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("║");

            // Header separator
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(indent + headSep);

            // Data rows
            for (int i = 0; i < slice.Count; i++)
            {
                var h     = slice[i];
                int rowNo = start + i + 1;

                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.Write(indent + "║");

                WriteCell(" " + $"{rowNo,3}" + " ",                                                     wNo,   ConsoleColor.DarkGray);
                WriteCell(" " + Clip(h.HallName, wName - 2).PadRight(wName - 2) + " ",                  wName, ConsoleColor.White);
                WriteCell(" " + Clip(h.GetHallTypeDisplay(), wType - 2).PadRight(wType - 2) + " ",      wType, ConsoleColor.Cyan);
                WriteCell(" " + $"{h.Capacity,-8}" + " ",                                               wCap,  ConsoleColor.White);
                WriteCell(" " + Clip(ConsoleHelper.FormatCurrency(h.PricePerHour), wPrc - 2).PadRight(wPrc - 2) + " ", wPrc, ConsoleColor.Green);
                WriteCell(" " + (h.HasAC       ? "Yes" : "No").PadRight(5) + " ",                       wAC,   h.HasAC       ? ConsoleColor.Green : ConsoleColor.Red);
                WriteCell(" " + (h.HasProjector ? "Yes" : "No").PadRight(9) + " ",                      wProj, h.HasProjector ? ConsoleColor.Green : ConsoleColor.Red);
                WriteCell(" " + (h.HasWifi     ? "Yes" : "No").PadRight(6) + " ",                       wWifi, h.HasWifi     ? ConsoleColor.Green : ConsoleColor.Red);
                WriteCell(" " + Clip(h.Location, wLoc - 2).PadRight(wLoc - 2) + " ",                   wLoc,  ConsoleColor.DarkGray);


                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine("║");

                if (i < slice.Count - 1)
                {
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    Console.WriteLine(indent + rowSep);
                }
            }

            // Bottom border
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(indent + botBorder);
            Console.ResetColor();
        }

        private static string Bar(char ch, int w) => new string(ch, w);
        private static string Clip(string s, int max) => s.Length > max ? s[..max] : s;

        private static void WriteHdr(string text, int colWidth)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(text.PadRight(colWidth)[..colWidth]);
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write("║");
        }

        private static void WriteCell(string text, int colWidth, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            string padded = text.PadRight(colWidth);
            Console.Write(padded.Length > colWidth ? padded[..colWidth] : padded);
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write("║");
        }

        // ─────────────────────────────────────────────────────────────────────
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
                ConsoleHelper.PrintWarning("No bookings available for this hall.");
            else
                ConsoleHelper.PrintTable(allBookings, BookingService.TableColumns);

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
            ConsoleHelper.PrintReadOnlyId("Hall ID",         hall.HallId.ToString());
            ConsoleHelper.PrintNonEditableField("Hall Name",  hall.HallName);
            ConsoleHelper.PrintNonEditableField("Type",       hall.GetHallTypeDisplay());
            ConsoleHelper.PrintNonEditableField("Capacity",   $"{hall.Capacity} persons");
            ConsoleHelper.PrintNonEditableField("Price",      ConsoleHelper.FormatCurrency(hall.PricePerHour));
            ConsoleHelper.PrintNonEditableField("Description",hall.Description);
            ConsoleHelper.PrintNonEditableField("Location",   hall.Location);
            ConsoleHelper.PrintNonEditableField("Air Conditioning", ConsoleHelper.FormatBool(hall.HasAC));
            ConsoleHelper.PrintNonEditableField("Projector",  ConsoleHelper.FormatBool(hall.HasProjector));
            ConsoleHelper.PrintNonEditableField("WiFi",       ConsoleHelper.FormatBool(hall.HasWifi));
        }
    }
}
