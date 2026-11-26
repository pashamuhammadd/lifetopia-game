# M3.4 — Fishing System & Environment Visual Update (Asset Pack 01 C/D/E)

Tanggal: 26 November 2026 · Basis: commit `0f35061` (M3.3) · Production saat mulai: `6cd1a5bf` (M3.3) · Status: **M3.4 DITUTUP 26-11-2026** — founder acceptance test 15/15 (founder-reported, Preview + HP nyata), EditMode 176/176; commit/push dan production deploy disetujui founder (detail §13).

Ruang lingkup: import 46 aset C/D/E, transisi terrain dual-grid, kios & papan info original, dekorasi, pantai + dermaga, Fishing System. **Tidak** dikerjakan: market/economy (M3.5), housing, quest, multiplayer, blockchain, local save lengkap, `lifetopia-platform`, database.

Sumber dibaca: paket `lifetopia-gameplay-assets-01-cde` (START-HERE, IMPLEMENTATION-PROMPT, asset-manifest.json, asset-register.csv, QC.txt, preview.png), CLAUDE.md, GDD 2.0 (§03 dock selatan, §04 Fishing ≤ 8 s, gagal tanpa kehilangan item), Art Bible, DECISIONS, ROADMAP, BACKLOG, laporan M3.3, WEB-DEPLOYMENT.md.

---

## 1. Ringkasan hasil

| Kebutuhan | Hasil |
|---|---|
| Import 46 aset sesuai manifest | ✅ 46/46 SHA256 cocok; 64 PPU, pivot manifest (prop/VFX/rope post), Full Rect, Bilinear, Clamp, no mipmap, uncompressed, alpha |
| Transisi jalan/rumput natural | ✅ dual-grid (NW1/NE2/SE4/SW8), layer visual setengah sel; 891 tile, **0 kombinasi tak didukung** |
| Kios & papan info original | ✅ `prop_market_stall`, `prop_info_sign` (label INFO tetap dari game, bukan dibake) |
| Dekorasi proporsional | ✅ 2 pot hibiscus, 1 peti kayu, 2 batu, 2 pagar tali, 4 tiang tali dermaga |
| Pantai tropis + dermaga | ✅ pasir 2 baris, shoreline 12 mask (tanpa 5/10), shallow water, dermaga 2 × 3 sel walkable |
| Fishing dengan rod & `fish_reef` existing | ✅ tanpa item ID baru, tanpa bait/biaya/RNG reward |
| Casting, menunggu gigitan, timing, hasil | ✅ state machine Idle → Casting → WaitingForBite → Timing → Success/Failure/Cancelled |
| Pelampung, riak, tali, percikan | ✅ 2 frame per efek + scale/fade; tali = sprite 1 px yang diregangkan |
| Gaya, PPU, pivot, Y-sort, fitur lama | ✅ regresi farming M3.3 identik; EditMode 173/173 |
| PC + HP | ✅ E/Esc, tombol Reel In/Cancel, tombol Interact sentuh |

## 2. Arsitektur

```
World (tracked code)                       Fishing (tracked code)
  DualGridMask   mask NW1/NE2/SE4/SW8,      FishingSettings / FishingConfig (SO)   tuning + rod/fish item
                 Classify() terrain/shore/  FishingSession   state machine, waktu sendiri, step dibatasi
                 water/unsupported          FishingRewards   CanStart + TryAward (1 ikan per attempt ID)
  DualGridTileSet (SO; instance LOKAL)      FishingText      teks English
  DualGridPainter  logical tilemaps →       FishingController  input, gate, fokus, view, panel
                   visual tilemap (-0.5)    FishingSpot      IInteractable di ujung dermaga
                                            FishingView      pelampung, tali, riak, percikan
UI: FishingPanelUI (prefab publik)          FishingVisualSet (SO; instance LOKAL) / FishingPlaceholderSprites
```

