using System;
using System.Windows;
using System.Windows.Threading;
using Lahanku.Models.Errors;
using Lahanku.Services;
using Lahanku.ViewModels;

namespace Lahanku
{
    public partial class App : Application
    {
        private IErrorHandler? _errorHandler;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Inisialisasi layanan penanganan error terpusat (SRP & DIP)
            var errorHandler = new ErrorHandler();
            _errorHandler = errorHandler;

            // Global exception handler untuk thread background/non-UI
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                if (args.ExceptionObject is Exception ex)
                {
                    _errorHandler?.HandleError(AppError.Critical("Terjadi kesalahan sistem di latar belakang.", ex));
                }
            };

            var authService = new AuthService();
            var landService = new LandService();
            var mainViewModel = new MainViewModel(authService, landService, errorHandler);

            var mainWindow = new MainWindow(mainViewModel);
            this.MainWindow = mainWindow;
            mainWindow.Show();
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // Jika mainWindow belum terbuka, tampilkan MessageBox fallback agar user tahu
            if (MainWindow == null || !MainWindow.IsLoaded)
            {
                MessageBox.Show($"Terjadi kesalahan saat memulai aplikasi:\n{e.Exception.Message}", "LahanKu - Kesalahan", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                _errorHandler?.HandleError(AppError.Critical("Terjadi kesalahan tak terduga pada antarmuka aplikasi.", e.Exception));
            }
            e.Handled = true;
        }
    }
}
