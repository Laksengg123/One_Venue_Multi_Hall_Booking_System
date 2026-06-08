using System;

namespace VenueBookingSystem.Features.Halls.Events
{
    public class HallStatusChangedEventArgs : EventArgs
    {
        public int HallId { get; }
        public bool IsActive { get; }

        public HallStatusChangedEventArgs(int hallId, bool isActive)
        {
            HallId = hallId;
            IsActive = isActive;
        }
    }
}
