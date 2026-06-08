using System;

namespace VenueBookingSystem.Shared.UI
{
    public static class MenuHelper
    {
        public static void PrintMenuTitle(string title)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"=== {title.ToUpper()} ===");
            Console.ResetColor();
        }
    }
}
