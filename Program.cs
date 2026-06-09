using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using VenueBookingSystem.Storage;
using VenueBookingSystem.Features.Authentication;
using VenueBookingSystem.Features.Authentication.Events;
using VenueBookingSystem.Features.Halls;
using VenueBookingSystem.Features.Bookings;
using VenueBookingSystem.Features.Bookings.Events;
using VenueBookingSystem.Features.Payments;
using VenueBookingSystem.Features.Payments.Events;
using VenueBookingSystem.Features.Reports;
using VenueBookingSystem.Features.Admin;
using VenueBookingSystem.Features.Customers;
using VenueBookingSystem.Shared;

// 🚦 Track how the program ends — 0 means "closed normally, no errors"

int exitCode = 0;

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// 📖 STEP 1: READ THE OPERATIONS MANUAL (appsettings.json)
//    Before opening, read the settings file — like a checklist
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)  // Look for the file in the same folder as the app
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    //                                ↑ File MUST exist (optional: false) — crash if missing
    //                                                   ↑ Don't re-read if file changes mid-run
    .Build();

// Read the app name from settings — if missing, use "Hall Booking System" as backup
// '??' = "if null/missing, use this default instead"
var appName = config["AppSettings:AppName"] ?? "Hall Booking System";


DatabaseContext.Initialize(config); // Create and configure the database connection
var dbContext = DatabaseContext.Instance!; // Get the single shared instance
//                                      ↑ '!' tells C#: "I guarantee this won't be null, trust me"

