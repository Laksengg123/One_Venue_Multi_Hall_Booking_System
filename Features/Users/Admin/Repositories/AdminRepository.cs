using System.Threading.Tasks;
using VenueBookingSystem.Features.Users.Admin.Interfaces;
using VenueBookingSystem.Features.Users.Admin.Models;

namespace VenueBookingSystem.Features.Users.Admin.Repositories
{
    public class AdminRepository : IAdminRepository
    {
        public Task<AdminProfile?> GetAdminProfileAsync(int userId)
        {
            // Simulated DB access
            return Task.FromResult<AdminProfile?>(new AdminProfile { UserId = userId, AdminId = 1, Department = "Management" });
        }

        public Task<bool> UpdateAdminDepartmentAsync(int adminId, string newDepartment)
        {
            // Simulated DB access
            return Task.FromResult(true);
        }
    }
}
