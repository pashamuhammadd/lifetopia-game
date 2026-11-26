# M2 — Art Calibration: laporan akhir

Tanggal: 2026-11-26 · Unity 6000.3.29f1 · 2D Built-In RP · target Web
Status: **M2 DITUTUP sebagai milestone kalibrasi** — disetujui founder 2026-11-26 dengan pengecualian
E1–E3 (§7). E4 (uji HP nyata) dan E5 (hak distribusi art) tetap **gate wajib sebelum public release**.
Artwork tetap lokal. M3 belum dimulai. Prosedur commit: §13.

Laporan pendukung: `M2-A0-static.md` (A0 static, disetujui founder), `M2-A0-02-art-brief.md` (brief art).

---

## 1. Ringkasan

| Area | Hasil |
|---|---|
| Animasi karakter | Idle + walk empat arah (D/L/R/U) dari paket A0-02, memakai sistem yang sudah ada |
| Gameplay M1 | Gerak, kamera, collision, interaksi, dialog, touch, focus: tetap lulus |
| Portrait | Overlay "Please rotate your device" pada perangkat sentuh |
| EditMode | **27/27 lulus** |
| Console Editor | **0 error / 0 warning** |
| Web build | **Succeeded, 0 error / 0 warning, 9,73 MB** |
| Browser (laptop, Intel UHD) | 60,1 FPS desktop 1280×720; 60,1 FPS emulasi HP 844×390; 0 frame >33 ms; 0 error |
| HP nyata | **Belum diuji** |

## 2. Audit awal (Tahap 1)

- Git: `main` di 5104ce0 (M1). Pekerjaan M2 sebelumnya ada sebagai file untracked + `Web.asset` modified. Tidak ada perubahan di luar M2.
- Unity: Editor ready, Pipeline 1.0.1 terhubung (CLI + MCP), kompilasi bersih, Console 0/0.
- Scene `ArtCalibration`, prefab A0, data `anim_player_male_a0`, dan script animasi (§13 `M2-A0-static.md`) utuh.
- File M1 (`FirstPlayable.unity`, `Player.prefab`, semua script M1, ProjectSettings, Packages) **identik** dengan 5104ce0, dicek sebelum dan sesudah pekerjaan.

## 3. Paket A0-02 dan QC independen

Paket: `lifetopia-m2-a0-02-animation` (folder lokal di samping repo, tidak di-commit). Yang diimport hanya `RuntimeAssets/Characters/Frames`
(48 PNG). `OptionalSheets`, preview, dan GIF tidak masuk ke Assets.

QC sendiri, sebelum import (skrip Python stdlib, tanpa dependensi baru):

| Cek | Hasil |
|---|---|
| SHA-256 vs manifest | 48/48 cocok |
| Format | 48/48 128×128 RGBA, sudut transparan |
| Garis kaki | dasar alpha di y=112 (top-origin) = y 16 Unity, **semua frame** |
| Pose unik walk | **4 per arah**, urutan slot `0,0,1,1,2,2,3,3` |
| Pose unik idle | **3 per arah**, urutan `0,1,2,1` (breathing turun 1–2 px, kaki tidak bergerak) |
| Jitter kepala | centroid horizontal kepala bergeser ≤ 1,1 px antar pose |

Klaim paket tentang keterbatasannya terbukti benar. Tidak ada klaim "8 pose unik".

## 4. Import dan integrasi (Tahap 2)

**Import** → `Assets/_Lifetopia/Art/Characters/Frames/{idle,walk}/{d,l,r,u}/`. Setting diverifikasi per file
(48/48): Sprite Single, **Full Rect, PPU 64, pivot custom (0.5, 0.125) = (64, 16) px**, Bilinear, Clamp,
tanpa mipmap, tanpa kompresi, alpha is transparency.

**Sistem yang dipakai ulang:** `SpriteFrameMath`, `CharacterAnimationSet`, `SpriteCharacterView`,
`anim_player_male_a0.asset`, `Player_A0.prefab`.

- `anim_player_male_a0`: idle 4 slot/arah **4 fps**; walk 8 slot/arah **12 fps**. Karena tiap pose ditahan
  2 slot, pose walk berganti 6×/detik dan satu siklus berdurasi 0,67 detik.
