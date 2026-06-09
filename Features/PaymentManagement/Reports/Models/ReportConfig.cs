namespace VenueBookingSystem.Features.Reports.Models
{
    public class ReportConfig
    {
        public bool IncludeHeader { get; set; } = true;
        public string Format { get; set; } = "CSV";
    }
}
