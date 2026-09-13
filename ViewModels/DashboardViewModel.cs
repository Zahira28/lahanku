using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lahanku.Models;
using Lahanku.Services;

namespace Lahanku.ViewModels
{
    /// <summary>
    /// ViewModel Dashboard utama dengan indikator loading, penanganan error, dan banner coba lagi (SRP).
    /// </summary>
    public partial class DashboardViewModel : ViewModelBase
    {
        private readonly MainViewModel _main;
        private readonly ILandService _landService;
        private readonly IAuthService _authService;
        private readonly IErrorHandler _errorHandler;

        [ObservableProperty]
        private ObservableCollection<Land> _lands = new();

        [ObservableProperty]
        private string _totalLands = "0";

        [ObservableProperty]
        private string _totalArea = "0";

        [ObservableProperty]
        private string _totalVarieties = "0";

        [ObservableProperty]
        private bool _hasLands;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _hasError;

        [ObservableProperty]
        private string? _errorMessage;

        [ObservableProperty]
        private string _greetingText = "Selamat Siang!";

        [ObservableProperty]
        private string _selectedNav = "Ringkasan";

        [ObservableProperty]
        private bool _isSettingsView;

        public string CurrentUserName => !string.IsNullOrWhiteSpace(_authService.CurrentUser?.FullName)
            ? _authService.CurrentUser.FullName
            : (!string.IsNullOrWhiteSpace(_authService.CurrentUser?.Username) ? _authService.CurrentUser.Username : "Petani");

        public string CurrentUserUsername => !string.IsNullOrWhiteSpace(_authService.CurrentUser?.Username)
            ? $"@{_authService.CurrentUser.Username}"
            : "@petani";

        public DashboardViewModel(MainViewModel main, ILandService landService, IAuthService authService, IErrorHandler? errorHandler = null)
        {
            _main = main;
            _landService = landService;
            _authService = authService;
            _errorHandler = errorHandler ?? new ErrorHandler();

            UpdateGreeting();
            _ = LoadLandsAsync();
        }

        private void UpdateGreeting()
        {
            var hour = DateTime.Now.Hour;
            if (hour >= 4 && hour < 11)
            {
                GreetingText = "Selamat Pagi!";
            }
            else if (hour >= 11 && hour < 15)
            {
                GreetingText = "Selamat Siang!";
            }
            else if (hour >= 15 && hour < 18)
            {
                GreetingText = "Selamat Sore!";
            }
            else
            {
                GreetingText = "Selamat Malam!";
            }
        }

        [RelayCommand]
        public async Task LoadLandsAsync()
        {
            IsLoading = true;
            HasError = false;
            ErrorMessage = null;

            try
            {
                var currentUserId = _authService.CurrentUser?.Id;
                var landsList = await _landService.GetLandsAsync(currentUserId);
                Lands.Clear();
                foreach (var item in landsList)
                {
                    Lands.Add(item);
                }

                HasLands = Lands.Count > 0;

                if (HasLands)
                {
                    TotalLands = Lands.Count.ToString();

                    double totalHa = Lands.Sum(l => l.AreaHectares);
                    TotalArea = totalHa.ToString("0.##", CultureInfo.InvariantCulture);

                    var distinctCrops = Lands
                        .SelectMany(l => (l.CropType ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                        .Distinct()
                        .Count();

                    TotalVarieties = distinctCrops.ToString();
                }
                else
                {
                    TotalLands = "0";
                    TotalArea = "0";
                    TotalVarieties = "0";
                }
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = "Gagal memuat data dari Supabase. Silakan periksa koneksi internet Anda.";
                _errorHandler.HandleError(ex, ErrorMessage);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void OpenAddLandModal()
        {
            _main.OpenModal(new LandModalViewModel(_main, _landService, this));
        }

        [RelayCommand]
        private void OpenEditLandModal(Land? land)
        {
            if (land != null)
            {
                _main.OpenModal(new LandModalViewModel(_main, _landService, this, land));
            }
        }

        [RelayCommand]
        private void OpenDeleteLandModal(Land? land)
        {
            if (land != null)
            {
                _main.OpenModal(new DeleteConfirmationViewModel(_main, _landService, this, land));
            }
        }

        [RelayCommand]
        private void SelectLand(Land? land)
        {
            if (land != null)
            {
                _main.NavigateToLandDetail(land);
            }
        }

        [RelayCommand]
        private void SelectNav(string nav)
        {
            SelectedNav = nav;
            IsSettingsView = nav == "Pengaturan";
        }

        [RelayCommand]
        private void Logout()
        {
            _authService.Logout();
            _main.NavigateToLogin();
        }
    }
}
