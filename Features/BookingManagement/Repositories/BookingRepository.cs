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
    // ╔══════════════════════════════════════════════════════════════════════╗
    // ║           🗄️ BOOKING REPOSITORY — THE FILING CABINET WORKER         ║
    // ║  This is the ONLY class that directly talks to the SQL database.    ║
    // ║  Nobody else should run SQL — they ask this worker to do it.        ║
    // ║  Think: Filing Cabinet Room. You ask the worker, they fetch it.     ║
    // ╚══════════════════════════════════════════════════════════════════════╝
    public class BookingRepository : IBookingRepository // "I follow the IBookingRepository contract"
    {
        // 🔑 The key to the database — private and readonly (set once, never changed)
        private readonly DatabaseContext _dbContext;

        // 🏗️ CONSTRUCTOR — "Give me a database connection or I refuse to work"
        //    If no database is given (null), throw an error immediately
        //    Because without a database, this class is completely useless
        public BookingRepository(DatabaseContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            //                     ↑ If dbContext is null → crash with a clear error message
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 📦 GET ALL BOOKINGS — Fetch every booking from the database
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        public async Task<List<Booking>> GetAllBookingsAsync()
        {
            var bookings = new List<Booking>(); // Start with an empty list

            await using var conn = await _dbContext.CreateConnectionAsync(); // Open DB connection
            await using var cmd = new SqlCommand("sp_GetAllBookings", conn)  // Use stored procedure
            {
                CommandType = CommandType.StoredProcedure
            };

            // Read rows one by one — like reading each line of a spreadsheet
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync()) // "Keep reading until no more rows"
                bookings.Add(MapBooking(reader)); // Convert each row → Booking object

            return bookings; // Return the full list
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 🔎 FIND ONE BOOKING — Get a single booking by its ID
        //    Returns null if the booking ID does not exist in the database
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        public async Task<Booking?> GetBookingByIdAsync(int bookingId)
        {
            await using var conn = await _dbContext.CreateConnectionAsync();
            await using var cmd = new SqlCommand("sp_GetBookingById", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@BookingId", bookingId); // Tell DB which booking we want

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) // If NO rows came back → booking not found
                return null; // Return nothing (null)

            return MapBooking(reader); // Convert row → Booking and return it
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 👤 GET BY CUSTOMER — All bookings made by one specific customer
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        public async Task<List<Booking>> GetBookingsByCustomerAsync(int customerId)
        {
            var bookings = new List<Booking>();
            await using var conn = await _dbContext.CreateConnectionAsync();
            await using var cmd = new SqlCommand("sp_GetBookingsByCustomer", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@CustomerId", customerId); // Which customer?

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                bookings.Add(MapBooking(reader));

            return bookings;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // ➕ CREATE BOOKING — Save a brand new booking to the database
        //    Returns the new Booking ID assigned by the database
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        public async Task<int> CreateBookingAsync(Booking booking)
        {
            await using var conn = await _dbContext.CreateConnectionAsync();
            await using var cmd = new SqlCommand("sp_CreateBooking", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            // Fill in ALL the booking form fields for the database
            cmd.Parameters.AddWithValue("@CustomerId",   booking.CustomerId);   // Who is booking
            cmd.Parameters.AddWithValue("@CustomerName", booking.CustomerName); // Customer's name
            cmd.Parameters.AddWithValue("@HallId",       booking.HallId);       // Which hall
            cmd.Parameters.AddWithValue("@HallName",     booking.HallName);     // Hall's name
            cmd.Parameters.AddWithValue("@StartDateTime",booking.StartDateTime); // When it starts
            cmd.Parameters.AddWithValue("@EndDateTime",  booking.EndDateTime);   // When it ends
            cmd.Parameters.AddWithValue("@TotalHours",   booking.TotalHours);   // Duration
            cmd.Parameters.AddWithValue("@TotalAmount",  booking.TotalAmount);  // Total cost
            cmd.Parameters.AddWithValue("@Status",       (int)booking.Status);  // Status as number
            cmd.Parameters.AddWithValue("@Purpose",      booking.Purpose);      // What it's for
            cmd.Parameters.AddWithValue("@GuestCount",   booking.GuestCount);   // How many guests

            // Send to database and get back the new Booking ID
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result); // Convert the result to an integer and return
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 🔄 UPDATE STATUS — Change the status of an existing booking
        //    e.g. Pending → Confirmed, Confirmed → Cancelled
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        public async Task<bool> UpdateBookingStatusAsync(int bookingId, BookingStatus newStatus, string remarks)
        {
            await using var conn = await _dbContext.CreateConnectionAsync();
            await using var cmd = new SqlCommand("sp_UpdateBookingStatus", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@BookingId", bookingId);        // Which booking to update
            cmd.Parameters.AddWithValue("@Status",    (int)newStatus);   // New status as a number
            cmd.Parameters.AddWithValue("@Remarks",   remarks);          // Why was status changed?

            await cmd.ExecuteNonQueryAsync(); // Run the update (no return value needed)
            return true; // Return true = success
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // ❌ CANCEL BOOKING — Mark a booking as cancelled in the database
        //    Also saves the reason why it was cancelled
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        public async Task<bool> CancelBookingAsync(int bookingId, string remarks)
        {
            await using var conn = await _dbContext.CreateConnectionAsync();
            await using var cmd = new SqlCommand("dbo.usp_CancelBooking", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@BookingId", bookingId); // Which booking to cancel
            cmd.Parameters.AddWithValue("@Reason",    remarks);   // Reason for cancellation

            await cmd.ExecuteNonQueryAsync(); // Run it — no data comes back
            return true;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 🔄 MAP BOOKING — Translates ONE database row → C# Booking object
        //    The database gives a raw spreadsheet row (SqlDataReader)
        //    This method reads each named column and builds a proper Booking
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private static Booking MapBooking(SqlDataReader reader) => new Booking(
            reader.GetInt32(reader.GetOrdinal("BookingId")),    // Read "BookingId" column → int
            reader.GetInt32(reader.GetOrdinal("CustomerId")),   // Read "CustomerId" column → int

            // Text columns need a NULL check first — database cells can be empty!
            // Pattern: "Is it empty? → use blank text : otherwise read the actual value"
            reader.IsDBNull(reader.GetOrdinal("CustomerName")) ? string.Empty : reader.GetString(reader.GetOrdinal("CustomerName")),
            reader.GetInt32(reader.GetOrdinal("HallId")),
            reader.IsDBNull(reader.GetOrdinal("HallName")) ? string.Empty : reader.GetString(reader.GetOrdinal("HallName")),

            reader.GetDateTime(reader.GetOrdinal("StartDateTime")), // Read date+time column
            reader.GetDateTime(reader.GetOrdinal("EndDateTime")),
            reader.GetDecimal(reader.GetOrdinal("TotalHours")),     // Read decimal column
            reader.GetDecimal(reader.GetOrdinal("TotalAmount")),

            // Status is stored as a number (1, 2, 3...) — convert it to the BookingStatus enum
            (BookingStatus)reader.GetInt32(reader.GetOrdinal("Status")),

            reader.IsDBNull(reader.GetOrdinal("Purpose")) ? string.Empty : reader.GetString(reader.GetOrdinal("Purpose")),
            reader.GetInt32(reader.GetOrdinal("GuestCount")),
            reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
        );
    }
}
