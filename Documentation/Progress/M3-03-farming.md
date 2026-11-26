# M3.3 — Farming System + Gameplay Asset Pack 01 (Batch A & B)

Tanggal: 26 November 2026 · Basis: commit `dc327aa` · Status: **SELESAI — disetujui founder 26-11-2026.** Production `https://play.lifetopiaworld.io` = deployment `6cd1a5bf` (Build ID `M3-03-20261126-1741`). Founder-reported playtest (preview + HP nyata) 12/12 lulus. Lihat §14.

Ruang lingkup: farming (6 petak, Tropical Fruit / nanas), import aset item & UI original, local-only art binding. **Tidak** dikerjakan: fishing (M3.4), market, housing, multiplayer, blockchain, akun, database, repository `lifetopia-platform`.

Sumber yang dibaca: `START-HERE.md`, `IMPLEMENTATION-PROMPT.md`, `asset-manifest.json`, `asset-register.csv`, `QC.txt`, `preview.png` (paket `lifetopia-gameplay-assets-01-ab`), CLAUDE.md, GDD 2.0 (§03, §04 Farming/Inventory/Waktu, §05 Save), Art Bible (§03 skala/sorting, §05, §08 import), DECISIONS, laporan M3.1/M3.2/M3.2-D.

---

## 1. Ringkasan hasil

| Kebutuhan | Hasil |
|---|---|
| Import aset item & UI original | ✅ 22 dari 33 PNG (8 ikon item, 7 ikon UI, 3 soil, 4 fase nanas), 64 PPU, pivot manifest, Bilinear, no mipmap, Clamp, alpha, uncompressed |
| Ganti placeholder inventory, hotbar, coin, backpack, time HUD | ✅ lewat **local-only binding** (scene lokal); scene publik tetap placeholder |
| 6 petak dekat rumah | ✅ 3 × 2 petak di barat daya rumah, selatan jalan utama, tidak menghalangi jalan |
| Cangkul, tanam, siram, tumbuh, panen | ✅ `FarmActions` + `FarmFieldHost` |
| Integrasi Inventory, Hotbar, Game Time | ✅ hotbar aktif menentukan aksi; bibit dikonsumsi dari inventory; pertumbuhan = `IGameClock.ActivePlaySeconds` |
| Nanas sebagai visual Tropical Fruit, ID tetap | ✅ crop ID `tropical_fruit`, item `seed_tropical` → `crop_tropical_fruit` (ID & harga tidak berubah) |
| Tanpa duplikasi/kehilangan item | ✅ validasi dulu baru ubah (all-or-nothing); diuji EditMode + Play Mode |
| Fitur & artwork lama dipertahankan | ✅ regresi EditMode 116 lama lulus; movement, kamera, HUD, day/night, bag, dialog diuji |
| PC + mobile | ✅ keyboard (E, 1-5, B) + tombol Interact sentuh diuji |
| Testing + dokumentasi | ✅ §8, laporan ini |

## 2. Arsitektur

```
Data (ScriptableObject, tracked, tanpa art)      Runtime state (C# murni, diuji EditMode)
  CropDefinition  crop_tropical_fruit.asset        FarmPlot        Tilled + stage (Empty/Planted/Watered/Ready)
  FarmingConfig   FarmingConfig.asset              FarmField       6 plot independen + FarmFieldData (save)
                                                   FarmActions     Till/Plant/Water/Harvest/Resolve/Perform
Visual (local-only asset atau placeholder)         FarmText        teks prompt/pesan English
  FarmVisualSet   FarmVisualSet_Pack01.asset (LOKAL)
  FarmPlaceholderSprites (bentuk prosedural, publik)
Scene components
  FarmFieldHost   pemilik state; terhubung PlayerProfileHost, GameClockHost, InteractionDetector
  FarmPlotView    IInteractable + IPromptChangeNotifier; soil/crop/indikator/highlight renderer
  FarmDebugTools  DEV: Shift+F9 = +30 s pertumbuhan (Editor/Development Build saja)
Ikon (local-only binding)
  ItemIcons (registry) ← ItemIconOverrides_Pack01.asset (LOKAL) ← LocalArtBindings (hanya di GameplayPrototype)
  ItemSlotView memakai ItemIcons.Get(item): override per stable ID → ItemDefinition.Icon → placeholder
```

Alur waktu: `GameClockHost` → `IGameClock.ActivePlaySeconds` (detik nyata selama jam **berjalan**; berhenti saat bag/dialog terbuka, fokus hilang, `timeScale 0`, `PauseClock`). Tidak terpengaruh percepatan DEV (Shift+F7) maupun lompatan jam (Shift+F1…F6). `FarmFieldHost` memperbarui visual pada event `MinuteChanged` (bukan timer per frame per petak); status Ready dihitung saat dibutuhkan (`now - wateredAt >= 120`).

