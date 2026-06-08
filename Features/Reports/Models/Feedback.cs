namespace VenueBookingSystem.Features.Reports.Models;

public record FeedbackEntry(
    int FeedbackId,
    int UserId,
    int HallId,
    int BookingId,
    int Rating,
    string Comment,
    bool IsVisible,
    DateTime CreatedAt
);
