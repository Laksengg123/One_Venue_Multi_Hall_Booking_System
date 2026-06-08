using System;

namespace VenueBookingSystem.Shared.Models
{
    public struct DateRange : IEquatable<DateRange>
    {
        public DateTime Start { get; }
        public DateTime End { get; }

        public DateRange(DateTime start, DateTime end)
        {
            if (end < start)
                throw new ArgumentException("End date cannot be before start date.");
                
            Start = start;
            End = end;
        }

        public bool Equals(DateRange other)
        {
            return Start == other.Start && End == other.End;
        }

        public override bool Equals(object? obj)
        {
            return obj is DateRange other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Start, End);
        }

        public static bool operator ==(DateRange left, DateRange right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DateRange left, DateRange right)
        {
            return !(left == right);
        }
        
        public override string ToString()
        {
            return $"{Start:dd-MM-yyyy} — {End:dd-MM-yyyy}";
        }
    }
}
