# M3.2-D — Cloudflare Web Deployment Foundation

Tanggal: 26 November 2026 · Basis: commit `79cbae8` + perubahan M3.2 yang belum di-commit · Status: **SELESAI, disetujui founder 26-11-2026**. Production `play.lifetopiaworld.io` = deployment `002b0658` (build original-art `M3-02-20261126-1527`), dan playtest HP nyata oleh founder lulus 10/10.

Ruang lingkup hanya otomasi build/deploy Web, info versi, dan dokumentasi. M3.3 Farming tidak dimulai. `lifetopia-platform`, database, DNS, nameserver, dan custom domain tidak disentuh. Tidak ada layanan berbayar.

Panduan pemakaian: [`Documentation/WEB-DEPLOYMENT.md`](../WEB-DEPLOYMENT.md).

---

## 1. Audit (Phase 1)

### 1.1 Cloudflare Pages (dibaca dengan Wrangler 4.168.0, read-only)

| Item | Hasil |
|---|---|
| Project | `lifetopia-play`, domain `lifetopia-play.pages.dev` + `play.lifetopiaworld.io`, Git provider: No (Direct Upload) |
| Deployment | 1 deployment: `2cbfb971-…`, Environment **Production**, branch **main**, source `79cbae8` |
| Production branch | `main`, terverifikasi tidak langsung karena deployment Production berada di branch `main`. Wrangler tidak menampilkan setting ini (backlog T-35) |
| Login | Wrangler terhubung. Token tersimpan di profil pengguna (`%USERPROFILE%\.wrangler`), bukan di repo |
| `.wrangler/` di root repo | Cache lokal (account id, nama project, folder temp). Belum di-ignore → **ditambahkan ke `.gitignore`** |

### 1.2 Header HTTP production (diukur dengan curl dan `Verify-WebBuild.ps1 -Url`)

| URL | Status | Content-Type | Content-Encoding | Cache-Control |
|---|---|---|---|---|
| `https://play.lifetopiaworld.io/` | 200 | text/html; charset=utf-8 | – | no-cache, no-transform |
| `/Build/M3-02.loader.js` | 200 | application/javascript | – | no-cache, no-transform |
| `/Build/M3-02.wasm.br` | 200 | **application/wasm** | **br** | no-cache, no-transform |
| `/Build/M3-02.framework.js.br` | 200 | application/javascript | br | no-cache, no-transform |
| `/Build/M3-02.data.br` | 200 | application/octet-stream | br | no-cache, no-transform |
| `http://play.lifetopiaworld.io/` | 301 → https | – | – | – |
| `https://lifetopia-play.pages.dev/` | 200 | text/html | – | sama |
| `/nope.txt`, `/_headers` | **200 (index.html)** | text/html | – | – |

Semua halaman juga mengirim `X-Robots-Tag: noindex` dan `X-Content-Type-Options: nosniff`. Content-Length sama dengan ukuran file `.br`, artinya Cloudflare tidak mengompres ulang.

**Temuan:** path yang tidak ada dijawab 200 dengan index.html (fallback SPA karena tidak ada `404.html`). Akibatnya file build yang hilang tidak terlihat sebagai error. Diperbaiki untuk deployment berikutnya: skrip deploy selalu menyertakan `404.html`.

### 1.3 Browser (Playwright, desktop 1280×720, production)

- Game termuat dalam **6,6 s** (lewat internet), 60,3 FPS, frame terburuk 17,6 ms.
- Console: **0 error**, 6 warning engine `WebGL: INVALID_ENUM: getInternalformatParameter` (sama dengan build lokal sejak M1).
- Judul tab: `lifetopia-game`, diperbaiki di template (§3).

### 1.4 Build M3-02 dan `_headers` milik founder

