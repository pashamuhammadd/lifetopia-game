# M2 · A0 Static Calibration — laporan progres

Tanggal: 26 November 2026 · Milestone: **M2 Art Calibration — sub-tahap A0 static** · Status: **menunggu persetujuan visual founder**.
**M2 belum selesai**: A0-02 (idle 4 frame / walk 8 frame arah depan), arah L/R/U, avatar perempuan, layer modular, transisi terrain, dan review hak/lisensi aset masih pending.

## 1. Audit awal
| Item | Hasil |
| --- | --- |
| Git | `main` bersih, HEAD `5104ce0` (M1), sinkron dengan origin |
| Unity / CLI / MCP | 6000.3.29f1 ready; CLI 1.0.1 dan MCP berfungsi |
| Console | 0 error / 0 warning |
| Paket aset | `lifetopia-m2-a0-static`: 7 PNG runtime, manifest, register, QC, 2 preview. Tidak ada kode/metadata/secret |

## 2. Verifikasi PNG independen (sebelum import)
Pillow tidak terpasang, dan tidak dipasang; dipakai pembaca PNG kecil berbasis stdlib (zlib) di luar repo. Hasil cocok dengan QC.txt:

| File | Ukuran | Temuan |
| --- | --- | --- |
| char_player_male_idle_d_v01 | 128×128 RGBA | piksel terbawah tepat 16 px dari bawah (foot anchor 64,16 ✓); figur 104 px = 1.63 unit (Art Bible 96–112 px ✓) |
| prop_coconut_palm_v01 | 192×256 RGBA | dasar 16 px ✓; pangkal batang ≈ −0.05…−0.1 unit dari pivot (collider digeser −0.08) |
| bld_starter_house_v01 | 384×384 RGBA | dasar tangga 16 px ✓; hanya tangga s/d +0.28 u, pondasi batu +0.38 u, teras penuh mulai +0.66 u; lebar visual 5.5 u |
| prop_hibiscus_shrub_v01 | 128×128 RGBA | dasar 16 px ✓ |
| terrain_grass/path_01 | 64×64 | alpha 255 penuh (opaque) ✓ |
| ui_hud_panel_01 | 128×64 RGBA | **tanpa teks ter-bake** ✓ (teks "Lifetopia" di assembly-preview hanya contoh) |
Semua sprite world memiliki alpha 0 di tepi kanvas (tidak ada piksel terpotong).

## 3. Import (sesuai asset-manifest.json)
Disalin hanya 7 PNG runtime ke `Assets/_Lifetopia/Art/{Characters,Props,Buildings,Terrain,UI}/` (preview, manifest, QC tidak diimport). Skrip menolak menimpa file berbeda. Nilai di bawah dibaca kembali dari sprite hasil import:

| Asset | Texture | PPU | Pivot (normalized) | Mesh | Border |
| --- | --- | --- | --- | --- | --- |
| Karakter | 128×128 RGBA32 | 64 | (0.5, 0.125) | Tight | – |
| Palem | 192×256 RGBA32 | 64 | (0.5, 0.0625) | Tight | – |
| Rumah | 384×384 RGBA32 | 64 | (0.5, 0.0417) | Tight | – |
| Semak | 128×128 RGBA32 | 64 | (0.5, 0.125) | Tight | – |
| Grass / Path | 64×64 RGB24* | 64 | (0.5, 0.5) | Full Rect | – |
| HUD panel | 128×64 RGBA32 | 100 | (0.5, 0.5) | Full Rect | L16 B16 R16 T16 |

Semua: Sprite (Single), Bilinear, mipmap off, Alpha Is Transparency, Clamp, **Uncompressed** (khusus kalibrasi), NPOT none, max 2048.
\*Unity otomatis menyimpan tile opaque tanpa channel alpha.

## 4. Scene dan prefab
- `ArtCalibration.unity` = salinan `FirstPlayable.unity` (GUID baru). **FirstPlayable, prefab M1, dan seluruh file tracked tidak berubah** (diverifikasi `git status`).
- Prefab baru di `Assets/_Lifetopia/Prefabs/A0/` (prefab M1 tidak disentuh):

| Prefab | Struktur | Collider | Sorting |
| --- | --- | --- | --- |
| `Player_A0` | **Prefab Variant** dari `Player.prefab`: child `Sprite_A0` (sprite depan) aktif; `Body` greybox dan `Label_TEMP` nonaktif; `PlaceholderCharacterView` **disabled** (tanpa bob palsu); shadow 0.7×0.22 | sama dengan M1 (kaki r 0.28) | SortingGroup World |
| `House_A0` | root = **tepi depan teras**; child `Sprite` di (0, −0.6) sehingga dasar tangga 0.6 u di depan root | Box 4.3 × 2.4 (badan rumah saja; tangga bebas) | SortingGroup World |
| `CoconutPalm_A0` | root = pangkal batang | Circle r 0.25, offset (−0.08, 0.18) | SortingGroup World |
| `HibiscusShrub_A0` | dekoratif | **tanpa collider** | SortingGroup World |

