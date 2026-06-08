using System;

namespace VenueBookingSystem.Features.Users.Admin.Models
{
    public class AdminProfile
    {
        public int AdminId { get; set; }
        public int UserId { get; set; }
        public string Department { get; set; } = string.Empty;
        public string PrivilegeLevel { get; set; } = "Standard";
        public DateTime AssignedDate { get; set; } = DateTime.UtcNow;
    }
}
