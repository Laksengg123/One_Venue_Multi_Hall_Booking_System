using System;

namespace VenueBookingSystem.Features.Halls
{
    // ╔══════════════════════════════════════════════════════════════════════╗
    // ║               🏛️ BASE HALL — THE MASTER BLUEPRINT                   ║
    // ║  This is NOT a real hall — it is a TEMPLATE/RULE that every         ║
    // ║  hall type MUST follow. You cannot create a "BaseHall" directly.    ║
    // ║  Think of it like a government rule: "Every hall MUST have these."  ║
    // ╚══════════════════════════════════════════════════════════════════════╝

    // 'abstract' = this is just a template, cannot be created directly
    public abstract class BaseHall
    {
        // 🔢 Every hall gets a unique ID number
        //    'public get'       = anyone can READ the hall number
        //    'protected set'    = only this class or its children can CHANGE it
        //    (outsiders cannot randomly assign a new ID)
        public int HallId { get; protected set; }

        // 🏷️ The hall's name — e.g. "Grand Banquet Hall"
        //    = string.Empty means it starts as blank until the child fills it in
        public string HallName { get; protected set; } = string.Empty;

        // 🗂️ What category of hall is this? (Conference / Banquet / Auditorium...)
        public string HallType { get; protected set; } = string.Empty;

        // 👥 Maximum number of people this hall can hold
        public int Capacity { get; protected set; }

        // 💵 How much does it cost per hour to rent this hall?
        public decimal PricePerHour { get; protected set; }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 📌 ABSTRACT METHODS — Rules that EVERY child hall MUST follow
        //    I (BaseHall) don't know the answer — child classes must answer
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        // ❓ "What type of hall are you?" — show a readable label
        //    e.g. "Conference" → "Conference Hall"
        //    Every hall type must implement this in its own way
        public abstract string GetHallTypeDisplay();

        // 💰 "If I book you for X hours, what is the total cost?"
        //    Every hall may calculate differently (e.g. Conference adds 10% surcharge)
        public abstract decimal CalculateTotal(int hours);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 📋 VIRTUAL METHOD — Has a default version, but children CAN replace it
        //    'virtual' = "I have a default version, you can override if you want"
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        // 🖥️ Shows basic hall info on the screen
        //    Children (like Hall.cs) can override this to show more details
        public virtual void DisplayBasicInfo()
        {
            Console.WriteLine($"  Hall: {HallName} | Type: {HallType} | Capacity: {Capacity} | Price: Rs.{PricePerHour:N2}");
        }

        // ✅ Quick YES/NO check: "Does this hall have enough space for the guests?"
        //    e.g. Hall capacity = 500, guests = 300 → 500 >= 300 → TRUE (fits!)
        //         Hall capacity = 200, guests = 300 → 200 >= 300 → FALSE (too small!)
        public bool IsCapacitySufficient(int requiredGuests) => Capacity >= requiredGuests;
    }
}
