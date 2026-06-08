using System;
using System.Text.RegularExpressions;

namespace VenueBookingSystem.Shared
{
  
    public static partial class ValidationHelper
    {
        
        public static event Action<string, string>? ValidationFailed;

        private static readonly Regex _nameRegex = new Regex(@"^[a-zA-Z\s]+$", RegexOptions.Compiled);
        private static readonly Regex _phoneRegex = new Regex(@"^\d{10}$", RegexOptions.Compiled);
        private static readonly Regex _pwdUpperRegex = new Regex(@"[A-Z]", RegexOptions.Compiled);
        private static readonly Regex _pwdLowerRegex = new Regex(@"[a-z]", RegexOptions.Compiled);
        private static readonly Regex _pwdDigitRegex = new Regex(@"\d", RegexOptions.Compiled);
        private static readonly Regex _usernameRegex = new Regex(@"^[a-zA-Z0-9_]{4,20}$", RegexOptions.Compiled);


        public static string ReadValidatedInput(
            string prompt,
            string formatHint,
            Func<string, bool> validator,
            string errorMessage,
            bool allowBack = true)
        {
            return ConsoleHelper.ReadInput(prompt, formatHint, required: true, allowBack: allowBack, input => 
            {
                if (validator(input)) return null;
                ValidationFailed?.Invoke(prompt, errorMessage);
                return errorMessage;
            });
        }

        public static bool ValidateName(string input) =>
            !string.IsNullOrWhiteSpace(input)
            && input.Length >= 2
            && input.Length <= 50
            && _nameRegex.IsMatch(input);

        public static bool ValidateEmail(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;
            int at = input.IndexOf('@');
            return at > 0 && at < input.Length - 2 && input.LastIndexOf('.') > at + 1;
        }


        public static bool ValidatePhone(string input) =>
            !string.IsNullOrWhiteSpace(input) && _phoneRegex.IsMatch(input);

        public static bool ValidatePassword(string input) =>
            !string.IsNullOrWhiteSpace(input)
            && input.Length >= 6
            && _pwdUpperRegex.IsMatch(input)
            && _pwdLowerRegex.IsMatch(input)
            && _pwdDigitRegex.IsMatch(input);

        public static bool ValidateUsername(string input) =>
            !string.IsNullOrWhiteSpace(input)
            && _usernameRegex.IsMatch(input);


        public static bool ValidatePositiveInt(string input) =>
            int.TryParse(input, out int v) && v > 0;

 
        public static bool ValidatePositiveDecimal(string input) =>
            decimal.TryParse(input, out decimal v) && v > 0;

        public static bool ValidateNotEmpty(string input) =>
            !string.IsNullOrWhiteSpace(input);


        public static bool ValidateCapacity(string input) =>
            int.TryParse(input, out int v) && v >= 1 && v <= 10_000;

        public static bool ValidatePurpose(string input) =>
            !string.IsNullOrWhiteSpace(input)
            && input.Length >= 5
            && input.Length <= 200;

   
        public static bool ValidateGuestCount(string input) =>
            int.TryParse(input, out int v) && v > 0;

        public static bool ValidateDateFuture(string input)
        {
            if (!DateTime.TryParseExact(input, "dd-MM-yyyy",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out DateTime dt))
                return false;
            return dt.Date > DateTime.Today;
        }

        public static string ReadName(string prompt = "Enter Full Name") =>
            ReadValidatedInput(
                prompt,
                "letters and spaces, 2-50 chars",
                ValidateName,
                "Name must be 2–50 characters containing only letters and spaces.");

        public static string ReadEmail(string prompt = "Email") =>
            ReadValidatedInput(
                prompt,
                "example@domain.com",
                ValidateEmail,
                "Invalid email format.");

        public static string ReadPhone(string prompt = "Phone Number") =>
            ReadValidatedInput(
                prompt,
                "10 digits",
                ValidatePhone,
                "Phone number must contain exactly 10 digits.");

        public static string ReadUsername(string prompt = "Username") =>
            ReadValidatedInput(
                prompt,
                "4-20 chars, letters/digits/underscore",
                ValidateUsername,
                "Username must be 4–20 characters: letters, digits, or underscore only.");

        public static string ReadLoginIdentifier(string prompt = "Username or Email") =>
            ReadValidatedInput(
                prompt,
                "username or user@example.com",
                input => ValidateUsername(input) || ValidateEmail(input),
                "Enter a valid username or email address.");