## 3. State machine petak

Status pertumbuhan GDD tetap 4: **Empty, Planted, Watered, Ready**. Cangkul memakai **flag terpisah `Tilled`**.

| Dari | Aksi (item terpilih di hotbar) | Ke | Catatan |
|---|---|---|---|
| Empty, belum dicangkul | Hoe → *Till soil* | Empty, Tilled | alat tidak habis |
| Empty, Tilled | Tropical Seeds → *Plant seeds* | Planted | tepat 1 bibit dikonsumsi **setelah** validasi |
| Planted | Watering Can → *Water crop* | Watered | hanya sekali; menyiram ulang tidak me-reset waktu |
| Watered | (tunggu 120 s aktif) | Ready | tidak layu |
| Ready | item apa pun → *Harvest Tropical Fruit* | Empty, Tilled | +1 `crop_tropical_fruit`; jika tas penuh → tetap Ready |

Aksi tidak valid tidak mengubah apa pun dan menampilkan pesan English (contoh: "Select the Hoe to till this soil.", "Inventory full. Tropical Fruit stays on the plot.").

**Usulan desain hoe/soil reset (keputusan founder):** setelah panen, petak **tetap tilled** (`keepTilledAfterHarvest = true`) agar loop tanam ulang cepat. Alternatif `false`: petak kembali belum dicangkul dan perlu Hoe lagi (sprite `soil_harvested` dari paket bisa dipakai untuk status ini; belum diimpor).

**Mapping fase visual:** Planted → `crop_pineapple_01_seeded`; Watered 0–50 % → `02_sprout`; Watered 50–100 % → `03_young`; Ready → `04_ready`. Soil: belum dicangkul → `soil_unworked`; tilled → `soil_tilled_dry`; Watered/Ready → `soil_tilled_wet`. Ambang 50 % diatur di `FarmVisualSet.youngAtFraction`.

**Indikator (tidak hanya warna):** Planted → ikon tetes air (`ui_water_drop`); Ready → centang (`ui_check`); badge kecil di sudut kanan atas petaknya, layer `Overhead` (tidak ikut gelap saat malam). Petak yang sedang ditarget mendapat outline. Prompt menyebut aksi atau hint ("Growing - 1:05 left", "Needs water: select Watering Can").

## 4. Konfigurasi (Inspector)

- **Crop:** `Assets/_Lifetopia/Data/Farming/crop_tropical_fruit.asset` — Crop Id (`tropical_fruit`, jangan diganti), Seed Item, Harvest Item, Harvest Quantity (1), Grow Seconds (120 detik waktu bermain aktif).
- **Aturan farm:** `Data/Farming/FarmingConfig.asset` — Hoe Item, Watering Can Item, Crops (daftar), Keep Tilled After Harvest.
- **Petak:** scene → objek `Farm` (komponen `FarmFieldHost`: Config, Visuals, Clock Host, Profile Host, Detector, Plots[6]). Setiap `FarmPlot_n` adalah instance prefab `Prefabs/World/FarmPlot.prefab`; pindahkan posisinya di Scene View. Tambah petak = tambah instance + isi ke array Plots.
- **Visual (lokal):** `Data/LocalArt/FarmVisualSet_Pack01.asset` — sprite soil, fase per crop ID, ambang young, sprite & skala indikator. Kosongkan field = placeholder.
- **Ikon (lokal):** `Data/LocalArt/ItemIconOverrides_Pack01.asset` (item ID → sprite) dan objek `LocalArtBindings` di GameplayPrototype (ikon Bag/Close/Coin dan ikon periode Morning/Daytime/Sunset = sun, Night = moon).

**Konsep untuk founder:** *ScriptableObject* adalah file data di Project (bukan objek di scene) — dipakai untuk aturan yang dibaca game. *Runtime state* (isi petak saat bermain) adalah objek C# biasa yang dibuat saat Play, sehingga file data tidak pernah berubah ketika bermain.

## 5. Aset yang dipakai

| Kelompok | File (di `Assets/_Lifetopia/Art/GameplayPack01/`, LOKAL) |
|---|---|
| Ikon item (8) | `Items/tool_hoe, tool_watering_can, tool_fishing_rod, seed_tropical, crop_tropical_fruit, fish_reef, material_driftwood, furniture_wooden_stool` |
| Ikon UI (7) | `UI/ui_coin, ui_backpack, ui_close, ui_sun, ui_moon, ui_water_drop, ui_check` |
| Soil (3) | `Soil/soil_unworked, soil_tilled_dry, soil_tilled_wet` |
| Crop nanas (4) | `Crops/Pineapple/crop_pineapple_01_seeded … 04_ready` |

