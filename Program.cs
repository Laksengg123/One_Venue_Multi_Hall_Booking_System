using Microsoft.Extensions.Configuration;
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

/// <summary>
/// Application entry point and Composition Root.
/// Responsible for bootstrapping the configuration, establishing database connectivity,
/// wiring up dependency injection (manual DI in this console app), 
/// and subscribing to domain events for decoupled cross-feature communication.
/// </summary>
int exitCode = 0;


var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .Build();

var appName = config["AppSettings:AppName"] ?? "Hall Booking System";

DatabaseContext.Initialize(config);
var dbContext = DatabaseContext.Instance!;
try
{
    using (var conn = await dbContext.CreateConnectionAsync())
    {
        string correctHash = BCrypt.Net.BCrypt.HashPassword("password", 12);
        using (var cmd = new Microsoft.Data.SqlClient.SqlCommand(
            "UPDATE Users SET PasswordHash = @Hash WHERE Username = 'admin1' AND PasswordHash = '$2a$12$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uSve/iPXi'", conn))
        {
            cmd.Parameters.AddWithValue("@Hash", correctHash);
            int rows = await cmd.ExecuteNonQueryAsync();
            if (rows > 0)
                ConsoleHelper.PrintInfo("[Init] Corrected broken admin1 password hash in database.");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine("DB INIT ERROR: " + ex.Message);
}

// Ensure Cancellations, Refunds, and Invoices tables exist
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
                CancellationDate DATETIME      NOT NULL DEFAULT GETDATE(),
                CreatedAt        DATETIME      NOT NULL DEFAULT GETDATE()
            );
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
        END", connSchema);
    await cmdSchema.ExecuteNonQueryAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"[Schema] Warning: Could not verify Cancellations/Refunds/Invoices tables: {ex.Message}");
}

ConsoleHelper.PrintInfo($"Connecting to {appName} database...");
bool connected = await dbContext.TestConnectionAsync();
if (!connected)
{
    ConsoleHelper.PrintError("Cannot connect to database!");
    ConsoleHelper.PrintWarning("Please ensure SQL Server is running and VenueBookingDB exists.");
    ConsoleHelper.PrintInfo("Connection string: " + dbContext.ConnectionString);
    ConsoleHelper.PressAnyKey();
    return;
}
ConsoleHelper.PrintSuccess("Database connected successfully!");

var authService = new AuthService(dbContext);

// ── Test-login shortcut ───────────────────────────────────────────────────────
var cmdArgs = Environment.GetCommandLineArgs();
bool hasTestLogin = false;
foreach (var arg in cmdArgs)
{
    if (arg.Equals("testlogin", StringComparison.OrdinalIgnoreCase))
    {
        hasTestLogin = true;
        break;
    }
}

if (hasTestLogin)
{
    var testUser = await authService.LoginAsync("admin1", "password");
    Console.WriteLine(testUser != null
        ? $"Login SUCCESS for {testUser.Username} (Role: {testUser.Role})"
        : "Login FAILED for admin1");
    return;
}

// ── Service wiring ────────────────────────────────────────────────────────────
var hallService    = new HallService(dbContext);
var bookingService = new BookingService(dbContext);
var paymentService = new PaymentService(dbContext, bookingService);
var loginScreen    = new LoginScreen(authService);
var reportService  = new ReportService(dbContext, bookingService, hallService);
var bookingHistory = new BookingHistory(dbContext);
var adminService   = new VenueBookingSystem.Features.Users.Admin.Services.AdminService();
var customerService = new VenueBookingSystem.Features.Users.Customers.Services.CustomerService();

// ── Domain event subscriptions ────────────────────────────────────────────────
bookingService.BookingCreated   += (_, e) => ConsoleHelper.PrintInfo($"[Event] New booking created: #{e.Booking.BookingId} — {e.Booking.HallName}");
bookingService.BookingConfirmed += (_, e) => ConsoleHelper.PrintSuccess($"[Event] Booking confirmed: #{e.Booking.BookingId}");
bookingService.BookingCancelled += (_, e) => ConsoleHelper.PrintWarning($"[Event] Booking cancelled: #{e.Booking.BookingId}");
paymentService.PaymentProcessed += (_, e) => ConsoleHelper.PrintSuccess($"[Event] Payment processed: #{e.Payment.PaymentId} — {ConsoleHelper.FormatCurrency(e.Payment.Amount)}");
authService.UserLoggedIn        += (_, e) => ConsoleHelper.PrintInfo($"[Event] Login: {e.User.FullName} ({e.User.Role})");
authService.UserLoggedOut       += (_, _) => ConsoleHelper.PrintInfo("[Event] User logged out.");

// ── Main application loop ─────────────────────────────────────────────────────
bool running = true;
while (running)
{
    ConsoleHelper.ShowWelcomeScreen();

    var user = await loginScreen.ShowAsync();

    if (user == null)
    {
        if (ConsoleHelper.Confirm("Are you sure you want to exit?"))
        {
            running = false;
            break;
        }
        continue;
    }

    switch (user.Role)
    {
        case UserRole.Admin:
            var adminDash = new AdminDashboard(
                authService, hallService, bookingService,
                paymentService, reportService, bookingHistory);
            await adminDash.RunAsync();
            break;

        case UserRole.Customer:
            var customerDash = new CustomerDashboard(
                user, hallService, bookingService,
                paymentService, bookingHistory, authService);
            await customerDash.RunAsync();
            break;

        default:
            ConsoleHelper.PrintError($"Unknown role: {user.Role}. Contact your system administrator.");
            ConsoleHelper.PressAnyKey();
            break;
    }

    authService.Logout();
}

// ── Goodbye screen ────────────────────────────────────────────────────────────
ConsoleHelper.ShowGoodbyeScreen();
Environment.Exit(exitCode);
