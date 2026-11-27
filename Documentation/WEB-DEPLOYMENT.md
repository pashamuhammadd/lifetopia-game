# Web Build & Cloudflare Pages Deployment

Panduan operasional build Web Unity dan deployment ke Cloudflare Pages untuk Lifetopia World.
Berlaku sejak M3.2-D (26 November 2026). Semua command dijalankan dari root repo di **Windows PowerShell**.

> **Aturan utama (untuk founder dan Claude):**
> 1. Tidak ada deployment (preview maupun production) tanpa persetujuan eksplisit founder untuk deployment **itu**.
> 2. **Artwork (keputusan founder 26-11-2026):** artwork original Lifetopia World **boleh** dipublikasikan di dalam Web build lewat Cloudflare Pages (preview dan production), sehingga build `Local` boleh di-deploy dengan `-ArtworkApproved` setelah deployment itu disetujui. File PNG asli, master artwork, dan backup **tidak boleh** masuk repository GitHub. URL preview **bisa diakses siapa pun** yang tahu alamatnya. `noindex` hanya mencegah indeks mesin pencari, bukan pembatas akses.
> 2a. Deployment untuk playtest founder memakai varian **`Local`** (artwork original). Varian **`Public`** hanya untuk memverifikasi bahwa repository publik bisa di-build, dan **tidak boleh** menggantikan production (skrip menolak tanpa `-AllowPlaceholderProduction`).
> 3. Default deployment = branch `preview`. Production (`main`) hanya lewat `-Production -SkipBuild` dengan build yang sudah direview.
> 4. Tidak ada auto-deploy: tidak ada Git integration, CI, hook, atau watcher. Deployment selalu manual.
> 5. Jangan membuat project Cloudflare baru dan jangan mengubah DNS, nameserver, atau custom domain.
> 6. Hanya satu Unity (Editor atau batch) pada satu waktu. Laptop punya RAM 8 GB.

---

## 1. Infrastruktur yang sudah ada

**Status saat ini (26-11-2026, M3.5):** production `https://play.lifetopiaworld.io` = deployment `94abb7f9`, Build `M3-05-20261126-2211` (varian Local, artwork original, v0.3.5 M3.5; kode di commit `4bc582f`). Preview yang diuji founder: `ad464534` (build yang sama, acceptance 16/16). Rollback target: `5784f2eb` (M3.4, build `M3-04-20261126-2103`), lalu `6cd1a5bf` (M3.3), lalu `002b0658` (M3.2).

| Item | Nilai |
|---|---|
| Cloudflare Pages project | `lifetopia-play` (Direct Upload, tanpa Git provider) |
| Production branch | `main` (terverifikasi tidak langsung: deployment Production aktif berada di branch `main`) |
| Preview branch default | `preview` → alias `https://preview.lifetopia-play.pages.dev` |
| Domain Pages | https://lifetopia-play.pages.dev |
| Custom domain | https://play.lifetopiaworld.io (DNS di Hostinger; status Active; SSL aktif; HTTP → HTTPS 301) |
| Wrangler | 4.168.0, login lewat `wrangler login`; token tersimpan di profil pengguna Windows, **bukan** di repo |
| Unity | 6000.3.29f1, profil `Assets/_Lifetopia/Settings/BuildProfiles/Web.asset`, kompresi Brotli, template `LifetopiaResponsive` |

Cache lokal Wrangler (`/.wrangler/`) berisi account id dan file sementara. Folder ini di-ignore Git.

## 2. File

| File | Fungsi |
|---|---|
| `Tools/Build-Web.ps1` | Build Unity batchmode yang aman: cek Editor tertutup, cek tidak ada build lain, cek RAM, build ke `_incoming`, tulis `version.json`, verifikasi, baru dipindah ke `Builds/Web/<Name>` |
| `Tools/Verify-WebBuild.ps1` | Verifikasi folder build, deployment live (header), dan scan secret di repo. Hanya membaca |
| `Tools/Deploy-Web.ps1` | Deploy ke Cloudflare Pages: (build) → verifikasi → gerbang artwork → cek Wrangler/project/branch → folder staging → upload → cek pasca-deploy → riwayat lokal |
| `Tools/LifetopiaWeb.psm1` | Fungsi bersama ketiga skrip di atas |
| `Tools/WebDeploy.config.psd1` | Konfigurasi: path Unity, scene, versi game, milestone, nama project, branch, batas Cloudflare |
| `Tools/Web/404.html` | Halaman 404. Dengan halaman ini Cloudflare tidak lagi menjawab path yang tidak ada dengan index.html (200) |
| `Assets/_Lifetopia/Editor/WebBuildCommand.cs` | Entry point batchmode untuk build dengan scene tertentu (varian Public / FirstPlayable) |
| `Assets/WebGLTemplates/LifetopiaResponsive/` | Template halaman: judul "Lifetopia World", favicon placeholder, info versi di layar loading + label kecil di kanan bawah |

