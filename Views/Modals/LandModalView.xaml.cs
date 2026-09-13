using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Lahanku.ViewModels;

namespace Lahanku.Views.Modals
{
    public partial class LandModalView : UserControl
    {
        public LandModalView()
        {
            InitializeComponent();
            Loaded += LandModalView_Loaded;
            Unloaded += LandModalView_Unloaded;
        }

        private void LandModalView_Loaded(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            if (window != null)
            {
                window.PreviewMouseDown += Window_PreviewMouseDown;
            }
        }

        private void LandModalView_Unloaded(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            if (window != null)
            {
                window.PreviewMouseDown -= Window_PreviewMouseDown;
            }
        }

        private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is LandModalViewModel vm && vm.IsCropDropdownOpen)
            {
                if (CropTriggerBorder.IsMouseOver)
                    return;

                if (CropPopupContent != null && CropPopupContent.IsMouseOver)
                    return;

                vm.IsCropDropdownOpen = false;
            }
        }

        private void CropPopup_Opened(object sender, EventArgs e)
        {
            CropSearchTextBox?.Focus();
        }

        private void CropSearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (DataContext is LandModalViewModel vm)
                {
                    vm.IsCropDropdownOpen = false;
                }
                e.Handled = true;
            }
        }
    }
}

