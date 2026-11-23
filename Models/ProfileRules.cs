namespace Lahanku.Models
{
    // Input checks for the profile page. Returns an error message, or null when the input is fine.
    public static class ProfileRules
    {
        public const int MaxNameLength = 50;
        public const int MinPasswordLength = 6;

        public static string? ValidateFullName(string? name)
        {
            var trimmed = name?.Trim() ?? string.Empty;
            if (trimmed.Length == 0)
            {
                return "Nama tidak boleh kosong.";
            }
            return trimmed.Length > MaxNameLength ? $"Nama maksimal {MaxNameLength} karakter." : null;
        }

        public static string? ValidatePasswordChange(string? current, string? next, string? confirm)
        {
            if (string.IsNullOrEmpty(current))
            {
                return "Password saat ini wajib diisi.";
            }
            if (string.IsNullOrEmpty(next) || next.Length < MinPasswordLength)
            {
                return $"Password baru minimal {MinPasswordLength} karakter.";
            }
            if (next != confirm)
            {
                return "Konfirmasi password tidak sama.";
            }
            return next == current ? "Password baru harus berbeda dari password saat ini." : null;
        }
    }
}
