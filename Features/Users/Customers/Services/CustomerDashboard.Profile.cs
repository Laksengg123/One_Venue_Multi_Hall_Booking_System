using System;
using System.Threading.Tasks;
using VenueBookingSystem.Shared;

namespace VenueBookingSystem.Features.Customers
{
    public partial class CustomerDashboard
    {
        private async Task EditProfileAsync()
        {
            Console.Clear();
            ConsoleHelper.PrintHeader("Edit Profile");

            ConsoleHelper.PrintNonEditableField("Username", _currentUser.Username);
            ConsoleHelper.PrintNonEditableField("Email", _currentUser.Email);
            Console.WriteLine();

            ConsoleHelper.PrintInfo("You can update your Full Name and Phone Number.");
            Console.WriteLine();

            string newName = _currentUser.FullName;
            while (true)
            {
                string input = ConsoleHelper.ReadInput(
                    $"Full Name [{_currentUser.FullName}]",
                    "letters and spaces, 2-50 chars",
                    required: false,
                    allowBack: true);

                if (input == ConsoleHelper.BACK_COMMAND) return;

                if (string.IsNullOrWhiteSpace(input))
                {
                    newName = _currentUser.FullName;
                    ConsoleHelper.PrintInfo("Full Name unchanged.");
                    break;
                }

                if (!ValidationHelper.ValidateName(input))
                {
                    ConsoleHelper.PrintError("Name must be 2-50 characters and contain only letters and spaces.");
                    continue;
                }

            
                newName = input;
                ConsoleHelper.PrintSuccess("  Full Name accepted.");
                break;
            }

            string newPhone = _currentUser.Phone;
            while (true)
            {
                string input = ConsoleHelper.ReadInput(
                    $"Phone [{_currentUser.Phone}]",
                    "10 digits",
                    required: false,
                    allowBack: true);

                if (input == ConsoleHelper.BACK_COMMAND) return;

                if (string.IsNullOrWhiteSpace(input))
                {
                    newPhone = _currentUser.Phone;
                    ConsoleHelper.PrintInfo("Phone unchanged.");
                    break;
                }

                if (!ValidationHelper.ValidatePhone(input))
                {
                    ConsoleHelper.PrintError("Phone number must be exactly 10 digits.");
                    continue;
                }

                newPhone = input;
                ConsoleHelper.PrintSuccess("  Phone number accepted.");
                break;
            }


            string newPassword;
            while (true)
            {
                newPassword = ConsoleHelper.ReadPassword("New Password (leave blank to keep current)");
                if (newPassword == ConsoleHelper.BACK_COMMAND) return;

                if (string.IsNullOrWhiteSpace(newPassword))
                    break;

                if (ValidationHelper.ValidatePassword(newPassword))
                {
                    ConsoleHelper.PrintSuccess("  New password accepted.");
                    break;
                }

                ConsoleHelper.PrintError("Password must be at least 6 characters and include uppercase, lowercase, and a digit.");
            }

            if (!ConsoleHelper.Confirm("Save profile changes?")) return;

            bool success = await _authService.UpdateUserAsync(_currentUser.UserId, _currentUser.Email, newPhone, newName);

            if (success)
            {
                if (!string.IsNullOrWhiteSpace(newPassword))
                {
                    ConsoleHelper.PrintInfo("To change your password, verify your current password.");
                    while (true)
                    {
                        string currentPassword = ConsoleHelper.ReadPassword("Current Password");
                        if (currentPassword == ConsoleHelper.BACK_COMMAND)
                            break;

                        if (string.IsNullOrWhiteSpace(currentPassword))
                        {
                            ConsoleHelper.PrintError("Current Password cannot be empty.");
                            continue;
                        }

                        bool pwdSuccess = await _authService.ChangePasswordAsync(_currentUser.UserId, currentPassword, newPassword);
                        if (pwdSuccess)
                        {
                            ConsoleHelper.PrintSuccess("Password changed successfully.");
                            break;
                        }

                        ConsoleHelper.PrintError("Incorrect current password. Please try again.");
                    }
                }

                ConsoleHelper.PrintSuccess("Profile updated successfully! (Changes will take effect next login)");
            }
            else
            {
                ConsoleHelper.PrintError("Failed to update profile. Please try again.");
            }

            ConsoleHelper.PressAnyKey();
        }
    }
}
