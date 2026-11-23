using Lahanku.Models;
using Xunit;

namespace Lahanku.Tests
{
    public class ProfileRulesTests
    {
        [Theory]
        [InlineData("Aulia Nur", null)]
        [InlineData("  Pak Budi  ", null)]
        [InlineData("", "Nama tidak boleh kosong.")]
        [InlineData("   ", "Nama tidak boleh kosong.")]
        public void Full_name_must_not_be_empty(string name, string? expected)
        {
            Assert.Equal(expected, ProfileRules.ValidateFullName(name));
        }

        [Fact]
        public void Full_name_has_a_length_limit()
        {
            Assert.Null(ProfileRules.ValidateFullName(new string('a', 50)));
            Assert.Equal("Nama maksimal 50 karakter.", ProfileRules.ValidateFullName(new string('a', 51)));
        }

        [Fact]
        public void Valid_password_change_passes()
        {
            Assert.Null(ProfileRules.ValidatePasswordChange("lama123", "baru456", "baru456"));
        }

        [Theory]
        [InlineData("", "baru456", "baru456", "Password saat ini wajib diisi.")]
        [InlineData("lama123", "123", "123", "Password baru minimal 6 karakter.")]
        [InlineData("lama123", "baru456", "baru457", "Konfirmasi password tidak sama.")]
        [InlineData("lama123", "lama123", "lama123", "Password baru harus berbeda dari password saat ini.")]
        public void Invalid_password_change_explains_why(string current, string next, string confirm, string expected)
        {
            Assert.Equal(expected, ProfileRules.ValidatePasswordChange(current, next, confirm));
        }
    }
}
