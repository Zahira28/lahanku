using System;
using System.Text.Json.Serialization;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Lahanku.Models
{
    // Profil user dari tabel profiles. Password dikelola Supabase Auth, nggak disimpan di sini.
    [Table("profiles")]
    public class User : BaseModel
    {
        [PrimaryKey("id", true)]
        public Guid Id { get; set; }

        [Column("username")]
        public string Username { get; set; } = string.Empty;

        [Column("full_name")]
        public string? FullName { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        public string DisplayName => !string.IsNullOrWhiteSpace(FullName) ? FullName : Username;

        [JsonIgnore]
        public string Handle => $"@{Username}";
    }
}
