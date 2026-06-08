// Domain event EventArgs classes are now defined in their respective feature Events/ folders:
//
//   Authentication/Events/
//     - UserLoggedInEventArgs.cs
//     - UserLoggedOutEventArgs.cs
//
//   BookingManagement/Events/
//     - BookingCreatedEventArgs.cs
//     - BookingConfirmedEventArgs.cs
//     - BookingCancelledEventArgs.cs
//
//   PaymentManagement/Events/
//     - PaymentProcessedEventArgs.cs
//
// This Shared/Events folder is reserved for any cross-cutting event infrastructure
// (e.g. IEventDispatcher, IDomainEvent base types) added in the future.
namespace VenueBookingSystem.Shared.Events
{
    // Reserved for future cross-cutting event infrastructure.
}