Tidak diimpor (tetap di folder paket): `ui_settings`, `ui_sprout`, `soil_harvested`, tomat (4), pepaya (4). Tidak ada item ID, harga, resep, atau mekanik baru. File art lama tidak ditimpa.

## 6. Pemisahan publik / lokal

| Lokal saja (`.git/info/exclude`) | Publik (tracked) |
|---|---|
| `Art/GameplayPack01/` (22 PNG + meta) | Semua script farming, `ItemIcons`, `ItemIconOverrides` (class), `LocalArtBindings` (class) |
| `Data/LocalArt/` (ItemIconOverrides_Pack01, FarmVisualSet_Pack01) | `Data/Farming/` (crop + config, tanpa art) |
| `Scenes/GameplayPrototype.unity` (memakai visual lokal + LocalArtBindings) | `Prefabs/World/FarmPlot.prefab`, `InventoryHUD.prefab` (child `Icon` nonaktif) |
| `Documentation/Progress/M3-03-screenshots-local/` | `Scenes/FirstPlayable.unity` (6 petak, placeholder) |

Verifikasi: GUID dari 31 file meta lokal dicari di FirstPlayable, FarmPlot.prefab, InventoryHUD.prefab, crop & config → **0 referensi**. GameplayPrototype (lokal) memuat 7 referensi lokal (sesuai rencana). Build Public dibuat terpisah (§8.5).

## 7. File baru dan perubahan

Baru (publik): `Scripts/Farming/` (CropDefinition, FarmingConfig, FarmPlot, FarmActions, FarmField, FarmText, FarmVisualSet, FarmPlaceholderSprites, FarmPlotView, FarmFieldHost), `Scripts/Items/ItemIcons.cs`, `Scripts/Items/ItemIconOverrides.cs`, `Scripts/UI/LocalArtBindings.cs`, `Scripts/Interaction/IPromptChangeNotifier.cs`, `Scripts/DevTools/FarmDebugTools.cs`, `Tests/EditMode/FarmingTests.cs`, `Data/Farming/*`, `Prefabs/World/FarmPlot.prefab` (+ meta).

Diubah: `ItemSlotView.cs` (ikon via `ItemIcons.Get`), `InteractionPromptUI.cs` (ikut `PromptChanged` target saat ini), `TimeHudUI.cs` (`SetPeriodSprites`), `InventoryHUD.prefab` (3 child `Icon` nonaktif), `FirstPlayable.unity` (Farm + 6 petak + FarmDebugTools).

Lokal: `GameplayPrototype.unity` (Farm + visual lokal + LocalArtBindings + FarmDebugTools); backup scene sebelum perubahan di `../lifetopia-m3-3-scene-backup-<tanggal>/`.

## 8. Hasil pengujian (aktual)

### 8.1 Kompilasi
Kompilasi offline (Roslyn bawaan Unity) runtime + test: 0 error. Kompilasi Unity Editor: 0 error, 0 warning.

### 8.2 EditMode — **146/146 lulus** (116 lama + 30 baru)

Test farming baru (`FarmingTests.cs`): siklus penuh; Ready tepat pada 120,0 s (119,999 → Watered); tanpa air tidak tumbuh; tidak layu; keepTilled=false; Hoe wajib & tidak diulang; alat tidak habis; tanam di tanah belum dicangkul/item bukan bibit/tanpa bibit/petak terisi → tidak ada bibit hilang; siram wajib Watering Can, hanya sekali, tidak me-reset; panen sebelum siap gagal; panen ganda → hanya 1 buah; tas penuh → tetap Ready, 0 item hilang, berhasil setelah ada slot kosong; panen dengan item apa pun; resolve aksi per item; pesan untuk item salah; bibit habis → slot hotbar kosong; 6 petak independen; pertumbuhan mengikuti active play seconds termasuk pause 500 s dan ganti hari (23:30 → Day 2); kebal speed DEV ×60 dan SetTime; roundtrip JSON 6 petak; data rusak (jumlah petak, crop asing, crop di tanah belum dicangkul, growth negatif, versi baru, null) ditolak tanpa perubahan; validasi config; mapping fase visual; placeholder semua status + pivot crop (0.5, 0.125); teks prompt English; urutan ikon override → definisi → null.

Satu kegagalan awal ada di **test** (asumsi salah: membuang 1 ikan tidak mengosongkan slot), bukan di kode; test diperbaiki dan kini memeriksa kedua kasus.

### 8.3 Play Mode — GameplayPrototype (artwork lokal), input keyboard disuntikkan