- Tile baru `Assets/_Lifetopia/Tilemaps/A0/tile_grass_01_v01`, `tile_path_01_v01`; di ArtCalibration tile greybox ditukar (SwapTile). Air tetap greybox.
- Instance player di scene ditukar ke `Player_A0` dengan `ReplacePrefabAssetOfPrefabInstance`; semua referensi scene terverifikasi tetap terhubung (joystick, tombol, gate, action, target kamera, spawner, dialog, detector).
- Komposisi: rumah di (−9, 2.6) dengan tangga tepat di ujung jalan; 17 palem dan 16 semak dengan penempatan tidak beraturan di luar jalan (palem di tepi hutan utara, pantai selatan, sekitar kolam; semak di tepi jalan/plaza). **Tidak ada flip/mirror/scale** pada sprite (menjaga arah cahaya kiri-atas & 64 PPU).
- HUD: skin panel A0 (sliced) pada `InteractionPrompt`, `DialoguePanel`, dan latar `KeyboardHint` (teks hint sebelumnya sulit terbaca di atas art). Semua teks tetap teks live (Legacy Text), bahasa Inggris.
- Tetap greybox (belum ada aset A0): toko, air, papan INFO (target interaksi dipertahankan).

## 5. Karakter — batasan yang jujur
Hanya ada **satu sprite statis tampak depan**. Tidak ada animasi, flip, atau distorsi. `PlayerFacing` tetap menghitung arah (Down/Left/Right/Up) dan Idle/Walking — terverifikasi berubah saat bergerak — tetapi visual selalu tampak depan sampai sheet A0-02 dan arah L/R/U tersedia. Label debug status bisa diaktifkan lagi lewat `Player_A0/Label_TEMP`.

## 6. Hasil pengujian Editor (benar-benar dijalankan)
| Uji | Hasil |
| --- | --- |
| Kompilasi / Console | 0 error / 0 warning sepanjang sesi |
| Gerak W/S/A/D | |v| 4.00, facing benar, `flipX=False` |
| Diagonal | (2.83, 2.83), |v| 4.00 |
| Rumah depan | berhenti y 2.12 (prediksi 2.12) — tangga tetap bisa dipijak |
| Rumah samping | berhenti x −6.57 (prediksi −6.57) |
| Belakang rumah | dapat dijangkau; player tertutup rumah (screenshot 03) |
| Palem | collider pangkal batang membelokkan player (meluncur, tidak tersangkut) |
| Semak | dapat dilewati (non-blocking) |
| Air / batas timur | −7.92 / 15.72 |
| Kamera | tetap di-clamp ke tepi map |
| Interaksi & dialog | prompt, E buka/lanjut/tutup, Esc, gerak terblokir saat dialog |
| Focus hilang | A ditahan → |v| 0 |
| Joystick | |v| 4.00, dilepas → 0 |
| Y-sort | depan rumah ✓, belakang rumah ✓, belakang batang palem ✓ (screenshot 02/03/05) |

Satu langkah test awalnya gagal karena skrip test saya (menekan E tanpa melepas tombol sebelumnya); diulang dengan urutan benar dan lulus.

## 7. Temuan kalibrasi visual (untuk keputusan founder)
1. **Skala pintu**: pintu ±1.42 unit, karakter 1.63 unit → pintu ≈ 87% tinggi karakter (screenshot 04). Bila ingin pintu ≥ tinggi karakter, sebaiknya art rumah diekspor ulang lebih besar; tidak dilakukan scale transform agar 64 PPU konsisten.
2. **Pengulangan terrain**: tanpa seam antar tile; pola bintik/rumput berulang tiap 1 tile terlihat saat diperbesar, halus pada zoom gameplay. Perlu 2–3 varian tile untuk memecah pengulangan.
3. **Batas rumput–jalan** tegas tanpa transisi (sesuai paket); garis terang 1 px akibat filter Bilinear di perbatasan dua tile berbeda.
4. **Tepi transparan**: outline bersih; fringe terang sangat tipis pada beberapa tepi daun palem hanya pada zoom ekstrem.
5. **Konsistensi gaya**: toko, air, dan papan INFO greybox kontras dengan art A0.
6. Bayangan kontak karakter masih elips placeholder (art karakter tidak berisi bayangan).

## 8. Web build & performa
Build Profile **Web**: scene list kini `ArtCalibration` (pertama), `FirstPlayable`, `FoundationSmoke`. Output `Builds/Web/ArtCalibration` (di-ignore Git).

