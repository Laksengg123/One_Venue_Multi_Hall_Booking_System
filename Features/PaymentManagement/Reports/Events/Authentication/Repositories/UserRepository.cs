using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using VenueBookingSystem.Features.Authentication.Interfaces;
using VenueBookingSystem.Features.Authentication.Models;
using VenueBookingSystem.Storage;

namespace VenueBookingSystem.Features.Authentication.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly DatabaseContext _dbContext;

        public UserRepository(DatabaseContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            await using var conn = await _dbContext.CreateConnectionAsync();
            await using var cmd = new SqlCommand("sp_GetUserByUsername", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@Username", username);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            return MapUser(reader);
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            await using var conn = await _dbContext.CreateConnectionAsync();
            await using var cmd = new SqlCommand("sp_GetUserByEmail", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@Email", email);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            return MapUser(reader);
        }

        public async Task<bool> UpdateUserPasswordAsync(int userId, string passwordHash)
        {
            await using var conn = await _dbContext.CreateConnectionAsync();
            await using var updateCmd = new SqlCommand("sp_UpdateUserPassword", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            updateCmd.Parameters.AddWithValue("@UserId", userId);
            updateCmd.Parameters.AddWithValue("@PasswordHash", passwordHash);

            await updateCmd.ExecuteNonQueryAsync();
            return true;
        }

        public async Task<bool> UpdateUserAsync(int userId, string email, string phone, string fullName)
        {
            await using var conn = await _dbContext.CreateConnectionAsync();
            await using var updateCmd = new SqlCommand("sp_UpdateUser", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            updateCmd.Parameters.AddWithValue("@UserId", userId);
            updateCmd.Parameters.AddWithValue("@Email", email);
            updateCmd.Parameters.AddWithValue("@Phone", phone);
            updateCmd.Parameters.AddWithValue("@FullName", fullName);

            await updateCmd.ExecuteNonQueryAsync();
            return true;
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            var users = new List<User>();
            await using var conn = await _dbContext.CreateConnectionAsync();
            await using var cmd = new SqlCommand("sp_GetAllUsers", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                users.Add(MapUser(reader));

            return users;
        }

        public async Task<bool> CreateUserAsync(string fullName, string username, string email, string phone, string passwordHash, UserRole role)
        {
            await using var conn = await _dbContext.CreateConnectionAsync();
            await using var cmd = new SqlCommand("sp_CreateUser", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@FullName", fullName);
            cmd.Parameters.AddWithValue("@Username", username);
            cmd.Parameters.AddWithValue("@Email", email);
            cmd.Parameters.AddWithValue("@Phone", phone);
            cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
            cmd.Parameters.AddWithValue("@Role", (int)role);

            try
            {
                await cmd.ExecuteNonQueryAsync();
                return true;
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 547)
            {
                return false;
            }
        }

        private static User MapUser(SqlDataReader reader) => new User(
            UserId:       reader.GetInt32(reader.GetOrdinal("UserId")),
            Username:     reader.GetString(reader.GetOrdinal("Username")),
            PasswordHash: reader.GetString(reader.GetOrdinal("PasswordHash")),
            Email:        reader.GetString(reader.GetOrdinal("Email")),
            Phone:        reader.GetString(reader.GetOrdinal("Phone")),
            FullName:     reader.GetString(reader.GetOrdinal("FullName")),
            Role:         (UserRole)reader.GetInt32(reader.GetOrdinal("Role")),
            IsActive:     reader.GetBoolean(reader.GetOrdinal("IsActive")),
            CreatedAt:    reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
        );
    }
}