Output (semua di `Builds/`, di-ignore Git):

```
Builds/Web/<Name>/                 build tervalidasi (yang di-deploy)
Builds/Web/_incoming/<Name>/       build yang sedang/gagal dibuat (tidak pernah di-deploy)
Builds/Web/_previous/<Name>-<ts>/  build lama yang digantikan karena -Replace
Builds/Web/_deploy/<Name>-<ts>/    salinan staging yang diunggah (boleh dihapus)
Builds/Web/_logs/<Name>.log        log Unity
Builds/Web/last-successful-build.txt
Builds/Web/deploy-history.csv      riwayat deploy lokal (id production sebelumnya = target rollback)
```

## 3. Varian build

| Varian | Scene utama | Isi | Siapa yang bisa build |
|---|---|---|---|
| `Local` (default) | `GameplayPrototype` | Artwork lokal A0/A0-02 (belum disetujui untuk distribusi) | Hanya laptop founder |
| `Public` | `FirstPlayable` | Greybox placeholder | Siapa pun yang clone repo publik. Hanya untuk verifikasi build publik, bukan untuk playtest atau production |

- `Local` memakai perintah yang sudah terbukti: `-activeBuildProfile Web.asset -build <path>`. Skrip mewajibkan scene pertama di profil Web lokal adalah GameplayPrototype.
- `Public` memakai `-executeMethod Lifetopia.EditorTools.WebBuildCommand.Build` dengan scene FirstPlayable. Profil Web tetap memberi template dan kompresi; file profil tidak diubah.
- **Sejak M3.6:** scene interior `HomeInterior` dimuat saat runtime (additive). Varian `Public` menambahkannya lewat `PublicExtraScenes` di `WebDeploy.config.psd1` → argumen `-ltExtraScenes`. Varian `Local` memakai daftar scene profil Web lokal, yang sekarang juga memuat `HomeInterior.unity` (urutan: GameplayPrototype, FirstPlayable, FoundationSmoke, HomeInterior). `HomeInterior` tidak berisi artwork; art lokal diteruskan dari scene GameplayPrototype.
- `version.json` mencatat `contentMode`: `local-art` atau `public-placeholder`. Build lama tanpa `version.json` dianggap `unknown`.

## 4. Command

### 4.1 Build saja

```powershell
.\Tools\Build-Web.ps1                                   # varian Local, nama otomatis mis. M3-02-20261126-1530
.\Tools\Build-Web.ps1 -Variant Public                   # placeholder publik
.\Tools\Build-Web.ps1 -Name M3-02-rc1 -Milestone M3.2 -GameVersion 0.3.2
.\Tools\Build-Web.ps1 -DryRun                           # hanya cek, tampilkan command Unity
```

Parameter: `-Variant Local|Public`, `-Name`, `-GameVersion`, `-Milestone`, `-Replace` (folder lama dipindah ke `_previous`, tidak dihapus), `-IgnoreLowMemory`, `-TimeoutMinutes` (default 60), `-DryRun`.

Sebelum build: **simpan pekerjaan dan tutup Unity Editor**, tutup tab browser yang berat, pastikan RAM bebas ≥ 1,5 GB. Build memakan 13–23 menit.

### 4.2 Verifikasi

```powershell
.\Tools\Verify-WebBuild.ps1 -BuildPath Builds/Web/M3-02
.\Tools\Verify-WebBuild.ps1 -Url https://play.lifetopiaworld.io, https://preview.lifetopia-play.pages.dev
.\Tools\Verify-WebBuild.ps1 -ScanRepo                   # scan secret di file repo (tracked + untracked, tidak termasuk yang di-ignore)
```

