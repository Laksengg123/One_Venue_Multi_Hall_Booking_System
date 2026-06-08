using System;

namespace VenueBookingSystem.Exceptions
{
    // Concept: Sealed Class & Custom Exception
    public sealed class ValidationException : Exception
    {
        public string FieldName { get; }
        public object? InvalidValue { get; }

        public ValidationException(string fieldName, string message) : base(message)
        {
            FieldName = fieldName;
        }

        public ValidationException(string fieldName, object? invalidValue, string message) : base(message)
        {
            FieldName = fieldName;
            InvalidValue = invalidValue;
        }
    }
}
