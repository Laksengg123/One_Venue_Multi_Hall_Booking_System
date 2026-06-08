using System;

namespace VenueBookingSystem.Shared
{
  
    public static partial class ValidationHelper
    {
        public static (bool IsValid, string ErrorMsg) ValidateGuestCapacity(int guestCount, int hallCapacity)
        {
            if (guestCount <= 0)
            {
                return (false, "Guest count must be greater than zero.");
            }
            if (guestCount > hallCapacity)
            {
                return (false, $"Guest count ({guestCount}) exceeds the hall's maximum capacity ({hallCapacity}).");
            }
            return (true, string.Empty);
        }

      
        public static (bool IsValid, string ErrorMsg) ValidateGuestCapacity(string guestCountInput, int hallCapacity)
        {
            if (!int.TryParse(guestCountInput, out int guestCount))
            {
                return (false, "Guest count must be a valid integer.");
            }
            return ValidateGuestCapacity(guestCount, hallCapacity);
        }
    }
}
