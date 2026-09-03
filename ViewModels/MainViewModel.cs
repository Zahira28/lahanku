using System;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lahanku.Models;
using Lahanku.Services;

namespace Lahanku.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        public IAuthService AuthService { get; }
        public ILandService LandService { get; }

        private readonly DispatcherTimer _toastTimer;

        [ObservableProperty]
        private ViewModelBase? _currentView;

        [ObservableProperty]
        private ViewModelBase? _activeModal;

        [ObservableProperty]
        private string? _toastMessage;

        [ObservableProperty]
        private bool _isToastVisible;

        public MainViewModel(IAuthService authService, ILandService landService)
        {
            AuthService = authService;
            LandService = landService;

            _toastTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3.5)
            };
            _toastTimer.Tick += (s, e) =>
            {
                IsToastVisible = false;
                _toastTimer.Stop();
            };

            // Start on Dashboard if logged in, or Login
            NavigateToLogin();
        }

        public void NavigateToLogin()
        {
            CurrentView = new LoginViewModel(this, AuthService);
        }

        public void NavigateToSignUp()
        {
            CurrentView = new SignUpViewModel(this, AuthService);
        }

        public void NavigateToDashboard()
        {
            CurrentView = new DashboardViewModel(this, LandService, AuthService);
        }

        public void NavigateToLandDetail(Land land)
        {
            CurrentView = new LandDetailViewModel(this, LandService, land);
        }

        public void OpenModal(ViewModelBase modal)
        {
            ActiveModal = modal;
        }

        [RelayCommand]
        public void CloseModal()
        {
            ActiveModal = null;
        }

        public void ShowToast(string message)
        {
            ToastMessage = message;
            IsToastVisible = true;
            _toastTimer.Stop();
            _toastTimer.Start();
        }

        [RelayCommand]
        public void DismissToast()
        {
            IsToastVisible = false;
            _toastTimer.Stop();
        }
    }
}
