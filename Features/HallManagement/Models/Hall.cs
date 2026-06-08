using System;

namespace VenueBookingSystem.Features.Halls
{
    
    public class Hall : BaseHall
    {
        public string Description { get; init; } = string.Empty;
        public string Location { get; init; } = string.Empty;
        public bool HasAC { get; init; }
        public bool HasProjector { get; init; }
        public bool HasWifi { get; init; }
        public bool IsActive { get; init; }
        public DateTime CreatedAt { get; init; }

        public Hall() { }
        public Hall(
            int     hallId,
            string  hallName,
            string  hallType,
            int     capacity,
            decimal pricePerHour,
            string  description,
            string  location,
            bool    hasAC,
            bool    hasProjector,
            bool    hasWifi,
            bool    isActive,
            DateTime createdAt)
        {
            HallId       = hallId;
            HallName     = hallName;
            HallType     = hallType;
            Capacity     = capacity;
            PricePerHour = pricePerHour;
            Description  = description;
            Location     = location;
            HasAC        = hasAC;
            HasProjector = hasProjector;
            HasWifi      = hasWifi;
            IsActive     = isActive;
            CreatedAt    = createdAt;
        }

        public static bool operator >(Hall a, Hall b) => a.Capacity > b.Capacity;
        public static bool operator <(Hall a, Hall b) => a.Capacity < b.Capacity;

        public override string GetHallTypeDisplay() => HallType switch
        {
            "Conference"  => "Conference Hall",
            "Banquet"     => "Banquet Hall",
            "Exhibition"  => "Exhibition Hall",
            "Training"    => "Training Room",
            "Auditorium"  => "Auditorium",
            _             => HallType   
        };

        public override decimal CalculateTotal(int hours)
        {
            decimal multiplier = HallType == "Conference" ? 1.1m : 1.0m;
            return Math.Round(PricePerHour * hours * multiplier, 2);
        }

    
        public override void DisplayBasicInfo()
        {
            base.DisplayBasicInfo();
            Console.WriteLine($"  AC: {(HasAC ? "Yes" : "No")} | Projector: {(HasProjector ? "Yes" : "No")} | WiFi: {(HasWifi ? "Yes" : "No")}");
        }

        public override string ToString()  => $"Hall#{HallId}: {HallName} ({HallType}) - Capacity:{Capacity}";
    }
}