| Langkah | Hasil aktual |
|---|---|
| Berdiri di petak 1, hotbar 1 (Hoe) | prompt "[E] Till soil", outline aktif |
| E | `soil_unworked` → `soil_tilled_dry` |
| E lagi dengan Hoe | tidak berubah; toast "Select seeds in the hotbar to plant." |
| Hotbar 4 (seeds), E | Planted, `crop_pineapple_01_seeded`, indikator `ui_water_drop`, seeds 3 → **2** |
| E lagi | tetap Planted, seeds tetap 2 |
| Hotbar 2 (can), E | Watered, `soil_tilled_wet`, `02_sprout`, prompt "Growing - 2:00 left" |
| 3 s berjalan | +3,0 s pertumbuhan, prompt "1:57 left" |
| Tas terbuka 3 s | `paused=True`, pertumbuhan **tidak bertambah**; prompt disembunyikan |
| E saat tumbuh | toast "Tropical Fruit is still growing." |
| Shift+F9 ×2 (+60 s) | `03_young` (65,9 s) |
| Shift+F9 ×2 | Ready (120,0 s), `04_ready`, indikator `ui_check`, prompt "Harvest Tropical Fruit" |
| E | fruit 0 → **1**, toast "+1 Tropical Fruit", petak Empty + tilled |
| E lagi | fruit tetap 1 (tidak ganda) |
| Tas benar-benar penuh, panen | `InventoryFull`, petak tetap Ready, fruit 0, toast "Inventory full. Tropical Fruit stays on the plot." → setelah 1 slot kosong: berhasil, fruit 1 |
| Restore 6 status lewat `TryRestoreState` | 6 petak menampilkan unworked/tilled/planted/sprout/young/ready |
| Berjalan dari x −13,6 ke kanan 1 s | sampai x −9,76 → petak **tidak menghalangi** gerak (trigger) |
| Malam 22:00 | soil & crop ikut tint malam; indikator tetap terbaca; HUD ikon bulan |

### 8.4 Play Mode — FirstPlayable (publik, placeholder) dan touch

- Placeholder: hotbar "HOE/WC/FR/TS", tombol "Bag", lingkaran koin, petak bentuk prosedural (tetes biru = perlu air, belah ketupat kuning = siap panen). Tidak ada referensi art lokal.
- Touch: `TouchControlsVisibility` → AlwaysShow; event pointer UI ke tombol **Interact** → petak 2 Planted, seeds 3 → 2; prompt tanpa "[E]" pada mode sentuh; outline target tampil (844×390).

### 8.5 Web build

Dibuat dengan `Tools/Build-Web.ps1` setelah Unity Editor ditutup, berurutan (tidak paralel). **Belum di-deploy.**

| Build | Varian | Hasil | Durasi | RAM bebas (awal → terendah) | Ukuran (Brotli) |
|---|---|---|---|---|---|
| `Builds/Web/M3-03-20261126-1625` | Public (FirstPlayable, placeholder) | exit 0, verifikasi lulus, `contentMode: public-placeholder` | 11:22 | 3,09 → **0,34 GB** | 8,73 MiB (data.br 1.768.593 B) |
| `Builds/Web/M3-03-20261126-1637` | Local (GameplayPrototype, artwork) | exit 0, verifikasi lulus, `contentMode: local-art` | 2:47 | 3,12 → 0,65 GB | 9,72 MiB (data.br 2.807.866 B, wasm.br 7.276.331 B) |

Build Public lebih lama karena kompilasi IL2CPP ulang setelah kode baru; build Local memakai cache. RAM sempat 0,34 GB — masih aman, tetapi dekat batas (T-22/T-24).

**Uji Web build Local di browser (Playwright, server lokal 127.0.0.1, bukan Cloudflare):**

| Cek | Hasil |
|---|---|
| Loading / judul / info versi | 1,7 s (lokal), "Lifetopia World", "Development Build · v0.3.3 · M3.3 · Built 2026-11-26" |
| Console | 0 error, 6 warning engine `getInternalformatParameter` (sudah dikenal) |
| FPS desktop 1280×720 | 60,2 (frame terburuk 16,9 ms) |
| Jalan ke farm (A ~3 s), prompt | "[E] Till soil", outline petak 1 |
| Till → Plant (seeds 3 → 2) → Water | soil dry → seeded → wet + sprout; prompt "Growing - 1:39 left" pada ±20 s |
| Tas terbuka 10 s | jam tetap 08:40 dan hitung mundur tetap 1:39 → pertumbuhan **berhenti** |
| Tunggu real-time sampai 120 s aktif | young → ready + centang; "[E] Harvest Tropical Fruit" |
| E | Tropical Fruit 1 di tas (ikon nanas); E lagi tidak memberi buah kedua |
| Ikon UI | koin, backpack, close (X kayu), sun, 8 ikon item tampil dengan art original |
| Emulasi HP 844×390 (touch) | load 2,0 s, 58,6 FPS, 0 error; joystick, Interact, hotbar, HUD tampil; joystick menutupi kolom kiri petak saat farm di tepi layar (T-44) |

