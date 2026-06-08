using System;

namespace VenueBookingSystem.Features.Payments;

public record RefundCandidate(
    int CancellationId,
    int BookingId,
    string CustomerName,
    string HallName,
    decimal RefundAmount,
    DateTime CancellationDate,
    string Reason,
    string RefundStatus
);

public record RefundResult(
    int RefundId,
    int CancellationId,
    int BookingId,
    decimal RefundAmount,
    string RefundStatus,
    DateTime? RefundDate,
    string ProcessedBy,
    string Remarks
);
