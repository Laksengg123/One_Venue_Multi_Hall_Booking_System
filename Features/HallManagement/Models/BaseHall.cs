using System;

namespace VenueBookingSystem.Features.Halls
{
    
    public abstract class BaseHall
    {
        public int HallId { get; protected set; }
        public string HallName { get; protected set; } = string.Empty;
        public string HallType { get; protected set; } = string.Empty; 
        public int Capacity { get; protected set; }
        public decimal PricePerHour { get; protected set; }

        public abstract string GetHallTypeDisplay();
        public abstract decimal CalculateTotal(int hours);


        public virtual void DisplayBasicInfo()
        {
            Console.WriteLine($"  Hall: {HallName} | Type: {HallType} | Capacity: {Capacity} | Price: Rs.{PricePerHour:N2}");
        }

        public bool IsCapacitySufficient(int requiredGuests) => Capacity >= requiredGuests;
    }
}
