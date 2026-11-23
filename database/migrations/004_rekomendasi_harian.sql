-- Rekomendasi harian + fokus ke kebun pekarangan.
-- Jalankan di SQL Editor setelah 003.

-- 1. katalog: buang tanaman sawah, ladang luas, dan perkebunan
update public.lahan
set id_tanaman = null
where id_tanaman in (1, 2, 3, 4, 5, 6, 7, 10, 11, 13, 18, 87, 88, 89, 90, 91, 92, 93, 94, 95);

delete from public.tanaman
where id_tanaman in (1, 2, 3, 4, 5, 6, 7, 10, 11, 13, 18, 87, 88, 89, 90, 91, 92, 93, 94, 95);

-- 2. luas maksimal 1 ha; NOT VALID biar lahan lama nggak bikin gagal
alter table public.lahan drop constraint if exists lahan_luas_check;
alter table public.lahan add constraint lahan_luas_check
    check (luas_hektar > 0 and luas_hektar <= 1) not valid;

-- 3. evaluasi_irigasi jadi catatan rekomendasi harian (1 baris per lahan per hari)
alter table public.evaluasi_irigasi add column if not exists tanggal date;
alter table public.evaluasi_irigasi add column if not exists keputusan text;
alter table public.evaluasi_irigasi add column if not exists volume_standar double precision;
alter table public.evaluasi_irigasi add column if not exists hujan_prakiraan_mm double precision;
alter table public.evaluasi_irigasi add column if not exists peluang_hujan double precision;

-- tabelnya masih kosong, jadi aman langsung not null
alter table public.evaluasi_irigasi alter column tanggal set not null;

alter table public.evaluasi_irigasi drop constraint if exists evaluasi_keputusan_check;
alter table public.evaluasi_irigasi add constraint evaluasi_keputusan_check
    check (keputusan in ('siram', 'tunda', 'siram_ekstra'));

alter table public.evaluasi_irigasi drop constraint if exists evaluasi_lahan_tanggal_unique;
alter table public.evaluasi_irigasi add constraint evaluasi_lahan_tanggal_unique unique (id_lahan, tanggal);

-- cek
select (select count(*) from public.tanaman) as jumlah_tanaman,
       (select count(*) from information_schema.columns
        where table_schema = 'public' and table_name = 'evaluasi_irigasi'
          and column_name in ('tanggal', 'keputusan', 'volume_standar', 'hujan_prakiraan_mm', 'peluang_hujan')) as kolom_baru;
