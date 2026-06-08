using System;

namespace VenueBookingSystem.Features.Authentication.Events
{
    /// <summary>
    /// Provides data for the <see cref="AuthService.UserLoggedOut"/> domain event.
    /// Raised when the currently authenticated session is terminated via <see cref="AuthService.Logout"/>.
    /// </summary>
    public sealed class UserLoggedOutEventArgs : EventArgs
    {
        // No additional data — logout carries no payload beyond the signal itself.
        // Extend this class if you later need to pass the previous user or session duration.
    }
}
