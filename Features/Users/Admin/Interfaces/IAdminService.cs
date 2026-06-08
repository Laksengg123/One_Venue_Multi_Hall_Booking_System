using System.Threading.Tasks;
using VenueBookingSystem.Features.Users.Admin.Models;

namespace VenueBookingSystem.Features.Users.Admin.Services
{
    public interface IAdminService
    {
        Task<AdminProfile?> GetAdminProfileAsync(int userId);
        Task<bool> UpdateAdminDepartmentAsync(int adminId, string newDepartment);
    }
}
