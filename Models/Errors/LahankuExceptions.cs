using System;

namespace Lahanku.Models.Errors
{
    /// <summary>
    /// Base class untuk semua exception internal aplikasi LahanKu (OOP Inheritance & Polymorphism).
    /// </summary>
    public abstract class LahankuException : Exception
    {
        public string ErrorCode { get; }
        public ErrorCategory Category { get; }

        protected LahankuException(string message, string errorCode, ErrorCategory category, Exception? innerException = null)
            : base(message, innerException)
        {
            ErrorCode = errorCode;
            Category = category;
        }

        public virtual AppError ToAppError() =>
            new(Message, null, ErrorCode, ErrorSeverity.Error, Category, InnerException?.Message, this);
    }

    public class NetworkException : LahankuException
    {
        public NetworkException(string message, Exception? innerException = null)
            : base(message, "ERR_NET_CONN", ErrorCategory.Network, innerException)
        {
        }
    }

    public class DatabaseException : LahankuException
    {
        public DatabaseException(string message, Exception? innerException = null)
            : base(message, "ERR_DB_QUERY", ErrorCategory.Database, innerException)
        {
        }
    }

    public class ValidationException : LahankuException
    {
        public string? TargetField { get; }

        public ValidationException(string message, string? targetField = null)
            : base(message, "ERR_VALIDATION", ErrorCategory.Validation)
        {
            TargetField = targetField;
        }

        public override AppError ToAppError() =>
            AppError.Validation(Message, TargetField);
    }

    public class AuthenticationException : LahankuException
    {
        public AuthenticationException(string message)
            : base(message, "ERR_AUTH", ErrorCategory.Authentication)
        {
        }

        public override AppError ToAppError() =>
            AppError.Authentication(Message);
    }
}