- **Grid logis tidak berubah fungsinya.** `Ground`, `Path`, `Water` (+ collider) tetap sumber kebenaran; `Dock` tilemap baru. Di scene lokal, `DualGridPainter` (komponen pada `Grid`) melukis tilemap `VisualDualGrid` (posisi lokal −0,5, −0,5) saat start, lalu menyembunyikan renderer Ground/Water/Path — collider tetap aktif.
- **Aturan vertex:** tanpa air → tile terrain `mask(path)`; semua air → `water_shallow`; campuran → shore `mask(air)` bila sudut darat semuanya pasir dan mask bukan 5/10; selain itu `Unsupported` (dicatat, fallback ke tile polos — tidak diam-diam memakai tepi yang salah). Di luar area peta, sel terdekat diulang.
- **Pasir = tekstur path (C15)**: pantai dan bibir kolam ditambahkan ke tilemap `Path` (walkable seperti sebelumnya), sehingga rumput tidak pernah menyentuh air (tidak ada tile rumput/air di paket).

## 3. Perubahan peta (kedua scene, geometri publik setara)

| Perubahan | Detail | Alasan |
|---|---|---|
| Dunia diperluas ke selatan | `WorldBounds` 32 × 26 di (0, −2) → y −15..11 (sebelumnya −11..11) | Dermaga + pelampung harus terlihat di atas panel mini game; kamera menempel ke batas dunia |
| Pantai | baris y −9 (dulu air) dan −8 menjadi pasir | Kontrak shore butuh pasir di antara rumput dan air |
| Laut | baris y −10..−15 air (`Water` + collider) | Ruang dermaga & pelampung |
| Bibir pasir kolam timur laut | 22 sel di sekeliling kolam (x 9..15, y 4..9) | Rumput tidak boleh menyentuh air |
| Dermaga | `Dock` tilemap x −1..0, y −10 (entry), −11 (deck), −12 (end); sel air di bawahnya dihapus dari collider | Walkable 2 sel; tepi dermaga dibatasi collider air |
| Kios | collider 2,9 × 1,1 (footprint dasar 3 × 2) | Mengikuti art baru |
| Dekorasi | prefab `Prefabs/World/Env/EnvProp_*` (collider footprint) di posisi yang sama pada kedua scene | Publik: placeholder bentuk; lokal: art |

Rumah, farm (6 petak), spawn, jalan utama, papan, kios tidak dipindah.

## 4. Fishing — aturan & alur

| Tahap | Detail |
|---|---|
| Mulai | pilih **Fishing Rod** di hotbar (jumlah > 0), berdiri di ujung dermaga (prompt "[E] Fish"), E / tombol Interact. Ditolak dengan pesan bila rod tidak dipilih ("Select the Fishing Rod in the hotbar to fish.") atau tas tidak muat ikan ("Inventory full. Make room for Reef Fish first.") |
| Casting | 0,3 s; pelampung melengkung ke anchor di air samping dermaga |
| Wait for a bite | 1,2–2,0 s acak (nilai acak diberikan controller, session deterministik); pelampung bergoyang, riak berulang |
| Reel in! | marker bolak-balik 0,75 siklus/s; zona hijau 0,48–0,76 (28 %), batas zona garis gelap; percikan saat gigitan |
| Hasil | tekan di zona → "Caught a Reef Fish!" +1 `fish_reef`; tekan sebelum gigitan → "Too early - the fish swam away"; di luar zona → "Missed"; tanpa tekan sampai 8 s → "Too late - the fish got away"; Esc/Cancel → "Cancelled" |
| Selesai | hasil tampil 1,4 s (Close untuk menutup lebih cepat), lalu panel tutup, gate dilepas, hotbar tampil lagi |

