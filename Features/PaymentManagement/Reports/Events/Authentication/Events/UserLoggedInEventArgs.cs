using System;

namespace VenueBookingSystem.Features.Authentication.Events
{
    /// <summary>
    /// Provides data for the <see cref="AuthService.UserLoggedIn"/> domain event.
    /// Carries the authenticated <see cref="User"/> who successfully signed in.
    /// </summary>
    public sealed class UserLoggedInEventArgs : EventArgs
    {
        /// <summary>Gets the user who logged in.</summary>
        public User User { get; }

        public UserLoggedInEventArgs(User user)
        {
            User = user ?? throw new ArgumentNullException(nameof(user));
        }
    }
}