// ── 🗂️ ENSURE REQUIRED TABLES EXIST (Auto-create if missing) ─────────────
// Check if Cancellations, Refunds, and Invoices tables exist in the database
// If they don't — create them automatically. This is safe to run every startup.
try
{
    using var connSchema = await dbContext.CreateConnectionAsync();
    using var cmdSchema = new Microsoft.Data.SqlClient.SqlCommand(@"
        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Cancellations')
        BEGIN
            CREATE TABLE Cancellations (
                CancellationId   INT IDENTITY(1,1) PRIMARY KEY,
                BookingId        INT           NOT NULL,
                Reason           NVARCHAR(500) NOT NULL DEFAULT '',
                RefundAmount     DECIMAL(10,2) NOT NULL DEFAULT 0,
                Status           NVARCHAR(20)  NOT NULL DEFAULT 'Pending',
                AdminRemarks     NVARCHAR(500) NULL,
                CancellationDate DATETIME      NOT NULL DEFAULT GETDATE(),
                CreatedAt        DATETIME      NOT NULL DEFAULT GETDATE()
            );
        END
        ELSE
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Cancellations' AND COLUMN_NAME = 'Status')
            BEGIN
                ALTER TABLE Cancellations ADD Status NVARCHAR(20) NOT NULL DEFAULT 'Pending';
            END
            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Cancellations' AND COLUMN_NAME = 'AdminRemarks')
            BEGIN
                ALTER TABLE Cancellations ADD AdminRemarks NVARCHAR(500) NULL;
            END
        END

        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Refunds')
        BEGIN
            CREATE TABLE Refunds (
                RefundId        INT IDENTITY(1,1) PRIMARY KEY,
                CancellationId  INT           NOT NULL,
                RefundStatus    NVARCHAR(50)  NOT NULL DEFAULT 'Pending',
                RefundDate      DATETIME      NULL,
                ProcessedBy     NVARCHAR(100) NULL,
                Remarks         NVARCHAR(500) NULL,
                CreatedAt       DATETIME      NOT NULL DEFAULT GETDATE()
            );
        END

        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Invoices')
        BEGIN
            CREATE TABLE Invoices (
                InvoiceId     INT IDENTITY(1,1) PRIMARY KEY,
                BookingId     INT           NOT NULL,
                InvoiceNumber NVARCHAR(50)  NOT NULL,
                TotalAmount   DECIMAL(10,2) NOT NULL,
                GeneratedDate DATETIME2     NOT NULL DEFAULT GETDATE(),
                IssuedTo      NVARCHAR(150) NULL,
                Notes         NVARCHAR(MAX) NULL,
                CONSTRAINT UQ_Invoices_BookingId UNIQUE (BookingId),
                CONSTRAINT UQ_Invoices_Number UNIQUE (InvoiceNumber)
            );
        END
        
        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Policies')
        BEGIN
            CREATE TABLE Policies (
                PolicyId      INT IDENTITY(1,1) PRIMARY KEY,
                PolicyType    NVARCHAR(50)  NOT NULL,
                PolicyRule    NVARCHAR(500) NOT NULL,
                SequenceOrder INT           NOT NULL DEFAULT 1
            );
            INSERT INTO Policies (PolicyType, PolicyRule, SequenceOrder) VALUES
            ('Booking', 'Booking is confirmed only after successful payment.', 1),
            ('Booking', 'Hall availability is subject to real-time status.', 2),
            ('Booking', 'Booking once confirmed cannot be modified.', 3),
            ('Booking', 'Users must provide valid details during booking.', 4),
            ('Booking', 'Management reserves the right to cancel bookings in special circumstances.', 5),
            ('Refund', 'Cancellation before 7 days -> 80% refund', 1),
            ('Refund', 'Cancellation 3-7 days before -> 50% refund', 2),
            ('Refund', 'Cancellation within 3 days -> No refund', 3),
            ('Refund', 'Refund processed within 5-10 working days', 4),
            ('Refund', 'Service charges are non-refundable in all tiers.', 5),
            ('Cancellation', 'Cancel >= 7 days before event date -> 80% refund (Green tier)', 1),
            ('Cancellation', 'Cancel 3-6 days before event date -> 50% refund (Yellow tier)', 2),
            ('Cancellation', 'Cancel within 3 days of event date -> No refund (Red tier)', 3),
            ('Cancellation', 'Cancellations must be submitted via the booking system.', 4),
            ('Cancellation', 'No-show on event day = full forfeiture (Red tier applies).', 5),
            ('Cancellation', 'Service charges (5%) are non-refundable in all tiers.', 6),
            ('Cancellation', 'Refunds are processed within 5-10 working days.', 7),
            ('Cancellation', 'Force majeure / emergencies reviewed on a case-by-case basis.', 8);
        END", connSchema);
    await cmdSchema.ExecuteNonQueryAsync(); // Run the SQL — create tables if needed
}
catch (Exception ex)
{
    Console.WriteLine($"[Schema] Warning: Could not verify Cancellations/Refunds/Invoices tables: {ex.Message}");
}

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// 🔐 STEP 3: TEST THE DATABASE CONNECTION
//    "Knock on the database door — is it awake and responding?"
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
ConsoleHelper.PrintInfo($"Connecting to {appName} database...");
bool connected = await dbContext.TestConnectionAsync(); // Ping the database

if (!connected) // If database doesn't respond → STOP the program
{
    ConsoleHelper.PrintError("Cannot connect to database!");
    ConsoleHelper.PrintWarning("Please ensure SQL Server is running and VenueBookingDB exists.");
    ConsoleHelper.PrintInfo("Connection string: " + dbContext.ConnectionString);
    ConsoleHelper.PressAnyKey(); // Wait for user to read the error
    return; // Exit the program — no point running without a database
}
ConsoleHelper.PrintSuccess("Database connected successfully!"); // ✅ All good, proceed

// ── 🧹 SELF-HEALING SCHEMA UPGRADE ──────────────────────────────────────
// If the database has the old schema (e.g. Bookings table uses CustomerId), drop old objects
// so the migration script can rebuild everything cleanly under the new schema.
try
{
    using (var conn = await dbContext.CreateConnectionAsync())
    using (var cmd = new Microsoft.Data.SqlClient.SqlCommand(@"
        IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Bookings' AND COLUMN_NAME = 'CustomerId')
        BEGIN
            -- Drop views
            IF OBJECT_ID('dbo.vw_BookingSummary', 'V') IS NOT NULL DROP VIEW dbo.vw_BookingSummary;
            IF OBJECT_ID('dbo.vw_HallRevenue', 'V') IS NOT NULL DROP VIEW dbo.vw_HallRevenue;

            -- Drop tables in order of dependency
            IF OBJECT_ID('dbo.Invoices', 'U') IS NOT NULL DROP TABLE dbo.Invoices;
            IF OBJECT_ID('dbo.Refunds', 'U') IS NOT NULL DROP TABLE dbo.Refunds;
            IF OBJECT_ID('dbo.Cancellations', 'U') IS NOT NULL DROP TABLE dbo.Cancellations;
            IF OBJECT_ID('dbo.Payments', 'U') IS NOT NULL DROP TABLE dbo.Payments;
            IF OBJECT_ID('dbo.Feedback', 'U') IS NOT NULL DROP TABLE dbo.Feedback;
            IF OBJECT_ID('dbo.BookingStatusHistory', 'U') IS NOT NULL DROP TABLE dbo.BookingStatusHistory;
            IF OBJECT_ID('dbo.HallAmenities', 'U') IS NOT NULL DROP TABLE dbo.HallAmenities;
            IF OBJECT_ID('dbo.Bookings', 'U') IS NOT NULL DROP TABLE dbo.Bookings;
            IF OBJECT_ID('dbo.Halls', 'U') IS NOT NULL DROP TABLE dbo.Halls;
            IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;
        END", conn))
    {
        await cmd.ExecuteNonQueryAsync();
    }
}
catch (Exception ex)
{
    ConsoleHelper.PrintWarning($"[Init] Database pre-clean warning: {ex.Message}");
}

// ── 🔄 AUTOMATIC DATABASE SCHEMA MIGRATION ──────────────────────────────
// Execute OneVenueDB.sql on startup to ensure all stored procedures and tables are up-to-date
ConsoleHelper.PrintInfo("[Init] Syncing database schema and stored procedures from OneVenueDB.sql...");

string? sqlFilePath = null;
string currentDir = AppContext.BaseDirectory;
while (!string.IsNullOrEmpty(currentDir))
{
    string possiblePath = Path.Combine(currentDir, "Features", "OneVenueDB.sql");
    if (File.Exists(possiblePath))
    {
        sqlFilePath = possiblePath;
        break;
    }
    possiblePath = Path.Combine(currentDir, "OneVenueDB.sql");
    if (File.Exists(possiblePath))
    {
        sqlFilePath = possiblePath;
        break;
    }
    string? parent = Directory.GetParent(currentDir)?.FullName;
    if (parent == currentDir) break;
    currentDir = parent!;
}

if (sqlFilePath != null)
{
    try
    {
        string sqlText = await File.ReadAllTextAsync(sqlFilePath);

        // Remove any "USE <dbname>;" statements (and master) to prevent switching database context
        sqlText = System.Text.RegularExpressions.Regex.Replace(
            sqlText,
            @"^\s*USE\s+\[?\w+\]?;?\s*$",
            "",
            System.Text.RegularExpressions.RegexOptions.Multiline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Split by GO separator on its own line
        string[] batches = System.Text.RegularExpressions.Regex.Split(
            sqlText,
            @"^\s*GO\s*$",
            System.Text.RegularExpressions.RegexOptions.Multiline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        using (var conn = await dbContext.CreateConnectionAsync())
        {
            foreach (var batch in batches)
            {
                string trimmedBatch = batch.Trim();
                if (string.IsNullOrWhiteSpace(trimmedBatch)) continue;

                // Skip USE statements so all objects are created in the connection string's configured database
                if (trimmedBatch.StartsWith("USE ", StringComparison.OrdinalIgnoreCase)) continue;

                try
                {
                    using (var cmd = new Microsoft.Data.SqlClient.SqlCommand(trimmedBatch, conn))
                    {
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                catch (Microsoft.Data.SqlClient.SqlException sqlEx)
                {
                    // Log the warning/error so we can see why a batch fails
                    ConsoleHelper.PrintWarning($"[Init] Batch warning/error: {sqlEx.Message} (Error Number: {sqlEx.Number})");
                }
            }
        }
        ConsoleHelper.PrintSuccess("[Init] Database schema sync completed successfully!");
    }
    catch (Exception ex)
    {
        ConsoleHelper.PrintError($"[Init] Migration failed: {ex.Message}");
    }
}
else
{
    ConsoleHelper.PrintWarning("[Init] Could not find OneVenueDB.sql for automatic database schema initialization.");
}

// ── 🔑 CORRECT BROKEN ADMIN HASH ─────────────────────────────────────────
try
{
    using (var conn = await dbContext.CreateConnectionAsync())
    {
        // Generate the correct password hash for "Password@123"
        string correctHash = BCrypt.Net.BCrypt.HashPassword("Password@123", 12);
        using (var cmd = new Microsoft.Data.SqlClient.SqlCommand(
            // Only update the row IF the old broken hash is still there
            "UPDATE dbo.Users SET PasswordHash = @Hash WHERE Username = 'admin1' AND PasswordHash = '$2a$12$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uSve/iPXi'", conn))
        {
            cmd.Parameters.AddWithValue("@Hash", correctHash);
            int rows = await cmd.ExecuteNonQueryAsync(); // Run the update
            if (rows > 0) // If a row was actually changed
                ConsoleHelper.PrintInfo("[Init] Corrected broken admin1 password hash in database.");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine("DB INIT ERROR: " + ex.Message); // If something goes wrong, log it
}


// ── 🔑 AUTHENTICATION SERVICE — The Security Guard ───────────────────────
var authService = new AuthService(dbContext); // Creates the login/logout manager

// ── 🔍 TEST-LOGIN SHORTCUT (for developers only) ─────────────────────────
// If the app is launched with the argument "testlogin" from command line
// it will quickly test if admin1 login works and then exit
var cmdArgs = Environment.GetCommandLineArgs(); // Get launch arguments
bool hasTestLogin = false;
foreach (var arg in cmdArgs) // Loop through each argument
{
    if (arg.Equals("testlogin", StringComparison.OrdinalIgnoreCase)) // Case-insensitive check
    {
        hasTestLogin = true;
        break; // Found it — no need to keep looking
    }
}

if (hasTestLogin)
{
    // Try logging in as admin1 with password "Password@123" and report result
    var testUser = await authService.LoginAsync("admin1", "Password@123");
    Console.WriteLine(testUser != null
        ? $"Login SUCCESS for {testUser.Username} (Role: {testUser.Role})"
        : "Login FAILED for admin1");
    return; // Exit after test — don't open the full app
}

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// 👥 STEP 4: ASSIGN STAFF THEIR ROLES (Service wiring)
//    Create each service and give them what they need to work
//    This is Dependency Injection: "give services their dependencies"
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
var hallService    = new HallService(dbContext);    // 🏛️ Hall Manager — manages all halls
var bookingService = new BookingService(dbContext); // 📋 Booking Clerk — handles reservations
var paymentService = new PaymentService(dbContext, bookingService);
//   ↑ 💰 Cashier — needs the booking clerk too (to look up booking details for payment)
var loginScreen    = new LoginScreen(authService);  // 🔐 Login counter — needs the security guard
var reportService  = new ReportService(dbContext, bookingService, hallService);
//   ↑ 📊 Report generator — needs bookings AND halls data to create reports
var bookingHistory = new BookingHistory(dbContext); // 📜 Logbook keeper — tracks status changes
var adminService   = new VenueBookingSystem.Features.Users.Admin.Services.AdminService();
var customerService = new VenueBookingSystem.Features.Users.Customers.Services.CustomerService();

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// 📢 STEP 5: SET UP THE PA ANNOUNCEMENT SYSTEM (Event subscriptions)
//    Using '+=' to SUBSCRIBE to each event channel
//    Whenever an event fires, the lambda (=>) runs automatically
//    '_' means "I don't care about the sender object"
//    'e' means "give me the event data (the booking/payment/user details)"
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

// 🔔 When a new booking is created → print a blue info message
bookingService.BookingCreated   += (_, e) => ConsoleHelper.PrintInfo($"[Event] New booking created: #{e.Booking.BookingId} — {e.Booking.HallName}");

// ✅ When a booking is confirmed by admin → print a green success message
bookingService.BookingConfirmed += (_, e) => ConsoleHelper.PrintSuccess($"[Event] Booking confirmed: #{e.Booking.BookingId}");

// ⚠️ When a booking is cancelled → print a yellow warning message
bookingService.BookingCancelled += (_, e) => ConsoleHelper.PrintWarning($"[Event] Booking cancelled: #{e.Booking.BookingId}");

// 💰 When a payment is processed → print a green success with amount
paymentService.PaymentProcessed += (_, e) => ConsoleHelper.PrintSuccess($"[Event] Payment processed: #{e.Payment.PaymentId} — {ConsoleHelper.FormatCurrency(e.Payment.Amount)}");

// 👤 When someone logs in → print a blue info with their name and role
authService.UserLoggedIn        += (_, e) => ConsoleHelper.PrintInfo($"[Event] Login: {e.User.FullName} ({e.User.Role})");

// 🚪 When someone logs out → print a simple info message
//    Second '_' = we don't care about either sender OR event data
authService.UserLoggedOut       += (_, _) => ConsoleHelper.PrintInfo("[Event] User logged out.");

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// 🔄 STEP 6: OPEN THE VENUE — THE MAIN LOOP
//    Keep running until the user explicitly chooses to exit
//    Like a revolving door: one person leaves, next person comes in
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
bool running = true;
while (running) // Keep looping until 'running' is set to false
{
    ConsoleHelper.ShowWelcomeScreen(); // Show the welcome banner at the top

    var user = await loginScreen.ShowAsync(); // Show login screen and wait for user to log in

    if (user == null) // Nobody logged in (they pressed Escape or skipped)
    {
        // Ask: "Are you sure you want to exit?"
        if (ConsoleHelper.Confirm("Are you sure you want to exit?"))
        {
            running = false; // Set flag to stop the loop
            break;           // Exit the while loop immediately
        }
        continue; // Go back to the top of the loop — show welcome screen again
    }

    // 🪪 Check WHO just logged in and route them to the right place
    switch (user.Role)
    {
        case UserRole.Admin: // This person is an Admin
            // Create the Admin Dashboard and give it all the services it needs
            var adminDash = new AdminDashboard(
                authService, hallService, bookingService,
                paymentService, reportService, bookingHistory);
            await adminDash.RunAsync(); // Open the Admin menu — wait until they're done
            break;

        case UserRole.Customer: // This person is a Customer
            // Create the Customer Dashboard with the services it needs
            var customerDash = new CustomerDashboard(
                user, hallService, bookingService,
                paymentService, bookingHistory, authService);
            await customerDash.RunAsync(); // Open the Customer menu — wait until they're done
            break;

        default: // Unknown role — this shouldn't happen in normal use
            ConsoleHelper.PrintError($"Unknown role: {user.Role}. Contact your system administrator.");
            ConsoleHelper.PressAnyKey(); // Wait for user to read the error
            break;
    }

    // 🚪 User is done — log them out before the next person comes in
    authService.Logout();
}

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// 👋 STEP 7: CLOSE THE VENUE — GOODBYE SCREEN
//    The loop ended — user confirmed exit
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
ConsoleHelper.ShowGoodbyeScreen(); // Show a farewell message

// Exit the application cleanly
// exitCode = 0 means "everything was fine — normal shutdown"
Environment.Exit(exitCode);
