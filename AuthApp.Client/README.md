# AuthApp.Client

Frontend SPA untuk AuthApp — Blazor WebAssembly (.NET 8) yang menyediakan halaman Register,
Login, Welcome, dan Lock Screen, mengikuti sitemap & flowchart register/login pada soal test.

Untuk overview keseluruhan solusi (termasuk backend), lihat [README di root](../README.md).
Untuk detail API yang dikonsumsi frontend ini, lihat [`AuthApp.Api/README.md`](../AuthApp.Api/README.md).

## Daftar Isi

- [Tech Stack](#tech-stack)
- [Struktur Folder](#struktur-folder)
- [Konfigurasi](#konfigurasi)
- [Menjalankan](#menjalankan)
- [Halaman & Routing](#halaman--routing)
- [Service Utama](#service-utama)
- [Sesi & Autentikasi](#sesi--autentikasi)
- [Validasi Form](#validasi-form)

## Tech Stack

| Komponen | Detail |
|---|---|
| Framework | Blazor WebAssembly (Standalone), .NET 8 |
| Styling | Bootstrap 5 (bawaan template) + CSS kustom (`wwwroot/css/auth.css`) |
| Notifikasi | SweetAlert2 — **di-vendor lokal** (`wwwroot/js/sweetalert2.min.js`), bukan CDN, supaya tidak bergantung koneksi internet saat demo/dinilai |
| HTTP Client | `IHttpClientFactory` (`AddHttpClient<AuthApiService>`) dengan `DelegatingHandler` kustom untuk menyertakan cookie lintas origin |

## Struktur Folder

```
AuthApp.Client/
├── Layout/               MainLayout — layout single-column (tanpa sidebar; sesuai app auth-only)
├── Models/               DTO client (terpisah dari DTO server — lihat catatan di bawah)
├── Pages/                Login.razor (/), Register.razor, Welcome.razor, LockScreen.razor
├── Services/             AuthApiService, AuthSession, LoginAttemptTracker, NotificationService, dll.
├── wwwroot/
│   ├── css/auth.css      Styling halaman auth (card, ikon, footer)
│   ├── js/               notifications.js (wrapper SweetAlert2) + sweetalert2.min.js (vendored)
│   └── appsettings.json  ApiBaseUrl
├── App.razor             Root component — memulihkan sesi dari cookie sebelum routing dimulai
└── Program.cs            Composition root: registrasi HttpClient & service
```

> **Catatan desain**: `Models/` di project ini adalah salinan independen dari DTO backend
> (bukan referencing project `AuthApp.Api`), supaya dependency server-only (FluentValidation,
> JwtBearer, dll.) tidak ikut masuk ke bundle WebAssembly yang dikirim ke browser.

## Konfigurasi

`wwwroot/appsettings.json`:

```json
{ "ApiBaseUrl": "http://localhost:5227/" }
```

Ubah nilai ini kalau backend berjalan di alamat/port lain.

## Menjalankan

```bash
dotnet run
```

Tersedia di `http://localhost:5269`, otomatis terbuka di browser. **Backend harus sudah
berjalan** (lihat [`AuthApp.Api/README.md`](../AuthApp.Api/README.md)) sebelum mencoba
Register/Login — halaman akan menampilkan pesan error yang jelas ("Unable to reach the
server...") kalau backend belum aktif, bukan gagal diam-diam.

## Halaman & Routing

| Route | Halaman | Bisa diakses kapan | Redirect kalau tidak memenuhi syarat |
|---|---|---|---|
| `/` | Login | Hanya saat **belum login** & belum terkunci | → `/welcome` (sudah login) atau `/lockscreen` (terkunci) |
| `/register` | Register | Hanya saat **belum login** | → `/welcome` |
| `/welcome` | Welcome (halaman sukses) | Hanya saat **sudah login** | → `/` |
| `/lockscreen` | Lock Screen | Hanya saat **benar-benar terkunci** (`LoginFailed > 3`) | → `/` |

Semua guard di atas diterapkan lewat `NavigationManagerExtensions.EnsureOr(...)` di
`OnInitialized`/`OnInitializedAsync` masing-masing halaman — termasuk menutup celah akses
langsung lewat URL (mis. mengetik `/lockscreen` tanpa pernah gagal login sungguhan).

## Service Utama

| Service | Lifetime | Tanggung jawab |
|---|---|---|
| `AuthApiService` | Scoped (via `AddHttpClient`) | Satu-satunya pemanggil HTTP ke backend (register/login/logout/getCurrentUser); halaman tidak pernah pegang `HttpClient` langsung |
| `AuthSession` | Scoped | State login in-memory (`Username`, `IsAuthenticated`) untuk sesi app saat ini |
| `LoginAttemptTracker` | Scoped | Counter `LoginFailed`, bertahan lintas navigasi dalam app (tidak reset saat pindah ke Register lalu kembali) |
| `NotificationService` | Scoped | Wrapper aman untuk toast SweetAlert2 — kegagalan JS interop tidak pernah menjatuhkan pemanggilnya |
| `CredentialsIncludedHandler` | Transient | `DelegatingHandler` yang memastikan cookie ikut terkirim di setiap request lintas origin |

## Sesi & Autentikasi

Token JWT **tidak pernah disimpan** di `localStorage`/`sessionStorage`, dan sejak
penambahan HttpOnly cookie, client bahkan tidak pernah memegang token mentah sama sekali.

Alur pemulihan sesi saat app dimuat/di-reload:

1. `App.razor` memanggil `GET /api/auth/me` sekali sebelum `<Router>` dirender (menampilkan
   "Loading..." sebentar).
2. Backend membaca cookie `AuthToken` (HttpOnly, otomatis terkirim browser) dan
   mengonfirmasi identitas user.
3. Kalau valid, `AuthSession` diisi ulang **sebelum** halaman mana pun sempat menjalankan
   guard-nya — sehingga reload di `/welcome` tetap menampilkan Welcome, bukan Login.

Logout memanggil `POST /api/auth/logout`, yang menghapus cookie **dan** mencabut token
secara server-side — bukan cuma membersihkan state di browser.

## Validasi Form

Validasi client-side (via `DataAnnotationsValidator`) mencerminkan aturan backend secara
sengaja disamakan (lihat [`AuthApp.Api/README.md#aturan-validasi`](../AuthApp.Api/README.md#aturan-validasi)),
supaya kesalahan input umum (username terlalu pendek, password kurang dari 8 karakter, dst.)
langsung terlihat di bawah field terkait **tanpa** memanggil API — tapi validasi final tetap
di server; validasi client murni untuk UX, bukan satu-satunya lapisan pertahanan.
