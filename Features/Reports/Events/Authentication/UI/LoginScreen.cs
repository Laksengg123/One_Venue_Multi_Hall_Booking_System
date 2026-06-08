using System;
using System.Threading.Tasks;
using VenueBookingSystem.Exceptions;
using VenueBookingSystem.Shared;

namespace VenueBookingSystem.Features.Authentication;

/// <summary>
/// DESIGN SPECIFICATIONS: Authentication & Onboarding Interface
/// ─────────────────────────────────────────────────────────────────────────────
/// 1. LAYOUT & VISUAL HIERARCHY
///    - Screens use dynamic widths matching ConsoleHelper dimensions.
///    - Features a double-line boundary card for demo user credentials on landing.
///    - Clear indentation is enforced to align prompts away from console edge.
/// 
/// 2. COLOR PALETTE & SIGNALS
///    - Cyan / White: Headers, menu titles, and labels.
///    - Yellow / Green: Distinctive highlights for user roles and demo credentials.
///    - Dark Cyan: Decorative outer frame for high-contrast visibility.
///    - Red: High-visibility error reporting for validation failures.
///    - Dark Gray: System hints, input tips, and background divider borders.
/// 
/// 3. INTERACTIVE FEEDBACK
///    - Live inputs are verified using ValidationHelper.
///    - Success notifications (Green checkmarks) highlight accepted fields.
///    - Password mask '*' with dynamic visibility toggle via [Tab].
/// ─────────────────────────────────────────────────────────────────────────────
/// </summary>
public class LoginScreen
{
    private readonly AuthService _authService;

    public LoginScreen(AuthService authService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
    }



    private async Task RegisterNewCustomerAsync()
    {
        Console.Clear();
        ConsoleHelper.PrintHeader("Register New Customer");

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

        string email = ConsoleHelper.ReadInput(
            "Email",
            "example@domain.com",
            required: true,
            allowBack: true,
            input =>
            {
                if (!ValidationHelper.ValidateEmail(input))
                    return "Invalid email format.";

                var existingUser = _authService.GetUserByEmailAsync(input).GetAwaiter().GetResult();
                if (existingUser != null)
                    return "Email already registered.";

                return null;
            });
        if (email == ConsoleHelper.BACK_COMMAND) return;
        ConsoleHelper.PrintSuccess("Email accepted.");

        string phone = ValidationHelper.ReadPhone("Phone");
        if (phone == ConsoleHelper.BACK_COMMAND) return;
        ConsoleHelper.PrintSuccess("Phone number accepted.");

        bool success = await _authService.CreateUserAsync(fullName, username, email, phone, password, UserRole.Customer);

        if (success)
            ConsoleHelper.PrintSuccess($"Registration successful! Customer '{username}' registered. Please login.");
        else
            ConsoleHelper.PrintError("Registration failed. The username or email may already exist.");

        ConsoleHelper.PressAnyKey();
    }

    public async Task<User?> ShowAsync()
    {
        while (true)
        {
            try { Console.Clear(); } catch { }
            ConsoleHelper.PrintHeader("Hall Booking System");

            ConsoleHelper.PrintMenu(
                title: "Please select an option",
                options: ["Login", "Register new customer", "Exit"],
                showBack: false   
            );

            int choice = ConsoleHelper.ReadMenuChoice(min: 1, max: 3);

            if (choice == 3)
                return null;

            if (choice == 2)
            {
                await RegisterNewCustomerAsync();
                continue;
            }

            if (choice == 1)
            {
                while (true)
                {
                    string username = ConsoleHelper.ReadInput(
                        "Username",
                        "username or user@example.com",
                        required: true,
                        allowBack: true,
                        input =>
                        {
                            try
                            {
                                // Specific per-condition username validation messages
                                if (string.IsNullOrWhiteSpace(input))
                                    return "Username cannot be empty.";

                                bool looksLikeEmail = input.Contains('@');

                                if (looksLikeEmail)
                                {
                                    if (!ValidationHelper.ValidateEmail(input))
                                        return "Invalid email format. Use: user@example.com";
                                }
                                else
                                {
                                    if (input.Length < 3)
                                        return "Username must be at least 3 characters long.";
                                    if (input.Length > 20)
                                        return "Username must not exceed 20 characters.";
                                    if (!System.Text.RegularExpressions.Regex.IsMatch(input, @"^[a-zA-Z0-9_]+$"))
                                        return "Username may only contain letters, digits, or underscores.";
                                }

                                User? loginUser = looksLikeEmail
                                    ? _authService.GetUserByEmailAsync(input).GetAwaiter().GetResult()
                                    : _authService.GetUserByUsernameAsync(input).GetAwaiter().GetResult();

                                if (loginUser != null)
                                    return null;

                                string field = looksLikeEmail ? "Email" : "Username";
                                return $"{field} not found. Please check and try again.";
                            }
                            catch (Exception ex)
                            {
                                return $"Database error: {ex.Message}";
                            }
                        });

                    if (username == ConsoleHelper.BACK_COMMAND)
                        break;

                    while (true)
                    {
                        string password = ConsoleHelper.ReadPassword(
                            "Password",
                            allowBack: true,
                            validator: pwd =>
                            {
                                if (string.IsNullOrWhiteSpace(pwd))
                                    return "Password cannot be empty.";
                                if (pwd.Length < 6)
                                    return "Password must be at least 6 characters long.";
                                return null;
                            });
                        if (password == ConsoleHelper.BACK_COMMAND)
                            break;

                        Console.Write(ConsoleHelper.GetIndent() + "Authenticating...");
                        try
                        {
                            User? user = await _authService.LoginAsync(username, password);

                            if (user is not null)
                            {
                                Console.WriteLine();
                                ConsoleHelper.PrintSuccess($"Welcome back, {user.FullName}!");
                                ConsoleHelper.PressAnyKey();
                                return user;
                            }
                           
                            Console.WriteLine();
                            ConsoleHelper.PrintError("Invalid username/email or password. Please try again.");
                            ConsoleHelper.PressAnyKey();
                            continue;
                        }
                        catch (VenueBookingSystem.Features.Bookings.Exceptions.BookingException ex)
                        {
                            Console.WriteLine();
                            ConsoleHelper.PrintError(ex.Message);
                            ConsoleHelper.PressAnyKey();
                            break;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine();
                            ConsoleHelper.PrintError($"An unexpected error occurred: {ex.Message}");
                            ConsoleHelper.PressAnyKey();
                            break;
                        }
                    }
                }
            }
        }
    }
}