- `Builds/Web/M3-02`: index.html, `_headers` (buatan founder), dan Build/ (loader 26.983 B, framework.js.br 75.452 B, wasm.br 7.271.209 B, data.br 2.443.552 B). Total 9,37 MiB.
- `_headers` founder sudah benar untuk Brotli. Skrip membuat versi yang setara secara otomatis dan menambahkan `nosniff` serta `Referrer-Policy`. File founder **tidak diubah** (MD5 sama sebelum dan sesudah).
- ⚠️ **Build M3-02 berasal dari scene GameplayPrototype (artwork lokal A0/A0-02).** Deployment production ini dibuat manual oleh founder, sehingga artwork tersebut sekarang dapat diakses publik di play.lifetopiaworld.io. Ini terkait gate G-2. **Keputusan founder 26-11-2026:** artwork original boleh dipublikasikan di Web build Cloudflare Pages, dan production ini dipertahankan. PNG/master/backup tetap tidak masuk GitHub.

## 2. Finalisasi M3.2 (Phase 2)

M3.2 sudah terdokumentasi lengkap di `Progress/M3-02-game-time.md` (EditMode 116/116, regresi, Web build 9,37 MiB, verifikasi "Hotbar: 1-5"). M3.2 **belum di-commit**. Usulan commit ada di §8.

## 3. Info versi, judul, dan favicon (Phase 5)

Perubahan template `Assets/WebGLTemplates/LifetopiaResponsive/index.html`:
- `<title>Lifetopia World</title>`. `productName` di Player Settings **tidak** diubah karena merupakan identitas project; perubahannya butuh persetujuan terpisah.
- `<link rel="icon" href="favicon.svg">` + file baru `favicon.svg`: placeholder kotak hijau `#377F55` dengan huruf "L", matahari, dan pasir. Diganti nanti saat logo resmi tersedia.
- `<meta name="generator" content="LifetopiaResponsive">`. Dipakai `Verify-WebBuild.ps1` untuk memastikan build memakai template yang benar.
- Layar loading membaca `version.json` dan menampilkan "Lifetopia World / Development Build · v0.3.2 · M3.2 / Built 2026-11-26".
- Setelah game termuat, label 10 px semi-transparan muncul di **kanan bawah** ("Dev Build v0.3.2 · M3.2 · 2026-11-26") dengan `pointer-events: none`, sehingga klik dan sentuhan tetap sampai ke game.
- Bila `version.json` tidak ada (build lama), tidak ada yang tampil dan tidak ada error.

**Konsep untuk founder:** template Web adalah halaman HTML pembungkus game. Unity mengisi placeholder seperti `{{{ LOADER_FILENAME }}}` saat build. Info versi sengaja ditaruh di HTML, bukan di scene Unity, agar layout gameplay tidak berubah dan versi bisa dibaca sebelum game selesai dimuat.

## 4. Build automation (Phase 3)

| File | Isi |
|---|---|
| `Tools/Build-Web.ps1` | Varian `Local` (GameplayPrototype; command terbukti `-activeBuildProfile Web.asset -build`) dan `Public` (FirstPlayable; `-executeMethod`).<br>Cek: versi Unity = ProjectVersion.txt, Editor tidak membuka project (lockfile `Temp/UnityLockfile` + command line `-projectPath`), lock `.build.lock` (satu build), RAM bebas ≥ 1,5 GB.<br>Build ke `_incoming/<Name>` → cek log (`error CS`, "Build Finished, Result: Success") → tulis `version.json` → verifikasi → baru dipindah ke `Builds/Web/<Name>`. Folder lama dipindah ke `_previous` hanya dengan `-Replace`. Saat build berjalan, RAM dipantau tiap 15 detik |
| `Tools/Verify-WebBuild.ps1` | Verifikasi folder (§WEB-DEPLOYMENT 4.2), deployment live (`-Url`), scan secret repo (`-ScanRepo`) |
| `Tools/Deploy-Web.ps1` | 3 mode + `-DryRun`, gerbang artwork/production, staging dengan daftar izin, `_headers` otomatis, 404.html, cek pasca-deploy, riwayat CSV |
| `Tools/LifetopiaWeb.psm1` | Fungsi bersama |
| `Tools/WebDeploy.config.psd1` | Konfigurasi (tanpa secret) |
| `Tools/Web/404.html` | Halaman 404 |
| `Assets/_Lifetopia/Editor/Lifetopia.Editor.asmdef` + `WebBuildCommand.cs` | Assembly khusus Editor (tidak masuk build pemain). `WebBuildCommand.Build` mengaktifkan profil Web, lalu `BuildPipeline.BuildPlayer` dengan satu scene. Exit code 0/1 |

