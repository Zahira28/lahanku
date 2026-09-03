using System.Windows;
using Lahanku.Services;
using Lahanku.ViewModels;

namespace Lahanku
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var authService = new AuthService();
            var landService = new LandService();
            var mainViewModel = new MainViewModel(authService, landService);

            var mainWindow = new MainWindow(mainViewModel);
            mainWindow.Show();
        }
    }
}
