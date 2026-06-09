using System.Collections.Generic;
using System.Threading.Tasks;

namespace VenueBookingSystem.Features.Authentication.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetUserByUsernameAsync(string username);
        Task<User?> GetUserByEmailAsync(string email);
        Task<bool> UpdateUserPasswordAsync(int userId, string passwordHash);
        Task<bool> UpdateUserAsync(int userId, string email, string phone, string fullName);
        Task<List<User>> GetAllUsersAsync();
        Task<bool> CreateUserAsync(string fullName, string username, string email, string phone, string passwordHash, UserRole role);
    }
}