- `Player_A0/Sprite_A0` → `SpriteCharacterView` kini juga mereferensikan `PlayerMotor2D`.
- Sprite awal prefab = `idle_d_00`, supaya tampilan Editor sama dengan runtime.
- Sprite statis A0-01 **tidak dihapus**. Untuk mengembalikan tampilan statis, cukup kosongkan array di asset.

**Perubahan kode (tambahan, bukan sistem baru):**
- `SpriteFrameMath.IsVisiblyWalking(intent, speed, min)`. Walk hanya diputar jika ada input **dan** Rigidbody
  benar-benar bergerak (≥ 0,5 u/s), sesuai instruksi paket "use actual movement state". Saat mendorong dinding,
  karakter tampil idle, tidak berjalan di tempat.
- `SpriteCharacterView` membaca state di `LateUpdate`, tanpa alokasi per frame:
  - Arah berubah → langsung ganti clip.
  - Lepas tombol → langsung idle.
  - Tertahan objek sambil tetap menekan tombol → idle setelah tertahan ≥ 0,08 s (debounce anti-kedip).
- Arah terakhir dipertahankan saat berhenti (dari `PlayerFacing` M1, tidak diubah).
- Tidak ada flipX, scale, atau bob. Kiri dan kanan memakai sprite masing-masing.
- 2 test baru: timing 12 fps dan aturan walk-state.

Konsep untuk founder: **ScriptableObject** (`anim_player_male_a0`) menyimpan data frame dan fps. **View**
hanya membaca "arah hadap" dan "sedang bergerak". Jadi animasi bisa diganti atau dituning lewat Inspector
tanpa menyentuh kode gerak.

## 5. Visual polish (Tahap 3)

| Item | Hasil |
|---|---|
| Y-sorting | Benar untuk karakter beranimasi (di belakang batang palem, di depan/belakang rumah); sprite root tidak berubah |
| Tembus bangunan | Tidak. Collider rumah menahan di y 2,12 (depan) dan x −6,57 (samping) |
| Collider pohon | Lingkaran r 0,25 di pangkal batang; player dibelokkan, kanopi tidak menghalangi |
| Skala rumah vs karakter | Tidak diubah. Pintu ≈ 87% tinggi karakter. Pembanding skala rumah 1,15× (sementara, Play Mode) ada di screenshot 05; perbedaannya kecil, tetapi membuat tekstur buram dan menggeser collider |
| Terrain | Tidak ada sambungan antar tile. Pengulangan pola terlihat saat zoom; belum ada tile transisi (tidak dibuat, sesuai instruksi) |
| UI | Hint keyboard, prompt, dialog, dan tombol touch terbaca di 1280×720 dan 844×390 |
| Touch | Joystick + tombol Interact tetap. Uji emulasi: drag → walk kanan, lepas → idle |
| Kamera | Tidak diubah (M1: interpolasi Rigidbody + follow LateUpdate) |
| Portrait | **Baru:** overlay di `Assets/WebGLTemplates/LifetopiaResponsive/index.html`, aktif bila `orientation: portrait` **dan** `pointer: coarse` (perangkat sentuh). Menutupi canvas supaya tidak ada sentuhan nyasar. Animasi ikon dimatikan bila `prefers-reduced-motion`. Teks bahasa Inggris |

Tidak ada aset baru yang dibuat.

## 6. Pengujian (Tahap 4) — yang benar-benar dijalankan

### Editor
- **EditMode 27/27 lulus.**
- **Skenario animasi (Play Mode, input keyboard disuntikkan):**
  - Walk D/L/R/U masing-masing memakai clip arahnya sendiri. Stop → idle di arah terakhir. Idle berputar
    `00→01→02→03` setiap ~0,25 s; walk berganti slot setiap ~0,08 s.
  - Diagonal W+D memakai arah dari aturan anti-flicker M1.
  - Mendorong rumah → walk lalu idle-Up. Tap cepat → walk/idle langsung.
  - flipX 0 frame, scale berubah 0 frame, posisi lokal sprite tetap 0,000.
