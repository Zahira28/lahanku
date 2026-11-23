using System;

namespace Lahanku.Models.Errors
{
    public enum ErrorSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }

    public enum ErrorCategory
    {
        General,
        Network,
        Database,
        Validation,
        Authentication
    }

    /// <summary>
    /// Merepresentasikan entitas error terstruktur dalam aplikasi (OOP Encapsulation).
    /// </summary>
    public class AppError
    {
        public string Code { get; }
        public string Title { get; }
        public string Message { get; }
        public string? TechnicalDetails { get; }
        public ErrorSeverity Severity { get; }
        public ErrorCategory Category { get; }
        public DateTime Timestamp { get; }
        public Exception? OriginalException { get; }

        public AppError(
            string message,
            string? title = null,
            string code = "ERR_GENERAL",
            ErrorSeverity severity = ErrorSeverity.Error,
            ErrorCategory category = ErrorCategory.General,
            string? technicalDetails = null,
            Exception? originalException = null)
        {
            Message = message;
            Title = title ?? GetDefaultTitleForCategory(category);
            Code = code;
            Severity = severity;
            Category = category;
            TechnicalDetails = technicalDetails ?? originalException?.Message;
            OriginalException = originalException;
            Timestamp = DateTime.UtcNow;
        }

        private static string GetDefaultTitleForCategory(ErrorCategory category) => category switch
        {
            ErrorCategory.Network => "Gangguan Jaringan",
            ErrorCategory.Database => "Kesalahan Database",
            ErrorCategory.Validation => "Validasi Gagal",
            ErrorCategory.Authentication => "Autentikasi Gagal",
            _ => "Terjadi Kesalahan"
        };

        // Factory Methods (OOP Creational Pattern)
        public static AppError Network(string message, Exception? ex = null) =>
            new(message, "Gangguan Koneksi", "ERR_NET", ErrorSeverity.Error, ErrorCategory.Network, ex?.ToString(), ex);

        public static AppError Database(string message, Exception? ex = null) =>
            new(message, "Koneksi Database", "ERR_DB", ErrorSeverity.Error, ErrorCategory.Database, ex?.ToString(), ex);

        public static AppError Validation(string message, string? field = null) =>
            new(message, "Data Tidak Valid", "ERR_VAL", ErrorSeverity.Warning, ErrorCategory.Validation, field != null ? $"Field: {field}" : null);

        public static AppError Authentication(string message) =>
            new(message, "Gagal Masuk", "ERR_AUTH", ErrorSeverity.Warning, ErrorCategory.Authentication);

        public static AppError Critical(string message, Exception? ex = null) =>
            new(message, "Kesalahan Sistem Kritis", "ERR_CRIT", ErrorSeverity.Critical, ErrorCategory.General, ex?.ToString(), ex);
    }
}
