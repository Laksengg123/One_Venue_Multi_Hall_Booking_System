using System;

namespace VenueBookingSystem.Features.Authentication.Models
{
    internal class AuthSession
    {
        public string SessionId { get; } = Guid.NewGuid().ToString();
        public DateTime SessionStart { get; } = DateTime.UtcNow;
    }
}
