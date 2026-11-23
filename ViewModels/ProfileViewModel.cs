using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lahanku.Models;
using Lahanku.Services;

namespace Lahanku.ViewModels
{
    // Profile page (replaces the old "Pengaturan" menu): account data, edit name, change password, logout.
    public partial class ProfileViewModel : ViewModelBase, IKeyboardShortcutHandler
    {
        private readonly MainViewModel _main;
        private readonly IAuthService _auth;

        public ProfileViewModel(MainViewModel main, IAuthService auth)
        {
            _main = main;
            _auth = auth;
            _fullNameInput = auth.CurrentUser?.FullName ?? auth.CurrentUser?.Username ?? string.Empty;
        }

        public string DisplayName => _auth.CurrentUser?.DisplayName ?? "Petani";
        public string Handle => _auth.CurrentUser?.Handle ?? string.Empty;
        public string Username => _auth.CurrentUser?.Username ?? string.Empty;

        public string JoinedText => _auth.CurrentUser == null
            ? string.Empty
            : _auth.CurrentUser.CreatedAt.ToLocalTime().ToString("d MMMM yyyy", CultureInfo.GetCultureInfo("id-ID"));

        public bool IsDemo => _auth.IsDemoAccount;
        public bool CanEdit => !IsDemo;

        // edit name
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveNameCommand))]
        private string _fullNameInput;

        [ObservableProperty]
        private string? _nameError;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveNameCommand))]
        private bool _isSavingName;

        private bool CanSaveName() => CanEdit && !IsSavingName
            && FullNameInput.Trim() != (_auth.CurrentUser?.FullName ?? string.Empty);

        [RelayCommand(CanExecute = nameof(CanSaveName))]
        private async Task SaveNameAsync()
        {
            NameError = ProfileRules.ValidateFullName(FullNameInput);
            if (NameError != null)
            {
                return;
            }

            IsSavingName = true;
            var (ok, message) = await _auth.UpdateFullNameAsync(FullNameInput);
            IsSavingName = false;

            if (!ok)
            {
                NameError = message;
                return;
            }

            FullNameInput = FullNameInput.Trim();
            OnPropertyChanged(nameof(DisplayName));
            SaveNameCommand.NotifyCanExecuteChanged();
            _main.RefreshCurrentUser();
            _main.ShowSuccessToast(message);
        }

        // change password
        [ObservableProperty] private string _currentPassword = string.Empty;
        [ObservableProperty] private string _newPassword = string.Empty;
        [ObservableProperty] private string _confirmPassword = string.Empty;
        [ObservableProperty] private string? _passwordError;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ChangePasswordCommand))]
        private bool _isSavingPassword;

        private bool CanChangePassword() => CanEdit && !IsSavingPassword;

        [RelayCommand(CanExecute = nameof(CanChangePassword))]
        private async Task ChangePasswordAsync()
        {
            PasswordError = ProfileRules.ValidatePasswordChange(CurrentPassword, NewPassword, ConfirmPassword);
            if (PasswordError != null)
            {
                return;
            }

            IsSavingPassword = true;
            var (ok, message) = await _auth.ChangePasswordAsync(CurrentPassword, NewPassword);
            IsSavingPassword = false;

            if (!ok)
            {
                PasswordError = message;
                return;
            }

            CurrentPassword = NewPassword = ConfirmPassword = string.Empty;
            _main.ShowSuccessToast(message);
        }

        // clear the error as soon as the user edits again
        partial void OnFullNameInputChanged(string value) => NameError = null;
        partial void OnCurrentPasswordChanged(string value) => PasswordError = null;
        partial void OnNewPasswordChanged(string value) => PasswordError = null;
        partial void OnConfirmPasswordChanged(string value) => PasswordError = null;

        // Esc back to the dashboard
        public bool HandleKey(Key key, ModifierKeys modifiers)
        {
            if (key == Key.Escape && modifiers == ModifierKeys.None)
            {
                _main.NavigateToDashboard();
                return true;
            }
            return false;
        }
    }
}
