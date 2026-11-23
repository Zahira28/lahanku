using System.Threading.Tasks;
using Lahanku.Models;

namespace Lahanku.Services
{
    public interface IAuthService
    {
        User? CurrentUser { get; }

        Task<(User? User, string? Error)> LoginAsync(string username, string password);

        Task<(bool Success, string Message)> RegisterAsync(string username, string password);

        Task LogoutAsync();

        // the shared demo account can't change its name or password
        bool IsDemoAccount { get; }

        Task<(bool Success, string Message)> UpdateFullNameAsync(string fullName);

        Task<(bool Success, string Message)> ChangePasswordAsync(string currentPassword, string newPassword);
    }
}