        public static string ReadFullName(string prompt = "Full Name") =>
            ReadName(prompt);

        public static string ReadHallName(string prompt = "Hall Name") =>
            ReadValidatedInput(
                prompt,
                "letters and spaces, 2-50 chars",
                ValidateName,
                "Hall name must be 2-50 characters containing only letters and spaces.");

        public static int ReadCapacity(string prompt = "Capacity")
        {
            string input = ReadValidatedInput(
                prompt,
                "1-10000",
                s => s == ConsoleHelper.BACK_COMMAND || (int.TryParse(s, out int v) && v > 0),
                "Capacity must be greater than 0.");
            if (input == ConsoleHelper.BACK_COMMAND) return int.MinValue;
            return int.Parse(input);
        }

        public static decimal ReadPrice(string prompt = "Price")
        {
            string input = ReadValidatedInput(
                prompt,
                "100-100000",
                s => s == ConsoleHelper.BACK_COMMAND || decimal.TryParse(s, out decimal v),
                "Price must be a valid number.");
            if (input == ConsoleHelper.BACK_COMMAND) return decimal.MinValue;
            return decimal.Parse(input);
        }

        public static decimal ReadPaymentAmount(string prompt, decimal maxAmount) =>
            ReadDecimal(prompt, 0.01m, maxAmount, $"0.01-{maxAmount:F2}");

        public static string ReadPassword(string prompt = "Password", bool allowBack = true)
        {
            return ConsoleHelper.ReadPassword(prompt, allowBack, pwd => 
            {
                if (ValidatePassword(pwd)) return null;
                ValidationFailed?.Invoke("Password", "Password must contain at least 6 characters.");
                return "Password must contain at least 6 characters.";
            });
        }

        public static string ReadConfirmPassword(string originalPassword, string prompt = "Confirm Password")
        {
            return ConsoleHelper.ReadPassword(prompt, true, confirm => 
            {
                if (confirm == originalPassword) return null;
                ValidationFailed?.Invoke(prompt, "Passwords do not match.");
                return "Passwords do not match.";
            });
        }

        public static int ReadInt(string prompt, int min, int max, string formatHint = "")
        {
            string hint = formatHint.Length > 0 ? formatHint : $"{min}–{max}";

            string input = ReadValidatedInput(
                prompt,
                hint,
                s => s == ConsoleHelper.BACK_COMMAND || (int.TryParse(s, out int v) && v >= min && v <= max),
                $"Please enter an integer between {min} and {max}.");

            if (input == ConsoleHelper.BACK_COMMAND) return int.MinValue;
            return int.Parse(input);
        }

        // Concept: Out Parameters & Lambda Expressions
        public static decimal ReadDecimal(string prompt, decimal min, decimal max, string formatHint = "")
        {
            string hint = formatHint.Length > 0 ? formatHint : $"{min}–{max}";

            string input = ReadValidatedInput(
                prompt,
                hint,
                s => s == ConsoleHelper.BACK_COMMAND || (decimal.TryParse(s, out decimal v) && v >= min && v <= max),
                $"Please enter a decimal between {min} and {max}.");

            if (input == ConsoleHelper.BACK_COMMAND) return decimal.MinValue;
            return decimal.Parse(input);
        }

        public static DateTime ReadFutureDate(string prompt, string formatHint = "dd-MM-yyyy HH:mm")
        {
            string input = ConsoleHelper.ReadInput(prompt, formatHint, required: true, allowBack: true, s => 
            {
                if (!DateTime.TryParseExact(s, "dd-MM-yyyy HH:mm",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None,
                        out DateTime dt))
                {
                    ValidationFailed?.Invoke(prompt, $"Use format: {formatHint}");
                    return $"Invalid format. Use: {formatHint}";
                }

                if (dt <= DateTime.Now)
                {
                    ValidationFailed?.Invoke(prompt, "Booking date cannot be in the past.");
                    return "Booking date cannot be in the past.";
                }

                return null;
            });

            if (input == ConsoleHelper.BACK_COMMAND)
                return DateTime.MinValue;

            return DateTime.ParseExact(input, "dd-MM-yyyy HH:mm", System.Globalization.CultureInfo.InvariantCulture);
        }

        public static int ReadMenuChoice(int max) =>
            ConsoleHelper.ReadMenuChoice(1, max);
    }
}