Screenshot: `M3-03-screenshots-local/web33_*.png` (lokal).

**Usulan deployment (menunggu persetujuan, belum dijalankan):**
```powershell
.\Tools\Deploy-Web.ps1 -SkipBuild -BuildPath "Builds/Web/M3-03-20261126-1637" -ArtworkApproved   # preview
```
Production tetap `002b0658` sampai ada persetujuan terpisah.

### 8.6 Console
0 error game, 0 warning game pada Editor dan Play Mode. 2 entri error berasal dari server Pipeline/MCP ("Cannot access a disposed object" saat request bertabrakan dengan reload Play Mode) — bukan dari game.

### 8.7 Screenshot Game View (lokal, tidak dipush)
`Documentation/Progress/M3-03-screenshots-local/`: `m33_01…08` (alur keyboard), `m33_10/12_all_stages_day*` (6 status siang, sebelum/sesudah perbaikan indikator), `m33_11_all_stages_night`, `m33_20_public_firstplayable_placeholders`, `m33_21_public_touch_844x390`.

### 8.8 Tidak diuji
- HP nyata dan gamepad untuk farming (perlu playtest founder; G-1/V-3).
- Drag & drop inventory di HP (V-2) — tidak berubah di M3.3.
- Durasi 120 s secara real-time penuh di Play Mode (dipercepat dengan Shift+F9; durasi tepat diuji di EditMode dengan clock runner nyata).

## 9. Bug yang ditemukan dan diperbaiki selama M3.3
1. **Indikator salah tempat:** semula 1,35 unit di atas pusat petak → tetes air tampil di jalan dan centang petak bawah jatuh di petak atas. Dipindah ke sudut kanan atas petaknya sendiri, skala dikecilkan.
2. **Test inventory penuh** (lihat §8.2).

## 10. Technical debt & catatan review visual
- **T-37** Crop 128 px (2 unit) lebih tinggi dari petak 1 unit; daun fase young/ready menutupi sebagian petak di belakangnya. Sesuai manifest; perlu review visual founder (opsi: skala crop 0,75 atau petak berjarak).
- **T-38** Semua sprite dunia memakai Sprite Sort Point **Center** (bukan Pivot seperti Art Bible). Crop mengikuti Center agar konsisten dengan pemain (ukuran & pivot sama → urutan setara). Penyeragaman ke Pivot butuh pengujian ulang seluruh prop.
- **T-39** Setelah stok bibit habis, slot hotbar dikosongkan (aturan M3.1); bibit baru harus di-assign ulang lewat tas. Usulan: ingat slot terakhir dan isi otomatis.
- **T-40** FirstPlayable: kanopi pohon placeholder (−13,5, −4,4) menutupi sebagian kolom kiri petak (hanya scene publik greybox).
- **T-41** Farm state belum disimpan (hook `CaptureState/TryRestoreState` siap; local save = milestone berikutnya, bersama `GameClockData` dan `PlayerProfileData`).
- **T-42** Tidak ada animasi aksi cangkul/siram/panen, suara, atau floating reward (GDD feedback); saat ini hanya pulse skala singkat + toast.
- **T-43** Ikon 128 px dipakai tanpa kompresi (kalibrasi); evaluasi atlas + kompresi setelah review (T-23).
- **T-44** Joystick virtual dapat menutupi petak kolom kiri bila farm berada di tepi kiri layar HP (kamera mengikuti pemain, jadi hanya sesaat).

## 11. Rekomendasi M3.4 (Fishing) — butuh persetujuan
Dock di selatan (GDD), item `fish_reef` + `tool_fishing_rod` sudah ada beserta ikon; minigame indikator ≤ 8 s, gagal tidak menghilangkan item; gunakan pola yang sama: data SO publik, state C# murni, `IGameClock` untuk cooldown, local-only visual set, test EditMode untuk transaksi. Sebelum itu disarankan: playtest farming di HP, keputusan T-37 dan reset soil.

## 12. Langkah reproduksi manual (founder)
1. Buka `GameplayPrototype`, Play. Jalan ke barat daya rumah (6 petak di selatan jalan).
2. Tekan 1 (Hoe), berdiri di petak, E → tanah dicangkul.
3. Tekan 4 (Tropical Seeds), E → bibit tertanam (jumlah bibit −1).
4. Tekan 2 (Watering Can), E → tanah basah, tunas muncul, prompt menghitung mundur 2:00.
5. Tunggu 2 menit bermain (atau Shift+F9 = +30 s), E → +1 Tropical Fruit.
6. Coba buka tas (B) saat tumbuh: hitungan berhenti.

---

## 13. Revisi setelah review founder (26-11-2026)

### 13.1 Keputusan founder dan penerapannya

