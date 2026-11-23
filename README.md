# LahanKu

*LahanKu* adalah aplikasi desktop cerdas berbasis C# WPF dan PostgreSQL yang dirancang untuk mendukung Climate-Smart Agriculture (SDG 13). 
Aplikasi ini membantu penggiat urban farming dan kebun skala rumah tangga beradaptasi dengan anomali iklim dengan cara memadukan manajemen 
lahan (CRUD) dan data satelit cuaca secara real-time. 

**tim trio**
* **Ketua Kelompok:** Aulia Nur Fajri Tri Anggoro - 24/535054/TK/59327
* **Anggota 1:** Zahira Anindya Putri - 24/543202/TK/60347
* **Anggota 2:** Josiah Hermes - 24/543958/TK/60463
## Cara Menjalankan

**Prasyarat:** .NET 8 SDK dan WebView2 Runtime (sudah bawaan Windows 11).

1. Salin file konfigurasi contoh:
   ```powershell
   Copy-Item appsettings.example.json appsettings.json
   ```
2. Isi `appsettings.json` dengan kredensial dari dashboard Supabase (**Project Settings → API**):
   ```json
   {
     "Supabase": { "Url": "https://<project-ref>.supabase.co", "AnonKey": "<anon-key>" },
     "Maps": { "CartoApiKey": "<opsional>" }
   }
   ```
   `appsettings.json` sudah masuk `.gitignore` — **jangan di-commit**.
3. Jalankan aplikasi:
   ```powershell
   dotnet run
   ```

### Alternatif: Environment Variable
Environment variable selalu diprioritaskan di atas `appsettings.json`:

| Variable | Keterangan |
|---|---|
| `LAHANKU_SUPABASE_URL` | URL project Supabase |
| `LAHANKU_SUPABASE_ANON_KEY` | Anon (public) key Supabase |
| `LAHANKU_CARTO_API_KEY` | Opsional, key basemap CARTO |
