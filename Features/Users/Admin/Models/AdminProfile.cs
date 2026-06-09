using System;

namespace VenueBookingSystem.Features.Users.Admin.Models
{
    // ╔══════════════════════════════════════════════════════════════════════╗
    // ║              🪪 ADMIN PROFILE — THE ADMINISTRATOR ID CARD           ║
    // ║  Stores the extra details about an Admin user beyond their login.   ║
    // ║  Links to the User account via UserId.                              ║
    // ║  Used to display the Admin identity badge on screen.               ║
    // ╚══════════════════════════════════════════════════════════════════════╝
    public class AdminProfile
    {
        // 🔢 Unique ID for this admin record (different from their login UserId)
        //    e.g. shown as "ADM-SYS-0001" on the badge
        public int AdminId { get; set; }

        // 🔗 Links this admin profile to their user login account
        //    e.g. UserId = 1 links to the 'admin1' login record in the Users table
        public int UserId { get; set; }

        // 🏢 Which department does this admin manage?
        //    e.g. "Operations & Management" or "Finance"
        //    = string.Empty means it starts as blank until set from the database
        public string Department { get; set; } = string.Empty;

        // 👑 How powerful is this admin?
        //    "Standard" = normal admin access
        //    "SuperAdmin" or "Owner" = full system control (shown in RED on badge)
        //    Defaults to "Standard" if not specified
        public string PrivilegeLevel { get; set; } = "Standard";

        // 📅 When did this person become an admin?
        //    DateTime.UtcNow = "right now, in universal time" — used as default
        public DateTime AssignedDate { get; set; } = DateTime.UtcNow;
    }
}