- **Satu hadiah per attempt:** `FishingRewards` mengingat attempt ID terakhir yang dibayar; spam tekan/award tidak menggandakan. Kapasitas dicek saat mulai dan saat award (atomik); bila penuh saat award → tidak ada ikan, tidak ada item hilang, pesan "Inventory full".
- **Rod tidak habis; gagal tidak mengambil item apa pun. Tidak ada coin otomatis.**
- **Input:** fishing memblokir gate (gerak & interaksi dunia berhenti, tas tidak bisa dibuka). Selama blok, E/tombol Interact datang sebagai `UiSubmit` → Reel in; Esc → Cancel. Tekan pada frame yang membuka fishing diabaikan. Semua jalur keluar (hasil, cancel, `OnDisable`) melepas gate.
- **Waktu mini game terpisah dari jam game:** session memakai waktunya sendiri (`Time.deltaTime`, tiap langkah ≤ 0,1 s), tidak maju saat fokus/tab hilang, sehingga tidak ada "catch-up". **Revisi founder (review M3.4):** jam game dan pertumbuhan tanaman **tetap berjalan** selama fishing, termasuk saat mini game. Fishing hanya mengunci gerakan/interaksi dunia (`gate.Block(this, pausesGameTime: false)`); tas dan dialog tetap menghentikan waktu seperti M3.2. Waktu tetap berhenti saat fokus/tab hilang, `timeScale 0`, atau dev pause.

**Konsep untuk founder:** *state machine* = daftar keadaan yang hanya boleh berpindah lewat aturan tertentu (misalnya dari "menunggu gigitan" tidak bisa langsung "tertangkap"). Dengan begitu setiap tombol hanya punya satu arti di setiap keadaan, dan hadiah tidak bisa dobel.

## 5. Konfigurasi (Inspector)

- `Assets/_Lifetopia/Data/Fishing/FishingConfig.asset` — Rod Item, Fish Item, Catch Quantity (1), Settings (cast, bite min/max, max attempt 8 s, kecepatan marker, pusat & lebar zona, max step), Result Seconds.
- Objek scene `Fishing` (FishingController): referensi config/profile/gate/input/detector/player/view/panel/spots. `Fishing/FishingSpot_DockEnd` (trigger 1,8 × 0,9) + `BobberAnchor` (1,85; −11,0).
- `Grid` → `DualGridPainter` (lokal): Tile Set, Target (`VisualDualGrid`), Path Sources, Water Sources (`Water`, `Dock`), Area, Hide Renderers.
- Prefab UI `Prefabs/UI/FishingPanel.prefab` (di `GameplayUI/SafeArea`), `Hide While Open` = Hotbar.
- Lokal: `Data/LocalArt/DualGridTileSet_Pack01.asset`, `FishingVisualSet_Pack01.asset`, `Data/LocalArt/Tiles/*` (16 terrain, 12 shore, water shallow/deep, dock entry/deck/end).

## 6. Mapping aset

| Aset | Pemakaian |
|---|---|
| `terrain_grass_path_00..15` | tile dual-grid mask 0..15 (00 rumput murni, 15 path/pasir murni) |
| `shore_sand_water_01..14` (12) | tile shore per mask air; 5 & 10 tidak ada → desain menghindari diagonal |
| `water_shallow` | laut & kolam |
| `water_deep` | cadangan (tile dibuat, tidak dipakai) |
| `dock_entry/deck/end` | tilemap `Dock` y −10/−11/−12 |
| `dock_rope_post` | 4 tiang di tepi luar dermaga (tanpa collider) |
| `prop_market_stall`, `prop_info_sign` | ganti visual placeholder kios/papan (scene lokal) |
| `prop_hibiscus_planter`, `prop_wood_crate`, `prop_tropical_rocks`, `prop_rope_fence` | dekorasi `EnvProp_*` |
| `fishing_bobber_idle/bite`, `fishing_ripple_01/02`, `fishing_splash_01/02` | `FishingVisualSet_Pack01` |
| Ikon `tool_fishing_rod`, `fish_reef` (Batch A) | hotbar/tas & ikon panel (sudah terpasang) |

## 7. Publik / lokal

| Lokal saja (dikecualikan Git) | Publik |
|---|---|
| `Art/GameplayPack01/{Terrain,Environment,Dock,Shore,Water,VFX}` (46 PNG) | Semua script baru, `FishingConfig.asset`, `FishingPanel.prefab`, `EnvProp_*` prefab, `tmp_tile_dock.asset` |
| `Data/LocalArt/` (tile set, visual set, 33 Tile) | `FirstPlayable.unity`: peta diperluas, pantai/dermaga, dekorasi placeholder, fishing dengan placeholder |
| `GameplayPrototype.unity` (dual-grid, art) | |
| `Documentation/Progress/M3-04-screenshots-local/` | |

