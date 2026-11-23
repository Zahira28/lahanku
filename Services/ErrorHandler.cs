using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Sockets;
using Lahanku.Models.Errors;

namespace Lahanku.Services
{
    /// <summary>
    /// Implementasi layanan penanganan error terpusat (SRP - Single Responsibility Principle).
    /// Bertanggung jawab mencatat log, memetakan exception, dan mendispatch notifikasi ke UI.
    /// </summary>
    public class ErrorHandler : IErrorHandler
    {
        public event EventHandler<AppError>? ErrorOccurred;

        // Callback delegates yang dihubungkan ke MainViewModel
        public Action<string, string?>? ShowErrorToastAction { get; set; }
        public Action<string, string?>? ShowSuccessToastAction { get; set; }
        public Action<string, string?>? ShowWarningToastAction { get; set; }
        public Action<AppError, Action?>? ShowErrorDialogAction { get; set; }

        public void HandleError(Exception ex, string? userFriendlyMessage = null)
        {
            var appError = MapExceptionToAppError(ex, userFriendlyMessage);
            HandleError(appError);
        }

        public void HandleError(AppError error)
        {
            // 1. Log error untuk keperluan debugging/diagnostik
            Debug.WriteLine($"[ErrorHandler] [{error.Severity}] [{error.Code}] {error.Title}: {error.Message}");
            if (!string.IsNullOrWhiteSpace(error.TechnicalDetails))
            {
                Debug.WriteLine($"[ErrorHandler] Details: {error.TechnicalDetails}");
            }

            // 2. Trigger event jika ada observer
            ErrorOccurred?.Invoke(this, error);

            // 3. Dispatch visual feedback ke UI sesuai tingkat keparahan
            if (error.Severity == ErrorSeverity.Critical)
            {
                ShowErrorDialogAction?.Invoke(error, null);
            }
            else
            {
                ShowErrorToastAction?.Invoke(error.Message, error.Title);
            }
        }

        public void ShowErrorToast(string message, string? title = null)
        {
            ShowErrorToastAction?.Invoke(message, title ?? "Terjadi Kesalahan");
        }

        public void ShowSuccessToast(string message, string? title = null)
        {
            ShowSuccessToastAction?.Invoke(message, title ?? "Berhasil");
        }

        public void ShowWarningToast(string message, string? title = null)
        {
            ShowWarningToastAction?.Invoke(message, title ?? "Perhatian");
        }

        public void ShowErrorDialog(AppError error, Action? onRetry = null)
        {
            ShowErrorDialogAction?.Invoke(error, onRetry);
        }

        /// <summary>
        /// Menerjemahkan berbagai jenis raw exception menjadi pesan ramah bahasa Indonesia.
        /// </summary>
        private static AppError MapExceptionToAppError(Exception ex, string? userFriendlyMessage)
        {
            if (ex is LahankuException lahankuEx)
            {
                return lahankuEx.ToAppError();
            }

            var exMsg = ex.Message.ToLowerInvariant();

            // Deteksi gangguan koneksi jaringan / socket
            if (ex is HttpRequestException || ex is SocketException || exMsg.Contains("connection refused") || exMsg.Contains("timeout") || exMsg.Contains("no such host"))
            {
                var msg = userFriendlyMessage ?? "Tidak dapat terhubung ke server. Periksa koneksi internet Anda.";
                return AppError.Network(msg, ex);
            }

            // Deteksi error Supabase / Postgrest
            if (exMsg.Contains("pgrst") || exMsg.Contains("supabase") || exMsg.Contains("postgrest"))
            {
                var msg = userFriendlyMessage ?? "Gagal memproses data di database Supabase.";
                return AppError.Database(msg, ex);
            }

            var defaultMsg = userFriendlyMessage ?? (string.IsNullOrWhiteSpace(ex.Message) ? "Terjadi kesalahan pada sistem." : ex.Message);
            return new AppError(defaultMsg, "Kesalahan Sistem", "ERR_SYS", ErrorSeverity.Error, ErrorCategory.General, ex.ToString(), ex);
        }
    }
}
