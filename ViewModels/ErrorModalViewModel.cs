using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lahanku.Models.Errors;
using System.Windows.Input;

namespace Lahanku.ViewModels
{
    /// <summary>
    /// ViewModel for the modern error dialog with Retry action and technical details.
    /// </summary>
    public partial class ErrorModalViewModel : ViewModelBase, IKeyboardShortcutHandler
    {
        private readonly MainViewModel _main;
        private readonly Action? _onRetry;

        public AppError Error { get; }

        public string Title => Error.Title;
        public string Message => Error.Message;
        public string? TechnicalDetails => Error.TechnicalDetails;
        public bool HasTechnicalDetails => !string.IsNullOrWhiteSpace(TechnicalDetails);
        public bool HasRetryAction => _onRetry != null;

        [ObservableProperty]
        private bool _isDetailsExpanded;

        public ErrorModalViewModel(MainViewModel main, AppError error, Action? onRetry = null)
        {
            _main = main;
            Error = error;
            _onRetry = onRetry;
        }

        [RelayCommand]
        private void Retry()
        {
            _main.CloseModal();
            _onRetry?.Invoke();
        }

        [RelayCommand]
        private void Close()
        {
            _main.CloseModal();
        }

        [RelayCommand]
        private void ToggleDetails()
        {
            IsDetailsExpanded = !IsDetailsExpanded;
        }

        public bool HandleKey(Key key, ModifierKeys modifiers) =>
            key == Key.Enter && modifiers == ModifierKeys.None && CloseCommand.TryRun();
    }
}
