namespace VenueBookingSystem.Features.Authentication;

//enum for the group of named constants
public enum UserRole
{
    Admin = 1,
    Customer = 2
}

public enum BookingStatus
{
    Pending = 1,
    Confirmed = 2,
    Cancelled = 3,
    Completed = 4,

    Rejected = 5
}

public enum PaymentMethod
{
    Cash = 1,
    Card = 2,
    UPI = 3,
    BankTransfer = 4,
    CashOnDelivery = 5
}

public enum PaymentStatus
{
    Pending = 1,
    Completed = 2,
    Failed = 3,
    Refunded = 4
}

public record User(
    int UserId,
    string Username,
    string PasswordHash,
    string Email,
    string Phone,
    string FullName,
    UserRole Role,
    bool IsActive,
    DateTime CreatedAt
);


public static class UserExtensions
{
 
    public static string GetRoleDisplay(this User user) => user.Role == UserRole.Admin ? "Administrator" : "Customer";

    public static bool IsAdmin(this User user)    => user.Role == UserRole.Admin;
}
