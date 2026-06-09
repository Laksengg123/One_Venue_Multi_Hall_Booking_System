using System;

namespace VenueBookingSystem.Features.Reports.Events
{
    public class ReportGeneratedEventArgs : EventArgs
    {
        public string ReportType { get; }
        public DateTime GeneratedAt { get; }

        public ReportGeneratedEventArgs(string reportType)
        {
            ReportType = reportType;
            GeneratedAt = DateTime.Now;
        }
    }
}
