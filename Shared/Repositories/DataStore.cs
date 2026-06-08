using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using VenueBookingSystem.Storage;

namespace VenueBookingSystem.Shared
{
    public class GenericRepository<T> where T : class
    {
        private readonly DatabaseContext _dbContext;

        private readonly Func<SqlDataReader, T> _mapper;

        public GenericRepository(DatabaseContext dbContext, Func<SqlDataReader, T> mapper)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _mapper    = mapper    ?? throw new ArgumentNullException(nameof(mapper));
        }

       
        public async Task<List<T>> GetAllAsync(
            string storedProcedure,
            SqlParameter[]? parameters = null)
        {
            var results = new List<T>();

            await using var conn = await _dbContext.CreateConnectionAsync();
            await using var cmd  = new SqlCommand(storedProcedure, conn);
            cmd.CommandType = CommandType.StoredProcedure;

            if (parameters is not null)
                cmd.Parameters.AddRange(parameters);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(_mapper(reader));   // delegate call: SqlDataReader → T
            }

            return results;
        }


        public async Task<T?> GetByIdAsync(
            string storedProcedure,
            SqlParameter[] parameters)
        {
            await using var conn = await _dbContext.CreateConnectionAsync();
            await using var cmd  = new SqlCommand(storedProcedure, conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddRange(parameters);

            await using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
                return _mapper(reader);

            return null;
        }

        public async Task<int> ExecuteAsync(
            string storedProcedure,
            SqlParameter[]? parameters = null)
        {
            await using var conn = await _dbContext.CreateConnectionAsync();
            await using var cmd  = new SqlCommand(storedProcedure, conn);
            cmd.CommandType = CommandType.StoredProcedure;

            if (parameters is not null)
                cmd.Parameters.AddRange(parameters);

            return await cmd.ExecuteNonQueryAsync();
        }

  
        public async Task<object?> ExecuteScalarAsync(
            string storedProcedure,
            SqlParameter[]? parameters = null)
        {
            await using var conn = await _dbContext.CreateConnectionAsync();
            await using var cmd  = new SqlCommand(storedProcedure, conn);
            cmd.CommandType = CommandType.StoredProcedure;

            if (parameters is not null)
                cmd.Parameters.AddRange(parameters);

            object? scalar = await cmd.ExecuteScalarAsync();
            return scalar == DBNull.Value ? null : scalar;
        }

        
        public async Task<(List<T> Items, int TotalCount)> GetPagedAsync(
            string storedProcedure,
            int page,
            int pageSize,
            SqlParameter[]? parameters = null)
        {
            var results = new List<T>();
            int totalCount = 0;

            await using var conn = await _dbContext.CreateConnectionAsync();
            await using var cmd  = new SqlCommand(storedProcedure, conn);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@Page",     page);
            cmd.Parameters.AddWithValue("@PageSize", pageSize);

            if (parameters is not null)
                cmd.Parameters.AddRange(parameters);

            await using var reader = await cmd.ExecuteReaderAsync();

            bool firstRow = true;
            while (await reader.ReadAsync())
            {
                // Read TotalCount from the first row (all rows have the same value)
                if (firstRow)
                {
                    int totalOrdinal = reader.GetOrdinal("TotalCount");
                    totalCount = reader.IsDBNull(totalOrdinal) ? 0 : reader.GetInt32(totalOrdinal);
                    firstRow = false;
                }

                results.Add(_mapper(reader));   // delegate call: SqlDataReader → T
            }

            return (results, totalCount);
        }
    }
}