Verifikasi: 196 GUID lokal dicari di semua file YAML publik yang berubah → **0 referensi**. Build Public dibuat terpisah (§8.5).

## 8. Hasil pengujian (aktual)

### 8.1 EditMode — **173/173 lulus** (149 + 24 baru)
Fishing: transisi state; bite delay di-clamp (termasuk NaN); tekan terlalu cepat (casting/menunggu); di zona = tangkap, di luar = meleset; batas zona inklusif; timeout 8 s; langkah besar dibatasi 0,1 s (tanpa catch-up) dan delta negatif/NaN/∞ diabaikan; tanpa re-entry, attempt selesai tidak berubah; cancel dari setiap state aktif; ping-pong marker; setting invalid ditolak. Reward: rod wajib dipilih & dimiliki; sibuk/penuh menolak mulai; tangkap = tepat 1 ikan, rod tidak habis; attempt sama tidak dibayar dua kali; gagal/cancel/timeout tidak memberi & tidak menghilangkan item; tas penuh saat award → tidak ada ikan, tidak ada item hilang, lalu berhasil setelah ada ruang; spam tekan = 1 ikan; teks English; validasi config. Dual-grid: bit sudut, sel di sekitar vertex, blok 3×3 (sudut luar, tepi, tengah, sudut cekung), klasifikasi terrain/shore/water/unsupported (rumput–air, diagonal 5/10).

### 8.2 Play Mode — GameplayPrototype (art lokal), input keyboard disuntikkan

| Uji | Hasil aktual |
|---|---|
| Dual-grid | 891 tile dilukis, 0 unsupported |
| Jalan ke selatan ke dermaga | berhenti di y −11,91 (ujung dermaga y −12) — tidak masuk air |
| Jalan timur / barat di deck | berhenti x 0,71 / −0,72 (tepi dermaga ±1,0 − radius 0,3) |
| Hoe dipilih, E di spot | prompt "Select Fishing Rod to fish"; toast hint; tidak mulai |
| Rod (3), E | prompt "Fish" → Casting, gate & jam game di-pause, hotbar disembunyikan |
| Sukses | Timing pada ±1,9 s; tekan saat marker 0,55–0,56 → Caught, fish 0 → 1, rod tetap 1, toast "+1 Reef Fish" |
| Terlalu cepat | 0,36 s → "Too early…", fish tetap |
| Meleset | marker 0,28 → "Missed" |
| Fokus hilang 2 s saat menunggu | elapsed 0,61 → 0,61 (tidak maju) |
| Esc | Cancelled; setelah tutup, gerak kembali (y −11,0 → −9,56), gate bebas |
| Timeout | Failure TooLate tepat 8,00 s |
| Spam E 4× di zona | fish +1 saja |
| Tas penuh (fish 99 + slot lain penuh) | tidak mulai; "Inventory full. Make room for Reef Fish first."; isi tas utuh |
| Malam 22:00 | pelampung/tali/riak tetap terlihat, panel terbaca |
| Regresi farming M3.3 | identik: skala fase 0,68/0,81/0,78/1,00, pause saat tas, panen 1×, bibit 0 redup + aktif lagi, 6/6 petak tertarget, berjalan melintasi petak |

### 8.3 Play Mode — FirstPlayable (publik, placeholder)
Peta/dermaga/collider sama; tombol **Interact sentuh** memulai fishing, tombol **Reel In** panel menangkap (fish 0 → 1), panel tutup dan gate bebas (844 × 390).

### 8.4 Console
0 error, 0 warning.

### 8.5 Web build & preview

Build dengan `Tools/Build-Web.ps1` / `Deploy-Web.ps1` setelah Unity Editor ditutup, berurutan.

