using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lahanku.Services;

namespace Lahanku.ViewModels
{
    public partial class SignUpViewModel : ViewModelBase
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
                return;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Silakan isi password.";
                return;
            }

            if (Password != ConfirmPassword)
            {
                ErrorMessage = "Konfirmasi password tidak cocok.";
                return;
            }

            IsLoading = true;
            try
            {
                var (success, message) = await _authService.RegisterAsync(Username, Password);
                if (success)
                {
                    _main.ShowToast("Akun berhasil didaftarkan!");
                    _main.NavigateToDashboard();
                }
                else
                {
                    ErrorMessage = message;
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
    }
}
