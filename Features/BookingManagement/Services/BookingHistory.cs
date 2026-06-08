using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using VenueBookingSystem.Shared;
using VenueBookingSystem.Storage;
using VenueBookingSystem.Features.Bookings.DTOs;

namespace VenueBookingSystem.Features.Bookings;

public class BookingHistory
{
    private readonly DatabaseContext _dbContext;

    public BookingHistory(DatabaseContext dbContext)
    {
        _dbContext = dbContext;
    }



    private static Dictionary<string, Func<BookingHistoryEntry, string>> HistoryColumns => new()
    {
        ["#"]          = h => h.HistoryId.ToString(),
        ["Old Status"] = h => h.OldStatus,
        ["New Status"] = h => h.NewStatus,
        ["Changed At"] = h => ConsoleHelper.FormatDateTime(h.ChangedAt),
        ["Changed By"] = h => h.ChangedBy,
        ["Remarks"]    = h => h.Remarks
    };

  
    private static BookingHistoryEntry MapEntry(SqlDataReader r) => new BookingHistoryEntry(
        r.GetInt32(r.GetOrdinal("HistoryId")),
        r.GetInt32(r.GetOrdinal("BookingId")),
        r.IsDBNull(r.GetOrdinal("OldStatus"))   ? string.Empty : r.GetString(r.GetOrdinal("OldStatus")),
        r.IsDBNull(r.GetOrdinal("NewStatus"))   ? string.Empty : r.GetString(r.GetOrdinal("NewStatus")),
        r.GetDateTime(r.GetOrdinal("ChangedAt")),
        r.IsDBNull(r.GetOrdinal("ChangedBy"))   ? string.Empty : r.GetString(r.GetOrdinal("ChangedBy")),
        r.IsDBNull(r.GetOrdinal("Remarks"))     ? string.Empty : r.GetString(r.GetOrdinal("Remarks"))
    );

   
    public async Task<List<BookingHistoryEntry>> GetHistoryByBookingAsync(int bookingId)
    {
        var entries = new List<BookingHistoryEntry>();

        await using var conn = await _dbContext.CreateConnectionAsync();
        await using var cmd  = new SqlCommand("sp_GetBookingHistory", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@BookingId", bookingId);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            entries.Add(MapEntry(reader));

        return entries;
    }

    public async Task DisplayHistoryAsync(int bookingId)
    {
        ConsoleHelper.PrintHeader($"Status History — Booking #{bookingId}");

        var history = await GetHistoryByBookingAsync(bookingId);

        if (history.Count == 0)
        {
            ConsoleHelper.PrintWarning("No history records found for this booking.");
            return;
        }

        ConsoleHelper.PrintTable(history, HistoryColumns, $"Booking #{bookingId} History");
        ConsoleHelper.PressAnyKey();
    }
}
