using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
            PreviewKeyDown += Window_PreviewKeyDown;
        }

        private void Backdrop_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.CloseModalCommand.Execute(null);
            }
        }

        // Keyboard shortcuts: the active modal gets them first, otherwise the page.
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (DataContext is not MainViewModel vm)
            {
                return;
            }

            var key = e.Key == Key.System ? e.SystemKey : e.Key;
            var focused = Keyboard.FocusedElement as DependencyObject;

            if (key == Key.Enter)
            {
                if (ControlHandlesEnterItself(focused))
                {
                    return;
                }

                // make sure the text being typed reaches the ViewModel before submitting
                (focused as TextBox)?.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
            }

            object? target = vm.ActiveModal ?? vm.CurrentView;
            if (target is IKeyboardShortcutHandler handler && handler.HandleKey(key, Keyboard.Modifiers))
            {
                e.Handled = true;
                return;
            }

            if (key == Key.Escape && vm.ActiveModal != null)
            {
                vm.CloseModalCommand.Execute(null);
                e.Handled = true;
            }
        }

        private static bool ControlHandlesEnterItself(DependencyObject? focused) => focused switch
        {
            TextBox { AcceptsReturn: true } => true,      // kolom catatan multi-baris
            ButtonBase => true,                            // Enter on a focused button = click that button
            ComboBox { IsDropDownOpen: true } => true,
            ComboBoxItem => true,
            UIElement element when element.InputBindings.OfType<KeyBinding>().Any(k => k.Key == Key.Enter) => true,
            _ => false
        };
    }
}