- **Regression M1/M2 lengkap** (31 langkah, semua lulus):
  - Kecepatan 4,00 di semua arah, diagonal (2,83; 2,83).
  - Collision rumah/palem/air/batas map, semak bisa dilewati, area belakang rumah terjangkau.
  - Dialog buka/next/tutup; selama dialog input gerak diblok dan karakter idle.
  - Focus-lost mereset input (resets +1) → idle.
  - Joystick: vel 4,00 dengan walk-Right, lepas → 0 dan idle.
- Screenshot Game View: §9.

Kendala saat pengujian (dicatat jujur):
1. Dua langkah awal skenario animasi menunjukkan jeda frame tidak teratur (0,2–0,38 s). Dugaan saya, Editor
   sedang tidak fokus sehingga update-nya diperlambat; langkah berikutnya stabil di 0,08 s.
2. Satu run regression gagal total untuk keyboard (mouse/joystick tetap jalan). Penyebab: Editor kehilangan
   fokus, sehingga Input System me-reset keyboard (`ResetAndDisableNonBackgroundDevices`). Mengubah setting
   sementara tidak memulihkannya. Setelah Play Mode di-restart, semua lulus. Ini kondisi lingkungan uji
   Editor, bukan bug game; di browser, focus ditangani `WebFocusBridge` (lulus).
3. Setting input Editor diubah sementara di memori untuk pengujian, lalu dikembalikan. File di disk tidak berubah.

### Web build (lokal, HTTP + Brotli)
| Item | Nilai |
|---|---|
| BuildReport | Succeeded, 0 error / 0 warning, ±9,2 menit |
| Total output | 9,73 MB (A0 static: 9,46 MB) |
| `.data.br` | 2,38 MB (sebelumnya 2,08 MB → +0,30 MB untuk 48 frame) |
| `.wasm.br` | 7,25 MB |
| Memori tekstur frame | 48 × 128×128 RGBA32 ≈ 3,0 MiB (tanpa kompresi, sesuai manifest kalibrasi) |

### Browser (Playwright, Chromium, laptop ini)
| Uji | Hasil |
|---|---|
| Desktop 1280×720: walk 4 arah + idle sesudahnya | Lulus (screenshot crop per arah) |
| FPS desktop (5 s) | **60,1 FPS**, 0 frame >33 ms |
| Window blur sambil menahan D | Dunia berhenti (selisih piksel 0% vs 46,5% saat berjalan) |
| Overlay rotate di desktop | Tersembunyi (`display: none`) |
| Emulasi HP 844×390, DPR 2, UA Android, touch | Kontrol sentuh muncul; drag joystick → walk kanan; lepas → berhenti |
| FPS emulasi HP (5 s) | **60,1 FPS**, 0 frame >33 ms (**ini GPU laptop, bukan HP**) |
| Portrait 390×844 | Overlay tampil (`flex`); kembali ke landscape → hilang |
| Console | 0 error; 6 warning engine `getInternalformatParameter` (sama seperti M0/M1). 3 warning Canvas2D berasal dari skrip diff piksel saya, bukan dari game |

### Tidak diuji
- **HP nyata (Android/iOS), Safari, Firefox.** Angka FPS di atas tidak mewakili performa HP.
- Tekanan memori pada HP kelas bawah.

## 7. Acceptance criteria dan pengecualian (butuh keputusan founder)

Roadmap M2: *"One avatar, grass/path, palm, house, HUD; sample idle/walk — check seam, scale, foot anchor,
Y-sort, mobile readability and performance; founder approves art before mass production."*

| Kriteria | Status |
|---|---|
| Satu avatar, grass/path, palm, house, HUD | ✅ (A0 static disetujui founder) |
| Sample idle/walk | ✅ empat arah (melebihi sample "down" di Art Bible A0) |
| Seam | ✅ tidak ada sambungan; ⚠ pengulangan pola, tanpa tile transisi |
| Scale | ✅ PPU 64 konsisten; ⚠ pintu ≈ 87% tinggi karakter |
| Foot anchor | ✅ y=16 di semua 48 frame, terverifikasi file + Unity |
| Y-sort | ✅ |
| Mobile readability | ⚠ emulasi saja; karakter ±58 px CSS di 844×390 |
| Performance | ✅ desktop/emulasi terukur; ❌ HP nyata belum |
| Founder approval art | ✅ diterima sebagai kalibrasi dengan pengecualian E1–E3 (2026-11-26) |