**Konsep untuk founder:**
- *Batch mode* adalah Unity tanpa jendela yang menjalankan build lalu keluar. Project Unity tidak boleh dibuka dua kali bersamaan, karena itu skrip menolak berjalan jika Editor masih terbuka.
- *Exit code* adalah angka yang dikembalikan program (0 = sukses). Skrip deploy berhenti jika build atau verifikasi mengembalikan 1.

## 5. Cloudflare deployment (Phase 4)

Command final (tersedia dan sudah diuji dengan `-DryRun`):

```powershell
.\Tools\Deploy-Web.ps1 -ArtworkApproved                                                        # 1. build + preview (Local)
.\Tools\Deploy-Web.ps1 -Variant Public                                                          # 1. build + preview (placeholder)
.\Tools\Deploy-Web.ps1 -SkipBuild -BuildPath "Builds/Web/M3-02" -ArtworkApproved                # 2. build lama → preview
.\Tools\Deploy-Web.ps1 -Production -SkipBuild -BuildPath "Builds/Web/M3-02" -ArtworkApproved    # 3. → production (ketik "lifetopia-play")
```

Perbedaan dari command yang diminta: untuk build yang berisi artwork lokal (atau build lama yang isinya tidak diketahui) wajib ditambah **`-ArtworkApproved`**. Ini mengikuti aturan "deployment preview juga memerlukan persetujuan publikasi artwork". Build `Public` tidak memerlukannya.

## 6. Pengujian (Phase 6) — hasil aktual

Tidak ada upload yang terjadi. Semua tes memakai build `Builds/Web/M3-02` yang sudah ada, salinannya di scratchpad, atau `-DryRun`.

| # | Tes | Hasil |
|---|---|---|
| 1 | `Verify-WebBuild.ps1 -BuildPath Builds/Web/M3-02` | ✅ lulus, 3 warning yang benar (template lama, judul `lifetopia-game`, tanpa version.json) |
| 2 | `Verify-WebBuild.ps1 -Url https://play.lifetopiaworld.io` | ✅ header wasm/br benar; warning "missing paths return 200" dan "version.json not served" (sesuai temuan §1.2) |
| 3 | `Verify-WebBuild.ps1 -ScanRepo` | ✅ 306 file, 0 pola secret |
| 4 | Build tidak lengkap (wasm dihapus) | ✅ FAIL "Missing … wasm file", exit 1 |
| 5 | File 27 MiB (> batas 25 MiB) | ✅ FAIL "…27.0 MiB; Cloudflare Pages limit is 25 MiB", exit 1 |
| 6 | File terlarang/asing (`Editor.log`, `notes.txt`) | ✅ FAIL keduanya, exit 1 |
| 7 | Kompresi campur (br + gz) | ✅ FAIL "Mixed compression formats", exit 1 |
| 8 | Token palsu `ghp_…` di index.html | ✅ FAIL "GitHub token pattern", exit 1 |
| 9 | index.html bukan dari template Lifetopia | ✅ FAIL, exit 1 |
| 10 | Deploy build lama tanpa `-ArtworkApproved` | ✅ ABORTED sebelum cek Wrangler, exit 1 |
| 11 | `-Production` tanpa `-SkipBuild` | ✅ ABORTED, exit 1 |
| 12 | `-Branch main` tanpa `-Production` | ✅ ABORTED, exit 1 |
| 13 | `-BuildPath` tidak ada | ✅ ABORTED, exit 1 |
| 14 | Mode 2 `-DryRun` (preview) | ✅ Wrangler login, project ada, production `2cbfb971` di `main`; staging 7 file 9,37 MiB (404.html, index.html, `_headers`, 4 file Build); command `wrangler pages deploy … --branch preview` ditampilkan, tidak dijalankan |
| 15 | Mode 3 `-DryRun` (production) | ✅ sama, `--branch main` |
| 16 | `Build-Web.ps1 -DryRun` (Local dan Public) | ✅ semua cek lulus, command Unity benar |
| 17 | `Build-Web.ps1 -Name M3-02` (nama sudah ada) | ✅ ABORTED (build lama dilindungi) |
| 18 | `Build-Web.ps1` saat RAM 0,77 GB | ✅ ABORTED "need 1.5 GB" |
| 19 | Kompilasi `WebBuildCommand.cs` (Roslyn bawaan Unity + DLL Unity 6000.3.29f1) | ✅ 0 error |
| 20 | Template baru dirender manual dengan file M3-02 + `version.json`, dibuka di Playwright (server lokal 127.0.0.1) | ✅ judul "Lifetopia World", info versi di layar loading, label kanan bawah (desktop 1280×720 dan HP 844×390) tidak menimpa HUD/hotbar/tombol, elemen di bawah label = canvas, favicon 200, tanpa version.json → tidak tampil dan 0 error JS, game tetap jalan |

