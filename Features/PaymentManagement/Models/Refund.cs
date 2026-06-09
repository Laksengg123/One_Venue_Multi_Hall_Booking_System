using System;

namespace VenueBookingSystem.Features.Payments;

public record RefundCandidate(
    int      CancellationId,
    int      BookingId,
    string   CustomerName,
    string   HallName,
    decimal  RefundAmount,
    DateTime CancellationDate,
    string   Reason,
    string   RefundStatus,           // Refunds table: Initiated / Processed / Pending (no row yet)
    string   CancellationStatus,     // Cancellations.Status: Pending / Approved / Rejected
    string   AdminRemarks            // Cancellations.AdminRemarks
);

public record RefundResult(
    int       RefundId,
    int       CancellationId,
    int       BookingId,
    decimal   RefundAmount,
    string    RefundStatus,
    DateTime? RefundDate,
    string    ProcessedBy,
    string    Remarks
);
