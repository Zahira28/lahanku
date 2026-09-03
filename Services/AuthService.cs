using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lahanku.Models;

namespace Lahanku.Services
{
    public class AuthService : IAuthService
    {
        private readonly List<User> _users = new()
        {
            new User
            {
                Id = 1,
                Username = "admin",
                Password = "password123",
                FullName = "Administrator LahanKu",
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = 2,
                Username = "josiah",
                Password = "password123",
                FullName = "Josiah Hermes",
                CreatedAt = DateTime.UtcNow
            }
        };

        public User? CurrentUser { get; private set; }

        public async Task<User?> LoginAsync(string username, string password)
        {
            await Task.Delay(100); // Simulate network / DB IO

            var user = _users.FirstOrDefault(u => 
                string.Equals(u.Username, username.Trim(), StringComparison.OrdinalIgnoreCase) && 
                u.Password == password);

            if (user != null)
            {
                CurrentUser = user;
            }

            return user;
        }

        public async Task<(bool Success, string Message)> RegisterAsync(string username, string password)
        {
            await Task.Delay(100);

            if (string.IsNullOrWhiteSpace(username))
            {
                return (false, "Username tidak boleh kosong.");
            }

            if (string.IsNullOrWhiteSpace(password) || password.Length < 4)
            {
                return (false, "Password minimal 4 karakter.");
            }

            if (_users.Any(u => string.Equals(u.Username, username.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                return (false, "Username sudah digunakan.");
            }

            var newUser = new User
            {
                Id = _users.Count > 0 ? _users.Max(u => u.Id) + 1 : 1,
                Username = username.Trim(),
                Password = password,
                FullName = username.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _users.Add(newUser);
            CurrentUser = newUser;

            return (true, "Registrasi berhasil.");
        }

        public void Logout()
        {
            CurrentUser = null;
        }
    }
}
