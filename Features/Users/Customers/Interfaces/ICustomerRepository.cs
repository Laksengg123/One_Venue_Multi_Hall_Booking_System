using System.Threading.Tasks;
using VenueBookingSystem.Features.Users.Customers.Models;

namespace VenueBookingSystem.Features.Users.Customers.Interfaces
{
    public interface ICustomerRepository
    {
        Task<CustomerProfile?> GetCustomerProfileAsync(int userId);
        Task<bool> UpdateMembershipTierAsync(int customerId, string newTier);
    }
}