Screenshot lokal (tidak dipublikasikan, berisi artwork): `Documentation/Progress/M3-02-D-screenshots-local/` (`live_prod_m302.png`, `tpl_loading.png`, `tpl_ingame.png`, `tpl_mobile.png`).

### 6.1 Build nyata `Build-Web.ps1 -Variant Public` (dijalankan founder, 26-11-2026)

Founder menjalankan build ini lewat PowerShell dengan Unity Editor dan VS Code tertutup. Angka di bawah berasal dari laporan founder dan dicek ulang dari log dan folder.

| Item | Hasil |
|---|---|
| Build | `Builds/Web/M3-02-20261126-1457` (`last-successful-build.txt` menunjuk ke sini; `_incoming` kosong setelah dipindah) |
| Durasi | 3 m 36 s total; log: `[LifetopiaBuild] Result=Succeeded errors=0 warnings=0 size=9142433 time=00:03:12` |
| Unity exit code | 0. Log: "Build Finished, Result: Success", 0 `error CS`, 0 `warning CS`. Error "Licensing handshake" di log adalah noise batchmode dan tidak memengaruhi hasil |
| RAM | awal 2,8 GB bebas, terendah 0,68 GB |
| Ukuran | 8,72 MiB, Brotli: loader 26.983 B, framework.js.br 75.452 B, wasm.br 7.271.209 B, data.br 1.761.703 B (FirstPlayable tanpa artwork, sehingga data lebih kecil dari M3-02 yang 2.443.552 B) |
| Template | index.html berisi `<title>Lifetopia World</title>`, marker `LifetopiaResponsive`, favicon.svg ikut tersalin |
| `version.json` | v0.3.2, M3.2, buildDate 2026-11-26T08:01:07Z, commit 79cbae8 (dirty), `contentMode: public-placeholder`, Unity 6000.3.29f1 |
| Verifikasi | Passed |
| Efek samping | `.meta` baru dibuat Unity (Editor/, asmdef, WebBuildCommand.cs, favicon.svg). `Web.asset` hanya berisi diff lokal yang sama seperti sebelumnya (urutan scene + `preloadedAssets`, T-2). Tidak ada file lain berubah |

Jalur `Public` (`-executeMethod` + profil Web aktif) terbukti memakai template dan kompresi dari profil.

### 6.2 Deployment preview (disetujui founder)

Command: `.\Tools\Deploy-Web.ps1 -SkipBuild -BuildPath "Builds/Web/M3-02-20261126-1457"` (tanpa `-ArtworkApproved` karena `public-placeholder`). Exit code 0.

| Item | Hasil |
|---|---|
| Deployment | `https://3d3d4263.lifetopia-play.pages.dev`, alias `https://preview.lifetopia-play.pages.dev`, Environment **Preview**, branch `preview` |
| Upload | 9 file, 8,72 MiB (404.html, favicon.svg, index.html, version.json, `_headers`, 4 file Build). Wrangler: "Uploaded 5 files (3 already uploaded)", karena loader/framework/wasm identik dengan M3-02 dan Cloudflare men-deduplikasi berdasarkan hash |
| Header (kedua URL) | `/` 200, `no-cache, no-transform`; wasm.br → `application/wasm` + `br`; framework.js.br → `application/javascript` + `br`; data.br → `application/octet-stream` + `br`; loader.js → `application/javascript` |
| 404 | path yang tidak ada → **404** (sebelumnya 200 di production) |
| `version.json` | 200 `application/json` |
| Production tidak berubah | skrip: ID production sebelum = sesudah = `2cbfb971`. Cek independen: `wrangler pages deployment list` menampilkan `3d3d4263 Preview preview` dan `2cbfb971 Production main`; play.lifetopiaworld.io dan lifetopia-play.pages.dev masih melayani `M3-02.*` dengan judul `lifetopia-game` |
| Riwayat | `Builds/Web/deploy-history.csv` 1 baris (previousProductionId = 2cbfb971) |

