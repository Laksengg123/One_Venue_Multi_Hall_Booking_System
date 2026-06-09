using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using VenueBookingSystem.Exceptions;
using VenueBookingSystem.Storage;
using VenueBookingSystem.Shared;
using VenueBookingSystem.Features.Authentication.Events;
using VenueBookingSystem.Features.Authentication.Models;
using VenueBookingSystem.Features.Bookings.Exceptions;

namespace VenueBookingSystem.Features.Authentication
{
    /// <summary>
    /// Core security and identity management service.
    /// Handles user authentication, password hashing via BCrypt (Cost 12), and role-based session state.
    /// Manages transient lockouts after consecutive failed authentication attempts to mitigate brute-force attacks.
    /// </summary>
    public class AuthService
    {
        private User? _currentUser;
        private readonly DatabaseContext _dbContext;
        private int _failedAttempts;
        private const int MAX_ATTEMPTS = 3;

        public event EventHandler<UserLoggedInEventArgs>? UserLoggedIn;
        public event EventHandler<UserLoggedOutEventArgs>? UserLoggedOut;



        public AuthService(DatabaseContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public User? CurrentUser => _currentUser;

        public bool IsLoggedIn => _currentUser is not null;
        public bool IsAdmin => _currentUser?.Role == UserRole.Admin;

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            username = (username ?? string.Empty).Trim();
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
            email = (email ?? string.Empty).Trim();
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

        public User? GetUserByUsername(string username)
        {
            return GetUserByUsernameAsync(username).GetAwaiter().GetResult();
        }

        // Concept: Method Overloading (AuthenticateAsync with User object)
        public async Task<bool> AuthenticateAsync(User? user, string password)
        {
            if (user == null || !user.IsActive)
                return false;

            return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        }

        public async Task<bool> AuthenticateAsync(string username, string password)
        {
            User? user = await FindUserForLoginAsync(username);
            return await AuthenticateAsync(user, password);
        }

        // Concept: Method Overloading (Authenticate with User object)
        public bool Authenticate(User? user, string password)
        {
            if (user == null || !user.IsActive)
                return false;

            return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        }

        public bool Authenticate(string username, string password)
        {
            User? user = FindUserForLoginAsync(username).GetAwaiter().GetResult();
            return Authenticate(user, password);
        }

        // Concept: Method Overloading (LoginAsync with User object)
        public async Task<User?> LoginAsync(User? user, string password)
        {
            if (user == null)
                return null;

            if (_failedAttempts >= MAX_ATTEMPTS)
                throw new BookingException($"Account locked after {MAX_ATTEMPTS} failed attempts. Please restart the application.");

            bool passwordValid = await AuthenticateAsync(user, password);

            if (!passwordValid || !user.IsActive)
            {
                _failedAttempts++;

                if (_failedAttempts >= MAX_ATTEMPTS)
                    throw new BookingException(
                        $"Account locked after {MAX_ATTEMPTS} failed attempts. Please restart the application.");

                ConsoleHelper.PrintWarning($"Login failed: invalid password for username '{user.Username}'.");
                return null;
            }

            _currentUser = user;
            _failedAttempts = 0;

            var session = new AuthSession();

            UserLoggedIn?.Invoke(this, new UserLoggedInEventArgs(user));

            return user;
        }

        public async Task<User?> LoginAsync(string username, string password)
        {
            username = (username ?? string.Empty).Trim();
            password = (password ?? string.Empty).Trim();

            if (_failedAttempts >= MAX_ATTEMPTS)
                throw new BookingException($"Account locked after {MAX_ATTEMPTS} failed attempts. Please restart the application.");

            var user = await FindUserForLoginAsync(username);

            if (user == null)
            {
                _failedAttempts++;
                string loginType = ValidationHelper.ValidateEmail(username) ? "email" : "username";
                ConsoleHelper.PrintWarning($"Login failed: {loginType} '{username}' not found.");
                return null;
            }

            bool passwordValid = await AuthenticateAsync(user, password);
            if (!passwordValid || !user.IsActive)
            {
                _failedAttempts++;

                if (_failedAttempts >= MAX_ATTEMPTS)
                    throw new BookingException($"Account locked after {MAX_ATTEMPTS} failed attempts. Please restart the application.");

                ConsoleHelper.PrintWarning($"Login failed: invalid password for username '{user.Username}'.");
                return null;
            }

            _currentUser = user;
            _failedAttempts = 0;

            UserLoggedIn?.Invoke(this, new UserLoggedInEventArgs(user));
            return user;
        }

        private async Task<User?> FindUserForLoginAsync(string usernameOrEmail)
        {
            usernameOrEmail = (usernameOrEmail ?? string.Empty).Trim();

            return ValidationHelper.ValidateEmail(usernameOrEmail)
                ? await GetUserByEmailAsync(usernameOrEmail)
                : await GetUserByUsernameAsync(usernameOrEmail);
        }

        public void Logout()
        {
            _currentUser = null;
            _failedAttempts = 0;

            UserLoggedOut?.Invoke(this, new UserLoggedOutEventArgs());
        }

        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            await using var conn = await _dbContext.CreateConnectionAsync();

            await using var fetchUsernameCmd = new SqlCommand(
                "SELECT Username FROM Users WHERE UserId = @UserId", conn)
            {
                CommandType = CommandType.Text
            };
            fetchUsernameCmd.Parameters.AddWithValue("@UserId", userId);

            var username = (string?)await fetchUsernameCmd.ExecuteScalarAsync();
            if (username is null)
                return false;
            bool currentPasswordValid = await AuthenticateAsync(username, currentPassword);
            if (!currentPasswordValid)
                return false;

            string newHash = BCrypt.Net.BCrypt.HashPassword(newPassword, 12);

            await using var updateCmd = new SqlCommand("sp_UpdateUserPassword", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            updateCmd.Parameters.AddWithValue("@UserId", userId);
            updateCmd.Parameters.AddWithValue("@PasswordHash", newHash);

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

            if (_currentUser != null && _currentUser.UserId == userId)
            {
                _currentUser = _currentUser with { Email = email, Phone = phone, FullName = fullName };
            }

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

        public async Task<bool> CreateUserAsync(
            string fullName,
            string username,
            string email,
            string phone,
            string password,
            UserRole role)
        {
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(password, 12);

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
            catch (SqlException ex) when (ex.Number == 2627)
            {
                // Username duplicate (unique constraint)
                return false;
            }
            catch (SqlException ex) when (ex.Number == 547)
            {
                // Check constraint violation (e.g., phone format)
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
