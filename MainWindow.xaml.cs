using System.Windows;
using System.Windows.Input;
using Lahanku.ViewModels;

namespace Lahanku
{
    public partial class MainWindow : Window
    {
        public MainWindow() : this(new MainViewModel(new Services.AuthService(), new Services.LandService()))
        {
        }

        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void Backdrop_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.CloseModalCommand.Execute(null);
            }
        }
    }
}
