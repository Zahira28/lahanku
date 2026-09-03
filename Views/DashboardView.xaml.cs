using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Lahanku.Models;
using Lahanku.ViewModels;

namespace Lahanku.Views
{
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();
        }

        private void KebabButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.ContextMenu != null)
            {
                btn.ContextMenu.PlacementTarget = btn;
                btn.ContextMenu.DataContext = btn.DataContext;
                btn.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                btn.ContextMenu.IsOpen = true;
                e.Handled = true;
            }
        }

        private void EditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                var land = menuItem.DataContext as Land ??
                           ((menuItem.Parent as ContextMenu)?.PlacementTarget as FrameworkElement)?.DataContext as Land;

                if (land != null && DataContext is DashboardViewModel vm)
                {
                    vm.OpenEditLandModalCommand.Execute(land);
                }
            }
        }

        private void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                var land = menuItem.DataContext as Land ??
                           ((menuItem.Parent as ContextMenu)?.PlacementTarget as FrameworkElement)?.DataContext as Land;

                if (land != null && DataContext is DashboardViewModel vm)
                {
                    vm.OpenDeleteLandModalCommand.Execute(land);
                }
            }
        }

        private void LandCard_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Ignore if clicked on the kebab button itself
            if (e.OriginalSource is DependencyObject depObj)
            {
                var parentBtn = FindParent<Button>(depObj);
                if (parentBtn != null) return;
            }

            if (sender is FrameworkElement element && 
                element.DataContext is Land land && 
                DataContext is DashboardViewModel vm)
            {
                vm.SelectLandCommand.Execute(land);
            }
        }

        private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject current = child;
            while (current != null)
            {
                if (current is T match) return match;
                current = System.Windows.Media.VisualTreeHelper.GetParent(current);
            }
            return null;
        }
    }
}
