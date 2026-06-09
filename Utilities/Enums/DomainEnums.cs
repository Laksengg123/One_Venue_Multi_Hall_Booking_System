using System.ComponentModel;

namespace VenueBookingSystem.Utilities.Enums;

// ╔══════════════════════════════════════════════════════════════════════════╗
// ║         🏛️  DOMAIN ENUMS — Centralised enum definitions                 ║
// ║  All shared enums for the VenueBookingSystem live here so they can      ║
// ║  be referenced by any layer without circular dependencies.              ║
// ╚══════════════════════════════════════════════════════════════════════════╝

// ── Booking ───────────────────────────────────────────────────────────────

/// <summary>
/// Lifecycle states of a booking.
/// A booking moves through: Pending → Confirmed → Completed (or Cancelled/Rejected).
/// </summary>
public enum BookingState
{
    [Description("Pending Confirmation")]   Pending   = 1,
    [Description("Confirmed Booking")]      Confirmed = 2,
    [Description("Cancelled")]              Cancelled = 3,
    [Description("Completed Event")]        Completed = 4,
    [Description("Rejected by Admin")]      Rejected  = 5
}

// ── Cancellation ──────────────────────────────────────────────────────────

/// <summary>
/// Admin decision states for a cancellation request.
/// Created as Pending when a customer cancels; admin moves it to Approved or Rejected.
/// </summary>
public enum CancellationState
{
    [Description("Pending Admin Review")]   Pending  = 1,
    [Description("Approved — Refunded")]    Approved = 2,
    [Description("Rejected by Admin")]      Rejected = 3
}

// ── Refund ────────────────────────────────────────────────────────────────

/// <summary>
/// Processing states of a refund transaction.
/// </summary>
public enum RefundState
{
    [Description("Refund Initiated")]       Initiated = 1,
    [Description("Refund Processed")]       Processed = 2,
    [Description("Refund Failed")]          Failed    = 3
}

// ── Payment ───────────────────────────────────────────────────────────────

/// <summary>Payment method used by the customer.</summary>
public enum PaymentMethodType
{
    [Description("Cash")]                   Cash         = 1,
    [Description("Card (Debit/Credit)")]    Card         = 2,
    [Description("UPI / QR Code")]          UPI          = 3,
    [Description("Bank Transfer")]          BankTransfer = 4
}

/// <summary>Payment transaction status.</summary>
public enum PaymentState
{
    [Description("Payment Pending")]        Pending   = 1,
    [Description("Payment Completed")]      Completed = 2,
    [Description("Payment Failed")]         Failed    = 3,
    [Description("Payment Refunded")]       Refunded  = 4
}

// ── Hall ──────────────────────────────────────────────────────────────────

/// <summary>Type/category of a hall.</summary>
public enum HallCategory
{
    [Description("Banquet Hall")]           Banquet    = 1,
    [Description("Conference Room")]        Conference = 2,
    [Description("Training Room")]          Training   = 3,
    [Description("Auditorium")]             Auditorium = 4,
    [Description("Board Room")]             BoardRoom  = 5,
    [Description("Wedding Hall")]           Wedding    = 6,
    [Description("Exhibition Hall")]        Exhibition = 7
}

// ── User / Role ───────────────────────────────────────────────────────────

/// <summary>Role assigned to each system user.</summary>
public enum UserRole
{
    [Description("System Administrator")]   Admin    = 1,
    [Description("Customer")]               Customer = 2,
    [Description("Staff / Cashier")]        Staff    = 3
}

// ── Export Format ─────────────────────────────────────────────────────────

/// <summary>Supported file export formats for reports.</summary>
public enum ExportFileFormat
{
    [Description("Plain Text (.txt)")]      Txt  = 1,
    [Description("CSV Spreadsheet (.csv)")]Csv  = 2,
    [Description("Excel (.xlsx)")]          Xlsx = 3,
    [Description("Word Document (.docx)")] Docx = 4
}