**Pengecualian (keputusan founder 2026-11-26):** E1–E3 **diterima** sebagai batasan kalibrasi; target final tetap di Art Bible dan `BACKLOG.md`. E4–E5 tetap terbuka sebagai gate public release.
- **E1 — Walk 4 pose unik** (ditahan 2 slot), bukan 8 pose unik seperti `DECISIONS.md` ("8 per direction").
  Gerakan terbaca jelas, tetapi lebih "patah" daripada siklus 8 pose.
- **E2 — Idle berupa breathing prosedural** (1 pose gambar + 2 variasi geser 1–2 px), bukan idle digambar tangan.
- **E3 — Rendisi baru karakter** sedikit berbeda dari sprite statis A0-01 yang Anda setujui sebelumnya
  (lihat screenshot 01). Wajah, warna, dan pakaian konsisten antar frame A0-02.
- **E4 — Uji HP nyata belum dilakukan** (terbuka sejak M0).
- **E5 — Hak distribusi art** belum direview owner. PNG tidak boleh di-push ke repo publik tanpa izin.
- **E6 — Catatan visual minor:** pintu 87%, pengulangan terrain, toko/air/papan INFO masih greybox,
  tekstur tanpa kompresi (3 MiB).

Keputusan: E1–E3 diterima. Siklus walk 8 pose / idle digambar bisa menyusul di produksi A2 tanpa mengubah kode (cukup ganti sprite di asset).

E4 dan E5 tetap gate terbuka sebelum rilis publik.

## 8. Daftar perubahan (dibanding commit M1 5104ce0)

**Diubah (tracked):**
- `Assets/_Lifetopia/Settings/BuildProfiles/Web.asset`: `ArtCalibration` jadi scene pertama (+ baris
  `preloadedAssets` yang memang berubah sendiri tiap build, known issue M1).
- `Assets/WebGLTemplates/LifetopiaResponsive/index.html`: overlay rotate portrait.
- `CHANGELOG.md`, `Documentation/ASSET-REGISTER.csv`, `Documentation/DEVELOPMENT-ROADMAP.md`,
  `Documentation/DECISIONS.md`. Baru: `Documentation/BACKLOG.md`.

**Baru:**
```
Assets/_Lifetopia/Art/{Characters,Props,Buildings,Terrain,UI}/          7 PNG A0 static (+.meta)
Assets/_Lifetopia/Art/Characters/Frames/{idle,walk}/{d,l,r,u}/          48 PNG A0-02 (+.meta)
Assets/_Lifetopia/Data/Characters/anim_player_male_a0.asset
Assets/_Lifetopia/Prefabs/A0/  Player_A0 (variant), House_A0, CoconutPalm_A0, HibiscusShrub_A0
Assets/_Lifetopia/Tilemaps/A0/ tile_grass_01_v01, tile_path_01_v01
Assets/_Lifetopia/Scenes/ArtCalibration.unity
Assets/_Lifetopia/Scripts/Player/{SpriteFrameMath,CharacterAnimationSet,SpriteCharacterView}.cs
Assets/_Lifetopia/Tests/EditMode/SpriteFrameMathTests.cs
Documentation/Progress/{M2-A0-static,M2-A0-02-art-brief,M2-art-calibration-final}.md
```
Tidak diubah: scene/prefab/script M1, ProjectSettings, Packages, versi Unity, render pipeline,
repo `lifetopia-platform`.

Struktur `Player_A0` (Prefab Variant dari `Player.prefab`):
```
Player_A0  SortingGroup, Rigidbody2D, CircleCollider2D, PlayerInputReader, PlayerMotor2D, PlayerFacing,
           PlaceholderCharacterView (disabled)
├─ Shadow             SpriteRenderer (skala 0.7 × 0.22)
├─ Body (inactive)    Legs / Torso / Head / FacingMarker: greybox M1
├─ Label_TEMP (inactive)
├─ InteractionSensor  CircleCollider2D + InteractionDetector
└─ Sprite_A0          SpriteRenderer + SpriteCharacterView(facing, renderer, anim_player_male_a0, motor)
```

## 9. Screenshot (lokal, dikecualikan dari Git lewat `.git/info/exclude`)

