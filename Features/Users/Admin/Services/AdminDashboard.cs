using VenueBookingSystem.Features.Authentication;
using VenueBookingSystem.Features.Halls;
using VenueBookingSystem.Features.Bookings;
using VenueBookingSystem.Features.Payments;
using VenueBookingSystem.Features.Reports;
using VenueBookingSystem.Shared;

namespace VenueBookingSystem.Features.Admin;

public partial class AdminDashboard
{
    
    private readonly AuthService _auth;
    private readonly HallService _halls;
    private readonly BookingService _bookings;
    private readonly PaymentService _payments;
    private readonly ReportService _reports;
    private readonly BookingHistory _history;

    public AdminDashboard(
        AuthService authService,
        HallService hallService,
        BookingService bookingService,
        PaymentService paymentService,
        ReportService reportService,
        BookingHistory bookingHistory)
    {
        _auth = authService;
        _halls = hallService;
        _bookings = bookingService;
        _payments = paymentService;
        _reports = reportService;
        _history = bookingHistory;
    }

    public async Task RunAsync()
    {
        bool running = true;
        while (running)
        {
            Console.Clear();
            ConsoleHelper.PrintHeader($" ADMIN DASHBOARD — Welcome, {_auth.CurrentUser!.FullName}");
            ConsoleHelper.PrintMenu("Main Menu", new[]
            {
                "Hall Management",
                "Booking Management",
                "Payment Management",
                "User Management",
                "Reports",
                "Manage Policies"
            });

            int choice = ConsoleHelper.ReadMenuChoice(1, 6);
            switch (choice)
            {
                case 1: await ShowHallManagementAsync(); break;
                case 2: await ShowBookingManagementAsync(); break;
                case 3: await ShowPaymentManagementAsync(); break;
                case 4: await ShowUserManagementAsync(); break;
                case 5: await ShowReportsAsync(); break;
                case 6: await ShowPolicyManagementAsync(); break;
                case 0:
                    if (ConsoleHelper.Confirm("Are you sure you want to logout?"))
                        running = false;
                    break;
            }
        }
    }
}
