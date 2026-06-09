using System;

namespace VenueBookingSystem.Features.Users.Admin.Events
{
    public class AdminDepartmentUpdatedEventArgs : EventArgs
    {
        public int AdminId { get; }
        public string NewDepartment { get; }

        public AdminDepartmentUpdatedEventArgs(int adminId, string newDepartment)
        {
            AdminId = adminId;
            NewDepartment = newDepartment;
        }
    }
}
