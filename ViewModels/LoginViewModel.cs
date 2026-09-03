using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lahanku.Services;

namespace Lahanku.ViewModels
{
    public partial class LoginViewModel : ViewModelBase
    {
        private readonly MainViewModel _main;
        private readonly IAuthService _authService;

        [ObservableProperty]
        private string _username = "admin";

        [ObservableProperty]
        private string _password = "password123";

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
                return;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Silakan masukkan password.";
                return;
            }

            IsLoading = true;
            try
            {
                var user = await _authService.LoginAsync(Username, Password);
                if (user != null)
                {
                    _main.NavigateToDashboard();
                }
                else
                {
                    ErrorMessage = "Username atau password salah.";
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
