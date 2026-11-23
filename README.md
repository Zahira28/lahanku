<h1 align="center">LahanKu</h1>

<p align="center">
  <strong>Siram secukupnya, hemat airnya.</strong><br>
  Aplikasi desktop manajemen kebun pekarangan dengan rekomendasi penyiraman berbasis cuaca.
</p>

<p align="center">
  <a href="#akun-demo">Akun Demo</a> ·
  <a href="#menjalankan-aplikasi">Menjalankan</a> ·
  <a href="#setup-database">Setup Database</a> ·
  <a href="#membuat-file-exe">Build .exe</a>
</p>

## Tentang LahanKu

LahanKu membantu penggiat urban farming dan kebun skala rumah tangga beradaptasi dengan cuaca yang makin sulit diprediksi. Setiap pagi aplikasi membaca cuaca terkini dan prakiraan hujan untuk lokasi setiap lahan, lalu menyarankan apakah penyiraman perlu dilakukan, ditunda karena akan hujan, atau ditambah karena cuaca panas. Penyiraman yang dicatat dibandingkan dengan penyiraman rutin sehingga jumlah air yang berhasil dihemat dapat dilihat secara nyata.

Proyek ini mendukung **SDG 13 (Climate Action)** melalui praktik Climate-Smart Agriculture pada skala pekarangan.

## Fitur

| Area | Kemampuan |
| --- | --- |
| Akun | Registrasi dan login dengan Supabase Auth, data setiap pengguna dilindungi Row Level Security |
| Lahan | Tambah, ubah, dan hapus lahan; pilih lokasi di peta; 75 jenis tanaman pekarangan beserta kebutuhan airnya |
| Cuaca | Cuaca terkini dan prakiraan hujan 24 jam per lahan dari Open-Meteo |
| Rekomendasi harian | Siram, tunda karena hujan, atau tambah volume saat panas, lengkap dengan volume anjuran dan waktu terbaik menyiram |
| Catatan penyiraman | Riwayat penyiraman dengan evaluasi terhadap rekomendasi hari itu |
| Dampak | Air yang dihemat dibandingkan penyiraman rutin, grafik pemakaian harian, dan ringkasan per lahan untuk 7 hari, 30 hari, atau seluruh catatan |
| Profil | Ubah nama tampilan, ganti password, dan keluar akun |
| Akun demo | Akun bersama berisi riwayat 30 hari dari cuaca asli untuk keperluan presentasi |

## Teknologi

| Lapisan | Teknologi |
| --- | --- |
| Aplikasi | C# .NET 8, WPF, pola MVVM (CommunityToolkit.Mvvm) |
| Backend | Supabase: PostgreSQL, Auth, Row Level Security |
| Cuaca | Open-Meteo Forecast API |
| Peta | WebView2, Leaflet, basemap CARTO, geocoding Nominatim |
| Pengujian | xUnit |
| Otomasi | GitHub Actions untuk menjaga proyek Supabase tetap aktif |

## Struktur repository

```text
junpro/
├── Controls/           Komponen UI bersama: sidebar, grafik, pemilih tanggal
├── Converters/         Value converter untuk binding XAML
├── Helpers/            Konfigurasi, waktu lokal, dan helper input
├── Models/             Model data dan logika murni (rekomendasi, evaluasi, dampak)
├── Services/           Akses Supabase, cuaca, autentikasi, dan seeder akun demo
├── Styles/             Warna, ikon vektor, dan gaya kontrol
├── ViewModels/         Logika halaman dan modal
├── Views/              Halaman dan modal XAML
├── database/
│   └── migrations/     Skrip SQL untuk Supabase
└── Lahanku.Tests/      Unit test
```

## Akun Demo

Untuk mencoba aplikasi tanpa mendaftar, masuk dengan akun demo:

| Username | Password |
| --- | --- |
| `demo` | `demo123` |

Akun demo berisi tiga lahan pekarangan di sekitar Yogyakarta dengan riwayat penyiraman 30 hari terakhir, sehingga halaman **Dampak** langsung menampilkan grafik penghematan air. Nama dan password akun demo tidak dapat diubah dari aplikasi.

Data demo dibuat dari cuaca asli 30 hari terakhir dan dapat dibuat ulang kapan saja agar tetap terbaru:

```powershell
dotnet run -- --seed-demo
```

Perintah ini menghapus lahan lama milik akun demo lalu mengisinya kembali.

## Menjalankan aplikasi

Persyaratan:

