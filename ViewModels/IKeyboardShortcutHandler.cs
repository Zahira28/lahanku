using System.Windows.Input;

namespace Lahanku.ViewModels
{
    // Called by MainWindow on every key press. Return true if the key was handled.
    public interface IKeyboardShortcutHandler
    {
        bool HandleKey(Key key, ModifierKeys modifiers);
    }

    public static class CommandExtensions
    {
        public static bool TryRun(this ICommand command, object? parameter = null)
        {
            if (!command.CanExecute(parameter))
            {
                return false;
            }

            command.Execute(parameter);
            return true;
        }
    }
}