| Item | Hasil |
| --- | --- |
| BuildReport | **Succeeded, 0 error, 0 warning**, ±3.3 menit (cache IL2CPP) |
| Ukuran total | **9.46 MB** (M1: 9.09 MB) — wasm.br 7.28 MB, **data.br 2.07 MB** (M1 1.71 MB; naik karena tekstur A0 tanpa kompresi), framework 75 KB, loader 27 KB |
| Console browser | 0 error (6 warning engine `getInternalformatParameter`, sama seperti M0/M1) |
| Muat (localhost) | ±2.6 s sampai loading bar hilang (tanpa latensi jaringan nyata) |

Frame pacing gameplay (rAF di halaman, 5 detik sambil berjalan D/W/A, **setelah** splash Unity selesai). Mesin uji: laptop Windows, GPU terintegrasi Intel UHD Graphics (ANGLE/D3D11), Chromium 157 via Playwright:

| Viewport | Frame | Rata-rata | p95 | Maks | Frame > 33 ms |
| --- | --- | --- | --- | --- | --- |
| Desktop 1280×720 | 301 | 60.1 FPS (16.64 ms) | 16.9 ms | 17.4 ms | 0 |
| Emulasi HP 844×390 (`?touch=1`) | 301 | 60.2 FPS (16.62 ms) | 16.8 ms | 17.0 ms | 0 |

Catatan: pengukuran pertama tercampur splash screen "Made with Unity" sehingga dibuang dan diulang. Angka ini **bukan** performa HP; emulasi viewport hanya mengubah ukuran, bukan perangkat keras.
Temuan HP: pada 844×390 karakter hanya ±58 px CSS tinggi (1.63 dari 11 unit tinggi layar) — nilai keterbacaan di HP nyata; zoom kamera mobile bisa dipertimbangkan.
Splash "Made with Unity" tampil ±2–4 s di awal (Player Settings); tidak diubah.

## 9. Screenshot (lokal, dikecualikan dari Git lewat `.git/info/exclude`)
Folder `Documentation/Progress/M2-A0-screenshots/` — **tidak** untuk di-commit sebelum owner menyetujui publikasi art.

| File | Isi |
| --- | --- |
| `editor/01_spawn.png` | Play Mode, posisi spawn |
| `editor/02_house_front_on_stairs.png` | player di depan tangga (tergambar di depan rumah) |
| `editor/03_house_behind.png` | player di belakang rumah (tertutup) |
| `editor/04_house_door_scale_zoom.png` | zoom skala pintu vs karakter |
| `editor/05_palm_behind_vs_front.png` | player di belakang batang palem |
| `editor/06_overview.png` | overview desa (kamera size 11) |
| `editor/07_prompt_touch_hud.png` | prompt dengan skin HUD + kontrol sentuh |
| `editor/08_seam_zoom_grass_path.png` | zoom seam/pengulangan tile |
| `editor/09_halo_zoom_palm_edge.png` | zoom tepi transparan palem |
| `web/m2_web_desktop_*.png` | build Web desktop (spawn / setelah berjalan) |
| `web/m2_web_phone_landscape_emulated_*.png` | build Web 844×390 + kontrol sentuh |
Screenshot 02/03/06/08/09 memakai kamera bebas sementara (Play Mode) sehingga area di luar map (hijau tua) dapat terlihat; pada gameplay normal kamera tetap di-clamp.

## 10. File yang berubah
**Tracked (1):** `Assets/_Lifetopia/Settings/BuildProfiles/Web.asset` — scene list (+ArtCalibration) dan baris `preloadedAssets` (known issue M1, berubah oleh proses build Input System).

**Baru:**
```
Assets/_Lifetopia/Art/{Characters,Props,Buildings,Terrain,UI}/  7 PNG A0 + .meta
Assets/_Lifetopia/Prefabs/A0/   Player_A0 (variant), House_A0, CoconutPalm_A0, HibiscusShrub_A0
Assets/_Lifetopia/Tilemaps/A0/  tile_grass_01_v01, tile_path_01_v01
Assets/_Lifetopia/Scenes/ArtCalibration.unity
Documentation/Progress/M2-A0-static.md
```
**Tidak berubah:** `FirstPlayable.unity`, semua prefab M1, script C#, Project Settings, package, versi Unity, render pipeline. Kode baru A0-02 (tambahan, bukan perubahan M1) tercatat di §13.
**Belum di-commit / push** (sesuai instruksi). PNG art **tidak boleh di-push** tanpa persetujuan eksplisit owner (START-HERE paket A0).

## 11. Belum diuji / pending
- **HP nyata belum diuji** (M0, M1, A0).
- Animasi A0-02, arah L/R/U, avatar perempuan, modular layer, transisi terrain.
- Review hak/lisensi aset oleh owner — **art belum boleh di-push** ke GitHub tanpa izin eksplisit.
- Font final & TextMeshPro; pesan rotate untuk portrait.