- Windows 10 atau 11;
- .NET 8 SDK (hanya untuk menjalankan dari source);
- WebView2 Runtime (sudah bawaan Windows 11);
- proyek Supabase yang sudah disiapkan (lihat [Setup Database](#setup-database)).

### 1. Konfigurasi

Salin file konfigurasi contoh, lalu isi dengan kredensial dari dashboard Supabase (**Project Settings → API**):

```powershell
Copy-Item appsettings.example.json appsettings.json
```

```json
{
  "Supabase": { "Url": "https://<project-ref>.supabase.co", "AnonKey": "<anon-key>" },
  "Maps": { "CartoApiKey": "<opsional>" }
}
```

`appsettings.json` sudah masuk `.gitignore`. Jangan commit file ini.

Konfigurasi juga dapat diberikan lewat environment variable, yang selalu diprioritaskan di atas `appsettings.json`:

| Variable | Keterangan |
| --- | --- |
| `LAHANKU_SUPABASE_URL` | URL proyek Supabase |
| `LAHANKU_SUPABASE_ANON_KEY` | Anon (public) key Supabase |
| `LAHANKU_CARTO_API_KEY` | Opsional, key basemap CARTO |

### 2. Jalankan

```powershell
dotnet run
```

### Pintasan keyboard

| Tombol | Fungsi |
| --- | --- |
| `Ctrl+N` | Tambah lahan (Ringkasan) |
| `F5` | Muat ulang data |
| `Esc` | Tutup modal atau kembali ke Ringkasan |
| `1` / `2` / `3` | Periode 7 hari, 30 hari, atau semua (Dampak) |
| `Enter` | Kirim formulir |

## Membuat file .exe

```powershell
dotnet publish -c Release -r win-x64 -o publish
```

Hasilnya adalah satu file `publish/Lahanku.exe` yang sudah membawa .NET dan konfigurasi dari `appsettings.json`, sehingga dapat langsung dijalankan di komputer lain tanpa file tambahan.

Untuk memakai kredensial lain tanpa build ulang, letakkan `appsettings.json` baru di sebelah `Lahanku.exe`; file tersebut akan diprioritaskan di atas konfigurasi bawaan. Karena kredensial ikut tersimpan di dalam `.exe`, bagikan file ini hanya kepada pihak yang dipercaya.

## Setup Database

Login memakai **Supabase Auth**. Password di-hash oleh Supabase, dan setiap pengguna hanya dapat mengakses lahannya sendiri melalui Row Level Security.

1. **Matikan konfirmasi email:** Supabase Dashboard → *Authentication* → *Sign In / Providers* → *Email* → nonaktifkan **Confirm email** → *Save*.
   Username dipetakan ke email sintetis `username@lahanku.local`, jadi tidak ada email sungguhan yang dikirim.
2. Jalankan skrip migrasi berikut secara berurutan di *SQL Editor*:
   - [`001_supabase_auth_rls.sql`](database/migrations/001_supabase_auth_rls.sql): tabel profil, trigger pendaftaran, dan RLS;
   - [`003_lahan_kelembapan_ideal.sql`](database/migrations/003_lahan_kelembapan_ideal.sql): kelembapan ideal per lahan;
   - [`004_rekomendasi_harian.sql`](database/migrations/004_rekomendasi_harian.sql): catatan rekomendasi harian dan katalog tanaman pekarangan.
3. (Hanya untuk data lama) Setelah pemilik akun lama mendaftar ulang dengan username yang sama, jalankan [`002_migrate_legacy_users.sql`](database/migrations/002_migrate_legacy_users.sql) langkah A–B, periksa hasilnya, lalu langkah C.

Aturan akun: username 3–30 karakter (huruf, angka, `.`, `_`), password minimal 6 karakter.

## Pemeriksaan kualitas

```powershell
dotnet build
dotnet test Lahanku.Tests
```

Unit test mencakup logika rekomendasi, prakiraan hujan, evaluasi penyiraman, perhitungan dampak, kurva grafik, validasi profil, dan simulasi akun demo. Pengujian ini tidak memerlukan koneksi ke Supabase.

## Tim Trio

| Nama | NIM | Peran |
| --- | --- | --- |
| Aulia Nur Fajri Tri Anggoro | 24/535054/TK/59327 | Ketua |
| Zahira Anindya Putri | 24/543202/TK/60347 | Anggota |
| Josiah Hermes | 24/543958/TK/60463 | Anggota |