| Keputusan | Penerapan |
|---|---|
| Tanaman dewasa 100 %, ukuran tumbuh natural per fase (seed 10–15 %, sprout 25–35 %, growing 60–75 %, ready 100 %), bisa diatur di Inspector, tanpa pengecilan seragam/ganda | Skala per fase di `FarmVisualSet` → Crops → *Seeded/Sprout/Young/Ready Scale* (dikalikan dengan ukuran sprite; ≤ 0 = 1). Nilai dihitung dari ukuran piksel terlihat sprite (lihat §13.2). PPU 64, pivot dasar, ukuran petak, collider, interaksi tidak berubah |
| Setelah panen tanah tetap tercangkul | Sudah (`keepTilledAfterHarvest = true`), tetap |
| Hotbar mempertahankan referensi bibit saat 0; ikon redup + "0"; aktif otomatis saat bibit didapat; kecuali slot diganti/dikosongkan manual | `Hotbar` tidak lagi menghapus slot saat jumlah 0; `IsAvailable` / `SelectedAvailableItem`; `HotbarUI` menampilkan ikon alpha 0,35 + "0"; farming memakai item hanya bila jumlah > 0; save memuat referensi 0 lewat `Hotbar.Restore` |
| Uji ulang semua | §13.3 |
| Backlog animasi aksi, SFX, simpan data petak | T-41, T-42, T-45 |

### 13.2 Skala fase tanaman (diukur, bukan ditebak)

Ukuran piksel terlihat (alpha > 16) tiap sprite 128 × 128, dibanding tanaman dewasa (dimensi terbesar 103 px):

| Fase | Sprite terlihat | Relatif tanpa skala | Skala Inspector | Hasil terlihat | Target |
|---|---|---|---|---|---|
| Seed (`01_seeded`) | 19 × 14 px | 18 % | **0,68** | 12,5 % | 10–15 % |
| Sprout (`02_sprout`) | 38 × 33 px | 37 % | **0,81** | 30 % | 25–35 % |
| Growing (`03_young`) | 89 × 73 px | 86 % (lebih tinggi dari ready!) | **0,78** | 67 % (tinggi 57 px < 66 px ready) | 60–75 % |
| Ready (`04_ready`) | 103 × 66 px | 100 % | **1,00** | 100 % | 100 % |

Skala diterapkan pada transform renderer crop yang berporos di pivot dasar (0.5, 0.125), sehingga dasar tanaman tetap di pusat petak. Placeholder publik tidak diskalakan (bentuknya sudah bertambah besar per fase).

### 13.3 Perbaikan tambahan yang ditemukan saat uji ulang

1. **Y-sort crop:** pemain, rumah, dan pohon memakai *Sorting Group* (diurutkan dari posisi root = kaki). Crop semula Sort Point **Center** → tanaman tinggi bisa salah urut dengan pemain. Diubah ke **Pivot** (dasar tanaman), sesuai Art Bible. Diverifikasi: pemain di utara tanaman dewasa tertutup daun; di selatan tampil di depan (`v2_ysort_*`).
2. **Keterbacaan badge:** centang hijau menyatu dengan daun nanas. Ditambahkan latar lingkaran krem bergaris cokelat (prosedural, tanpa art baru) di belakang badge.
3. **Posisi farm:** kanopi pohon kelapa barat menutupi kolom kiri (fase bibit tak terlihat) dan semak hibiscus menutupi kolom kanan bila digeser. Batas sprite diukur; farm dipindah ke tepi selatan jalan utama: pusat x −12/−11/−10, y −1,5/−2,5 (kedua scene). Tidak ada collider padat di area farm; petak tetap trigger.

### 13.4 Hasil uji ulang (aktual)

- **EditMode: 149/149 lulus** (146 + 3 baru: hotbar menyimpan referensi 0 & aktif lagi; referensi bisa diganti/dikosongkan manual; save memuat referensi jumlah 0; skala fase). Test lama `ClearsSlot_WhenItemRunsOut` diganti perilaku baru; test save yang dulu menolak referensi hotbar tanpa item kini menerima referensi jumlah 0 (tidak memberi item) dan tetap menolak ID tak dikenal & duplikat.
- **Play Mode GameplayPrototype (input keyboard disuntikkan, posisi farm final):**

