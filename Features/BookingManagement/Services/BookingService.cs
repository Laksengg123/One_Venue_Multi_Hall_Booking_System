using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using VenueBookingSystem.Features.Bookings.Exceptions;
using VenueBookingSystem.Features.Authentication;
using VenueBookingSystem.Shared;
using VenueBookingSystem.Storage;
using VenueBookingSystem.Features.Bookings.Events;

namespace VenueBookingSystem.Features.Bookings;

/// <summary>
/// Serves as the primary domain service for managing the booking lifecycle.
/// Encapsulates complex business rules such as date-range collision detection, 
/// state transition validations (e.g. Pending -> Confirmed -> Cancelled), 
/// and acts as a publisher for domain events like <see cref="BookingCreated"/> 
/// to facilitate decoupled side-effects (like Payment processing).
/// </summary>
public class BookingService : IBooking
{
    private readonly DatabaseContext _dbContext;

    public event EventHandler<BookingConfirmedEventArgs>? BookingConfirmed;

    public event EventHandler<BookingCancelledEventArgs>? BookingCancelled;

    public event EventHandler<BookingCreatedEventArgs>? BookingCreated;


    public BookingService(DatabaseContext dbContext)
    {
        _dbContext = dbContext;
    }
    private static Booking MapBooking(SqlDataReader r) => new Booking(
        r.GetInt32(r.GetOrdinal("BookingId")),
        r.GetInt32(r.GetOrdinal("CustomerId")),
        r.IsDBNull(r.GetOrdinal("CustomerName")) ? string.Empty : r.GetString(r.GetOrdinal("CustomerName")),
        r.GetInt32(r.GetOrdinal("HallId")),
        r.IsDBNull(r.GetOrdinal("HallName")) ? string.Empty : r.GetString(r.GetOrdinal("HallName")),
        r.GetDateTime(r.GetOrdinal("StartDateTime")),
        r.GetDateTime(r.GetOrdinal("EndDateTime")),
        r.GetDecimal(r.GetOrdinal("TotalHours")),
        r.GetDecimal(r.GetOrdinal("TotalAmount")),
        (BookingStatus)r.GetInt32(r.GetOrdinal("Status")),
        r.IsDBNull(r.GetOrdinal("Purpose")) ? string.Empty : r.GetString(r.GetOrdinal("Purpose")),
        r.GetInt32(r.GetOrdinal("GuestCount")),
        r.GetDateTime(r.GetOrdinal("CreatedAt")),
        r.GetDateTime(r.GetOrdinal("UpdatedAt"))
    );

    public static Dictionary<string, Func<Booking, string>> TableColumns => new()
    {
        ["Booking Id"] = b => b.BookingId.ToString(),
        ["Hall"]     = b => b.HallName,
        ["Customer"] = b => b.CustomerName,
        ["Start"]    = b => ConsoleHelper.FormatDateTime(b.StartDateTime),
        ["End"]      = b => ConsoleHelper.FormatDateTime(b.EndDateTime),
        ["Duration"] = b => b.GetDurationDisplay(),
        ["Amount"]   = b => ConsoleHelper.FormatCurrency(b.TotalAmount),
        ["Status"]   = b => b.GetStatusDisplay(),
        ["Purpose"]  = b => b.Purpose.Length > 20 ? b.Purpose[..20] + "..." : b.Purpose
    };

