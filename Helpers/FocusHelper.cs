using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Lahanku.Helpers
{
    // helpers:FocusHelper.FocusOnLoad="True" → cursor goes straight to this field when the page/modal opens
    public static class FocusHelper
    {
        public static readonly DependencyProperty FocusOnLoadProperty =
            DependencyProperty.RegisterAttached(
                "FocusOnLoad",
                typeof(bool),
                typeof(FocusHelper),
                new PropertyMetadata(false, OnFocusOnLoadChanged));

        public static bool GetFocusOnLoad(DependencyObject d) => (bool)d.GetValue(FocusOnLoadProperty);
        public static void SetFocusOnLoad(DependencyObject d, bool value) => d.SetValue(FocusOnLoadProperty, value);

        private static void OnFocusOnLoadChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement element && e.NewValue is true)
            {
                element.Loaded += (_, _) =>
                    element.Dispatcher.BeginInvoke(DispatcherPriority.Input, () => Keyboard.Focus(element));
            }
        }
    }
}