| Build | Varian | Hasil | Durasi | RAM bebas (awal → terendah) | Ukuran |
|---|---|---|---|---|---|
| `M3-04-20261126-1844` | Public (FirstPlayable, placeholder) | exit 0, verifikasi lulus, `public-placeholder` | 8:08 | 3,1 → 0,57 GB | 8,71 MiB |
| **`M3-04-20261126-1852`** | Local (GameplayPrototype, artwork) | exit 0, verifikasi lulus, `local-art`, v0.3.4 M3.4 | 2:49 | 3,05 → 0,98 GB | 9,98 MiB |

**Preview (disetujui founder):** `.\Tools\Deploy-Web.ps1 -ArtworkApproved` → deployment **`4d322ee0`**, **https://preview.lifetopia-play.pages.dev** (langsung: https://4d322ee0.lifetopia-play.pages.dev).

| Cek | Hasil |
|---|---|
| Production tidak berubah | skrip: ID tetap `6cd1a5bf`; `wrangler pages deployment list` production terbaru `6cd1a5bf`; `play.lifetopiaworld.io/version.json` = `M3-03-20261126-1741` |
| Header preview | wasm.br `application/wasm` + `br`; data.br `br`; path tidak ada → 404; `version.json` = `M3-04-20261126-1852` v0.3.4 M3.4; isi wasm/data MD5 = folder build |
| Browser desktop 1280×720 | load 7,5 s; 60,2 FPS (terburuk 16,9 ms); 0 error, 6 warning engine yang dikenal |
| Jalan ke dermaga | S dari spawn ±2,7 s → berhenti di ujung dermaga; prompt "[E] Fish" setelah pilih rod (3), spot ter-highlight |
| 6 percobaan real-time (tekan pada jeda berbeda) | **3 tertangkap, 3 gagal** (Missed/terlalu cepat); tas: Reef Fish 3, rod tetap, coin tetap 100; toast "+1 Reef Fish" |
| Emulasi HP 844×390 (touch) | load 4,9 s, 59 FPS, 0 error; tombol **Interact** sentuh memulai fishing, panel muat (hint versi sentuh), ketukan **Reel In** diterima |

### 8.6 Screenshot lokal (tidak dipush)
`Documentation/Progress/M3-04-screenshots-local/`: `env_01_plaza_day`, `env_02_beach_dock_day`, `fish_00_dock_prompt`, `fish_01_timing`, `fish_02_caught`, `fish_03_too_early`, `fish_04_missed`, `fish_05_inventory_full`, `fish_06_night_timing`, `farm_regression_*`, `public_dock_touch_844x390`, `web_*` (preview).

### 8.7 Tidak diuji
HP nyata (G-1), gamepad (V-3), Safari iOS. Seam tile di GPU HP nyata. Animasi karakter memancing & SFX tidak ada (backlog).

## 9. Keputusan & risiko
Keputusan founder 26 Nov 2026 (review hasil awal M3.4):
1. **Perluasan dunia ke selatan sampai y −15 — disetujui.** Rumah, farm, plaza dan spawn tidak berpindah.
2. **Dual-grid + penyusunan shoreline dari paket — disetujui.** Art Bible §terrain dan DECISIONS diperbarui sesuai implementasi.
3. **Game time tetap berjalan selama fishing** (termasuk mini game); pertumbuhan tanaman ikut berjalan. Pemblokiran gerak dipisah dari pause waktu (lihat §12).
4. **Tuning tetap** (zona 28 %, 0,75 siklus/s, gigitan 1,2–2,0 s, cast 0,3 s, batas 8 s); semua diatur di Inspector `Data/Fishing/FishingConfig.asset`.
5. **Cozy:** gagal tidak menghilangkan coin, item, atau rod.

## 10. Technical debt / backlog baru
T-46 animasi karakter memancing + SFX fishing; T-47 gamepad untuk fishing (binding South/Start ada, belum diuji); T-48 deep water / variasi laut; T-49 dekorasi pantai tambahan & transisi pasir–rumput khusus (sekarang memakai tekstur path); T-50 tuning mini game setelah playtest; T-51 label "INFO" masih TextMesh placeholder di atas art papan.