| Langkah | Hasil |
|---|---|
| Hoe → E | tilled; prompt "Ready to plant: select seeds" |
| Seeds → E | Planted, `01_seeded` skala 0,68, seeds 3 → 2, badge tetes air |
| Can → E | Watered, `02_sprout` skala 0,81 |
| Tas 3 s | `paused=True`, pertumbuhan berhenti |
| +60 s | `03_young` skala 0,78 |
| +60 s | Ready, `04_ready` skala 1,00, badge centang |
| E / E lagi | fruit 0 → 1 / tetap 1; petak tetap tilled |
| Tanam sampai bibit 0 | slot 4 tetap `seed_tropical`, count 0, ikon alpha 0,35, label "0"; prompt "No seeds left"; E → "You have no seeds left.", petak tetap Empty, seeds tetap 0 |
| Dapat +1 bibit (dev F1) | slot aktif lagi (alpha 1,00, "1"), prompt "Plant seeds", tanam berhasil → kembali 0 & redup |
| Berdiri di tiap petak 1–6 | target = petak itu, outline aktif (6/6) |
| Jalan melintasi baris petak 1 s | x −13,60 → −9,84 (tidak terhalang) |
| Malam 22:00 | tanaman ikut gelap, badge tetap terbaca |

- **Play Mode FirstPlayable (publik):** placeholder + latar badge; tombol **Interact sentuh** menanam (seeds 3 → 2) pada 844 × 390.
- **Console:** 0 error, 0 warning.

### 13.5 Screenshot setiap fase (lokal, tidak dipush)

`Documentation/Progress/M3-03-screenshots-local/`: `v2_phase1_seed.png`, `v2_phase2_sprout.png`, `v2_phase3_growing.png`, `v2_phase4_ready.png`, `v2_final_all_phases_day_1280.png`, `v2_final_all_phases_night_1280.png`, `v2_ysort_player_behind_ready.png`, `v2_ysort_player_in_front_ready.png`, `v2_hotbar_zero_seeds.png`, `v2_hotbar_seed_reactivated.png`, `v2_public_touch_844x390.png`, serta `web_v2_*` dari preview.

### 13.6 Web build & preview

Command (disetujui founder): `.\Tools\Deploy-Web.ps1 -ArtworkApproved` (build Local + preview). Unity Editor ditutup sebelumnya.

| Item | Hasil |
|---|---|
| **Build ID** | `M3-03-20261126-1741` (Local, GameplayPrototype, `contentMode: local-art`), v0.3.3 M3.3, build 2026-11-26T10:50:19Z |
| Build | exit 0, 8:38, RAM bebas 3,59 → **0,39 GB** terendah; 9,73 MiB Brotli (wasm.br 7.281.448 B, data.br 2.810.449 B) |
| **Preview** | deployment `c6332a73` → **https://c6332a73.lifetopia-play.pages.dev**, alias **https://preview.lifetopia-play.pages.dev** |
| Header | wasm → `application/wasm` + `br`; framework/data `br`; path tidak ada → 404; `version.json` 200 |
| **Production tidak berubah** | `wrangler pages deployment list`: production terbaru tetap `002b0658` (branch main); isi play.lifetopiaworld.io (index, version.json, loader, framework, wasm, data) **MD5 identik** dengan build `M3-02-20261126-1527` |

**Bug skrip ditemukan & diperbaiki:** skrip melaporkan "Production deployment id changed" padahal tidak berubah. Penyebab: di Windows PowerShell 5.1 `@(... | ConvertFrom-Json)` membungkus seluruh array deployment (kini ada 2 deployment production) menjadi satu elemen, sehingga ID yang dibandingkan adalah daftar. `Get-ProductionDeployment` diperbaiki (assign dulu, lalu index; validasi `Id` string); dry-run ulang menampilkan satu ID `002b0658`. Baris riwayat lokal yang tercatat `System.Object[]` dikoreksi.

**Uji preview di browser (Playwright, internet, build rilis tanpa tombol DEV, real time):**

| Cek | Hasil |
|---|---|
| Loading / versi | 6,8 s; "Lifetopia World · Development Build · v0.3.3 · M3.3 · Built 2026-11-26" |
| Console | 0 error, 6 warning engine yang sudah dikenal |
| FPS | desktop 60,1 (terburuk 17 ms); emulasi HP 844×390 60,3, load 7,2 s, 0 error |
| Fase | seed (kecil + badge tetes air) → sprout segera setelah disiram → growing (±70 s) → ready dengan centang pada ±123 s real time |
| Bibit habis | 3 petak ditanam → slot 4 redup "0" tetap pada bibit; E di petak kosong: "You have no seeds left." |
| Panen | +1 Tropical Fruit di tas (ikon nanas); E kedua tidak memberi buah |

Screenshot: `web_v2_00…09_*.png` (lokal).

---

## 14. Penutupan M3.3 (26-11-2026)

### 14.1 Founder-reported manual playtest

Dilaporkan founder pada 26-11-2026 untuk preview Cloudflare (deployment `c6332a73`, Build ID `M3-03-20261126-1741`), termasuk pengujian melalui HP. **12/12 lulus:**

