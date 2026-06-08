namespace VenueBookingSystem.Features.Halls.Exceptions;


public class HallUnavailableException : Exception
{
    public int HallId { get; }
    public DateTime RequestedStart { get; }
    public DateTime RequestedEnd { get; }

    public HallUnavailableException(int hallId, DateTime start, DateTime end)
        : base($"Hall #{hallId} is not available from {start:dd-MMM-yyyy HH:mm} to {end:dd-MMM-yyyy HH:mm}.")
    {
        HallId = hallId;
        RequestedStart = start;
        RequestedEnd = end;
    }

    public HallUnavailableException(string message) : base(message) { }
}