## 11. Rekomendasi berikutnya
Playtest preview revisi di HP (fishing & jam berjalan). Setelah disetujui: commit + push M3.4, lalu (dengan persetujuan terpisah) production. M3.5 Market & Economy belum dimulai.

## 12. Revisi setelah review founder (26 Nov 2026)
**Perubahan kode**
- `Scripts/Core/GameplayInputGate.cs`: selain `BlockedChanged` (gerak/interaksi) kini ada `TimePauseChanged` + `PausesGameTime`. Overload baru `Block(source, pausesGameTime)`; `Block(source)` lama = pause waktu (perilaku tas/dialog tidak berubah).
- `Scripts/TimeSystem/GameClockHost.cs`: jam mendengarkan `TimePauseChanged`, bukan `BlockedChanged`.
- `Scripts/Fishing/FishingController.cs`: `gate.Block(this, pausesGameTime: false)`.
- `Tests/EditMode/GameplayInputGateTests.cs`: +3 test (menu pause waktu; fishing tidak; campuran fishing + dialog).
- Scene lokal: label "INFO" papan sedikit lebih gelap/tebal agar terbaca di atas art.
- Dokumen: DECISIONS (M3.4 disetujui + keputusan waktu), Art Bible (dual-grid menggantikan bitmask N/E/S/W), CHANGELOG, BACKLOG.

**Hasil uji ulang (aktual)**
| Uji | Hasil |
|---|---|
| EditMode | **176/176 lulus** (173 + 3) |
| Fishing (lokal, input keyboard disuntikkan) | sukses / terlalu cepat / meleset / timeout 8,01 s / Esc batal / spam = +1 / tas penuh menolak / alat salah → hint; hotbar disembunyikan & kembali; gate dilepas |
| Jam selama fishing | percobaan 1: active play **+2,55 s** dan pertumbuhan tanaman **+2,55 s**, `clockPaused=False` sepanjang semua percobaan; HUD 18:30 → 18:33 saat mini game (screenshot `v2_fish_sunset_clock_running`) |
| Fokus hilang saat fishing | jam **pause**, active +0,000 s dan mini game +0,000 s selama ±60 frame; pulih setelah fokus kembali |
| Tas saat fishing | tidak bisa dibuka (B diabaikan) |
| Tas/dialog (regresi M3.2) | buka 3 s → +0,00 menit; tutup → jalan lagi; fokus hilang, `timeScale 0`, dev pause, ×10, siang/malam & tint: identik M3.2 |
| Regresi M3.1 (45 langkah) | WASD/panah, tabrakan rumah & air (berhenti y −8,42), hotbar 1–5/numpad, tas B/I/Esc, drag mouse & sentuh, joystick, tas penuh, dialog papan: semua sesuai |
| Regresi farming M3.3 (sesi baru) | till → tanam → siram → tumbuh 4 fase (ukuran 0,68 / 0,81 / 0,78 / 1,00) → panen +1; 0 bibit = slot redup & "No seeds left"; 6 plot ter-highlight; jalan melintasi plot |
| Collision & Y-sort | dermaga: berhenti y −11,91, x ±0,71 (uji gerak). Y-sort: tidak ada kode/sorting yang diubah di revisi ini; dicek visual di screenshot baru (pemain di atas dermaga, props pantai & kios). Uji Y-sort rinci = putaran pertama M3.4 (§8.2) |
| Scene publik + sentuh | tombol **Interact** sentuh memulai fishing, ketuk saat di zona → **Caught**, ikan 0 → 1, jam +2,43 s tidak pause |
| Console | 0 error, 0 warning (setelah menghapus callback test saya yang gagal; bukan dari game) |

**Screenshot lokal baru:** `v2_env_market_sign_day`, `v2_env_sign_label`, `v2_env_beach_dock_day`, `v2_fish_sunset_clock_running`, `v2_fish_*` (skenario fishing).

