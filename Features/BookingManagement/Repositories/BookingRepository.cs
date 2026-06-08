using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using VenueBookingSystem.Features.Authentication;
using VenueBookingSystem.Features.Bookings.Interfaces;
using VenueBookingSystem.Storage;

namespace VenueBookingSystem.Features.Bookings.Repositories
{
    public class BookingRepository : IBookingRepository
    {
        private readonly DatabaseContext _dbContext;

        public BookingRepository(DatabaseContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<List<Booking>> GetAllBookingsAsync()
        {
            var bookings = new List<Booking>();
            await using var conn = await _dbContext.CreateConnectionAsync();
            await using var cmd = new SqlCommand("sp_GetAllBookings", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                bookings.Add(MapBooking(reader));

            return bookings;
        }

        public async Task<Booking?> GetBookingByIdAsync(int bookingId)
        {
            await using var conn = await _dbContext.CreateConnectionAsync();
            await using var cmd = new SqlCommand("sp_GetBookingById", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@BookingId", bookingId);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            return MapBooking(reader);
        }

        public async Task<List<Booking>> GetBookingsByCustomerAsync(int customerId)
        {
            var bookings = new List<Booking>();
            await using var conn = await _dbContext.CreateConnectionAsync();
            await using var cmd = new SqlCommand("sp_GetBookingsByCustomer", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@CustomerId", customerId);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                bookings.Add(MapBooking(reader));

            return bookings;
        }

        public async Task<int> CreateBookingAsync(Booking booking)
        {
            await using var conn = await _dbContext.CreateConnectionAsync();
            await using var cmd = new SqlCommand("sp_CreateBooking", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@CustomerId", booking.CustomerId);
            cmd.Parameters.AddWithValue("@CustomerName", booking.CustomerName);
            cmd.Parameters.AddWithValue("@HallId", booking.HallId);
            cmd.Parameters.AddWithValue("@HallName", booking.HallName);
            cmd.Parameters.AddWithValue("@StartDateTime", booking.StartDateTime);
            cmd.Parameters.AddWithValue("@EndDateTime", booking.EndDateTime);
            cmd.Parameters.AddWithValue("@TotalHours", booking.TotalHours);
            cmd.Parameters.AddWithValue("@TotalAmount", booking.TotalAmount);
            cmd.Parameters.AddWithValue("@Status", (int)booking.Status);
            cmd.Parameters.AddWithValue("@Purpose", booking.Purpose);
            cmd.Parameters.AddWithValue("@GuestCount", booking.GuestCount);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task<bool> UpdateBookingStatusAsync(int bookingId, BookingStatus newStatus, string remarks)
        {
            await using var conn = await _dbContext.CreateConnectionAsync();
            await using var cmd = new SqlCommand("sp_UpdateBookingStatus", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@BookingId", bookingId);
            cmd.Parameters.AddWithValue("@Status", (int)newStatus);
            cmd.Parameters.AddWithValue("@Remarks", remarks);

            await cmd.ExecuteNonQueryAsync();
            return true;
        }

        public async Task<bool> CancelBookingAsync(int bookingId, string remarks)
        {
            await using var conn = await _dbContext.CreateConnectionAsync();
            await using var cmd = new SqlCommand("sp_CancelBooking", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@BookingId", bookingId);
            cmd.Parameters.AddWithValue("@Remarks", remarks);

            await cmd.ExecuteNonQueryAsync();
            return true;
        }

        private static Booking MapBooking(SqlDataReader reader) => new Booking(
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
