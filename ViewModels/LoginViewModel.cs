using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lahanku.Services;

namespace Lahanku.ViewModels
{
    /// <summary>
    /// ViewModel untuk halaman Login pengguna (SRP & DIP).
    /// </summary>
    public partial class LoginViewModel : ViewModelBase
    {
        private readonly MainViewModel _main;
        private readonly IAuthService _authService;

        [ObservableProperty]
        private string _username = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private string? _errorMessage;

        [ObservableProperty]
        private bool _isPasswordVisible;

        [ObservableProperty]
        private bool _isLoading;

        public LoginViewModel(MainViewModel main, IAuthService authService)
        {
            _main = main;
            _authService = authService;
        }

        [RelayCommand]
        private async Task LoginAsync()
        {
            ErrorMessage = null;

            if (string.IsNullOrWhiteSpace(Username))
            {
                ErrorMessage = "Silakan masukkan username.";
                _main.ShowWarningToast("Username tidak boleh kosong.", "Validasi");
                return;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Silakan masukkan password.";
                _main.ShowWarningToast("Password tidak boleh kosong.", "Validasi");
                return;
            }

            IsLoading = true;
            try
            {
                var user = await _authService.LoginAsync(Username, Password);
                if (user != null)
                {
                    _main.ShowSuccessToast($"Selamat datang kembali, {user.FullName ?? user.Username}!", "Login Berhasil");
                    _main.NavigateToDashboard();
                }
                else
                {
                    ErrorMessage = "Username atau password salah.";
                    _main.ShowErrorToast("Username atau password salah. Silakan periksa kembali.", "Gagal Masuk");
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void NavigateToSignUp()
        {
            _main.NavigateToSignUp();
        }

        [RelayCommand]
        private void TogglePasswordVisibility()
        {
            IsPasswordVisible = !IsPasswordVisible;
        }
    }
}
