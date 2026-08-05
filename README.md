# AuthApp

Aplikasi web autentikasi (Register, Login, Welcome, Lock Screen) yang terdiri dari dua
project terpisah — **backend REST API** (ASP.NET Core Web API) dan **frontend SPA**
(Blazor WebAssembly) — dibangun sebagai submission untuk **Test Programmer C# Developer**,
PT Dinamis Infomadya.

Alur aplikasi (register → login → welcome, lockout setelah 4 kali gagal login berturut-turut)
mengikuti flowchart soal test, dengan beberapa penambahan di luar requirement literal (JWT
auth, session persistence, hardening keamanan).

## Daftar Isi

- [Arsitektur](#arsitektur)
- [Tech Stack](#tech-stack)
- [Struktur Project](#struktur-project)
- [Prasyarat](#prasyarat)
- [Menjalankan Aplikasi](#menjalankan-aplikasi)
- [Konfigurasi](#konfigurasi)
- [Menjalankan Test](#menjalankan-test)
- [Fitur Utama](#fitur-utama)
- [Keamanan](#keamanan)
- [Dokumentasi Tambahan](#dokumentasi-tambahan)

## Arsitektur

```
┌─────────────────────────┐        HTTPS/HTTP (JSON)        ┌──────────────────────────┐
│   AuthApp.Client         │ ───────────────────────────────▶│   AuthApp.Api             │
│   Blazor WebAssembly     │                                  │   ASP.NET Core Web API    │
│   (SPA, port 5269)       │ ◀─────────────────────────────── │   (port 5227)             │
└─────────────────────────┘   HttpOnly cookie (JWT) + JSON   └──────────────────────────┘
                                                                          │
                                                                          ▼
                                                              In-memory user store
                                                          (reset setiap restart proses)
```

- **Backend** dan **frontend** berjalan sebagai dua proses/origin terpisah (beda port),
  dihubungkan lewat REST API + cookie lintas origin (CORS dengan `AllowCredentials`).
- **Autentikasi**: JWT (HMAC-SHA256) diterbitkan saat login, disimpan di **HttpOnly cookie**
  (tidak bisa dibaca JavaScript — aman dari pencurian token lewat XSS) sekaligus dikembalikan
  di body response untuk keperluan pengujian manual lewat Swagger.
- **Data store**: in-memory (`ConcurrentDictionary`), sesuai kebutuhan test — data user
  hilang setiap proses backend di-restart.

## Tech Stack

| Layer | Teknologi |
|---|---|
| Backend | ASP.NET Core Web API (.NET 8), FluentValidation, JWT Bearer Authentication, `Microsoft.AspNetCore.RateLimiting` |
| Frontend | Blazor WebAssembly (.NET 8), Bootstrap 5, SweetAlert2 (vendored lokal) |
| Hashing | PBKDF2-SHA256 built-in .NET (600.000 iterasi, sesuai rekomendasi OWASP 2023) |
| Testing | xUnit, Moq, `Microsoft.AspNetCore.Mvc.Testing` (integration test lewat `WebApplicationFactory`) |
| Dokumentasi API | Swagger / OpenAPI (Swashbuckle), aktif di environment Development |

## Struktur Project

```
AuthApp.sln
├── AuthApp.Api/            Backend — ASP.NET Core Web API      → lihat AuthApp.Api/README.md
├── AuthApp.Api.Tests/      Unit & integration test backend (xUnit)
└── AuthApp.Client/         Frontend — Blazor WebAssembly SPA   → lihat AuthApp.Client/README.md
```

## Prasyarat

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (`dotnet --version` ≥ 8.0)
- Browser modern (Chrome/Edge/Firefox) untuk menjalankan frontend Blazor WebAssembly

## Menjalankan Aplikasi

### 1. Clone & restore

```bash
git clone <repository-url>
cd "PT Dinamis Infomadya"
dotnet restore
```

### 2. Siapkan JWT Signing Key (wajib, sekali saja)

Signing key JWT **sengaja tidak disimpan di `appsettings.json`** (tidak pernah masuk source
control) — harus diisi lewat [.NET User Secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets):

```bash
cd AuthApp.Api
dotnet user-secrets set "Jwt:SigningKey" "ganti-dengan-string-acak-minimal-32-karakter"
cd ..
```

Tanpa langkah ini, backend akan **fail-fast saat startup** dengan pesan error yang jelas
(bukan error samar di runtime) — lihat detail di [`AuthApp.Api/README.md`](./AuthApp.Api/README.md#konfigurasi).

### 3. Jalankan backend

```bash
cd AuthApp.Api
dotnet run
```

Backend akan tersedia di `http://localhost:5227`, dengan Swagger UI otomatis terbuka di
`http://localhost:5227/swagger`.

### 4. Jalankan frontend (di terminal terpisah)

```bash
cd AuthApp.Client
dotnet run
```

Frontend akan tersedia di `http://localhost:5269` dan otomatis terbuka di browser.

> Kedua project harus berjalan **bersamaan** — frontend memanggil backend lewat
> `http://localhost:5227` (lihat [konfigurasi `ApiBaseUrl`](./AuthApp.Client/README.md#konfigurasi)).

## Konfigurasi

Ringkasan konfigurasi penting (detail lengkap ada di README masing-masing project):

| Project | File | Key | Keterangan |
|---|---|---|---|
| Backend | User Secrets | `Jwt:SigningKey` | **Wajib diisi manual**, minimal 32 karakter (256-bit) |
| Backend | `appsettings.Development.json` | `Cors:AllowedOrigins` | Origin frontend yang diizinkan (sudah diisi untuk dev) |
| Frontend | `wwwroot/appsettings.json` | `ApiBaseUrl` | Alamat backend yang dipanggil frontend |

## Menjalankan Test

```bash
dotnet test
```

Backend memiliki **79 unit & integration test** (xUnit + Moq), mencakup validasi, password
hashing, JWT generation/validation, rate limiting, revocation token, dan alur end-to-end
lewat `WebApplicationFactory` (bukan cuma memanggil method controller langsung). Frontend
diverifikasi manual lewat browser untuk setiap fitur (belum ada automated test frontend).

## Fitur Utama

Mengikuti flowchart register/login pada soal test, ditambah beberapa penambahan yang
merupakan keputusan sadar (bukan scope creep tanpa alasan):

- **Register** — validasi username (3-32 karakter, alfanumerik/underscore/hyphen) &
  password (8-128 karakter, ditolak jika termasuk password umum/bocor), cek duplikasi
  username secara atomik.
- **Login** — validasi kredensial, hitung percobaan gagal di sisi client (`LoginFailed`,
  sesuai flowchart), redirect ke Lock Screen setelah **4 kali gagal berturut-turut**.
- **Welcome** — halaman tujuan setelah login sukses, hanya bisa diakses saat sudah login.
- **Lock Screen** — hanya bisa diakses saat benar-benar terkunci (bukan lewat akses URL
  langsung); satu-satunya jalan keluar adalah restart aplikasi (reload browser).
- **Sesi bertahan lintas reload** — lewat HttpOnly cookie + endpoint `/api/auth/me`, tanpa
  menyimpan token di `localStorage`/`sessionStorage`.
- **JWT issuance & revocation** — token diterbitkan saat login, benar-benar dicabut
  (server-side denylist) saat logout — bukan cuma dihapus di sisi client.

## Keamanan

Project ini melalui satu putaran **security review menyeluruh** yang menemukan dan
memperbaiki 8 celah, di antaranya: HTML injection lewat username, tidak ada rate limiting
sisi server, timing side-channel pada login, JWT yang tidak bisa dicabut, dan tidak adanya
pengecekan password umum. Ringkasan mitigasi yang aktif saat ini:

| Aspek | Mitigasi |
|---|---|
| Password | PBKDF2-SHA256, 600k iterasi, salt acak, dibandingkan dengan `FixedTimeEquals` |
| Brute-force | Rate limiting 10 request/menit per IP pada `/register` & `/login` |
| Timing side-channel | Login selalu menjalankan cost hashing yang sama, ada/tidaknya user |
| XSS via input | Charset username dibatasi; notifikasi frontend dirender sebagai plain text |
| Session hijack | Cookie `HttpOnly` + `SameSite=Lax`; JWT dicabut (server-side) saat logout |
| Password lemah | Ditolak jika masuk daftar password umum/bocor (offline, tanpa API eksternal) |
| Response header | `X-Content-Type-Options: nosniff`, HSTS (non-Development) |

Beberapa trade-off diterima secara sadar dan eksplisit (mis. pesan error Login yang
dibedakan per kondisi berisiko enumerasi username, demi UX yang lebih jelas) — bukan celah
yang terlewat.

## Dokumentasi Tambahan

- [`AuthApp.Api/README.md`](./AuthApp.Api/README.md) — dokumentasi backend: API reference,
  aturan validasi, konfigurasi, testing.
- [`AuthApp.Client/README.md`](./AuthApp.Client/README.md) — dokumentasi frontend: halaman
  & routing, service, konfigurasi.