`Documentation/Progress/M2-final-screenshots/`
- `editor/01_lineup_house_scale.png`: semua pose unik D/L di samping rumah (garis kaki, wajah, skala)
- `editor/02_lineup_right_up.png`: pose R/U
- `editor/03_walk_right_gameplay.png`: gameplay saat berjalan
- `editor/04_house_door_scale_animated_char.png`: karakter di depan pintu
- `editor/05_compare_house_scale_115_TEMP.png`: **pembanding sementara** rumah 1,15× (tidak disimpan)
- `editor/06_ysort_walk_up_behind_palm.png`: Y-sort di belakang batang palem
- `editor/07_touch_prompt_phone_aspect.png`: kontrol sentuh + prompt pada rasio HP
- `web/desk_walk_{right,down,left,up}.png`, `web/desk_idle_after_*.png`, `web/desk_overview_1280x720.png`
- `web/mob_landscape_spawn_touch.png`, `web/mob_landscape_joystick_walk_right.png`,
  `web/mob_portrait_rotate_overlay.png`, `web/mob_landscape_after_rotate_back.png`

## 10. Cara review manual
1. Buka `Assets/_Lifetopia/Scenes/ArtCalibration`, tekan Play, klik Game View.
2. Jalan ke empat arah, berhenti (cek arah terakhir), dorong ke dinding rumah (harus idle), lewati semak,
   jalan di belakang palem dan rumah.
3. Inspector `anim_player_male_a0`: ubah `walk.framesPerSecond` (mis. 10 atau 14) saat Play untuk tuning rasa.
4. Web: build profile **Web** → `Builds/Web/ArtCalibration`, sajikan lewat HTTP (bukan `file://`). Di HP, buka
   dalam portrait untuk melihat overlay rotate.

## 11. Aset yang belum tersedia
Walk 8 pose unik per arah; idle digambar tangan; avatar perempuan; layer modular (rambut/baju/aksesori);
tile transisi grass↔path dan varian tile; art toko, air/pantai, papan INFO; NPC; ikon item dan VFX (M3/A4).

## 12. Rekomendasi M3 (belum dimulai, butuh persetujuan)
1. Sebelum M3: uji HP nyata (minimal 1 Android Chrome + 1 iOS Safari) dan catat FPS/memori.
2. Gunakan `ArtCalibration` sebagai basis scene M3, atau buat scene gameplay baru dari prefab A0.
3. Urutan gameplay sesuai roadmap: local save → inventory → farming (4 state tanaman) → fishing → market →
   tutorial quest, dengan test untuk state tanaman, kapasitas inventory, transaksi, dan reward sekali.
4. Produksi art paralel (A1–A4) memakai standar yang lulus di sini: 128 px cell, pivot (64,16), PPU 64, baris D/L/R/U.
5. Sebelum aset bertambah banyak: Sprite Atlas + kompresi tekstur (uji transparansi), dan cek kuota Git LFS.
6. Keputusan hak distribusi art sebelum commit/push PNG ke repo publik.

## 13. Penutupan M2 dan prosedur commit (artwork dipisah dari source)

### 13.1 Prinsip
- Artwork A0/A0-02 **belum punya izin distribusi** → tidak boleh masuk ke commit di branch yang akan di-push,
  baik sekarang maupun "ikut terbawa" commit berikutnya.
- Semua yang **mereferensikan** artwork juga ditahan lokal: scene `ArtCalibration`, prefab `A0/*`, tile `A0/*`,
  data `anim_player_male_a0`, dan perubahan scene list di `Web.asset`. Kalau ikut di-commit tanpa PNG-nya,
  repo publik akan berisi referensi sprite yang hilang (missing sprite / missing scene di build profile).
- Source code M2 **tidak mereferensikan art apa pun**; ia generik dan aman di-commit.

### 13.2 Pemisahan yang sudah dilakukan (lokal, reversible)
- `.git/info/exclude` (file lokal, tidak ikut repo) kini memuat semua path art + art-dependent di atas.
  `git add -A --dry-run` → **0 file art** yang akan ter-stage.
- Backup lokal di luar repo: `../lifetopia-m2-local-art-backup/m2-art-local-2026-11-26.tar` (167 entri:
  PNG + .meta, scene, prefab, tile, data, `Web.asset` lokal).
- `Web.asset` lokal (ArtCalibration scene pertama) **sengaja tidak di-stage**; versi repo tetap versi M1.