Yang diperiksa pada folder build:
- index.html ada, dibuat dari template LifetopiaResponsive, dan mereferensikan keempat file Unity.
- Tepat satu loader, framework, wasm, dan data, dengan format kompresi yang sama (`br`, `gz`, atau none; `.unityweb` ditolak).
- Tidak ada file selain file runtime (index.html, Build/*, TemplateData/*, StreamingAssets/*, version.json, favicon, 404.html, _headers).
- Tidak ada tipe file terlarang (.log, .pdb, .meta, .cs, .unity, .asset, .psd, .zip, .env, .key, .md, .ps1, …) dan tidak ada symbols.
- Tidak ada file > 25 MiB atau total > 20.000 file (batas Cloudflare Pages Free).
- Isi `version.json` valid.
- Tidak ada pola secret (token GitHub/Cloudflare/AWS/Slack/OpenAI/Anthropic, JWT, private key) di file teks.

Yang diperiksa pada URL live:
- `/` menjawab 200.
- Keempat file Build menjawab 200 dengan Content-Type dan Content-Encoding yang benar (wasm → `application/wasm` + `br`).
- Path yang tidak ada menjawab 404.
- `version.json` tersedia.

Exit code 0 = lulus, 1 = gagal.

### 4.3 Deploy — tiga mode

```powershell
# Mode 1: build lalu deploy ke PREVIEW
.\Tools\Deploy-Web.ps1 -ArtworkApproved                       # varian Local (artwork) → wajib -ArtworkApproved
.\Tools\Deploy-Web.ps1 -Variant Public                        # placeholder → tidak perlu -ArtworkApproved

# Mode 2: deploy build yang sudah ada ke PREVIEW (tanpa build ulang)
.\Tools\Deploy-Web.ps1 -SkipBuild -BuildPath "Builds/Web/M3-02" -ArtworkApproved

# Mode 3: deploy build yang sudah disetujui ke PRODUCTION (tanpa build ulang)
.\Tools\Deploy-Web.ps1 -Production -SkipBuild -BuildPath "Builds/Web/M3-02" -ArtworkApproved
#   → diminta mengetik "lifetopia-play" sebagai konfirmasi
#   (non-interaktif: tambahkan -ConfirmProduction lifetopia-play)

# Semua mode: latihan tanpa upload
.\Tools\Deploy-Web.ps1 -SkipBuild -BuildPath "Builds/Web/M3-02" -ArtworkApproved -DryRun
```

Parameter lain: `-Branch <nama>` untuk branch preview lain (mis. `-Branch m3-03-test` → `https://m3-03-test.lifetopia-play.pages.dev`; tidak boleh `main`). Tanpa `-BuildPath` dan dengan `-SkipBuild`, skrip memakai `last-successful-build.txt`.

`-ArtworkApproved` adalah pernyataan Anda bahwa founder sudah menyetujui publikasi artwork **di build tersebut**. Claude tidak boleh menambahkan flag ini tanpa persetujuan tertulis di percakapan.

Urutan kerja Deploy-Web.ps1 (berhenti di langkah mana pun yang gagal; tidak ada yang terunggah):

1. Aturan branch: preview ≠ `main`; production wajib `-SkipBuild`.
2. Build (mode 1) melalui Build-Web.ps1.
3. Verifikasi folder build.
4. Gerbang artwork: `contentMode` selain `public-placeholder` wajib `-ArtworkApproved`.
5. Wrangler terpasang dan login. Project `lifetopia-play` ada (tidak pernah dibuat). Deployment Production aktif berada di branch `main`. ID production saat ini dicatat sebagai target rollback.
6. Folder staging `_deploy/…`: hanya file runtime dari daftar izin, `_headers` dibuat ulang sesuai kompresi sebenarnya, ditambah `404.html`. Folder ini diverifikasi ulang.
7. Konfirmasi production (ketik nama project).
8. `wrangler pages deploy <staging> --project-name lifetopia-play --branch <branch> --commit-hash <sha> --commit-message "<build> v<ver> <milestone> (<content>)" --commit-dirty=<bool>`.
9. Cek pasca-deploy: header URL deployment, plus alias preview atau domain production. Untuk preview, dicek juga bahwa ID production **tidak berubah**.
10. Riwayat ditulis ke `Builds/Web/deploy-history.csv`.

## 5. Header HTTP (`_headers`)

Dibuat otomatis per deploy oleh `New-LtHeadersContent` sesuai kompresi build. Contoh untuk Brotli:

```
/*
  Cache-Control: no-cache, no-transform
  X-Robots-Tag: noindex
  X-Content-Type-Options: nosniff
  Referrer-Policy: strict-origin-when-cross-origin

/Build/*.wasm.br
  Content-Type: application/wasm
  Content-Encoding: br

/Build/*.framework.js.br
  Content-Type: application/javascript
  Content-Encoding: br

/Build/*.data.br
  Content-Type: application/octet-stream
  Content-Encoding: br
```

- `_headers` yang ada di dalam folder build diabaikan dan selalu dibuat ulang, supaya tidak ada versi tulisan tangan yang berbeda.
- `no-transform` mencegah Cloudflare mengompres ulang file `.br`. Tanpanya browser bisa menerima data yang dikompres dua kali.
- **Caching:** semua file `no-cache`, artinya browser selalu bertanya ke server tetapi mendapat `304 Not Modified` bila file sama (ETag). Alasannya, nama file build tidak mengandung hash. Bila memakai cache panjang, setelah deploy ulang dengan nama build yang sama pemain bisa mendapat kombinasi file lama dan baru, sehingga game crash. Unity juga menyimpan `.data` di IndexedDB (`webGLDataCaching`). Cache `immutable` baru aman setelah "Name Files As Hashes" diaktifkan (backlog T-33).

## 6. Informasi versi

`Build-Web.ps1` menulis `version.json`:

```json
{ "product": "Lifetopia World", "channel": "Development Build", "version": "0.3.2", "milestone": "M3.2",
  "buildName": "M3-02-20261126-1530", "buildDate": "2026-11-26T08:30:00Z", "commit": "79cbae8", "dirty": true,
  "contentMode": "local-art", "unity": "6000.3.29f1" }
```

- Template menampilkannya di layar loading: "Lifetopia World / Development Build · v0.3.2 · M3.2 / Built 2026-11-26".
- Setelah game termuat, muncul label 10 px semi-transparan di kanan bawah ("Dev Build v0.3.2 · M3.2 · 2026-11-26"). Label tidak menangkap klik maupun sentuhan.
- Jika `version.json` tidak ada, tidak ada yang tampil dan tidak ada error.
- Ubah versi atau milestone di `Tools/WebDeploy.config.psd1` (`GameVersion`, `Milestone`), atau per build lewat `-GameVersion` / `-Milestone`.
- `productName` di Player Settings sengaja **tidak** diubah, karena merupakan identitas project. Judul tab ditulis langsung di template.

## 7. Update production (prosedur lengkap)

1. Build baru varian `Local` (`.\Tools\Deploy-Web.ps1 -ArtworkApproved` = build + preview sekaligus), atau `Build-Web.ps1` lalu deploy ke **preview** dengan `-SkipBuild`.
2. Founder melakukan playtest di `https://preview.lifetopia-play.pages.dev` (desktop + HP, §9).
3. Founder menyetujui build `Builds/Web/<Name>` untuk production, termasuk artwork-nya.
4. Jalankan `.\Tools\Deploy-Web.ps1 -Production -SkipBuild -BuildPath "Builds/Web/<Name>" [-ArtworkApproved]`. File yang diunggah persis sama dengan yang dites di preview.
5. Cek https://play.lifetopiaworld.io, lalu `.\Tools\Verify-WebBuild.ps1 -Url https://play.lifetopiaworld.io`.
6. Simpan folder build yang sedang live. Jangan dihapus, karena diperlukan untuk rollback lewat deploy ulang.

## 8. Rollback

Wrangler 4.168 **tidak** punya perintah rollback Pages. Dua cara (keduanya butuh persetujuan founder):

**A. Dashboard (instan, disarankan):**
Cloudflare Dashboard → Workers & Pages → `lifetopia-play` → tab *Deployments* → pada deployment **Production** sebelumnya klik menu `⋯` → *Rollback to this deployment* → konfirmasi. Domain production langsung melayani deployment lama.
- Hanya deployment production sebelumnya yang bisa menjadi target. ID-nya ada di `deploy-history.csv` (kolom `previousProductionId`) dan di `wrangler pages deployment list --project-name lifetopia-play --environment production`.
- Status 26-11-2026 (M3.5): production = `94abb7f9` (build `M3-05-20261126-2211`). Target rollback = **`5784f2eb`** (M3.4, build `M3-04-20261126-2103`), lalu `6cd1a5bf` (M3.3), lalu `002b0658` (M3.2). Folder build tersimpan di `Builds/Web/`.
- **Rollback M3.5 → M3.4** (hanya dengan persetujuan founder): Dashboard → `lifetopia-play` → Deployments → deployment Production `5784f2eb` → `⋯` → *Rollback to this deployment*. Alternatif deploy ulang build yang sama: `.\Tools\Deploy-Web.ps1 -Production -SkipBuild -BuildPath Builds/Web/M3-04-20261126-2103 -ArtworkApproved` (konfirmasi production diketik). Lalu cek `https://play.lifetopiaworld.io/version.json` = `M3-04-20261126-2103`. Rollback hanya mengembalikan Web build; repo GitHub tetap di commit M3.5.
- **Rollback M3.4 → M3.3** (hanya dengan persetujuan founder): Dashboard → `lifetopia-play` → Deployments → deployment Production `6cd1a5bf` → `⋯` → *Rollback to this deployment*. Alternatif lewat deploy ulang build yang sama: `.\Tools\Deploy-Web.ps1 -Production -SkipBuild -BuildPath Builds/Web/M3-03-20261126-1741 -ArtworkApproved` (konfirmasi production diketik). Lalu cek `https://play.lifetopiaworld.io/version.json` = `M3-03-20261126-1741`. Catatan: rollback hanya mengembalikan Web build; repo GitHub tetap di commit M3.4.

**B. Deploy ulang build lama:**
`.\Tools\Deploy-Web.ps1 -Production -SkipBuild -BuildPath "Builds/Web/<build lama>" [-ArtworkApproved]`

Setelah rollback: `.\Tools\Verify-WebBuild.ps1 -Url https://play.lifetopiaworld.io`. Minta pemain menutup lalu membuka ulang tab. Cache `no-cache` membuat versi baru langsung terambil.

## 9. Playtest di HP

1. Buka `https://preview.lifetopia-play.pages.dev` (preview) atau `https://play.lifetopiaworld.io` (production) di Chrome Android atau Safari iOS, lalu putar ke landscape.
2. Cek dan catat: waktu loading, FPS terasa, joystick, tombol Interact, Bag, tap dan drag hotbar/inventory, overlay rotasi saat portrait, safe area (notch), jam berhenti saat app di-background, label versi tidak menutupi tombol.
3. Debug jarak jauh: Android `chrome://inspect` di PC (USB debugging); iOS Safari → Develop menu di Mac.
4. Catat hasil di laporan milestone (gate G-1 / V-2). Hasil emulasi browser tidak dihitung sebagai uji HP.
5. Laporan playtest founder dicatat apa adanya sebagai *founder-reported manual playtest*. Model HP, browser, FPS, RAM, dan durasi hanya ditulis bila founder memberikannya.

**Riwayat playtest HP:**

| Tanggal | Build | Sumber | Hasil |
|---|---|---|---|
| 2026-11-26 | `M3-02-20261126-1527` (preview `bf3c9624`) | Founder-reported manual playtest, 1 HP nyata (model/browser tidak dicatat) | 10/10 lulus: game termuat, joystick, animasi karakter smooth, inventory buka/tutup, hotbar, jam berjalan, day/night, overlay rotate, UI tidak terpotong, tanpa crash |

## 10. Troubleshooting

- **Custom domain sesaat setelah deploy production (M3.4, 26-11-2026):** pemeriksaan otomatis pertama di `play.lifetopiaworld.io` sempat mendapat 404 untuk `framework.js.br` dan `loader.js`, sementara URL deployment langsung dan `lifetopia-play.pages.dev` sudah 200 semua. Sekitar 1 menit kemudian semua file 200, `Verify-WebBuild.ps1 -Url https://play.lifetopiaworld.io` lulus, dan isi file identik dengan build. Penyebab: jeda propagasi edge custom domain. Tindakan: tunggu ±1 menit lalu jalankan ulang verifikasi; jangan langsung rollback. Terjadi lagi pada deploy M3.5 (cek pertama custom domain masih M3.4; ±45 s kemudian M3.5 dan verifikasi lulus), dan alias `preview.` juga sempat tertinggal pada deploy preview M3.5.


| Gejala | Penyebab / solusi |
|---|---|
| `running scripts is disabled` | `powershell -ExecutionPolicy Bypass -File .\Tools\Deploy-Web.ps1 ...` (policy CurrentUser laptop ini: RemoteSigned, jadi skrip lokal boleh jalan) |
| `A Unity Editor has this project open` | Simpan, lalu tutup Unity Editor. Lockfile `Temp/UnityLockfile` sedang dipakai |
| `Only X GB RAM free` | Tutup Chrome, VS Code, dan Unity. `-IgnoreLowMemory` berisiko: build pertama M3.2 dihentikan Windows |
| `Another Lifetopia build is running` | Ada build lain. Bila yakin tidak ada, hapus `Builds/Web/.build.lock` |
| Build gagal, `error CS…` | Perbaiki compile error di Editor. Build lama tidak tersentuh; log ada di `Builds/Web/_logs/` |
| `Wrangler is not logged in` | `wrangler login` (browser), lalu ulangi |
| `Current production deployment is on branch …` | Production branch project berubah. Cek di dashboard. Skrip menolak deploy |
| Game macet "Unable to parse Build/….framework.js.br" | Header `Content-Encoding: br` hilang. Pastikan deploy lewat skrip (yang membuat `_headers`), lalu cek dengan `Verify-WebBuild.ps1 -Url` |
| File Build menjawab HTML/200 | File tidak ada di deployment (fallback SPA). Deploy lewat skrip sudah menyertakan 404.html |
| Versi lama masih tampil | Hard reload (Ctrl+Shift+R). Cek `version.json` di URL |
| Warning `WebGL: INVALID_ENUM: getInternalformatParameter` (6×) | Dari engine Unity, tidak berbahaya; sudah ada sejak M1 |

## 11. Keamanan

- Tidak ada token di repo: Wrangler memakai login OAuth di profil pengguna. Jangan menaruh `CLOUDFLARE_API_TOKEN` di file. Bila suatu saat perlu token, simpan hanya di environment variable sesi.
- `Verify-WebBuild.ps1 -ScanRepo` memeriksa pola secret di semua file repo yang tidak di-ignore.
- Yang diunggah hanya folder staging dari daftar izin. Source Unity, master art, backup, log, `.meta`, dan dokumen tidak pernah ikut.
- Build `Local` berisi artwork original. Publikasi lewat Cloudflare Pages **disetujui founder 26-11-2026**. Production saat ini (`94abb7f9`, build `M3-05-20261126-2211`) berisi artwork ini (termasuk sprite NPC Rio). Repository GitHub tetap tanpa PNG, master art, dan backup.

## 11a. Riwayat preview M3.6

| Tanggal | Build | Deployment | Catatan |
|---|---|---|---|
| 2026-11-27 | `M3-06-20261127-1037` (Local, v0.3.6) | Preview `aaa1b7ea` (https://aaa1b7ea.lifetopia-play.pages.dev, alias preview) | Disetujui founder; production tetap `94abb7f9` (M3.5). Local build dijalankan sebagai proses Windows terpisah (`Start-Process`) karena percobaan sebelumnya dihentikan oleh pembersih proses background Claude Code saat RAM rendah; pemeriksaan RAM skrip tetap aktif |

## 12. Catatan untuk Claude (sesi berikutnya)

- Jangan menjalankan `Deploy-Web.ps1` tanpa `-DryRun` sebelum founder menyetujui deployment spesifik itu: target (preview/production), build, dan publikasi artwork.
- Jangan menambahkan `-ArtworkApproved`, `-ConfirmProduction`, atau `-IgnoreLowMemory` atas inisiatif sendiri.
- Sebelum build: Editor harus tertutup (gunakan MCP hanya bila Editor memang terbuka untuk pekerjaan lain, lalu minta founder menutupnya), cek RAM, dan hanya satu build.
- Jangan mengulang Web build tanpa alasan. Untuk uji deploy, gunakan build yang ada dengan `-SkipBuild`.
- Jangan membuat project Pages baru, mengubah domain/DNS, atau menjalankan rollback tanpa persetujuan.
