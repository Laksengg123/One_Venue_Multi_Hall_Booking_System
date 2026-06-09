using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using VenueBookingSystem.Features.Payments.Exceptions;
using VenueBookingSystem.Features.Authentication;
using VenueBookingSystem.Features.Bookings;
using VenueBookingSystem.Features.Bookings.Events;
using VenueBookingSystem.Features.Payments.Events;
using VenueBookingSystem.Shared;
using VenueBookingSystem.Storage;

namespace VenueBookingSystem.Features.Payments;


public class PaymentService
{
    private readonly DatabaseContext _dbContext;
    public event EventHandler<PaymentProcessedEventArgs>? PaymentProcessed;

    public PaymentService(DatabaseContext dbContext, BookingService bookingService)
    {
        _dbContext = dbContext;

        bookingService.BookingConfirmed += OnBookingConfirmed;
    }

 
    private static Payment MapPayment(SqlDataReader r) => new Payment(
        r.GetInt32(r.GetOrdinal("PaymentId")),
        r.GetInt32(r.GetOrdinal("BookingId")),
        r.GetDecimal(r.GetOrdinal("Amount")),
        (PaymentMethod)r.GetInt32(r.GetOrdinal("PaymentMethod")),
        (PaymentStatus)r.GetInt32(r.GetOrdinal("PaymentStatus")),
        r.IsDBNull(r.GetOrdinal("TransactionRef")) ? string.Empty : r.GetString(r.GetOrdinal("TransactionRef")),
        r.IsDBNull(r.GetOrdinal("PaidAt"))         ? null         : r.GetDateTime(r.GetOrdinal("PaidAt")),
        r.GetDateTime(r.GetOrdinal("CreatedAt"))
    );

    public static Dictionary<string, Func<Payment, string>> TableColumns => new()
    {
        ["#"]               = p => p.PaymentId.ToString(),
        ["Booking ID"]      = p => p.BookingId.ToString(),
        ["Amount"]          = p => ConsoleHelper.FormatCurrency(p.Amount),
        ["Method"]          = p => p.GetMethodDisplay(),
        ["Status"]          = p => p.GetStatusDisplay(),
        ["Transaction Ref"] = p => p.TransactionRef,
        ["Paid At"]         = p => p.PaidAt.HasValue
                                       ? ConsoleHelper.FormatDateTime(p.PaidAt.Value)
                                       : "Not paid"
    };

    public static Dictionary<string, Func<RefundCandidate, string>> RefundColumns => new()
    {
        ["Cancellation"] = r => r.CancellationId.ToString(),
        ["Booking"]      = r => r.BookingId.ToString(),
        ["Customer"]     = r => r.CustomerName,
        ["Hall"]         = r => r.HallName,
        ["Amount"]       = r => ConsoleHelper.FormatCurrency(r.RefundAmount),
        ["Status"]       = r => r.RefundStatus,
        ["Cancelled"]    = r => ConsoleHelper.FormatDateTime(r.CancellationDate),
        ["Reason"]       = r => r.Reason.Length > 24 ? r.Reason[..24] + "..." : r.Reason
    };

    public static Dictionary<string, Func<Invoice, string>> InvoiceColumns => new()
    {
        ["Invoice"]  = i => i.InvoiceId.ToString(),
        ["Booking"]  = i => i.BookingId.ToString(),
        ["Number"]   = i => i.InvoiceNumber,
        ["Amount"]   = i => ConsoleHelper.FormatCurrency(i.TotalAmount),
        ["Issued To"] = i => i.IssuedTo,
        ["Generated"] = i => ConsoleHelper.FormatDateTime(i.GeneratedDate)
    };