### 6.3 Uji browser preview (Playwright, `https://preview.lifetopia-play.pages.dev`)

| Cek | Desktop 1280×720 | Emulasi HP 844×390 (touch) |
|---|---|---|
| Loading sampai game tampil | 4,8 s | 5,1 s |
| Judul tab / favicon | "Lifetopia World" / favicon.svg 200 `image/svg+xml` | – |
| Info versi di layar loading | "Lifetopia World / Development Build · v0.3.2 · M3.2 / Built 2026-11-26" | ✅ |
| Label pojok | "Dev Build v0.3.2 · M3.2 · 2026-11-26", kanan bawah | ✅ di bawah tombol Interact, tidak menimpa |
| FPS | 60,1, frame terburuk 16,8 ms | – |
| Console | 0 error, 6 warning engine `getInternalformatParameter` | 0 error |
| Gerak (D, A, W) | ✅ dunia bergeser, kamera mengikuti | joystick dan Interact tampil setelah tap |
| Hotbar tombol 2 | ✅ slot 2 (WC) terpilih | – |
| Tas (B) | ✅ Backpack terbuka/tertutup | – |
| Jam | ✅ 08:01 → 08:06 dalam ±5,7 s | ✅ 08:02 |
| Interaksi (E di papan INFO) | ✅ dialog "Welcome Board" | – |

Splash "Made with Unity" tampil singkat setelah loading. Ini perilaku standar lisensi Unity Personal, bukan dari template.

Screenshot: `Documentation/Progress/M3-02-D-screenshots-local/prev_*.png` (lokal).

### 6.4 Belum diuji

- **Build varian `Local` lewat `Build-Web.ps1`** belum dijalankan. Perintahnya sama dengan yang sudah terbukti di M3.1/M3.2 (T-32).
- **Deploy production lewat skrip.** Hanya dry-run; butuh persetujuan.
- Pause jam saat tab di-blur tidak diuji ulang di preview. Sudah diuji pada Web build M3.2 dan kode runtime tidak berubah.
- **Rollback aktual.** Tidak dilakukan sesuai instruksi. Belum ada target rollback karena baru ada 1 deployment production.
- **EditMode/Play Mode di Editor** tidak diulang. Tidak ada kode runtime yang berubah di M3.2-D, hanya template HTML dan assembly khusus Editor. Gameplay M1–M3.2 diuji langsung di build preview (§6.3), dan build batchmode mengompilasi semua script tanpa error.
- **HP nyata** (G-1) dan gamepad (V-3), masih pending dari M3.1/M3.2.

## 7. Technical debt baru

T-32 (varian Local lewat skrip belum dijalankan), T-33 (cache immutable setelah nama file ber-hash), T-34 (preview publik → pertimbangkan Cloudflare Access gratis), T-35 (cek production branch di dashboard), T-36 (`.meta` baru harus dibuat Unity sebelum commit). Detail di `BACKLOG.md`.

## 8. Usulan commit (menunggu persetujuan)

Dua commit agar riwayat jelas. **Jangan stage:** `Web.asset` (lokal: GameplayPrototype), `GameplayPrototype.unity`, folder screenshot, `Builds/`, `.wrangler/`, `.playwright-mcp/`, art.

File `.meta` untuk `Assets/_Lifetopia/Editor/` dan `favicon.svg` sudah dibuat Unity saat build Public, jadi T-36 selesai.

