using System;

namespace VenueBookingSystem.Features.Halls
{
    // ╔══════════════════════════════════════════════════════════════════════╗
    // ║                   🏛️ HALL — THE REAL HALL                           ║
    // ║  This is the actual hall — it INHERITS everything from BaseHall      ║
    // ║  (name, capacity, price, type) and ADDS its own extra details.       ║
    // ║  Think: BaseHall = building code rules | Hall = the actual building  ║
    // ╚══════════════════════════════════════════════════════════════════════╝

    // ': BaseHall' means "Hall gets everything BaseHall has, for free"
    public class Hall : BaseHall
    {
        // 📝 Extra description about the hall (floor details, special features, etc.)
        //    'init' = can only be set when creating the Hall, then it is LOCKED forever
        public string Description { get; init; } = string.Empty;

        // 📍 Where is this hall located? e.g. "Ground Floor, East Wing"
        public string Location { get; init; } = string.Empty;

        // ❄️ Does this hall have Air Conditioning? true = Yes, false = No
        public bool HasAC { get; init; }

        // 📽️ Does this hall have a Projector? true = Yes, false = No
        public bool HasProjector { get; init; }

        // 📶 Does this hall have WiFi? true = Yes, false = No
        public bool HasWifi { get; init; }

        // ✅ Is this hall currently active and available for booking?
        //    false = hall is disabled/under maintenance, won't show in search results
        public bool IsActive { get; init; }

        // 🕐 When was this hall added to the system?
        public DateTime CreatedAt { get; init; }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 🏗️ CONSTRUCTORS — Ways to create a Hall object
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        // Empty constructor — create a blank Hall and fill in later
        public Hall() { }

        // Full constructor — create a Hall with ALL details at once
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
            // Fill in the fields inherited from BaseHall
            HallId       = hallId;
            HallName     = hallName;
            HallType     = hallType;
            Capacity     = capacity;
            PricePerHour = pricePerHour;

            // Fill in Hall's own extra fields
            Description  = description;
            Location     = location;
            HasAC        = hasAC;
            HasProjector = hasProjector;
            HasWifi      = hasWifi;
            IsActive     = isActive;
            CreatedAt    = createdAt;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 🔁 OPERATOR OVERLOADING — Teaching C# new comparison tricks
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        // Now you can write: if (hallA > hallB) → means "Does Hall A fit more people?"
        public static bool operator >(Hall a, Hall b) => a.Capacity > b.Capacity;

        // Now you can write: if (hallA < hallB) → means "Does Hall A fit fewer people?"
        public static bool operator <(Hall a, Hall b) => a.Capacity < b.Capacity;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 🎭 OVERRIDING parent methods — Our own custom versions
        //    'override' = "I'm replacing BaseHall's version with my own"
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        // 🏷️ Convert the stored HallType code into a readable display label
        //    e.g. stored as "Conference" → display as "Conference Hall"
        //    If type is unknown, just show whatever is stored — don't crash
        public override string GetHallTypeDisplay() => HallType switch
        {
            "Conference"  => "Conference Hall",
            "Banquet"     => "Banquet Hall",
            "Exhibition"  => "Exhibition Hall",
            "Training"    => "Training Room",
            "Auditorium"  => "Auditorium",
            _             => HallType   // Default: show the raw stored value
        };

        // 💰 Calculate total booking cost for a given number of hours
        //    Conference Halls get a 10% surcharge (multiplier = 1.1)
        //    All other halls: normal price (multiplier = 1.0)
        //    Formula: PricePerHour × Hours × Surcharge, rounded to 2 decimal places
        public override decimal CalculateTotal(int hours)
        {
            // Conference halls are premium — 10% extra charge
            decimal multiplier = HallType == "Conference" ? 1.1m : 1.0m;

            // Round to 2 decimal places — like ₹40,000.00
            return Math.Round(PricePerHour * hours * multiplier, 2);
        }

        // 🖥️ Display hall details on screen — our enhanced version
        //    First: run BaseHall's version (shows name, type, capacity, price)
        //    Then:  add our own extra line (shows AC, Projector, WiFi)
        public override void DisplayBasicInfo()
        {
            base.DisplayBasicInfo(); // "Hey parent, run YOUR version first"
            Console.WriteLine($"  AC: {(HasAC ? "Yes" : "No")} | Projector: {(HasProjector ? "Yes" : "No")} | WiFi: {(HasWifi ? "Yes" : "No")}");
        }

        // 📤 When printing a Hall object directly, show a compact summary
        //    e.g. "Hall#3: Grand Banquet Hall (Banquet) - Capacity:500"
        public override string ToString()  => $"Hall#{HallId}: {HallName} ({HallType}) - Capacity:{Capacity}";
    }
}