    // Concept: Method Overloading (ProcessPaymentAsync with Booking object)
    public async Task<bool> ProcessPaymentAsync(Booking booking, PaymentMethod method)
    {
        if (booking == null)
            return false;

        try
        {
            var transactionRef = $"TXN{DateTime.Now:yyyyMMddHHmmss}{booking.BookingId}";
            var paidAt         = DateTime.Now;

            await using var connPay = await _dbContext.CreateConnectionAsync();
            await using var cmdPay  = new SqlCommand("sp_CreatePayment", connPay)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmdPay.Parameters.AddWithValue("@BookingId",      booking.BookingId);
            cmdPay.Parameters.AddWithValue("@Amount",         booking.TotalAmount);
            cmdPay.Parameters.AddWithValue("@PaymentMethod",  (int)method);
            cmdPay.Parameters.AddWithValue("@PaymentStatus",  (int)PaymentStatus.Completed);
            cmdPay.Parameters.AddWithValue("@TransactionRef", transactionRef);
            cmdPay.Parameters.AddWithValue("@PaidAt",         paidAt);

            await cmdPay.ExecuteNonQueryAsync();

            await using var connStatus = await _dbContext.CreateConnectionAsync();
            await using var cmdStatus  = new SqlCommand("dbo.sp_UpdateBookingStatus", connStatus)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmdStatus.Parameters.AddWithValue("@BookingId", booking.BookingId);
            cmdStatus.Parameters.AddWithValue("@Status",    (int)BookingStatus.Confirmed);
            cmdStatus.Parameters.AddWithValue("@Remarks",   "Payment received");

            await cmdStatus.ExecuteNonQueryAsync();

            var payment = new Payment(
                0,
                booking.BookingId,
                booking.TotalAmount,
                method,
                PaymentStatus.Completed,
                transactionRef,
                paidAt,
                DateTime.Now
            );

            PaymentProcessed?.Invoke(this, new PaymentProcessedEventArgs(payment));
            return true;
        }
        catch (SqlException ex)
        {
            throw new PaymentException($"Payment processing failed: {ex.Message}", ex);
        }
    }

