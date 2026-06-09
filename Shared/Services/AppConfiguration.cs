using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Configuration;

namespace VenueBookingSystem.Shared.Configuration
{
    /// <summary>
    /// Provides centralised access to application configuration values.
    /// </summary>
    public static class AppConfiguration
    {
        public static int PageSize(IConfiguration config) =>
            config.GetValue<int>("AppSettings:PageSize", 10);

        public static string AppName(IConfiguration config) =>
            config.GetValue<string>("AppSettings:AppName") ?? "Hall Booking System";

        public static int MaxLoginAttempts(IConfiguration config) =>
            config.GetValue<int>("AppSettings:MaxLoginAttempts", 3);

       
    }
}
