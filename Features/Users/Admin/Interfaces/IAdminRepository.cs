using System.Threading.Tasks;
using VenueBookingSystem.Features.Users.Admin.Models;

namespace VenueBookingSystem.Features.Users.Admin.Interfaces
{
    public interface IAdminRepository
    {
        Task<AdminProfile?> GetAdminProfileAsync(int userId);
        Task<bool> UpdateAdminDepartmentAsync(int adminId, string newDepartment);
    }
}
