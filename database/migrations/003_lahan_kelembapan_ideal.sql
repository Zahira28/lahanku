-- Kelembapan udara ideal per lahan (%).
-- Kosong = ikut nilai default dari katalog tanaman (tanaman.kelembapan_ideal_min/max).
-- Ide awal dari Josiah (branch feat/frontend). Aman dijalankan ulang.

alter table public.lahan add column if not exists kelembapan_min double precision;
alter table public.lahan add column if not exists kelembapan_max double precision;

alter table public.lahan drop constraint if exists lahan_kelembapan_range_check;
alter table public.lahan add constraint lahan_kelembapan_range_check check (
    (kelembapan_min is null or kelembapan_min between 0 and 100)
    and (kelembapan_max is null or kelembapan_max between 0 and 100)
    and (kelembapan_min is null or kelembapan_max is null or kelembapan_min <= kelembapan_max)
);

-- cek
select column_name, data_type
from information_schema.columns
where table_schema = 'public' and table_name = 'lahan' and column_name like 'kelembapan%';
