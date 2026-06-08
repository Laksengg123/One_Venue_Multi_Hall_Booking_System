using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VenueBookingSystem.Features.Authentication;
using VenueBookingSystem.Shared;

namespace VenueBookingSystem.Features.Admin
{
    public partial class AdminDashboard
    {
        private async Task ShowUserManagementAsync()
        {
            bool back = false;
            while (!back)
            {
                Console.Clear();
                ConsoleHelper.PrintHeader("Customer Management");

                string[] options = {
                    "View All Customers",
                    "Add New Customer"
                };

                ConsoleHelper.PrintMenu("Customer Management", options);
                int choice = ValidationHelper.ReadMenuChoice(2);

                switch (choice)
                {
                    case 0:
                        back = true;
                        break;
                    case 1:
                        await ViewAllCustomersAsync();
                        break;
                    case 2:
                        await AddNewCustomerAsync();
                        break;
                }
            }
        }

        private async Task ViewAllCustomersAsync()
        {
            Console.Clear();
            ConsoleHelper.PrintHeader("All Customers");

            var users = await _auth.GetAllUsersAsync();
            var customers = new List<User>();
            foreach (var u in users)
            {
                if (u.Role == UserRole.Customer)
                    customers.Add(u);
            }

  
            if (customers.Count == 0)
            {
                ConsoleHelper.PrintWarning("No customers found in the system.");
                ConsoleHelper.PressAnyKey();
                return;
            }

            var columns = new Dictionary<string, Func<User, string>>
            {
                ["ID"] = u => u.UserId.ToString(),
                ["Username"] = u => u.Username,
                ["Full Name"] = u => u.FullName,
                ["Email"] = u => u.Email,
                ["Phone"] = u => u.Phone
            };

            ConsoleHelper.ShowPaginatedTable(customers, columns, "All Customers");
            ConsoleHelper.PressAnyKey();
        }

        private async Task AddNewCustomerAsync()
        {
            Console.Clear();
            ConsoleHelper.PrintHeader("Add New Customer");

            string username = ValidationHelper.ReadUsername("Username");
            if (username == ConsoleHelper.BACK_COMMAND) return;
            ConsoleHelper.PrintSuccess("Username accepted.");

            string password = ValidationHelper.ReadPassword("Password");
            if (password == ConsoleHelper.BACK_COMMAND) return;
            ConsoleHelper.PrintSuccess("Password accepted.");

            string confirmPassword = ValidationHelper.ReadConfirmPassword(password, "Confirm Password");
            if (confirmPassword == ConsoleHelper.BACK_COMMAND) return;
            ConsoleHelper.PrintSuccess("Password confirmation accepted.");

            string fullName = ValidationHelper.ReadFullName("Full Name");
            if (fullName == ConsoleHelper.BACK_COMMAND) return;
            ConsoleHelper.PrintSuccess("Full Name accepted.");

            string email = ValidationHelper.ReadEmail("Email");
            if (email == ConsoleHelper.BACK_COMMAND) return;
            ConsoleHelper.PrintSuccess("Email accepted.");

            string phone = ValidationHelper.ReadPhone("Phone");
            if (phone == ConsoleHelper.BACK_COMMAND) return;
            ConsoleHelper.PrintSuccess("Phone number accepted.");

            bool success = await _auth.CreateUserAsync(fullName, username, email, phone, password, UserRole.Customer);

            if (success)
                ConsoleHelper.PrintSuccess($"Customer '{username}' added successfully!");
            else
                ConsoleHelper.PrintError("Registration failed. The username may already exist.");

            ConsoleHelper.PressAnyKey();
        }
    }
}
