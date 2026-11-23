using System;
using Lahanku.Models.Errors;

namespace Lahanku.Services
{
    /// <summary>
    /// Contract for the centralized error handling service (ISP & DIP).
    /// </summary>
    public interface IErrorHandler
    {
        event EventHandler<AppError>? ErrorOccurred;

        void HandleError(Exception ex, string? userFriendlyMessage = null);
        void HandleError(AppError error);

        void ShowErrorToast(string message, string? title = null);
        void ShowSuccessToast(string message, string? title = null);
        void ShowWarningToast(string message, string? title = null);
        void ShowErrorDialog(AppError error, Action? onRetry = null);
    }
}