1. Ikon inventory dan hotbar terlihat menarik.
2. Ukuran bibit kecil dan proporsional.
3. Pertumbuhan tanaman terlihat natural.
4. Ukuran tanaman dewasa sesuai keinginan.
5. Cangkul berfungsi dengan baik.
6. Menanam bibit berhasil.
7. Menyiram tanaman berhasil.
8. Tanaman dapat dipanen.
9. Hasil panen masuk inventory.
10. Tanah tetap tercangkul setelah panen.
11. Kontrol farming di HP nyaman.
12. Tidak terjadi crash atau bug yang mengganggu selama playtest.

Tidak dicatat (tidak diberikan): model HP, OS/browser, FPS, penggunaan RAM, waktu loading, durasi. Pengukuran performa perangkat tetap backlog (G-1).

### 14.2 Production deployment

| Item | Nilai |
|---|---|
| Build ID | `M3-03-20261126-1741` (Local, artwork original, v0.3.3 M3.3, built 2026-11-26T10:50:19Z) — **tidak di-build ulang** |
| Identik dengan preview yang diuji | ya: index.html, version.json, loader, framework, wasm, data, favicon di preview `c6332a73` = folder build (MD5) sebelum deploy |
| Command | `.\Tools\Deploy-Web.ps1 -Production -SkipBuild -BuildPath "Builds/Web/M3-03-20261126-1741" -ArtworkApproved -ConfirmProduction lifetopia-play` (konfirmasi non-interaktif atas persetujuan tertulis founder) |
| Deployment ID | **`6cd1a5bf-39e0-43ec-841d-c3f74ea5585a`** (Production, branch `main`), `https://6cd1a5bf.lifetopia-play.pages.dev` |
| Upload | 0 file baru (8 sudah ada dari preview) + `_headers` — byte yang sama dengan preview |
| Rollback target | `002b0658-edcc-4d0b-9c24-c978313f8132` (Build `M3-02-20261126-1527`, M3.2); sebelumnya `2cbfb971` (M3-02 lama) |

### 14.3 Verifikasi pasca-deploy (aktual)

| Cek | Hasil |
|---|---|
| `wrangler pages deployment list --environment production` | `6cd1a5bf` Production main (terbaru), lalu `002b0658`, `2cbfb971` |
| Isi `https://play.lifetopiaworld.io` | 7/7 file **MD5 identik** dengan `Builds/Web/M3-03-20261126-1741` |
| `version.json` | v0.3.3, M3.3, buildName `M3-03-20261126-1741`, contentMode `local-art` |
| Header (production, pages.dev, URL deployment) | `/` 200 `no-cache, no-transform`; wasm.br `application/wasm` + `br`; framework/data `br`; path tidak ada → 404 |
| Game (Playwright desktop) | judul "Lifetopia World"; info versi v0.3.3 M3.3; load 4,4 s; 60,1 FPS; 0 error console (6 warning engine yang dikenal); cangkul → tanam (seeds 3 → 2) → siram → "Growing - 2:00 left" |

### 14.4 Prosedur rollback (butuh persetujuan founder)

1. Dashboard: Cloudflare → Workers & Pages → `lifetopia-play` → Deployments → production `002b0658…` → `⋯` → *Rollback to this deployment*.
2. Atau: `.\Tools\Deploy-Web.ps1 -Production -SkipBuild -BuildPath "Builds/Web/M3-02-20261126-1527" -ArtworkApproved` (folder build dipertahankan di laptop).
3. Verifikasi: `.\Tools\Verify-WebBuild.ps1 -Url https://play.lifetopiaworld.io`.

### 14.5 Acceptance criteria

| Kriteria | Status |
|---|---|
| Aset A/B diimpor sesuai manifest, ID item tetap | ✅ |
| Placeholder diganti di inventory, hotbar, coin, backpack, time HUD (lokal) | ✅ |
| 6 petak dekat rumah, tidak menghalangi jalan | ✅ |
| Cangkul, tanam, siram, tumbuh 120 s aktif, panen | ✅ |
| Integrasi Inventory/Hotbar/Game Time; tanpa duplikasi/kehilangan item | ✅ (EditMode 149/149, Play Mode, Web) |
| Nanas sebagai visual Tropical Fruit | ✅ |
| Ukuran fase natural (keputusan founder) | ✅ seed 0,68 / sprout 0,81 / growing 0,78 / ready 1,0 |
| Tanah tetap tercangkul; hotbar menyimpan referensi 0 | ✅ |
| PC + mobile | ✅ founder playtest HP + Playwright |
| Repo publik bisa di-build tanpa artwork | ✅ build Public `M3-03-20261126-1625`; 0 GUID lokal di file publik |
| Preview → production setelah persetujuan | ✅ `c6332a73` → `6cd1a5bf` |
| Dokumentasi | ✅ |

**M3.3 ditutup.** Backlog lanjutan: T-41 penyimpanan data petak, T-42 animasi aksi farming, T-45 sound effects, V-3 gamepad, G-1 pengukuran performa perangkat.
