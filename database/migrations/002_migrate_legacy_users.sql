-- Pindahin lahan dari akun lama (tabel users) ke akun Supabase Auth.
-- Jalankan setelah 001, dan setelah pemilik akun lama daftar ulang
-- di app pakai username yang sama.

-- A. pindahin kepemilikan berdasarkan username
update public.lahan l
set owner_id = p.id
from public.users u
join public.profiles p on lower(p.username) = lower(u.username)
where l.id_user = u.id_user
  and l.owner_id is null;

-- B. cek hasilnya, pemilik_baru nggak boleh ada yang kosong
select l.id_lahan, l.nama_lahan, u.username as pemilik_lama, p.username as pemilik_baru
from public.lahan l
left join public.users u on u.id_user = l.id_user
left join public.profiles p on p.id = l.owner_id
order by l.id_lahan;

-- C. kalau B sudah benar, uncomment lalu jalankan buat hapus skema lama
-- alter table public.lahan alter column owner_id set not null;
-- alter table public.lahan drop column id_user;
-- drop table public.users;