**Commit 1 — `feat(m3.2): game clock, day/night tint, time HUD and dev time tools`**
```
Assets/_Lifetopia/Plugins/WebGL/LifetopiaFocus.jslib
Assets/_Lifetopia/Scenes/FirstPlayable.unity
Assets/_Lifetopia/Scripts/Controls/WebFocusBridge.cs
Assets/_Lifetopia/Scripts/DevTools/InventoryDebugTools.cs
Assets/_Lifetopia/Scripts/DevTools/TimeDebugTools.cs(.meta)
Assets/_Lifetopia/Scripts/TimeSystem.meta + Assets/_Lifetopia/Scripts/TimeSystem/**
Assets/_Lifetopia/Scripts/UI/Time.meta + Assets/_Lifetopia/Scripts/UI/Time/**
Assets/_Lifetopia/Shaders.meta + Assets/_Lifetopia/Shaders/**
Assets/_Lifetopia/Materials.meta + Assets/_Lifetopia/Materials/**
Assets/_Lifetopia/Data/Time.meta + Assets/_Lifetopia/Data/Time/**
Assets/_Lifetopia/Prefabs/UI/TimeHUD.prefab(.meta)
Assets/_Lifetopia/Tests/EditMode/GameTimeTests.cs(.meta)
Documentation/Progress/M3-02-game-time.md
```

**Commit 2 — `feat(m3.2-d): Cloudflare Pages build/verify/deploy tooling, version info and deployment docs`**
```
.gitignore
CLAUDE.md
CHANGELOG.md
Documentation/BACKLOG.md
Documentation/DECISIONS.md
Documentation/DEVELOPMENT-ROADMAP.md
Documentation/WEB-DEPLOYMENT.md
Documentation/Progress/M3-02-D-cloudflare-deployment.md
Assets/WebGLTemplates/LifetopiaResponsive/index.html
Assets/WebGLTemplates/LifetopiaResponsive/favicon.svg(.meta)
Assets/_Lifetopia/Editor.meta + Assets/_Lifetopia/Editor/** (asmdef, WebBuildCommand.cs + .meta)
Tools/Build-Web.ps1, Tools/Verify-WebBuild.ps1, Tools/Deploy-Web.ps1, Tools/LifetopiaWeb.psm1,
Tools/WebDeploy.config.psd1, Tools/Web/404.html
```
CHANGELOG, ROADMAP, BACKLOG, dan DECISIONS berisi entri M3.2 maupun M3.2-D, jadi ikut di commit 2 secara utuh.

## 9. Langkah berikut yang disarankan (butuh persetujuan)

1. Review M3.2 dan M3.2-D, lalu setujui commit (§8).
2. Playtest preview di HP nyata: `https://preview.lifetopia-play.pages.dev` (G-1).
3. Bila ingin production memakai template baru: setujui `.\Tools\Deploy-Web.ps1 -Production -SkipBuild -BuildPath "Builds/Web/M3-02-20261126-1457"` (placeholder) atau build Local baru dengan persetujuan artwork.
4. ~~Putuskan status artwork lokal di production~~: diputuskan 26-11-2026 (§10).
5. Setelah itu baru M3.3 Farming.

## 10. Keputusan founder (26-11-2026)

1. Dua commit lokal M3.2 dan M3.2-D disetujui, push ke `origin main` setelah pemeriksaan keamanan.
2. Artwork original Lifetopia World boleh dipublikasikan di Web build lewat Cloudflare Pages. PNG asli, master artwork, dan backup tidak boleh masuk GitHub.
3. Production `https://play.lifetopiaworld.io` (deployment `2cbfb971`, M3-02) dipertahankan dan tidak diganti dengan varian Public.
4. Deployment berikutnya untuk playtest memakai varian `Local` (artwork original). Varian `Public` hanya untuk verifikasi build repository publik.
5. Production deployment baru dan M3.3 tetap memerlukan persetujuan terpisah.

Diterapkan di `CLAUDE.md`, `WEB-DEPLOYMENT.md`, dan `DECISIONS.md`. `Deploy-Web.ps1` sekarang menolak deploy production untuk build placeholder kecuali dengan `-AllowPlaceholderProduction`.

## 11. Build Local, preview, production, dan playtest HP (26-11-2026)

### 11.1 Yang dijalankan founder (dari `Builds/Web/deploy-history.csv` dan Wrangler)

