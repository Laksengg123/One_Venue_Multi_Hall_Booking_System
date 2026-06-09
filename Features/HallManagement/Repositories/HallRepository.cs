using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using VenueBookingSystem.Features.Halls.Interfaces;
using VenueBookingSystem.Features.Halls.Mapping;
using VenueBookingSystem.Storage;

namespace VenueBookingSystem.Features.Halls.Repositories
{
    public class HallRepository : IHallRepository
    {
        private readonly DatabaseContext _dbContext;

        public HallRepository(DatabaseContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<List<Hall>> GetAllHallsAsync()
        {
            var halls = new List<Hall>();

            await using var conn = await _dbContext.CreateConnectionAsync();
            await using var cmd = new SqlCommand("sp_GetAllHalls", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                halls.Add(HallMapper.FromReader(reader));

            // Newest hall (highest HallId) appears first
            halls.Sort((a, b) => b.HallId.CompareTo(a.HallId));

            return halls;
        }

        public async Task<Hall?> GetHallByIdAsync(int hallId)
        {
            await using var conn = await _dbContext.CreateConnectionAsync();
            await using var cmd = new SqlCommand("sp_GetHallById", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@HallId", hallId);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            return HallMapper.FromReader(reader);
        }

        public async Task<bool> AddHallAsync(Hall hall)
        {
            await using var conn = await _dbContext.CreateConnectionAsync();
            await using var cmd = new SqlCommand("sp_AddHall", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@Name", hall.HallName);
            cmd.Parameters.AddWithValue("@HallType", hall.HallType);
            cmd.Parameters.AddWithValue("@Capacity", hall.Capacity);
            cmd.Parameters.AddWithValue("@PricePerHour", hall.PricePerHour);
            cmd.Parameters.AddWithValue("@Description", hall.Description);
            cmd.Parameters.AddWithValue("@Location", hall.Location);
            cmd.Parameters.AddWithValue("@HasAC", hall.HasAC);
            cmd.Parameters.AddWithValue("@HasProjector", hall.HasProjector);
            cmd.Parameters.AddWithValue("@HasWifi", hall.HasWifi);

            await cmd.ExecuteNonQueryAsync();
            return true;
        }

        public async Task<bool> UpdateHallAsync(Hall hall)
        {
            await using var conn = await _dbContext.CreateConnectionAsync();
            await using var cmd = new SqlCommand("sp_UpdateHall", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@HallId", hall.HallId);
            cmd.Parameters.AddWithValue("@Name", hall.HallName);
            cmd.Parameters.AddWithValue("@HallType", hall.HallType);
            cmd.Parameters.AddWithValue("@Capacity", hall.Capacity);
            cmd.Parameters.AddWithValue("@PricePerHour", hall.PricePerHour);
            cmd.Parameters.AddWithValue("@Description", hall.Description);
            cmd.Parameters.AddWithValue("@Location", hall.Location);
            cmd.Parameters.AddWithValue("@HasAC", hall.HasAC);
            cmd.Parameters.AddWithValue("@HasProjector", hall.HasProjector);
            cmd.Parameters.AddWithValue("@HasWifi", hall.HasWifi);
            cmd.Parameters.AddWithValue("@IsActive", hall.IsActive);

            await cmd.ExecuteNonQueryAsync();
            return true;
        }

        public async Task<bool> DeleteHallAsync(int hallId)
        {
            await using var conn = await _dbContext.CreateConnectionAsync();
            await using var cmd = new SqlCommand("sp_DeleteHall", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@HallId", hallId);

            await cmd.ExecuteNonQueryAsync();
            return true;
        }

        public async Task<bool> IsHallAvailableAsync(int hallId, DateTime start, DateTime end)
        {
            await using var conn = await _dbContext.CreateConnectionAsync();
            await using var cmd = new SqlCommand("sp_CheckHallAvailability", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@HallId", hallId);
            cmd.Parameters.AddWithValue("@StartDateTime", start);
            cmd.Parameters.AddWithValue("@EndDateTime", end);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result) == 1;
        }
    }
}
