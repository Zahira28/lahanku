using System.Threading.Tasks;
using Lahanku.Models;

namespace Lahanku.Services
{
    public interface IAuthService
    {
        User? CurrentUser { get; }
        Task<User?> LoginAsync(string username, string password);
        Task<(bool Success, string Message)> RegisterAsync(string username, string password);
        void Logout();
    }
}
