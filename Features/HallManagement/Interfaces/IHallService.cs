using System.Collections.Generic;
using System.Threading.Tasks;

namespace VenueBookingSystem.Features.Halls.Interfaces
{
    public interface IHallService
    {
        Task<List<Hall>> GetAllHallsAsync();
        Task<Hall?> GetHallByIdAsync(int hallId);
        Task<bool> AddHallAsync(Hall hall);
        Task<bool> UpdateHallAsync(Hall hall);
        Task<bool> DeleteHallAsync(int hallId);
        Task<bool> IsHallAvailableAsync(int hallId, System.DateTime start, System.DateTime end);
    }
}
