using System;
using System.Linq;
using System.Threading.Tasks;
using Lahanku.Helpers;
using Lahanku.Models;

namespace Lahanku.Services
{
    /// <summary>
    /// Layanan autentikasi pengguna menggunakan database Supabase (tabel users).
    /// Menerapkan Single Responsibility Principle (SRP) untuk manajemen sesi dan autentikasi.
    /// </summary>
    public class AuthService : IAuthService
    {
        public User? CurrentUser { get; private set; }

        public async Task<User?> LoginAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return null;
            }

            try
            {
                var client = await SupabaseConfig.GetClientAsync();
                var trimmed = username.Trim();

                var response = await client
                    .From<User>()
                    .Where(u => u.Username == trimmed)
                    .Get();

                var user = response.Models.FirstOrDefault();
                if (user != null && user.Password == password)
                {
                    CurrentUser = user;
                    return user;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AuthService] Login error: {ex.Message}");
            }

            return null;
        }

        public async Task<(bool Success, string Message)> RegisterAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return (false, "Username tidak boleh kosong.");
            }

            if (string.IsNullOrWhiteSpace(password) || password.Length < 4)
            {
                return (false, "Password minimal 4 karakter.");
            }

            try
            {
                var client = await SupabaseConfig.GetClientAsync();
                var trimmed = username.Trim();

                // Cek apakah username sudah ada
                var existing = await client
                    .From<User>()
                    .Where(u => u.Username == trimmed)
                    .Get();

                if (existing.Models.Count > 0)
                {
                    return (false, "Username sudah digunakan. Silakan gunakan username lain.");
                }

                var newUser = new User
                {
                    Username = trimmed,
                    Password = password,
                    FullName = trimmed,
                    CreatedAt = DateTime.UtcNow
                };

                var insertResponse = await client
                    .From<User>()
                    .Insert(newUser);

                var createdUser = insertResponse.Models.FirstOrDefault() ?? newUser;
                CurrentUser = createdUser;

                return (true, "Registrasi berhasil.");
            }
            catch (Exception ex)
            {
                return (false, $"Gagal mendaftar: {ex.Message}");
            }
        }

        public void Logout()
        {
            CurrentUser = null;
        }
    }
}
