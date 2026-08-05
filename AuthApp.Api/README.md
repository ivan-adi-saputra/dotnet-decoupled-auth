# AuthApp.Api

Backend REST API untuk AuthApp — ASP.NET Core Web API (.NET 8) yang menangani registrasi,
login, sesi (lewat JWT + HttpOnly cookie), dan logout dengan revocation token sisi server.

Untuk overview keseluruhan solusi (termasuk frontend), lihat [README di root](../README.md).

## Daftar Isi

- [Tech Stack](#tech-stack)
- [Struktur Folder](#struktur-folder)
- [Konfigurasi](#konfigurasi)
- [Menjalankan](#menjalankan)
- [API Reference](#api-reference)
- [Aturan Validasi](#aturan-validasi)
- [Keamanan](#keamanan)
- [Testing](#testing)

## Tech Stack

| Komponen | Detail |
|---|---|
| Framework | ASP.NET Core Web API, .NET 8 |
| Validasi | FluentValidation (`FluentValidation.DependencyInjectionExtensions`) |
| Autentikasi | JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`), HMAC-SHA256 |
| Password Hashing | PBKDF2-SHA256 built-in .NET (`Rfc2898DeriveBytes`), tanpa NuGet tambahan |
| Rate Limiting | `Microsoft.AspNetCore.RateLimiting` (built-in .NET 8) |
| Dokumentasi API | Swagger/OpenAPI (`Swashbuckle.AspNetCore`), aktif di Development |
| Data Store | In-memory (`ConcurrentDictionary`) — tidak persisten antar restart |

## Struktur Folder

```
AuthApp.Api/
├── Authentication/       Konstanta terkait cookie auth (nama cookie)
├── Controllers/          AuthController — satu-satunya controller (register/login/logout/me)
├── ErrorHandling/        GlobalExceptionHandler — penanganan exception tak terduga (IExceptionHandler)
├── Filters/              ValidationFilter — dispatcher otomatis untuk FluentValidation
├── Models/               Entity (User) & Dtos/ (request/response, semua bertipe record)
├── RateLimiting/         Konstanta nama policy rate limiter
├── Services/             Interface + implementasi (password hasher, JWT generator/validator,
│                         user store, token revocation store)
├── Validation/           Validator FluentValidation + daftar password umum
├── Program.cs            Composition root: DI, middleware pipeline, konfigurasi JWT/CORS/rate limit
├── appsettings.json      Konfigurasi default (SigningKey sengaja kosong — lihat Konfigurasi)
└── appsettings.Development.json
```

## Konfigurasi

### JWT Signing Key (wajib)

`Jwt:SigningKey` **sengaja dikosongkan** di `appsettings.json` dan tidak pernah disimpan di
source control. Backend akan **fail-fast saat startup** (melempar `InvalidOperationException`
dengan pesan jelas) kalau key ini kosong atau kurang dari 32 byte (256-bit) — di **semua**
environment, bukan cuma Production.

Isi lewat [.NET User Secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets)
(sekali saja per environment development):

```bash
dotnet user-secrets set "Jwt:SigningKey" "ganti-dengan-string-acak-minimal-32-karakter"
```

### Konfigurasi lain

| Key | Default (Development) | Keterangan |
|---|---|---|
| `Jwt:Issuer` | `AuthApp` | Issuer yang divalidasi pada setiap token |
| `Jwt:Audience` | `AuthApp.Client` | Audience yang divalidasi pada setiap token |
| `Jwt:ExpiryMinutes` | `60` | Umur token JWT (menit); umur cookie disamakan dengan ini |
| `Cors:AllowedOrigins` | `https://localhost:7197`, `http://localhost:5269` | Origin frontend yang diizinkan. **Fail-fast** kalau kosong di luar Development |

## Menjalankan

```bash
dotnet run
```

- HTTP: `http://localhost:5227`
- Swagger UI: `http://localhost:5227/swagger` (otomatis terbuka, hanya aktif di Development)

### Menguji lewat Swagger

Endpoint `/api/auth/me` dilindungi `[Authorize]`. Untuk mengujinya manual lewat Swagger:
1. Panggil `POST /api/auth/login`, salin nilai `token` dari response.
2. Klik tombol **Authorize** di Swagger UI, tempel token (tanpa prefix `Bearer `).
3. Panggil `GET /api/auth/me`.

## API Reference

Semua endpoint berada di bawah prefix `/api/auth`. Response error non-2xx selalu berbentuk
`{ "success": false, "message": "..." }` (atau `AuthResponse`/`LoginResponse`).

### `POST /api/auth/register`

Registrasi user baru. **Rate limited** (lihat [Keamanan](#keamanan)).

**Request body**
```json
{ "username": "budi123", "password": "SecurePass1" }
```

**Response**

| Status | Kondisi | Body |
|---|---|---|
| `200 OK` | Berhasil | `{ "success": true, "message": "Registration successful. You can now log in." }` |
| `400 Bad Request` | Validasi gagal (lihat [Aturan Validasi](#aturan-validasi)) | `{ "success": false, "message": "<pesan spesifik>" }` |
| `409 Conflict` | Username sudah terdaftar | `{ "success": false, "message": "Username 'x' is already taken." }` |
| `429 Too Many Requests` | Melebihi rate limit | — |

### `POST /api/auth/login`

Login. Saat sukses, JWT diterbitkan dan di-set sebagai **HttpOnly cookie** (`AuthToken`)
sekaligus dikembalikan di body response. **Rate limited**, berbagi bucket yang sama dengan
`register` per IP.

**Request body**
```json
{ "username": "budi123", "password": "SecurePass1" }
```

**Response**

| Status | Kondisi | Body |
|---|---|---|
| `200 OK` | Berhasil | `{ "success": true, "message": "Login successful. Welcome back!", "token": "<jwt>" }` |
| `400 Bad Request` | Validasi gagal | `{ "success": false, "message": "<pesan spesifik>" }` |
| `401 Unauthorized` | Username tidak ditemukan | `{ "success": false, "message": "Username not found.", "token": null }` |
| `401 Unauthorized` | Password salah | `{ "success": false, "message": "Incorrect password.", "token": null }` |
| `429 Too Many Requests` | Melebihi rate limit | — |

> Catatan desain: pesan error dibedakan sesuai kondisi sebenarnya (bukan pesan generik) atas
> permintaan eksplisit — konsekuensi (username enumeration lewat isi pesan) diterima secara
> sadar sebagai trade-off demi UX yang lebih jelas. Timing respons **tidak** membocorkan hal
> yang sama (lihat [Keamanan](#keamanan)) — keduanya diperlakukan sebagai dua celah yang
> berbeda.

### `POST /api/auth/logout`

Menghapus cookie `AuthToken` **dan** mencabut token secara server-side (denylist berbasis
`jti`), sehingga token yang sudah terlanjur bocor sebelum logout tidak bisa dipakai lagi.
Tidak memerlukan autentikasi — selalu mengembalikan `200 OK`, termasuk saat dipanggil tanpa
sesi aktif sama sekali.

**Response**: `200 OK`, tanpa body.

### `GET /api/auth/me`

Endpoint terproteksi (`[Authorize]`) — mengembalikan username pemilik token yang valid.
Menerima token lewat header `Authorization: Bearer <token>` **atau** cookie `AuthToken`
secara otomatis (browser mengirim cookie tanpa perlu kode tambahan di client).

| Status | Kondisi | Body |
|---|---|---|
| `200 OK` | Token valid | `{ "username": "budi123" }` |
| `401 Unauthorized` | Token tidak ada/tidak valid/kedaluwarsa/sudah dicabut | — |

## Aturan Validasi

| Field | Aturan | Berlaku di |
|---|---|---|
| Username | Wajib diisi | Register & Login |
| Username | 3–32 karakter | Register & Login |
| Username | Hanya huruf, angka, underscore (`_`), hyphen (`-`) | Register & Login |
| Password | Wajib diisi | Register & Login |
| Password | 8–128 karakter | Register & Login |
| Password | Tidak boleh termasuk password umum/bocor (`Validation/CommonPasswords.cs`) | **Register saja** |

Password umum dicek **hanya saat Register** (saat password *dibuat*), bukan saat Login
(memvalidasi ulang kredensial lama yang sudah ada tidak sesuai rekomendasi NIST 800-63B).

## Keamanan

Ringkasan mitigasi aktif — hasil dari satu putaran security review menyeluruh terhadap
backend & frontend:

- **Password hashing**: PBKDF2-SHA256, 600.000 iterasi (OWASP 2023), salt acak per user,
  perbandingan hash pakai `CryptographicOperations.FixedTimeEquals`.
- **Rate limiting**: 10 request/menit per IP pada `register` & `login` (berbagi satu
  bucket), lewat `Microsoft.AspNetCore.RateLimiting`.
- **Timing-safe login**: `Verify()` selalu dijalankan (terhadap hash dummy kalau user tidak
  ditemukan), supaya waktu respons tidak membocorkan keberadaan username.
- **JWT revocation**: logout mencabut `jti` token lewat denylist in-memory
  (`ITokenRevocationStore`), divalidasi dulu terhadap signing key server sebelum dicabut
  (mencegah pemalsuan token untuk memaksa logout sesi orang lain).
- **Cookie**: `HttpOnly` (tidak bisa dibaca JavaScript), `SameSite=Lax`, `Secure` otomatis
  aktif di luar Development.
- **Response header**: `X-Content-Type-Options: nosniff` di semua response; `Strict-Transport-Security`
  (HSTS) di luar Development.
- **Fail-fast config**: startup gagal dengan pesan jelas kalau `Jwt:*`/`Cors:AllowedOrigins`
  tidak dikonfigurasi dengan benar — bukan error runtime yang membingungkan.

## Testing

```bash
dotnet test
```

79 test (unit + integration), mencakup:

| Kategori | Contoh cakupan |
|---|---|
| Validator (`Validation/`) | Aturan panjang, charset, password umum, kombinasi valid/invalid |
| Service (`Services/`) | Password hashing (termasuk hash korup), JWT generation & expiry |
| Controller (unit, `Controllers/`) | Logika Register/Login/Me lewat mock, tanpa HTTP pipeline nyata |
| Integration (`Integration/`, via `WebApplicationFactory`) | `/me` benar-benar ditegakkan `[Authorize]` lewat pipeline HTTP asli (bukan cuma unit test yang melewati middleware), autentikasi lewat cookie, rate limiting, revocation token, response header |

Integration test sengaja memakai `WebApplicationFactory<Program>` (bukan cuma memanggil
method controller langsung) untuk kasus-kasus yang perilakunya ditentukan oleh middleware
(`[Authorize]`, rate limiter, cookie) — supaya lolos test benar-benar berarti "jalan lewat
HTTP sungguhan", bukan cuma "logic di dalam method benar".