| Langkah | Hasil aktual |
|---|---|
| Build `Local` lewat `Build-Web.ps1` | `Builds/Web/M3-02-20261126-1527`. Log: "Build Finished, Result: Success", "Exiting batchmode successfully", 0 `error CS`. 9,37 MiB Brotli (data.br 2.442.686 B, wasm.br 7.271.209 B, framework.js.br 75.452 B, loader.js 26.983 B). `version.json`: v0.3.2, M3.2, 2026-11-26T08:32:02Z, commit 461ec3b, `contentMode: local-art` |
| Preview | `bf3c9624` (branch `preview`, 08:32:41 UTC). `previousProductionId` 2cbfb971, sehingga production tidak berubah saat preview |
| Production | `002b0658` (branch `main`, 08:39:16 UTC) lewat `Deploy-Web.ps1 -Production -SkipBuild`, setelah playtest |

### 11.2 Verifikasi production oleh Claude

| Cek | Hasil |
|---|---|
| `wrangler pages deployment list` | `002b0658 Production main 461ec3b`, `bf3c9624 Preview`, `3d3d4263 Preview`, `2cbfb971 Production` (sebelumnya) |
| Isi live = build yang disetujui | index.html, version.json, dan 4 file Build di `play.lifetopiaworld.io`, `lifetopia-play.pages.dev`, serta preview `bf3c9624`: **MD5 identik** dengan `Builds/Web/M3-02-20261126-1527` |
| `Verify-WebBuild.ps1 -BuildPath … -Url …` | lulus. wasm.br → `application/wasm` + `br`; framework/data `br`; path tidak ada → 404; version.json 200; judul "Lifetopia World" |
| Browser desktop (Playwright 1280×720) | termuat 4,3 s; info versi "Development Build · v0.3.2 · M3.2 / Built 2026-11-26" dan label pojok tampil; artwork original tampil; tas terbuka; console 0 error, 6 warning engine yang sudah dikenal |

### 11.3 Playtest HP nyata (founder-reported manual playtest)

Dilaporkan founder pada 26-11-2026 untuk preview build `M3-02-20261126-1527`. Semua 10 cek lulus:

1. Game berhasil dimuat.
2. Joystick berfungsi dengan baik.
3. Animasi karakter smooth.
4. Inventory bisa dibuka dan ditutup.
5. Hotbar bisa dipilih.
6. Jam dalam game berjalan.
7. Day/Night Cycle berfungsi.
8. Overlay rotate device berfungsi.
9. UI tidak terpotong.
10. Tidak ada crash selama pengujian.

**Tidak dicatat** (tidak diberikan): model HP, OS/browser, FPS, penggunaan RAM, waktu loading, dan durasi pengujian. Karena itu G-1 belum ditutup penuh (perlu Android Chrome + iOS Safari dengan data terukur). V-2 sebagian terpenuhi (drag & drop di HP belum dilaporkan).

### 11.4 Acceptance criteria M3.2-D

| Kriteria | Status |
|---|---|
| Audit project, deployment, header, Console | ✅ §1 |
| M3.2 terdokumentasi dan di-commit | ✅ `6e067d1` |
| `Build-Web.ps1` (batchmode, Editor tertutup, satu build, Local + Public, tidak menimpa build lama) | ✅ Public `…1457` dan Local `…1527` |
| `Verify-WebBuild.ps1` (build tidak lengkap, batas ukuran, header, secret) | ✅ §6 |
| `Deploy-Web.ps1` 3 mode, preview default, production hanya disetujui, hanya file runtime | ✅ preview `3d3d4263`/`bf3c9624`, production `002b0658` |
| Preview tidak mengubah production | ✅ ID production tetap 2cbfb971 selama kedua preview |
| Info versi, judul, favicon | ✅ live di production |
| Rollback terdokumentasi | ✅ `WEB-DEPLOYMENT.md` §8 (target 2cbfb971); tidak dieksekusi |
| Tidak ada token di repo | ✅ scan secret |
| Tidak ada regresi gameplay M1–M3.2 | ✅ Playwright preview/production + playtest HP founder |
| Dokumentasi | ✅ `WEB-DEPLOYMENT.md`, laporan ini, CHANGELOG, ROADMAP, BACKLOG, CLAUDE.md |

**M3.2-D dinyatakan selesai.** Yang masih terbuka dan tidak memblokir: G-1 penuh (data perangkat terukur), V-2 drag & drop, V-3 gamepad, T-33 cache immutable, T-34 Access untuk preview, T-35 cek production branch di dashboard.
