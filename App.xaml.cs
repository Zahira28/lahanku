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

            if (!Helpers.SupabaseConfig.IsConfigured)
            {
                MessageBox.Show(
                    "Konfigurasi Supabase belum ditemukan.\n\n" +
                    "Yang kurang:\n• " + string.Join("\n• ", Helpers.SupabaseConfig.GetMissingSettings()) + "\n\n" +
                    "Cara memperbaiki:\n" +
                    "1. Salin appsettings.example.json menjadi appsettings.json\n" +
                    "2. Isi Url dan AnonKey dari dashboard Supabase (Project Settings → API)\n" +
                    "3. Jalankan ulang aplikasi",
                    "LahanKu - Konfigurasi Belum Lengkap",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                Shutdown(1);
                return;
            }

            // Init the centralized error handler service (SRP & DIP)
            var errorHandler = new ErrorHandler();
            _errorHandler = errorHandler;

            // Global exception handler for background/non-UI threads
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
            // If mainWindow isn't open yet, show a fallback MessageBox so the user knows
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
