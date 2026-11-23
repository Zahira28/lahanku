using System.Windows.Input;

namespace Lahanku.ViewModels
{
    // Dipanggil MainWindow tiap ada tombol ditekan. Return true kalau tombolnya dipakai.
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