    public async Task<int> CreateBookingAsync(Booking booking)
    {
        try
        {
            await using var conn = await _dbContext.CreateConnectionAsync();
            await using var cmd  = new SqlCommand("sp_CreateBooking", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@CustomerId",     booking.CustomerId);
            cmd.Parameters.AddWithValue("@HallId",         booking.HallId);
            cmd.Parameters.AddWithValue("@StartDateTime",  booking.StartDateTime);
            cmd.Parameters.AddWithValue("@EndDateTime",    booking.EndDateTime);
            cmd.Parameters.AddWithValue("@TotalHours",     booking.TotalHours);
            cmd.Parameters.AddWithValue("@TotalAmount",    booking.TotalAmount);
            cmd.Parameters.AddWithValue("@Status",         (int)BookingStatus.Pending);
            cmd.Parameters.AddWithValue("@Purpose",        (object?)booking.Purpose ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@GuestCount",     booking.GuestCount);

            var result = await cmd.ExecuteScalarAsync();
            int bookingId = result != null && result != DBNull.Value ? Convert.ToInt32(result) : 0;
            if (bookingId > 0)
            {
                var createdBooking = booking with { BookingId = bookingId };
                BookingCreated?.Invoke(this, new BookingCreatedEventArgs(createdBooking));
            }
            return bookingId;
        }
        catch (SqlException ex)
        {
            throw new BookingException($"Failed to create booking: {ex.Message}", ex);
        }
    }


    public async Task<List<Booking>> GetBookingsByCustomerAsync(int customerId, int page, int pageSize)
    {
        var bookings = new List<Booking>();

        await using var conn = await _dbContext.CreateConnectionAsync();
        await using var cmd  = new SqlCommand("sp_GetBookingsByCustomer", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@CustomerId", customerId);
        cmd.Parameters.AddWithValue("@Page",       page);
        cmd.Parameters.AddWithValue("@PageSize",   pageSize);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            bookings.Add(MapBooking(reader));

        return bookings;
    }

    public async Task<bool> CancelBookingAsync(int bookingId)
    {
        try
        {
            await using var conn = await _dbContext.CreateConnectionAsync();
            await using var cmd = new SqlCommand("sp_CancelBooking", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@BookingId", bookingId);
            cmd.Parameters.AddWithValue("@Remarks", "Cancelled by customer");

            await cmd.ExecuteNonQueryAsync();

            var booking = await GetBookingByIdAsync(bookingId);
            if (booking is not null)
                BookingCancelled?.Invoke(this, new BookingCancelledEventArgs(booking));

            return true;
        }
        catch (SqlException ex)
        {
            throw new BookingException($"Failed to cancel booking: {ex.Message}", ex);
        }
    }

    public async Task<bool> CheckAvailabilityAsync(int hallId, DateTime start, DateTime end)
    {
        await using var conn = await _dbContext.CreateConnectionAsync();
        await using var cmd  = new SqlCommand("sp_CheckHallAvailability", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@HallId",    hallId);
        cmd.Parameters.AddWithValue("@StartDateTime",  start);
        cmd.Parameters.AddWithValue("@EndDateTime",    end);

        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result) == 1;
    }

    public async Task<int> GetCustomerBookingCountAsync(int customerId)
    {
        await using var conn = await _dbContext.CreateConnectionAsync();
        await using var cmd  = new SqlCommand("sp_GetCustomerBookingCount", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@CustomerId", customerId);

        var result = await cmd.ExecuteScalarAsync();
        return result is null or DBNull ? 0 : Convert.ToInt32(result);
    }


    public async Task<List<Booking>> GetAllBookingsAsync(int page = 1, int pageSize = 10)
    {
        var bookings = new List<Booking>();

        await using var conn = await _dbContext.CreateConnectionAsync();
        await using var cmd  = new SqlCommand("sp_GetAllBookings", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@Page",     page);
        cmd.Parameters.AddWithValue("@PageSize", pageSize);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            bookings.Add(MapBooking(reader));

        return bookings;
    }

    public async Task<Booking?> GetBookingByIdAsync(int bookingId)
    {
        await using var conn = await _dbContext.CreateConnectionAsync();
        await using var cmd  = new SqlCommand("sp_GetBookingById", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@BookingId", bookingId);

        await using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? MapBooking(reader) : null;
    }


    public async Task<bool> UpdateBookingStatusAsync(int bookingId, BookingStatus status, string remarks = "")
    {
        try
        {
            await using var conn = await _dbContext.CreateConnectionAsync();
            await using var cmd  = new SqlCommand("sp_UpdateBookingStatus", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@BookingId", bookingId);
            cmd.Parameters.AddWithValue("@Status",    (int)status);
            cmd.Parameters.AddWithValue("@Remarks",   (object?)remarks ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync();

            if (status == BookingStatus.Confirmed || status == BookingStatus.Cancelled)
                {
                    var booking = await GetBookingByIdAsync(bookingId);
                    if (booking is not null)
                    {
                    if (status == BookingStatus.Confirmed)
                        BookingConfirmed?.Invoke(this, new BookingConfirmedEventArgs(booking));
                    else
                        BookingCancelled?.Invoke(this, new BookingCancelledEventArgs(booking));
                }
            }

            return true;
        }
        catch (SqlException ex)
        {
            throw new BookingException($"Failed to update booking status: {ex.Message}", ex);
        }
    }


    public async Task<int> GetTotalBookingCountAsync()
    {
        await using var conn = await _dbContext.CreateConnectionAsync();
        await using var cmd  = new SqlCommand("sp_GetTotalBookingCount", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        var result = await cmd.ExecuteScalarAsync();
        return result is null or DBNull ? 0 : Convert.ToInt32(result);
    }

   
    public async Task<List<Booking>> GetPendingBookingsAsync()
    {
        var bookings = new List<Booking>();

        await using var conn = await _dbContext.CreateConnectionAsync();
        await using var cmd  = new SqlCommand("sp_GetPendingBookings", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            bookings.Add(MapBooking(reader));

        return bookings;
    }


    public async Task<List<Booking>> GetBookingsByDateRangeAsync(DateTime start, DateTime end)
    {
        var bookings = new List<Booking>();

        await using var conn = await _dbContext.CreateConnectionAsync();
        await using var cmd  = new SqlCommand("sp_GetBookingsByDateRange", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@StartDate", start);
        cmd.Parameters.AddWithValue("@EndDate",   end);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            bookings.Add(MapBooking(reader));

        return bookings;
    }

    public async Task<List<Booking>> GetAllBookingsForHallAsync(int hallId)
    {
        var bookings = new List<Booking>();
        await using var conn = await _dbContext.CreateConnectionAsync();
        await using var cmd = new SqlCommand("sp_GetAllBookings", conn)
        {
            CommandType = CommandType.StoredProcedure
        };
        // Fetch all records; filter in-memory by hallId
        cmd.Parameters.AddWithValue("@Page", 1);
        cmd.Parameters.AddWithValue("@PageSize", 10000);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var b = MapBooking(reader);
            if (b.HallId == hallId)
                bookings.Add(b);
        }
        return bookings;
    }
}
