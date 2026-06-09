using System;

namespace VenueBookingSystem.Shared.Models
{

    public struct DateRange : IEquatable<DateRange>
    {
      
        public DateTime Start { get; }

        
        public DateTime End { get; }

        // 🏗️ CONSTRUCTOR — "Create a date range from start to end"
        //    But first: do a sanity check — end cannot be BEFORE start
        public DateRange(DateTime start, DateTime end)
        {
            // 🚨 Guard clause: "10 June to 5 June" makes no sense — throw an error
            if (end < start)
                throw new ArgumentException("End date cannot be before start date.");

            Start = start; // Save the start date
            End = end;     // Save the end date
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // ⚖️ EQUALITY — Teach C# how to compare two DateRanges
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        // Are two DateRanges the same? Both start AND end must match exactly
        public bool Equals(DateRange other)
        {
            return Start == other.Start && End == other.End;
        }

        // C# also calls this when comparing with a generic 'object'
        // Check: "Is this other object also a DateRange? Then compare them."
        public override bool Equals(object? obj)
        {
            return obj is DateRange other && Equals(other);
        }

        // Required when you override Equals — C# needs a unique hash for collections
        // HashCode.Combine makes a fingerprint from both Start and End
        public override int GetHashCode()
        {
            return HashCode.Combine(Start, End);
        }

        // Now you can write: if (range1 == range2) → checks if both dates match
        public static bool operator ==(DateRange left, DateRange right)
        {
            return left.Equals(right);
        }

        // Now you can write: if (range1 != range2) → checks if they are different
        public static bool operator !=(DateRange left, DateRange right)
        {
            return !(left == right); // Opposite of ==
        }

        // 📤 When printing a DateRange, show it in readable format
        //    e.g. "15-06-2026 — 20-06-2026"
        public override string ToString()
        {
            return $"{Start:dd-MM-yyyy} — {End:dd-MM-yyyy}";
        }
    }
}
