using System;

namespace VenueBookingSystem.Features.Bookings.DTOs
{
    public record BookingHistoryEntry(
        int HistoryId,
        int BookingId,
        string OldStatus,
        string NewStatus,
        DateTime ChangedAt,
        string ChangedBy,
        string Remarks
    );
}