**Web build & preview revisi**
| Item | Hasil |
|---|---|
| Build Local | **`M3-04-20261126-2103`** — `Tools/Build-Web.ps1 -Variant Local`, Unity Editor ditutup; exit 0; 9:20; RAM bebas terendah 0,48 GB; 9,98 MiB; verifikasi lulus; `version.json` v0.3.4 M3.4 `local-art` |
| Preview (disetujui founder) | `Deploy-Web.ps1 -SkipBuild -BuildPath Builds\Web\M3-04-20261126-2103 -ArtworkApproved` → deployment **`5aacffc2`**, https://preview.lifetopia-play.pages.dev (langsung https://5aacffc2.lifetopia-play.pages.dev); header wasm/br OK, 404 OK |
| Production | tidak berubah: skrip `6cd1a5bf`; `wrangler pages deployment list` production terbaru `6cd1a5bf`; `play.lifetopiaworld.io/version.json` = `M3-03-20261126-1741` |
| Desktop 1280×720 | 60,3 FPS, 0 error (6 warning engine yang dikenal); jalan S ke dermaga, pilih rod (3), E → panel "Reel in!", pelampung + tali; **jam 09:34 → 09:41 selama percobaan** (berjalan); percobaan dibiarkan habis waktu → rod & coin 100 tetap, prompt "[E] Fish" kembali |
| Emulasi HP 844×390 | build `M3-04-20261126-2103`, 55,1 FPS, 0 error; HUD/hotbar muat. Kontrol sentuh tidak diuji di browser pada putaran ini (tombol touch muncul setelah sentuhan pertama); sentuh diverifikasi di Editor (scene publik, di atas) dan pada preview putaran pertama |
| Tidak diuji | HP nyata, gamepad, Safari iOS |

Screenshot Web: `v2_web_dock_prompt`, `v2_web_fishing_timing`, `v2_web_after_attempt`, `v2_web_mobile_844x390`.

## 13. Founder acceptance & penutupan (26 Nov 2026)

### 13.1 Founder-reported manual playtest — 15/15 lulus
Dilakukan **oleh founder** lewat Cloudflare Preview (`5aacffc2`, build `M3-04-20261126-2103`), termasuk di **HP nyata**. Ini laporan founder, bukan pengukuran Claude; model HP, browser dan angka performa tidak dilaporkan (tetap backlog G-1).

| # | Pengujian | Hasil |
|---|---|---|
| 1 | Tepian rumput dan jalan terlihat natural | lulus |
| 2 | Kios pasar terlihat menarik | lulus |
| 3 | Dekorasi lingkungan sesuai art style | lulus |
| 4 | Pantai dan dermaga terlihat menarik | lulus |
| 5 | Karakter tidak bisa berjalan di atas air | lulus |
| 6 | Fishing rod dapat digunakan | lulus |
| 7 | Casting dan pelampung berfungsi | lulus |
| 8 | Mini game memancing menyenangkan | lulus |
| 9 | Tingkat kesulitan terasa nyaman | lulus |
| 10 | Ikan masuk inventory ketika berhasil | lulus |
| 11 | Tidak kehilangan item ketika gagal | lulus |
| 12 | Jam tetap berjalan selama memancing | lulus |
| 13 | Farming tetap berfungsi normal | lulus |
| 14 | Kontrol fishing di HP nyaman | lulus |
| 15 | Tidak ada crash atau bug yang mengganggu | lulus |

### 13.2 Pengujian aktual oleh Claude (ringkasan)
EditMode **176/176**; Play Mode lokal (fishing semua hasil, jam berjalan selama fishing, fokus hilang = pause, tas/dialog tetap pause, regresi M3.1 45 langkah, regresi farming M3.3, collision dermaga); scene publik + sentuh (Interact/Reel In → Caught); Web build Local `M3-04-20261126-2103` + Preview `5aacffc2` (desktop 60 FPS 0 error, jam berjalan saat fishing; emulasi HP 844×390 55 FPS 0 error). Rincian §8 dan §12.

**Tidak diuji oleh Claude:** HP nyata (hanya laporan founder), gamepad, Safari iOS, pengukuran performa perangkat.

