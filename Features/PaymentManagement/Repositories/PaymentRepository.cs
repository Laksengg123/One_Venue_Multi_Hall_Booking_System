using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using VenueBookingSystem.Features.Authentication;
using VenueBookingSystem.Features.Payments.Interfaces;
using VenueBookingSystem.Storage;

namespace VenueBookingSystem.Features.Payments.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly DatabaseContext _dbContext;

        public PaymentRepository(DatabaseContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<bool> CreatePaymentAsync(Payment payment)
        {
            await using var conn = await _dbContext.CreateConnectionAsync();
            await using var cmd = new SqlCommand("sp_CreatePayment", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@BookingId", payment.BookingId);
            cmd.Parameters.AddWithValue("@Amount", payment.Amount);
            cmd.Parameters.AddWithValue("@PaymentMethod", (int)payment.Method);
            cmd.Parameters.AddWithValue("@PaymentStatus", (int)payment.Status);
            cmd.Parameters.AddWithValue("@TransactionRef", payment.TransactionRef);
            cmd.Parameters.AddWithValue("@PaidAt", payment.PaidAt ?? (object)DBNull.Value);

            await cmd.ExecuteNonQueryAsync();
            return true;
        }

        public async Task<Payment?> GetPaymentByBookingAsync(int bookingId)
        {
            await using var conn = await _dbContext.CreateConnectionAsync();
            await using var cmd = new SqlCommand("sp_GetPaymentByBooking", conn)
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
            await using var cmd = new SqlCommand("sp_GetPaymentsByCustomer", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@CustomerId", customerId);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                payments.Add(MapPayment(reader));

            return payments;
        }

        public async Task<List<Payment>> GetAllPaymentsAsync(int page, int pageSize)
        {
            var payments = new List<Payment>();
            await using var conn = await _dbContext.CreateConnectionAsync();
            await using var cmd = new SqlCommand("sp_GetAllPayments", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@Page", page);
            cmd.Parameters.AddWithValue("@PageSize", pageSize);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                payments.Add(MapPayment(reader));

            return payments;
        }

        private static Payment MapPayment(SqlDataReader r) => new Payment(
            r.GetInt32(r.GetOrdinal("PaymentId")),
            r.GetInt32(r.GetOrdinal("BookingId")),
            r.GetDecimal(r.GetOrdinal("Amount")),
            (PaymentMethod)r.GetInt32(r.GetOrdinal("PaymentMethod")),
            (PaymentStatus)r.GetInt32(r.GetOrdinal("PaymentStatus")),
            r.IsDBNull(r.GetOrdinal("TransactionRef")) ? string.Empty : r.GetString(r.GetOrdinal("TransactionRef")),
            r.IsDBNull(r.GetOrdinal("PaidAt")) ? null : r.GetDateTime(r.GetOrdinal("PaidAt")),
            r.GetDateTime(r.GetOrdinal("CreatedAt"))
        );
    }
}
