-- Migrasi ke Supabase Auth + RLS.
-- Jalankan di SQL Editor sebelum pakai versi app yang login via Supabase Auth.
-- Aman dijalankan ulang, tidak menghapus data.

-- profil user, 1:1 dengan auth.users
create table if not exists public.profiles (
    id          uuid primary key references auth.users (id) on delete cascade,
    username    text not null unique,
    full_name   text,
    created_at  timestamptz not null default now()
);

-- isi profiles otomatis waktu ada user baru daftar
create or replace function public.handle_new_user()
returns trigger
language plpgsql
security definer
set search_path = ''
as $$
begin
    insert into public.profiles (id, username, full_name)
    values (
        new.id,
        coalesce(new.raw_user_meta_data ->> 'username', split_part(new.email, '@', 1)),
        coalesce(new.raw_user_meta_data ->> 'full_name', new.raw_user_meta_data ->> 'username')
    )
    on conflict (id) do nothing;
    return new;
end;
$$;

drop trigger if exists on_auth_user_created on auth.users;
create trigger on_auth_user_created
    after insert on auth.users
    for each row execute function public.handle_new_user();

-- pemilik lahan sekarang pakai uuid dari auth
alter table public.lahan
    add column if not exists owner_id uuid
        references auth.users (id) on delete cascade
        default auth.uid();

create index if not exists lahan_owner_id_idx on public.lahan (owner_id);

alter table public.lahan alter column id_user drop not null;

-- hapus policy lama biar nggak ada yang ketinggalan terbuka
do $$
declare
    r record;
begin
    for r in
        select policyname, tablename
        from pg_policies
        where schemaname = 'public'
          and tablename in ('users', 'profiles', 'lahan', 'tanaman', 'penyiraman', 'cuaca', 'evaluasi_irigasi')
    loop
        execute format('drop policy %I on public.%I', r.policyname, r.tablename);
    end loop;
end;
$$;

-- tabel users lama sengaja nggak dikasih policy, jadi terkunci
alter table public.users            enable row level security;
alter table public.profiles         enable row level security;
alter table public.lahan            enable row level security;
alter table public.tanaman          enable row level security;
alter table public.penyiraman       enable row level security;
alter table public.cuaca            enable row level security;
alter table public.evaluasi_irigasi enable row level security;

create policy profiles_select_own on public.profiles
    for select to authenticated using (id = auth.uid());
create policy profiles_update_own on public.profiles
    for update to authenticated using (id = auth.uid()) with check (id = auth.uid());

create policy tanaman_read_all on public.tanaman
    for select to anon, authenticated using (true);

create policy lahan_owner_all on public.lahan
    for all to authenticated
    using (owner_id = auth.uid())
    with check (owner_id = auth.uid());

create policy penyiraman_owner_all on public.penyiraman
    for all to authenticated
    using (exists (select 1 from public.lahan l where l.id_lahan = penyiraman.id_lahan and l.owner_id = auth.uid()))
    with check (exists (select 1 from public.lahan l where l.id_lahan = penyiraman.id_lahan and l.owner_id = auth.uid()));

create policy cuaca_owner_all on public.cuaca
    for all to authenticated
    using (exists (select 1 from public.lahan l where l.id_lahan = cuaca.id_lahan and l.owner_id = auth.uid()))
    with check (exists (select 1 from public.lahan l where l.id_lahan = cuaca.id_lahan and l.owner_id = auth.uid()));

create policy evaluasi_owner_all on public.evaluasi_irigasi
    for all to authenticated
    using (exists (select 1 from public.lahan l where l.id_lahan = evaluasi_irigasi.id_lahan and l.owner_id = auth.uid()))
    with check (exists (select 1 from public.lahan l where l.id_lahan = evaluasi_irigasi.id_lahan and l.owner_id = auth.uid()));

-- cek: semua harus rowsecurity = true
select tablename, rowsecurity
from pg_tables
where schemaname = 'public'
  and tablename in ('users', 'profiles', 'lahan', 'tanaman', 'penyiraman', 'cuaca', 'evaluasi_irigasi')
order by tablename;