### 13.3 Bukti repo tetap bisa dibangun tanpa artwork
Salinan bersih di luar repo = file tracked commit M1 (`Web.asset` versi M1) + **hanya** file public M2:
| Cek | Hasil |
|---|---|
| Import + kompilasi Unity batchmode (6000.3.29f1) | 0 error CS |
| EditMode test di salinan bersih | **27/27 lulus** |
| Scan GUID semua YAML (scene/prefab/asset/ProjectSettings) vs .meta proyek + package | **0 referensi tak terselesaikan** (35 file) |
| Scan yang sama di proyek lokal lengkap (kontrol) | 0 tak terselesaikan (43 file) |
| Web build dari salinan bersih (batchmode, profile Web, scene FirstPlayable) | **Build Finished, Result: Success**; output ±9,0 MB (`.data.br` 1,70 MB, `.wasm.br` 7,24 MB) |

### 13.4 Daftar file commit yang diusulkan (17 file)
```
Assets/_Lifetopia/Scripts/Player/SpriteFrameMath.cs (+ .meta)
Assets/_Lifetopia/Scripts/Player/CharacterAnimationSet.cs (+ .meta)
Assets/_Lifetopia/Scripts/Player/SpriteCharacterView.cs (+ .meta)
Assets/_Lifetopia/Tests/EditMode/SpriteFrameMathTests.cs (+ .meta)
Assets/WebGLTemplates/LifetopiaResponsive/index.html
CHANGELOG.md
Documentation/ASSET-REGISTER.csv
Documentation/BACKLOG.md
Documentation/DECISIONS.md
Documentation/DEVELOPMENT-ROADMAP.md
Documentation/Progress/M2-A0-static.md
Documentation/Progress/M2-A0-02-art-brief.md
Documentation/Progress/M2-art-calibration-final.md
```
**Tidak** di-commit: semua PNG, `Data/`, `Prefabs/A0`, `Tilemaps/A0`, `ArtCalibration.unity`, `Web.asset`,
screenshot (`M2-A0-screenshots/`, `M2-final-screenshots/`), `Builds/`.

Prosedur (stage per file, **jangan** `git add -A` / `git add .`):
```
git add Assets/_Lifetopia/Scripts/Player/SpriteFrameMath.cs Assets/_Lifetopia/Scripts/Player/SpriteFrameMath.cs.meta \
        Assets/_Lifetopia/Scripts/Player/CharacterAnimationSet.cs Assets/_Lifetopia/Scripts/Player/CharacterAnimationSet.cs.meta \
        Assets/_Lifetopia/Scripts/Player/SpriteCharacterView.cs Assets/_Lifetopia/Scripts/Player/SpriteCharacterView.cs.meta \
        Assets/_Lifetopia/Tests/EditMode/SpriteFrameMathTests.cs Assets/_Lifetopia/Tests/EditMode/SpriteFrameMathTests.cs.meta \
        Assets/WebGLTemplates/LifetopiaResponsive/index.html CHANGELOG.md \
        Documentation/ASSET-REGISTER.csv Documentation/BACKLOG.md Documentation/DECISIONS.md Documentation/DEVELOPMENT-ROADMAP.md \
        Documentation/Progress/M2-A0-static.md Documentation/Progress/M2-A0-02-art-brief.md Documentation/Progress/M2-art-calibration-final.md
git diff --cached --name-only            # harus persis 17 file di atas
git diff --cached --name-only | grep -i -E "\.png|/Art/|Prefabs/A0|Tilemaps/A0|Data/|ArtCalibration|Web.asset"   # harus kosong
git commit                               # pesan: lihat laporan akhir
```

### 13.5 Bila izin art nanti diberikan
1. Hapus blok "M2 art" dari `.git/info/exclude`.
2. Cek kuota Git LFS (PNG sudah tercakup aturan LFS di `.gitattributes`).
3. Commit terpisah: PNG + .meta, `Data/`, `Prefabs/A0`, `Tilemaps/A0`, `ArtCalibration.unity`, dan `Web.asset`
   (scene list), **bersama-sama** agar tidak pernah ada referensi yang hilang.
4. Sampai saat itu, jangan commit art di branch lokal yang suatu saat bisa ter-push.