## 12. Langkah review founder
1. Buka `Assets/_Lifetopia/Scenes/ArtCalibration`, Play, klik Game view, jelajahi desa.
2. Nilai: skala karakter vs pintu, kepadatan palem/semak, pengulangan rumput, keterbacaan HUD.
3. Bandingkan dengan `FirstPlayable` (M1 tetap greybox).
4. Beri keputusan: setujui / minta revisi art (mis. rumah lebih besar, varian tile) sebelum A0-02.

## 13. Lanjutan M2 (2026-11-26): A0 static disetujui founder → persiapan A0-02

Founder menyetujui tampilan A0 static dan meminta M2 dilanjutkan. Langkah M2 berikutnya adalah
A0-02 (idle 4 frame + walk-down 8 frame), tetapi **aset framenya belum ada** (`lifetopia-artwork`
kosong, paket A0 hanya berisi sprite statis). Karena itu yang dikerjakan hanya sisi Unity yang tidak
bergantung pada art, ditambah brief permintaan art. Tidak ada frame atau gerakan palsu yang dibuat.

**Kode baru** (namespace `Lifetopia.Player`, assembly `Lifetopia.Runtime`):

| File | Fungsi |
|---|---|
| `Scripts/Player/SpriteFrameMath.cs` | Matematika murni: indeks frame loop dari waktu/fps; aturan fallback clip (diminta → idle arah sama → idle Down). Satu frame = tidak pernah berganti. |
| `Scripts/Player/CharacterAnimationSet.cs` | ScriptableObject data animasi baked: clip `idle` & `walk`, masing-masing fps + daftar sprite D/L/R/U (urutan baris Art Bible). Read-only saat runtime. |
| `Scripts/Player/SpriteCharacterView.cs` | Membaca `PlayerFacing` (bukan input), memilih clip, mengganti `SpriteRenderer.sprite` per frame. Tidak pernah flip, scale, atau bob. Tanpa alokasi per frame. |
| `Tests/EditMode/SpriteFrameMathTests.cs` | 8 test baru (frame stepping 4 fps/8 frame, loop, fallback, kasus tanpa art). |

**Data & prefab:** `Assets/_Lifetopia/Data/Characters/anim_player_male_a0.asset` — idle Down = sprite
statis A0-01, clip lain kosong. `Player_A0/Sprite_A0` kini punya `SpriteCharacterView` (facing, renderer,
set terhubung). Player.prefab M1, `PlaceholderCharacterView`, dan scene FirstPlayable tidak disentuh.
Saat frame A0-02 datang, cukup isi array di asset tersebut — tanpa ubah kode/scene.

**Hasil uji (benar-benar dijalankan):**
- EditMode: **25/25 lulus** (17 lama + 8 baru).
- Play Mode regression ArtCalibration: gerak 4.00 semua arah + diagonal (2.83, 2.83); stop rumah depan
  y 2.12 / samping x −6.57; belakang rumah terjangkau; palem membelokkan; semak bisa dilewati; air & batas
  map menahan; dialog E buka/next/tutup; focus-lost reset. Di semua langkah sprite yang tampil
  `idle-Down#0`, flipX false, scale 1.00 — tidak ada animasi palsu.
  - Run pertama dialog-close & joystick gagal karena skrip test (E tidak dilepas; mouse ditekan di luar
    area joystick pada Game View 882 px). Setelah skrip diperbaiki: dialog lulus; joystick (tekan di
    tengah lalu geser kanan) vel 4.00, lepas → 0.
- Console Editor: 0 error / 0 warning (1 warning tool Pipeline saat test run terputus reload, bukan proyek).
- Web build: **Succeeded, 0 error / 0 warning**, 9,41 MB (sebelumnya 9,46 MB; variasi kompresi).
- Browser lokal 1280×720 (Intel UHD, sama seperti §8): **60,4 FPS**, 0 frame >33 ms, 0 error console
  (6 warning engine `getInternalformatParameter`, sama seperti sebelumnya). Screenshot
  `M2-A0-screenshots/web/m2_anim_view_web_walked.png`.
- HP nyata: **belum diuji**.

**Blocker:** frame A0-02 dari tim art. Spesifikasi lengkap ada di `M2-A0-02-art-brief.md`.
M2 **belum selesai**; belum ada commit/push.

## 14. A0-02 diterima dan diintegrasikan (2026-11-26)

Paket `lifetopia-m2-a0-02-animation` (48 frame, 4 arah) sudah diimport dan dipasang ke sistem §13 tanpa
membuat sistem animasi baru. Sprite statis A0-01 tetap ada di proyek (tidak dihapus) sebagai fallback.
Detail lengkap, hasil uji, dan pengecualian kalibrasi: **`M2-art-calibration-final.md`**.