    // Concept: Method Overloading (ProcessPaymentAsync with booking ID — overloaded version)
    public async Task<bool> ProcessPaymentAsync(int bookingId, PaymentMethod method)
    {
        try
        {
            await using var connLookup = await _dbContext.CreateConnectionAsync();
            await using var cmdLookup  = new SqlCommand("sp_GetBookingById", connLookup)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmdLookup.Parameters.AddWithValue("@BookingId", bookingId);

            Booking? booking = null;
            await using (var reader = await cmdLookup.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    booking = new Booking(
                        reader.GetInt32(reader.GetOrdinal("BookingId")),
                        reader.GetInt32(reader.GetOrdinal("CustomerId")),
                        reader.IsDBNull(reader.GetOrdinal("CustomerName")) ? string.Empty : reader.GetString(reader.GetOrdinal("CustomerName")),
                        reader.GetInt32(reader.GetOrdinal("HallId")),
                        reader.IsDBNull(reader.GetOrdinal("HallName")) ? string.Empty : reader.GetString(reader.GetOrdinal("HallName")),
                        reader.GetDateTime(reader.GetOrdinal("StartDateTime")),
                        reader.GetDateTime(reader.GetOrdinal("EndDateTime")),
                        reader.GetDecimal(reader.GetOrdinal("TotalHours")),
                        reader.GetDecimal(reader.GetOrdinal("TotalAmount")),
                        (BookingStatus)reader.GetInt32(reader.GetOrdinal("Status")),
                        reader.IsDBNull(reader.GetOrdinal("Purpose")) ? string.Empty : reader.GetString(reader.GetOrdinal("Purpose")),
                        reader.GetInt32(reader.GetOrdinal("GuestCount")),
                        reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
                    );
                }
            }

            if (booking is null)
                return false;

            return await ProcessPaymentAsync(booking, method);
        }
        catch (SqlException ex)
        {
            throw new PaymentException($"Payment processing failed: {ex.Message}", ex);
        }
    }


    public async Task<Payment?> GetPaymentByBookingAsync(int bookingId)
    {
        await using var conn = await _dbContext.CreateConnectionAsync();
        await using var cmd  = new SqlCommand("sp_GetPaymentByBooking", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@BookingId", bookingId);

        await using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? MapPayment(reader) : null;
    }

    public async Task<List<Payment>> GetPaymentsByCustomerAsync(int customerId)
    {
        var payments = new List<Payment>();

        await using var conn = await _dbContext.CreateConnectionAsync();
        await using var cmd  = new SqlCommand("sp_GetPaymentsByCustomer", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@CustomerId", customerId);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            payments.Add(MapPayment(reader));

        return payments;
    }

    public async Task<List<Payment>> GetAllPaymentsAsync(int page = 1, int pageSize = 10)
    {
        var payments = new List<Payment>();

        await using var conn = await _dbContext.CreateConnectionAsync();
        await using var cmd  = new SqlCommand("sp_GetAllPayments", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@Page",     page);
        cmd.Parameters.AddWithValue("@PageSize", pageSize);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            payments.Add(MapPayment(reader));

        return payments;
    }

    public async Task<List<RefundCandidate>> GetPendingRefundsAsync()
    {
        var refunds = new List<RefundCandidate>();

        await using var conn = await _dbContext.CreateConnectionAsync();
        await using var cmd = new SqlCommand(@"
            SELECT c.CancellationId, c.BookingId, c.RefundAmount, c.CancellationDate, c.Reason,
                   ISNULL(r.RefundStatus, 'Pending') AS RefundStatus,
                   b.CustomerNameSnapshot AS CustomerName, b.HallNameSnapshot AS HallName,
                   ISNULL(c.Status, 'Pending') AS CancellationStatus,
                   ISNULL(c.AdminRemarks, '') AS AdminRemarks
            FROM Cancellations c
            INNER JOIN Bookings b ON b.BookingId = c.BookingId
            LEFT JOIN Users u ON u.UserId = b.UserId
            LEFT JOIN Halls h ON h.HallId = b.HallId
            OUTER APPLY (
                SELECT TOP 1 RefundStatus
                FROM Refunds rr
                WHERE rr.CancellationId = c.CancellationId
                ORDER BY rr.RefundId DESC
            ) r
            WHERE c.RefundAmount > 0
              AND ISNULL(r.RefundStatus, 'Pending') <> 'Processed'
              AND ISNULL(c.Status, 'Pending') = 'Approved'
            ORDER BY c.CancellationDate DESC", conn)
        {
            CommandType = CommandType.Text
        };

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            refunds.Add(MapRefundCandidate(reader));
        }

        return refunds;
    }

    public async Task<int> GetPendingRefundCountAsync()
    {
        await using var conn = await _dbContext.CreateConnectionAsync();
        await using var cmd = new SqlCommand(@"
            SELECT COUNT(1)
            FROM dbo.Cancellations c
            OUTER APPLY (
                SELECT TOP 1 RefundStatus
                FROM dbo.Refunds rr
                WHERE rr.CancellationId = c.CancellationId
                ORDER BY rr.RefundId DESC
            ) r
            WHERE c.RefundAmount > 0
              AND ISNULL(r.RefundStatus, 'Pending') <> 'Processed'", conn)
        {
            CommandType = CommandType.Text
        };

        var result = await cmd.ExecuteScalarAsync();
        return result is null or DBNull ? 0 : Convert.ToInt32(result);
    }

    public async Task<RefundResult?> ProcessRefundAsync(
        int cancellationId,
        decimal refundAmount,
        string processedBy,
        string remarks)
    {
        if (refundAmount <= 0)
            throw new PaymentException("Refund amount must be greater than zero.");

        try
        {
            await using var conn = await _dbContext.CreateConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            int bookingId;
            decimal approvedAmount;
            await using (var checkCmd = new SqlCommand(@"
                SELECT BookingId, RefundAmount
                FROM Cancellations
                WHERE CancellationId = @CancellationId", conn, (SqlTransaction)tx))
            {
                checkCmd.Parameters.AddWithValue("@CancellationId", cancellationId);
                await using var reader = await checkCmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                    throw new PaymentException("Cancellation record not found.");

                bookingId = reader.GetInt32(reader.GetOrdinal("BookingId"));
                approvedAmount = reader.GetDecimal(reader.GetOrdinal("RefundAmount"));
            }

            if (refundAmount > approvedAmount)
                throw new PaymentException("Refund amount cannot exceed the approved refund amount.");

            await using (var duplicateCmd = new SqlCommand(@"
                SELECT COUNT(1)
                FROM Refunds
                WHERE CancellationId = @CancellationId
                  AND RefundStatus = 'Processed'", conn, (SqlTransaction)tx))
            {
                duplicateCmd.Parameters.AddWithValue("@CancellationId", cancellationId);
                if (Convert.ToInt32(await duplicateCmd.ExecuteScalarAsync()) > 0)
                    throw new PaymentException("Refund has already been processed for this cancellation.");
            }

            int refundId;
            await using (var existingCmd = new SqlCommand(@"
                SELECT TOP 1 RefundId
                FROM Refunds
                WHERE CancellationId = @CancellationId
                ORDER BY RefundId DESC", conn, (SqlTransaction)tx))
            {
                existingCmd.Parameters.AddWithValue("@CancellationId", cancellationId);
                var existing = await existingCmd.ExecuteScalarAsync();
                refundId = existing is null or DBNull ? 0 : Convert.ToInt32(existing);
            }

            if (refundId > 0)
            {
                await using var updateCmd = new SqlCommand(@"
                    UPDATE Refunds
                    SET RefundStatus = 'Processed',
                        RefundDate = GETDATE(),
                        ProcessedBy = @ProcessedBy,
                        Remarks = @Remarks
                    WHERE RefundId = @RefundId", conn, (SqlTransaction)tx);
                updateCmd.Parameters.AddWithValue("@RefundId", refundId);
                updateCmd.Parameters.AddWithValue("@ProcessedBy", string.IsNullOrWhiteSpace(processedBy) ? DBNull.Value : processedBy);
                updateCmd.Parameters.AddWithValue("@Remarks", string.IsNullOrWhiteSpace(remarks) ? DBNull.Value : remarks);
                await updateCmd.ExecuteNonQueryAsync();
            }
            else
            {
                await using var insertCmd = new SqlCommand(@"
                    INSERT INTO Refunds (CancellationId, RefundStatus, RefundDate, ProcessedBy, Remarks)
                    VALUES (@CancellationId, 'Processed', GETDATE(), @ProcessedBy, @Remarks);
                    SELECT SCOPE_IDENTITY();", conn, (SqlTransaction)tx);
                insertCmd.Parameters.AddWithValue("@CancellationId", cancellationId);
                insertCmd.Parameters.AddWithValue("@ProcessedBy", string.IsNullOrWhiteSpace(processedBy) ? DBNull.Value : processedBy);
                insertCmd.Parameters.AddWithValue("@Remarks", string.IsNullOrWhiteSpace(remarks) ? DBNull.Value : remarks);
                refundId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync());
            }

            await using (var updateCancellationCmd = new SqlCommand(@"
                UPDATE Cancellations
                SET RefundAmount = @RefundAmount
                WHERE CancellationId = @CancellationId", conn, (SqlTransaction)tx))
            {
                updateCancellationCmd.Parameters.AddWithValue("@RefundAmount", refundAmount);
                updateCancellationCmd.Parameters.AddWithValue("@CancellationId", cancellationId);
                await updateCancellationCmd.ExecuteNonQueryAsync();
            }

            await using (var updatePaymentCmd = new SqlCommand(@"
                UPDATE Payments
                SET PaymentStatus = @PaymentStatus
                WHERE BookingId = @BookingId", conn, (SqlTransaction)tx))
            {
                updatePaymentCmd.Parameters.AddWithValue("@PaymentStatus", (int)PaymentStatus.Refunded);
                updatePaymentCmd.Parameters.AddWithValue("@BookingId", bookingId);
                await updatePaymentCmd.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();

            return new RefundResult(
                refundId,
                cancellationId,
                bookingId,
                refundAmount,
                "Processed",
                DateTime.Now,
                processedBy,
                remarks
            );
        }
        catch (SqlException ex)
        {
            throw new PaymentException($"Refund processing failed: {ex.Message}", ex);
        }
    }

    public async Task<Invoice?> GenerateInvoiceAsync(int bookingId)
    {
        try
        {
            await using var conn = await _dbContext.CreateConnectionAsync();

            await using (var existingCmd = new SqlCommand(@"
                SELECT InvoiceId, BookingId, InvoiceNumber, TotalAmount, GeneratedDate, IssuedTo, ISNULL(Notes, '') AS Notes
                FROM Invoices
                WHERE BookingId = @BookingId", conn))
            {
                existingCmd.Parameters.AddWithValue("@BookingId", bookingId);
                await using var existingReader = await existingCmd.ExecuteReaderAsync();
                if (await existingReader.ReadAsync())
                    return MapInvoice(existingReader);
            }

            int userId;
            string customerName;
            decimal totalAmount;
            string status;
            string bookingReference;

            await using (var bookingCmd = new SqlCommand(@"
                SELECT CustomerId,
                       ISNULL(CustomerNameSnapshot, '') AS CustomerName,
                       TotalAmount,
                       BookingStatus,
                       BookingReference
                FROM Bookings
                WHERE BookingId = @BookingId", conn))
            {
                bookingCmd.Parameters.AddWithValue("@BookingId", bookingId);
                await using var bookingReader = await bookingCmd.ExecuteReaderAsync();
                if (!await bookingReader.ReadAsync())
                    throw new PaymentException("Booking not found.");

                userId = bookingReader.GetInt32(bookingReader.GetOrdinal("CustomerId"));
                customerName = bookingReader.GetString(bookingReader.GetOrdinal("CustomerName"));
                totalAmount = bookingReader.GetDecimal(bookingReader.GetOrdinal("TotalAmount"));
                status = bookingReader.GetString(bookingReader.GetOrdinal("BookingStatus"));
                bookingReference = bookingReader.GetString(bookingReader.GetOrdinal("BookingReference"));
            }

            if (!status.Equals("Confirmed", StringComparison.OrdinalIgnoreCase))
                throw new PaymentException("Invoices can only be generated for confirmed bookings.");

            if (string.IsNullOrWhiteSpace(customerName))
            {
                await using var userCmd = new SqlCommand(
                    "SELECT FullName FROM Users WHERE UserId = @UserId", conn);
                userCmd.Parameters.AddWithValue("@UserId", userId);
                customerName = Convert.ToString(await userCmd.ExecuteScalarAsync()) ?? "Customer";
            }

            string invoiceNumber = $"INV-{DateTime.Now:yyyy}-{bookingReference}";
            int invoiceId;

            await using (var insertCmd = new SqlCommand(@"
                INSERT INTO Invoices (BookingId, InvoiceNumber, TotalAmount, IssuedTo)
                VALUES (@BookingId, @InvoiceNumber, @TotalAmount, @IssuedTo);
                SELECT SCOPE_IDENTITY();", conn))
            {
                insertCmd.Parameters.AddWithValue("@BookingId", bookingId);
                insertCmd.Parameters.AddWithValue("@InvoiceNumber", invoiceNumber);
                insertCmd.Parameters.AddWithValue("@TotalAmount", totalAmount);
                insertCmd.Parameters.AddWithValue("@IssuedTo", customerName);
                invoiceId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync());
            }

            await using var fetchCmd = new SqlCommand(@"
                SELECT InvoiceId, BookingId, InvoiceNumber, TotalAmount, GeneratedDate, IssuedTo, ISNULL(Notes, '') AS Notes
                FROM Invoices
                WHERE InvoiceId = @InvoiceId", conn);
            fetchCmd.Parameters.AddWithValue("@InvoiceId", invoiceId);
            await using var reader = await fetchCmd.ExecuteReaderAsync();
            return await reader.ReadAsync() ? MapInvoice(reader) : null;
        }
        catch (SqlException ex)
        {
            throw new PaymentException($"Invoice generation failed: {ex.Message}", ex);
        }
    }

    private static Invoice MapInvoice(SqlDataReader r) => new Invoice(
        r.GetInt32(r.GetOrdinal("InvoiceId")),
        r.GetInt32(r.GetOrdinal("BookingId")),
        r.GetString(r.GetOrdinal("InvoiceNumber")),
        r.GetDecimal(r.GetOrdinal("TotalAmount")),
        r.GetDateTime(r.GetOrdinal("GeneratedDate")),
        r.GetString(r.GetOrdinal("IssuedTo")),
        r.GetString(r.GetOrdinal("Notes"))
    );

    // ──────────────────────────── EVENT HANDLER ────────────────────────────

    /// <summary>
    /// Handles the <see cref="BookingService.BookingConfirmed"/> event.
    /// Auto-creates a Pending payment record so that cashiers can process it later.
    /// </summary>
    /// <remarks>
    /// <c>async void</c> is used because event handlers cannot return <c>Task</c>.
    /// Any exception is caught and printed as a warning — it must not crash the app.
    /// </remarks>
    private async void OnBookingConfirmed(object? sender, BookingConfirmedEventArgs e)
    {
        try
        {
            await CreatePendingPaymentRecordAsync(e.Booking);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not create payment record: {ex.Message}");
        }
    }

    /// <summary>
    /// Inserts a Pending payment record so the booking appears in the payment queue.
    /// Called automatically via the <see cref="OnBookingConfirmed"/> event handler.
    /// </summary>
    private async Task CreatePendingPaymentRecordAsync(Booking booking)
    {
        await using var conn = await _dbContext.CreateConnectionAsync();
        await using var cmd  = new SqlCommand("sp_CreatePayment", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@BookingId",      booking.BookingId);
        cmd.Parameters.AddWithValue("@Amount",         booking.TotalAmount);
        cmd.Parameters.AddWithValue("@PaymentMethod",  (int)PaymentMethod.Cash);   // default; customer will specify at counter
        cmd.Parameters.AddWithValue("@PaymentStatus",  (int)PaymentStatus.Pending);
        cmd.Parameters.AddWithValue("@TransactionRef", $"PENDING-{booking.BookingId}");
        cmd.Parameters.AddWithValue("@PaidAt",         DBNull.Value);

        await cmd.ExecuteNonQueryAsync();
    }

    // ── ALL cancellation requests (admin view — every status) ──────────────
    public async Task<List<RefundCandidate>> GetAllCancellationRequestsAsync()
    {
        var list = new List<RefundCandidate>();

        await using var conn = await _dbContext.CreateConnectionAsync();
        await using var cmd  = new SqlCommand(@"
            SELECT
                c.CancellationId,
                c.BookingId,
                ISNULL(u.FullName,  '') AS CustomerName,
                ISNULL(h.HallName,  '') AS HallName,
                c.RefundAmount,
                c.CancellationDate,
                ISNULL(c.Reason,    '') AS Reason,
                ISNULL(r.RefundStatus,  'Pending') AS RefundStatus,
                ISNULL(c.Status,       'Pending') AS CancellationStatus,
                ISNULL(c.AdminRemarks, '')          AS AdminRemarks
            FROM   Cancellations c
            INNER JOIN dbo.Bookings  b ON b.BookingId  = c.BookingId
            LEFT  JOIN dbo.Users     u ON u.UserId      = b.UserId
            LEFT  JOIN dbo.Halls     h ON h.HallId      = b.HallId
            OUTER APPLY (
                SELECT TOP 1 RefundStatus
                FROM   dbo.Refunds rr
                WHERE  rr.CancellationId = c.CancellationId
                ORDER  BY rr.RefundId DESC
            ) r
            ORDER  BY c.CancellationDate DESC", conn)
        {
            CommandType = CommandType.Text
        };

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            list.Add(MapRefundCandidate(reader));

        return list;
    }

    // ── Approve or Reject a pending cancellation request ──────────────────
    public async Task<bool> UpdateCancellationStatusAsync(
        int    cancellationId,
        string status,          // 'Approved' or 'Rejected'
        string adminRemarks)
    {
        try
        {
            await using var conn = await _dbContext.CreateConnectionAsync();
            await using var cmd  = new SqlCommand("dbo.sp_UpdateCancellationStatus", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@CancellationId", cancellationId);
            cmd.Parameters.AddWithValue("@Status",         status);
            cmd.Parameters.AddWithValue("@AdminRemarks",
                string.IsNullOrWhiteSpace(adminRemarks) ? (object)DBNull.Value : adminRemarks);

            await using var reader = await cmd.ExecuteReaderAsync();
            return await reader.ReadAsync(); // SP returns the updated row
        }
        catch (SqlException ex)
        {
            throw new PaymentException($"Failed to update cancellation status: {ex.Message}", ex);
        }
    }

    // ── GET cancellations and refunds for a specific customer ──────────────
    public async Task<List<RefundCandidate>> GetCancellationsByCustomerAsync(int customerId)
    {
        var list = new List<RefundCandidate>();

        await using var conn = await _dbContext.CreateConnectionAsync();
        await using var cmd  = new SqlCommand(@"
            SELECT
                c.CancellationId,
                c.BookingId,
                ISNULL(u.FullName,  '') AS CustomerName,
                ISNULL(h.HallName,  '') AS HallName,
                c.RefundAmount,
                c.CancellationDate,
                ISNULL(c.Reason,    '') AS Reason,
                ISNULL(r.RefundStatus,  'Pending') AS RefundStatus,
                ISNULL(c.Status,       'Pending') AS CancellationStatus,
                ISNULL(c.AdminRemarks, '')          AS AdminRemarks
            FROM   Cancellations c
            INNER JOIN dbo.Bookings  b ON b.BookingId  = c.BookingId
            LEFT  JOIN dbo.Users     u ON u.UserId      = b.UserId
            LEFT  JOIN dbo.Halls     h ON h.HallId      = b.HallId
            OUTER APPLY (
                SELECT TOP 1 RefundStatus
                FROM   dbo.Refunds rr
                WHERE  rr.CancellationId = c.CancellationId
                ORDER  BY rr.RefundId DESC
            ) r
            WHERE  b.UserId = @CustomerId
            ORDER  BY c.CancellationDate DESC", conn)
        {
            CommandType = CommandType.Text
        };
        cmd.Parameters.AddWithValue("@CustomerId", customerId);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            list.Add(MapRefundCandidate(reader));

        return list;
    }

    private static RefundCandidate MapRefundCandidate(SqlDataReader r) => new RefundCandidate(
        r.GetInt32(r.GetOrdinal("CancellationId")),
        r.GetInt32(r.GetOrdinal("BookingId")),
        r.IsDBNull(r.GetOrdinal("CustomerName"))       ? string.Empty  : r.GetString(r.GetOrdinal("CustomerName")),
        r.IsDBNull(r.GetOrdinal("HallName"))           ? string.Empty  : r.GetString(r.GetOrdinal("HallName")),
        r.GetDecimal(r.GetOrdinal("RefundAmount")),
        r.GetDateTime(r.GetOrdinal("CancellationDate")),
        r.IsDBNull(r.GetOrdinal("Reason"))             ? string.Empty  : r.GetString(r.GetOrdinal("Reason")),
        r.IsDBNull(r.GetOrdinal("RefundStatus"))       ? "Pending"     : r.GetString(r.GetOrdinal("RefundStatus")),
        r.IsDBNull(r.GetOrdinal("CancellationStatus")) ? "Pending"     : r.GetString(r.GetOrdinal("CancellationStatus")),
        r.IsDBNull(r.GetOrdinal("AdminRemarks"))       ? string.Empty  : r.GetString(r.GetOrdinal("AdminRemarks"))
    );
}
