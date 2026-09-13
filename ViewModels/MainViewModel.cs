using System;
using System.Windows.Media;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lahanku.Models;
using Lahanku.Models.Errors;
using Lahanku.Services;

namespace Lahanku.ViewModels
{
    public enum ToastLevel
    {
        Success,
        Error,
        Warning,
        Info
    }

    public partial class MainViewModel : ViewModelBase
    {
        public IAuthService AuthService { get; }
        public ILandService LandService { get; }
        public IErrorHandler ErrorHandler { get; }

        private readonly DispatcherTimer _toastTimer;

        [ObservableProperty]
        private ViewModelBase? _currentView;

        [ObservableProperty]
        private ViewModelBase? _activeModal;

        [ObservableProperty]
        private string? _toastMessage;

        [ObservableProperty]
        private string _toastTitle = "Berhasil";

        [ObservableProperty]
        private ToastLevel _toastLevel = ToastLevel.Success;

        [ObservableProperty]
        private bool _isToastVisible;

        // Visual Brushes untuk dynamic multi-type toast UI
        public Brush ToastBackgroundBrush => ToastLevel switch
        {
            ToastLevel.Error => new SolidColorBrush(Color.FromRgb(0xFE, 0xF2, 0xF2)), // #FEF2F2 Red-50
            ToastLevel.Warning => new SolidColorBrush(Color.FromRgb(0xFF, 0xFB, 0xEB)), // #FFFBEB Amber-50
            _ => new SolidColorBrush(Color.FromRgb(0xEC, 0xFD, 0xF5)) // #ECFDF5 Emerald-50
        };

        public Brush ToastBorderBrush => ToastLevel switch
        {
            ToastLevel.Error => new SolidColorBrush(Color.FromRgb(0xFE, 0xCA, 0xCA)), // #FECACA
            ToastLevel.Warning => new SolidColorBrush(Color.FromRgb(0xFD, 0xE6, 0x8A)), // #FDE68A
            _ => new SolidColorBrush(Color.FromRgb(0xA7, 0xF3, 0xD0)) // #A7F3D0 Mint Border
        };

        public Brush ToastIconBackgroundBrush => ToastLevel switch
        {
            ToastLevel.Error => new SolidColorBrush(Color.FromRgb(0xFE, 0xE2, 0xE2)),
            ToastLevel.Warning => new SolidColorBrush(Color.FromRgb(0xFE, 0xF3, 0xC7)),
            _ => new SolidColorBrush(Color.FromRgb(0xD1, 0xFA, 0xE5)) // #D1FAE5 Mint Bg
        };

        public Brush ToastIconBrush => ToastLevel switch
        {
            ToastLevel.Error => new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26)), // Red-600
            ToastLevel.Warning => new SolidColorBrush(Color.FromRgb(0xD9, 0x77, 0x06)), // Amber-600
            _ => new SolidColorBrush(Color.FromRgb(0x05, 0x96, 0x69)) // #059669 Emerald
        };

        public Brush ToastTitleBrush => ToastLevel switch
        {
            ToastLevel.Error => new SolidColorBrush(Color.FromRgb(0x99, 0x1B, 0x1B)),
            ToastLevel.Warning => new SolidColorBrush(Color.FromRgb(0x92, 0x40, 0x0E)),
            _ => new SolidColorBrush(Color.FromRgb(0x06, 0x5F, 0x46))
        };

        public bool IsErrorToast => ToastLevel == ToastLevel.Error;
        public bool IsWarningToast => ToastLevel == ToastLevel.Warning;
        public bool IsSuccessToast => ToastLevel == ToastLevel.Success;

        public MainViewModel(IAuthService authService, ILandService landService, IErrorHandler? errorHandler = null)
        {
            AuthService = authService;
            LandService = landService;
            ErrorHandler = errorHandler ?? new ErrorHandler();

            // Hubungkan delegasi ErrorHandler ke UI MainViewModel
            if (ErrorHandler is ErrorHandler concreteHandler)
            {
                concreteHandler.ShowErrorToastAction = (msg, title) => ShowErrorToast(msg, title);
                concreteHandler.ShowSuccessToastAction = (msg, title) => ShowSuccessToast(msg, title);
                concreteHandler.ShowWarningToastAction = (msg, title) => ShowWarningToast(msg, title);
                concreteHandler.ShowErrorDialogAction = (err, retry) => ShowErrorModal(err, retry);
            }

            _toastTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(4)
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
            if (CurrentView is IDisposable disposable)
            {
                disposable.Dispose();
            }
            CurrentView = new DashboardViewModel(this, LandService, AuthService, ErrorHandler);
        }

        public void NavigateToLandDetail(Land land)
        {
            CurrentView = new LandDetailViewModel(this, LandService, land);
        }

        private readonly Stack<ViewModelBase> _modalStack = new();

        public void OpenModal(ViewModelBase modal)
        {
            if (ActiveModal != null && ActiveModal != modal)
            {
                _modalStack.Push(ActiveModal);
            }
            ActiveModal = modal;
        }

        [RelayCommand]
        public void CloseModal()
        {
            if (_modalStack.Count > 0)
            {
                ActiveModal = _modalStack.Pop();
            }
            else
            {
                ActiveModal = null;
            }
        }

        public void CloseAllModals()
        {
            _modalStack.Clear();
            ActiveModal = null;
        }

        public void ShowToast(string message, string? title = null, ToastLevel level = ToastLevel.Success)
        {
            ToastMessage = message;
            ToastTitle = title ?? (level == ToastLevel.Error ? "Terjadi Kesalahan" : level == ToastLevel.Warning ? "Peringatan" : "Berhasil");
            ToastLevel = level;

            OnPropertyChanged(nameof(ToastBackgroundBrush));
            OnPropertyChanged(nameof(ToastBorderBrush));
            OnPropertyChanged(nameof(ToastIconBackgroundBrush));
            OnPropertyChanged(nameof(ToastIconBrush));
            OnPropertyChanged(nameof(ToastTitleBrush));
            OnPropertyChanged(nameof(IsErrorToast));
            OnPropertyChanged(nameof(IsWarningToast));
            OnPropertyChanged(nameof(IsSuccessToast));

            IsToastVisible = true;
            _toastTimer.Stop();
            _toastTimer.Start();
        }

        public void ShowSuccessToast(string message, string? title = null) =>
            ShowToast(message, title ?? "Berhasil", ToastLevel.Success);

        public void ShowErrorToast(string message, string? title = null) =>
            ShowToast(message, title ?? "Terjadi Kesalahan", ToastLevel.Error);

        public void ShowWarningToast(string message, string? title = null) =>
            ShowToast(message, title ?? "Peringatan", ToastLevel.Warning);

        public void ShowErrorModal(AppError error, Action? onRetry = null)
        {
            OpenModal(new ErrorModalViewModel(this, error, onRetry));
        }

        [RelayCommand]
        public void DismissToast()
        {
            IsToastVisible = false;
            _toastTimer.Stop();
        }
    }
}
