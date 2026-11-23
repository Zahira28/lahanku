using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Lahanku.Helpers;
using Lahanku.Models;
using Supabase.Gotrue.Exceptions;

namespace Lahanku.Services
{
    // Supabase Auth butuh email, jadi username diubah jadi "username@lahanku.local".
    // "Confirm email" di dashboard Supabase harus dimatikan.
    public partial class AuthService : IAuthService
    {
        public const string SyntheticEmailDomain = "lahanku.local";
        public const int MinPasswordLength = 6;

        public User? CurrentUser { get; private set; }

        public static string ToSyntheticEmail(string username) =>
            $"{username.Trim().ToLowerInvariant()}@{SyntheticEmailDomain}";

        [GeneratedRegex("^[a-zA-Z0-9_.]{3,30}$")]
        private static partial Regex UsernamePattern();

        public async Task<(User? User, string? Error)> LoginAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return (null, "Username dan password wajib diisi.");
            }

            if (!UsernamePattern().IsMatch(username.Trim()))
            {
                return (null, "Username atau password salah.");
            }

            try
            {
                var client = await SupabaseConfig.GetClientAsync();
                var session = await client.Auth.SignIn(ToSyntheticEmail(username), password);

                if (session?.User?.Id == null)
                {
                    return (null, "Username atau password salah.");
                }

                CurrentUser = await LoadProfileAsync(session.User.Id, username.Trim());
                return (CurrentUser, null);
            }
            catch (GotrueException ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AuthService] Login error ({ex.Reason}): {ex.Message}");
                return (null, MapAuthError(ex));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AuthService] Login error: {ex.Message}");
                return (null, "Tidak dapat terhubung ke server. Periksa koneksi internet Anda.");
            }
        }

        public async Task<(bool Success, string Message)> RegisterAsync(string username, string password)
        {
            var trimmed = username?.Trim() ?? string.Empty;

            if (!UsernamePattern().IsMatch(trimmed))
            {
                return (false, "Username 3–30 karakter, hanya huruf, angka, titik, dan garis bawah.");
            }

            if (string.IsNullOrWhiteSpace(password) || password.Length < MinPasswordLength)
            {
                return (false, $"Password minimal {MinPasswordLength} karakter.");
            }

            try
            {
                var client = await SupabaseConfig.GetClientAsync();
                var options = new Supabase.Gotrue.SignUpOptions
                {
                    Data = new Dictionary<string, object>
                    {
                        ["username"] = trimmed,
                        ["full_name"] = trimmed
                    }
                };

                var session = await client.Auth.SignUp(ToSyntheticEmail(trimmed), password, options);

                if (session?.User?.Id == null || string.IsNullOrEmpty(session.AccessToken))
                {
                    // nggak dapat sesi berarti confirm email masih nyala
                    return (false, "Akun dibuat tetapi butuh konfirmasi email. Admin perlu mematikan \"Confirm email\" di Supabase.");
                }

                CurrentUser = await LoadProfileAsync(session.User.Id, trimmed);
                return (true, "Registrasi berhasil.");
            }
            catch (GotrueException ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AuthService] Register error ({ex.Reason}): {ex.Message}");
                return (false, MapAuthError(ex));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AuthService] Register error: {ex.Message}");
                return (false, "Tidak dapat terhubung ke server. Periksa koneksi internet Anda.");
            }
        }

        public async Task LogoutAsync()
        {
            CurrentUser = null;
            try
            {
                var client = await SupabaseConfig.GetClientAsync();
                await client.Auth.SignOut();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AuthService] Logout error: {ex.Message}");
            }
        }

        // profil dibuat trigger waktu sign up; kalau belum kebaca, pakai data seadanya dulu
        private static async Task<User> LoadProfileAsync(string userId, string fallbackUsername)
        {
            var id = Guid.Parse(userId);
            try
            {
                var client = await SupabaseConfig.GetClientAsync();
                var response = await client.From<User>().Where(u => u.Id == id).Get();
                var profile = response.Models.FirstOrDefault();
                if (profile != null)
                {
                    return profile;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AuthService] Load profile error: {ex.Message}");
            }

            return new User { Id = id, Username = fallbackUsername, FullName = fallbackUsername };
        }

        private static string MapAuthError(GotrueException ex) => ex.Reason switch
        {
            FailureHint.Reason.UserBadLogin or FailureHint.Reason.UserBadPassword or FailureHint.Reason.UserBadMultiple
                => "Username atau password salah.",
            FailureHint.Reason.UserAlreadyRegistered
                => "Username sudah digunakan. Silakan gunakan username lain.",
            FailureHint.Reason.UserEmailNotConfirmed
                => "Akun belum dikonfirmasi. Admin perlu mematikan \"Confirm email\" di Supabase.",
            FailureHint.Reason.UserTooManyRequests
                => "Terlalu banyak percobaan. Tunggu beberapa saat lalu coba lagi.",
            FailureHint.Reason.Offline or FailureHint.Reason.NetworkError or FailureHint.Reason.CloudflareNetworkError
                => "Tidak dapat terhubung ke server. Periksa koneksi internet Anda.",
            _ => ex.StatusCode == 422 && ex.Message.Contains("password", StringComparison.OrdinalIgnoreCase)
                ? $"Password terlalu lemah. Gunakan minimal {MinPasswordLength} karakter."
                : "Autentikasi gagal. Silakan coba lagi."
        };
    }
}