### 13.3 Baseline konfigurasi fishing (disetujui)
`Data/Fishing/FishingConfig.asset`: cast 0,3 s · gigitan 1,2–2,0 s · batas percobaan 8 s · penanda 0,75 siklus/s · pusat zona 0,62 · lebar zona 0,28 (28 %) · langkah maks 0,1 s · hasil tampil 1,4 s · 1 `fish_reef` per tangkapan · rod tidak pernah berkurang. Perubahan berikutnya butuh persetujuan founder + playtest (T-50).

### 13.4 Dipertahankan
Perluasan peta sampai y −15; dual-grid terrain/shoreline; game time & pertumbuhan tanaman tetap berjalan selama fishing.

### 13.5 Backlog yang dipertahankan
T-41 penyimpanan progres · T-42/T-46 animasi aksi farming & fishing · T-45/T-46b sound effects · V-3/T-47 gamepad · G-1 pengukuran performa perangkat + Safari iOS · T-48 air dalam · T-50 balancing mini game.

### 13.6 Commit, push & production (disetujui founder)
| Item | Nilai |
|---|---|
| Commit M3.4 | **`7a7ee0b`** `feat(m3.4): fishing system, dual-grid terrain, beach and dock, environment props` — 56 file di-stage eksplisit, di-push ke `origin/main` (`0f35061..7a7ee0b`). Commit docs penutupan menyusul (lihat `git log`) |
| Pemeriksaan sebelum commit | GUID: 401 file publik, 0 tidak ter-resolve (tidak ada referensi ke art lokal); tidak ada PNG/master art/screenshot/backup/build/scene privat yang di-stage (`Web.asset` tetap lokal); scan secret/kredensial/path pribadi bersih |
| Build ID | **`M3-04-20261126-2103`** (build yang sama dengan preview `5aacffc2` yang diuji founder; tidak di-build ulang). `version.json` menulis `commit 0f35061, dirty true` karena build dibuat sebelum commit; isi kode = commit `7a7ee0b` |
| Identitas file | MD5 wasm.br, data.br, framework.js.br, loader.js, index.html, version.json: folder build = preview `5aacffc2` = production — **identik** |
| Command | `.\Tools\Deploy-Web.ps1 -Production -SkipBuild -BuildPath "Builds/Web/M3-04-20261126-2103" -ArtworkApproved -ConfirmProduction lifetopia-play` (konfirmasi production non-interaktif, seperti M3.3) |
| Deployment production | **`5784f2eb-e02f-4972-b648-063ba650cf31`** (https://5784f2eb.lifetopia-play.pages.dev); `wrangler pages deployment list` production terbaru = `5784f2eb` (source `7a7ee0b`) |
| Rollback target | **`6cd1a5bf`** (M3.3, build `M3-03-20261126-1741`) — prosedur di `WEB-DEPLOYMENT.md` §8 |
| Verifikasi | Pemeriksaan pertama skrip: custom domain sesaat 404 untuk 2 file (propagasi edge), URL deployment & pages.dev 200. ±1 menit kemudian: semua 200; `Verify-WebBuild.ps1 -Url https://play.lifetopiaworld.io` **lulus**; `version.json` = `M3-04-20261126-2103` v0.3.4 M3.4; header wasm `application/wasm` + `br`, `no-cache, no-transform`, `nosniff`, `noindex`; path tidak ada → 404 |
| Gameplay production (desktop 1280×720, browser otomatis) | 60,4 FPS, 0 error (6 warning engine yang dikenal); jalan ke dermaga, pilih rod, E → panel "Reel in!" + pelampung; percobaan dibiarkan habis → rod & coin 100 tetap; jam berjalan (08:13 → 08:23); tas terbuka (B) berisi alat & bibit. Screenshot lokal `v2_prod_fishing`, `v2_prod_bag` |
| Tidak diuji Claude di production | HP nyata, Safari iOS, gamepad, pengukuran performa perangkat |

**M3.4 ditutup 26-11-2026.** M3.5 Market & Economy **belum dimulai** — founder akan mendesain sistem ekonomi dan menyiapkan aset NPC terlebih dahulu.
