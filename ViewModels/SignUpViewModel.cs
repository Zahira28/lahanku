using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lahanku.Services;
using System.Windows.Input;

namespace Lahanku.ViewModels
{
    /// <summary>
    /// ViewModel untuk pendaftaran akun pengguna baru (SRP & DIP).
    /// </summary>
    public partial class SignUpViewModel : ViewModelBase, IKeyboardShortcutHandler
    {
        private readonly MainViewModel _main;
        private readonly IAuthService _authService;

        [ObservableProperty]
        private string _username = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private string _confirmPassword = string.Empty;

        [ObservableProperty]
        private string? _errorMessage;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _isPasswordVisible;

        [RelayCommand]
        private void TogglePasswordVisibility()
        {
            IsPasswordVisible = !IsPasswordVisible;
        }

        public SignUpViewModel(MainViewModel main, IAuthService authService)
        {
            _main = main;
            _authService = authService;
        }

        [RelayCommand]
        private async Task SignUpAsync()
        {
            ErrorMessage = null;

            if (string.IsNullOrWhiteSpace(Username))
            {
                ErrorMessage = "Silakan isi username.";
                _main.ShowWarningToast("Username tidak boleh kosong.", "Validasi");
                return;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Silakan isi password.";
                _main.ShowWarningToast("Password tidak boleh kosong.", "Validasi");
                return;
            }

            if (Password != ConfirmPassword)
            {
                ErrorMessage = "Konfirmasi password tidak cocok.";
                _main.ShowWarningToast("Konfirmasi password tidak cocok.", "Validasi");
                return;
            }

            IsLoading = true;
            try
            {
                var (success, message) = await _authService.RegisterAsync(Username, Password);
                if (success)
                {
                    _main.ShowSuccessToast("Akun berhasil didaftarkan!", "Registrasi Berhasil");
                    _main.NavigateToDashboard();
                }
                else
                {
                    ErrorMessage = message;
                    _main.ShowErrorToast(message, "Gagal Mendaftar");
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void NavigateToLogin()
        {
            _main.NavigateToLogin();
        }

        public bool HandleKey(Key key, ModifierKeys modifiers) => key switch
        {
            Key.Enter when modifiers == ModifierKeys.None => SignUpCommand.TryRun(),
            _ => false
        };
    }
}
