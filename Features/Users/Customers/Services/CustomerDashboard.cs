using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using VenueBookingSystem.Features.Halls;
using VenueBookingSystem.Features.Bookings;
using VenueBookingSystem.Features.Payments;
using VenueBookingSystem.Features.Authentication;
using VenueBookingSystem.Shared;

namespace VenueBookingSystem.Features.Customers
{
    public partial class CustomerDashboard
    {
        private readonly User _currentUser;
        private readonly HallService _hallService;
        private readonly BookingService _bookingService;
        private readonly PaymentService _paymentService;
        private readonly BookingHistory _bookingHistory;
        private readonly AuthService _authService;

        private readonly Stack<string> _navigationHistory = new();

        public CustomerDashboard(
            User user,
            HallService hallService,
            BookingService bookingService,
            PaymentService paymentService,
            BookingHistory bookingHistory,
            AuthService authService)
        {
            _currentUser = user;
            _hallService = hallService;
            _bookingService = bookingService;
            _paymentService = paymentService;
            _bookingHistory = bookingHistory;
            _authService = authService;
        }

        public async Task RunAsync()
        {
            bool running = true;
            while (running)
            { 
                _navigationHistory.Push("Main Customer Dashboard");

                Console.Clear();
                ConsoleHelper.PrintHeader($" CUSTOMER DASHBOARD — Welcome, {_currentUser.FullName}");
                ConsoleHelper.PrintMenu("Main Menu", new[]
                {
                    "Browse Available Halls",
                    "My Bookings",
                    "Payments",
                    "Edit Profile"
                });

                int choice = ConsoleHelper.ReadMenuChoice(1, 4);

                switch (choice)
                {
                    case 1: await BrowseHallsAsync(); break;
                    case 2: await ViewMyBookingsAsync(); break;
                    case 3: await ShowPaymentMenuAsync(); break;
                    case 4: await EditProfileAsync(); break;
                    case 0:
                        if (ConsoleHelper.Confirm("Are you sure you want to log out?"))
                        {
                            running = false;
                        }
                        break;
                }
                
                if (running) _navigationHistory.Pop();
            }
        }
    }
}
