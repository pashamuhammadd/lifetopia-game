# M3.6 — Housing System (Housing Asset Pack 01)

Tanggal: 27 November 2026 · Basis: commit `be2d218` (M3.5 ditutup) · Status: **M3.6 DITUTUP 27-11-2026** — founder acceptance test **18/18** (founder-reported, Preview + HP), EditMode 230/230; commit/push dan production deploy disetujui founder (§13).

Sumber dibaca: paket `lifetopia-housing-assets-01` (START-HERE, IMPLEMENTATION-PROMPT, asset-manifest.json, asset-register.csv, QC.txt, preview.png, Reference/ ×4), CLAUDE.md, GDD 2.0, Art Bible, DECISIONS, ROADMAP, BACKLOG, laporan M3.5, WEB-DEPLOYMENT.md, kode aktual.

## 1. Ringkasan
| Area | Hasil |
|---|---|
| Aset | 48 PNG runtime → `Art/HousingPack01/` (lokal, di-exclude git), MD5 identik dengan paket. Reference/preview tidak di-import |
| Interior | Scene terpisah `HomeInterior.unity` (tanpa art), 8 × 8 sel, dimuat **additive**; pintu rumah lama → "Enter Home"; keluar lewat bukaan pintu atau tombol Exit |
| Furnitur awal | Tropical Bed (0,5), Wooden Table (4,4), Wooden Stool (5,2), sekali per sesi, tidak masuk tas |
| Decorating | Grid, ghost hijau/merah + alasan, Place/Move/Rotate (bed saja)/Store/Cancel/Done; mouse, sentuh, keyboard |
| Ekonomi | 7 furnitur di Buy Rio (harga kandidat, configurable di asset item); kategori "Farming"/"Furniture", list bisa scroll; baseline M3.5 utuh |
| Responsif | Kamera adaptif + HUD safe-area; diuji 1920×1080, 1366×768, 1024×768, 844×390, 667×375, portrait 390×844 (overlay) |
| Session | Inventory/hotbar/coin/jam/farm/furnitur tetap saat pindah scene; tidak ada service ganda |
| Tes | EditMode **230/230** (204 + 26 baru); Play Mode lokal & publik; Public Web Build **berhasil + diuji di browser** |

## 2. Arsitektur
- **Kenapa additive:** scene desa (PlayerProfileHost, GameClockHost, FarmFieldHost, HUD, pemain, kamera) tidak pernah di-unload, sehingga hanya ada **satu** inventory/wallet/jam. Interior dimuat di posisi jauh (200, 0) lalu di-unload saat keluar. Tanaman terus tumbuh karena pertumbuhan dihitung dari `ActivePlaySeconds` jam yang sama (bukan dari GameObject).
- **Penjelasan untuk founder:** bayangkan desa tetap "menyala" di belakang layar; rumah hanyalah ruangan tambahan yang dipasang dan dicabut. Karena itu tas dan koin tidak pernah dibuat ulang.
- `Scripts/Housing/` (namespace `Lifetopia.Housing`):
  - Data (ScriptableObject, tanpa art, publik): `FurnitureDefinition` (item, footprint, rotasi, layer, blocking), `FurnitureCatalog`.
  - Domain murni (dites): `HouseLayout` (grid, overlap per layer, sel tercadang, flood-fill ke pintu, `HouseLayoutData` versi 1), `HouseDecorService` (place/move/store atomik), `DecorateSession` (state ghost), `InteriorCameraMath`.
  - Scene: `HousingSessionHost` + `HomeTransitionController` + `HomeDoor` (desa), `HouseInteriorController`, `HouseRoomView`, `FurnitureView`, `DecoratingController`, `HousingHud`, `InteriorCameraFramer`, `HomeExitZone` (interior).
  - Art lokal: `HousingVisualSet` (`Data/LocalArt/HousingVisualSet_Pack01.asset`, lokal) dioper dari GameplayPrototype; publik tidak mengoper → greybox berlabel "(TEMP)".
- **Atomik (untuk founder):** setiap aksi dicek lengkap dulu; baru setelah lolos, tas dan rumah berubah bersama. Contoh Store: bila tas penuh, furnitur tetap di lantai, tidak hilang.

## 3. Aturan penempatan
| Aturan | Perilaku |
|---|---|
| Batas | footprint harus di dalam 8 × 8 |
| Overlap | tidak boleh dalam layer yang sama; rug (layer lantai) boleh di bawah furnitur |
| Pintu & spawn | sel (3,0),(4,0),(3,1),(4,1) selalu kosong |
| Pemain | furnitur padat tidak boleh di sel tempat pemain berdiri |
| Jalur keluar | setelah penempatan, semua sel kosong harus tetap tersambung ke pintu (flood fill 4 arah) — pemain tidak bisa terjebak |
| Rotasi | hanya bed (north 1×2 ↔ west 2×1) dengan sprite west asli; tombol Rotate tersembunyi bila tidak didukung |
| Move/Cancel | posisi lama tetap sampai Place valid; Cancel tidak mengubah apa pun |
| Double submit | 1 konfirmasi per frame + cooldown 0,25 s + service menolak panggilan re-entrant |
| Collider | footprint − 0,06 per sisi (koridor 1 sel tetap bisa dilewati) |

## 4. Kontrol
| Input | Living Mode | Decorating Mode |
|---|---|---|
| Mouse/sentuh | tombol Decorate, Exit | pilih item di list, tap lantai = pindah ghost, tap furnitur = pilih, tombol Place/Move, Rotate, Store, Cancel, Done |
| Keyboard | WASD, E di pintu | WASD geser ghost, E konfirmasi/Move, R rotasi, Tab item berikutnya, Delete/Backspace simpan, Esc batal/keluar |
| Saat Decorating | — | jam berhenti, gerak dikunci, hotbar/Bag/joystick/Interact/keyboard hint disembunyikan, tas & market tidak bisa dibuka, Exit ditolak |

## 5. Ekonomi (harga kandidat, belum baseline)
Wooden Stool 25 · Wooden Table 50 · Tropical Rug 60 · Indoor Plant 75 · Bookshelf 100 · Tropical Bed 150 · Wooden Wardrobe 150 — di `Data/Items/item_furniture_*.asset` (Buy Price). Furnitur Sell Price 0 (tidak dijual kembali; tas menampilkan "Can't be sold"). Stool lama: buy 50 → 25, sell 25 → 0. Seed 10 / buah 18 / ikan 12 dan modal awal **tidak berubah** (diuji).

## 6. Responsif
- Kamera: `size = max(tinggi/(2·usableH), lebar/(2·aspect·usableW))` terhadap area layar bebas (safe area dikurangi HUD), dengan batas keterbacaan 30 CSS px per sel; bila ruangan tidak muat pada batas itu, kamera mengikuti pemain/ghost dan di-clamp sehingga semua sisi terjangkau. Dunia/sprite/collider tidak pernah diskalakan.
- HUD interior di canvas sendiri (sort order 5, di bawah HUD desa = 10, sehingga tas/menu selalu menutupinya), anchor dalam SafeAreaFitter.

| Ukuran (browser desktop, Public build) | Living | Decorating | Tombol | FPS |
|---|---|---|---|---|
| 1920×1080 | ruangan muat | muat | 126 px | 60,5 |
| 1366×768 | muat | muat | 90 px | 60,5 |
| 1024×768 | muat | muat | 78 px | 60,5 |
| 844×390 | mengikuti pemain | **muat penuh**, ~30 px/sel | 50 px | 60,5 |
| 667×375 | mengikuti pemain | muat penuh | 44 px | 60,5 |
| 390×844 portrait (touch, DPR 2) | overlay "rotate" tampil | — | — | — |
Resize saat Decorating aktif dilakukan di urutan uji di atas (mode tetap terbuka, layout menyesuaikan). FPS = browser desktop Chromium di laptop, **bukan HP**.

## 7. Hasil pengujian (aktual)
### 7.1 EditMode — **230/230 lulus** (26 baru `HousingTests`)
Footprint/rotasi, anchor, bounds, orientasi tak didukung, sel tercadang, overlap + rug, sel pemain, kantong terkunci, ruangan terpotong dari pintu, place (1 item, ID unik), place gagal tanpa perubahan, re-entrancy, move invalid mempertahankan posisi, rotasi bed bolak-balik, store + tas penuh, starter sekali tanpa item tas, round-trip data + data invalid ditolak utuh, session place/cancel/move/rotate/store, market furnitur (harga dari item, slot non-stackable), asset harga 25/50/60/75/100/150/150 + baseline 10/18/12 + Sell hanya 2 item + hanya bed berotasi, matematika kamera, sel pemain.

### 7.2 Play Mode — GameplayPrototype (art lokal, Editor)
| Uji | Hasil |
|---|---|
| Pintu | prompt "Enter Home"; E (input asli) → masuk; 1 profile, 1 clock, 1 kamera, 1 EventSystem |
| Starter | bed/table/stool di posisi manifest, tas tanpa furnitur |
| Farm | plot 1 dicangkul-tanam-siram di desa sebelum masuk; state utuh setelah keluar/masuk |
| Decorating (klik mouse asli) | buka → jam beku (519,0 menit tetap), gerak terkunci; pilih meja → Move → (1,2) "Looks good!" → Place (instance #2 sama) |
| Store | stool → tas ×1; list menampilkan stool |
| Place invalid | sel pintu → merah "Keep the doorway clear.", Place nonaktif, E tidak menempatkan |
| Place valid | (6,1) → instance baru #4, tas −1 |
| Rotate | bed → sprite west asli, footprint 2×1, Place |
| Tas/market saat Decorating | tidak terbuka; Exit ditolak; hotbar tersembunyi |
| Keluar (tombol) | scene di-unload, pemain (−9, 1.2), kamera 5,5 dikembalikan |
| Spam | 2× RequestEnter + RequestExit dalam 1 frame → 1 transisi |
| Masuk lagi | susunan sama, collider sesuai footprint (bed padat, sel kosong bebas) |
| Keluar jalan kaki | zona di bukaan pintu → kembali ke desa |
| Market | header "Farming"/"Furniture", scroll otomatis ke Tropical Bed, beli 600 → 450; Sell 2 item, seed 10/buah 18/ikan 12 |
| Console | error lama (bug Label path, sudah diperbaiki sebelum tes final); 0 error di log setelah perbaikan |

### 7.3 Play Mode — FirstPlayable (publik)
Pintu placeholder → interior greybox "(TEMP)"; Decorating: pilih meja → Move → (5,6) valid.

### 7.4 Public Web Build
| Item | Hasil |
|---|---|
| Build | **`M3-06-public-20261127`** — Editor tertutup, `Build-Web.ps1 -Variant Public`; FirstPlayable + HomeInterior; Result Succeeded, 0 error, 0 warning, 12:02; 8,80 MiB; `Verify-WebBuild` lulus; `contentMode public-placeholder` |
| Desktop 1280×720 (keyboard + mouse asli) | jalan ke rumah, "[E] Enter Home", E masuk, dinding menahan pemain, jam jalan di dalam rumah, Decorate (klik) → jam berhenti, Move meja, Done, jalan keluar lewat pintu |
| HP emulasi 844×390 (sentuh, DPR 2) | tap Interact masuk, tap Decorate, tap meja → Move → tap sel → Place, Done, tap Exit, tap Interact masuk lagi → meja tetap di posisi baru |
| Console | 0 error, 6 warning engine yang dikenal |
| Dependensi publik | scan GUID: 464 file publik, **0** referensi ke 290 asset lokal (kontrol positif: GameplayPrototype 552 referensi lokal) |

### 7.5 Regresi M1–M3.5
EditMode seluruh suite lulus (gerak, kamera, inventory, hotbar, jam, farm, fishing, market). Play Mode: farming dan market diuji ulang di atas; fishing tidak diuji ulang di Play Mode (tidak ada perubahan kode fishing).

### 7.6 Tidak diuji
HP nyata, Safari iOS, gamepad, keyboard di Editor saat jendela Unity tidak fokus (Windows tidak memberi fokus; keyboard diuji di browser), Local Web build + Preview (§9), pertumbuhan tanaman di dalam rumah di build Local (di Editor jam berhenti saat Editor tidak fokus — perilaku T-31 — sehingga tidak terukur; secara arsitektur clock & farm yang sama tetap berjalan, dibuktikan jam 09:43 → 09:53 di dalam rumah di Web).

## 8. Keputusan & temuan untuk review founder
1. ~~Ukuran bed_north~~ → dikalibrasi atas keputusan founder (skala seragam 1,3), lihat §12.
2. Harga furnitur kandidat — perlu playtest.
3. Masuk rumah dengan Interact (bukan otomatis), keluar otomatis lewat pintu atau tombol Exit.
4. Kamera Living di HP mengikuti pemain (ruangan penuh terlalu kecil); Decorating menampilkan seluruh ruangan.
5. ~~Teks hint kecil di 667×375~~ → diperbaiki (§12).
6. Sell Price furnitur 0 (stool sebelumnya 25).
7. Reload browser mereset semuanya (rumah, tas, koin, jam, farm) — save permanen M3.7.

## 9. Web build Local — percobaan pertama
Percobaan pertama (`M3-06-20261127-0204`) dihentikan oleh Claude Code karena memori sistem kritis ±20 detik setelah mulai; tidak ada artefak. Dijalankan ulang setelah persetujuan founder sebagai proses Windows terpisah (tetap dengan pemeriksaan RAM bawaan skrip) — berhasil, lihat §12.

## 10. File
Baru: `Scripts/Housing/*` (16 file), `Tests/EditMode/HousingTests.cs`, `Scenes/HomeInterior.unity`, `Data/Housing/*` (catalog + 7 definisi), `Data/Items/item_furniture_{bed,table,rug_oval,monstera,bookshelf,wardrobe}.asset`, laporan ini. Diubah: `MarketPanelUI.cs`, `Prefabs/UI/MarketPanel.prefab` (ScrollRect + header), `MarketConfig_Rio.asset`, `ItemDatabase.asset`, `item_furniture_wooden_stool.asset`, `Scenes/FirstPlayable.unity` (Housing + HomeDoor), `Editor/WebBuildCommand.cs`, `Tools/Build-Web.ps1`, `Tools/WebDeploy.config.psd1` (0.3.6, PublicExtraScenes), docs. Lokal (tidak dipush): `Art/HousingPack01/`, `Data/LocalArt/HousingVisualSet_Pack01.asset`, `ItemIconOverrides_Pack01.asset`, `GameplayPrototype.unity`, `Web.asset` (+HomeInterior di daftar scene), screenshot `M3-06-screenshots-local/`.
Catatan commit nanti: `Web.asset` versi publik juga perlu `HomeInterior.unity` di daftar scene (tanpa GameplayPrototype) — stage lewat patch terpisah.

## 11. Langkah berikutnya
Founder acceptance test di Preview (desktop + HP nyata). Setelah disetujui: commit + push (stage eksplisit), lalu production deploy build yang sama (`-Production -SkipBuild`) dengan persetujuan terpisah. M3.7 belum dimulai. Handover: `Documentation/HANDOVER.md`.

## 12. Putaran founder 27-11-2026 (keputusan & hasil)
### 12.1 Keputusan founder
1) Kalibrasi `bed_north` agar proporsional (PPU, pivot, footprint, collider tetap). 2) Harga furnitur dipertahankan; belum bisa dijual kembali. 3) Keterbacaan teks Decorating di HP (667×375). 4–5) Uji ulang semua fitur + pertumbuhan tanaman di dalam rumah. 6–8) Local build + Preview dengan artwork original disetujui. 9) Production tidak berubah sebelum acceptance.

### 12.2 Perubahan
| Item | Perubahan |
|---|---|
| bed_north | `HousingVisualSet.FurnitureSprite.visualScale` (skala **seragam** X = Y di sekitar pivot). bed_north = **1,3**, diukur dari fitur yang sama (bantal ±30 vs ±40 px, motif ±10 vs ±14 px pada bed_west); panjang bed_north menjadi ±129 px = 2 sel = footprint. PNG tidak diedit; PPU 64, pivot manifest, footprint 1×2, collider 0,88×1,88 (diverifikasi di Play Mode) |
| Teks HP | `CanvasMinScale` (baru, `Scripts/UI`): canvas interior tidak mengecil di bawah 0,62 CSS px per reference px (`Screen.dpi` = 96 × DPR). Font judul 28, hint 21, list/tombol 21–23, best-fit min 16; title bar 100 ref. Di 667×375: hint ±13 CSS px, tombol Living 64×57 CSS px |
| HUD Living | Decorate + Exit berdampingan di bawah Bag (tidak bertumpuk) agar jauh dari tombol Interact di HP pendek; inset kamera disesuaikan |
| Lain | `FurnitureView.Show(..., visualScale)`; ghost Decorating memakai skala yang sama |

### 12.3 Pengujian putaran ini (aktual)
| Uji | Hasil |
|---|---|
| EditMode | **230/230** |
| Editor Play Mode (GameplayPrototype) | bed north skala 1,3, collider tetap; ghost west vs north sebanding; Cancel tidak mengubah susunan; fishing di dermaga: Begin OK → Caught, ikan +1, joran tetap; console 0 error / 0 warning |
| Local build | **`M3-06-20261127-1037`** — Editor tertutup, 9:00, exit 0, 10,48 MiB, verifikasi lulus, RAM terendah 0,53 GB |
| Preview | **`aaa1b7ea`** — https://aaa1b7ea.lifetopia-play.pages.dev · alias https://preview.lifetopia-play.pages.dev — HTTP 200, wasm `application/wasm` + `br`, 404 OK, `version.json` = `M3-06-20261127-1037` |
| Production | skrip: tidak berubah (`94abb7f9`); `play.lifetopiaworld.io/version.json` = `M3-05-20261126-2211` |
| Desktop 1280×720 di Preview (keyboard + mouse asli, art asli) | cangkul/tanam/siram (E) → "Growing - 2:00 left" pukul 10:08 → masuk rumah, diam 125 s → jam 12:32 di dalam → keluar jalan kaki → tanaman **Ready** ("Harvest Tropical Fruit") → panen → Rio: jual buah 100 → **118**, beli Wooden Stool → **93** (jam berhenti saat market) → kembali → Decorate: stool di daftar → tap sel → Place → bed: klik, **R**, **E** (rotasi west tersimpan), **Esc** ×2 keluar; jam lanjut |
| HP emulasi 667×375 & 844×390 (sentuh, DPR 2) di Preview | tap Interact masuk rumah, tap Decorate; teks terbaca, grid penuh terlihat; tombol Living tidak menumpuk dengan Interact/joystick |
| Console Preview | 0 error, 6 warning engine yang dikenal |
| Public build final | **`M3-06-public-20261127-final`** (kode final) — 8,83 MiB, verifikasi lulus; smoke test sentuh 667×375: masuk rumah + Decorating placeholder, 0 error console |
| Keamanan | scan GUID 466 file publik → 0 referensi ke 290 asset lokal; `Verify-WebBuild -ScanRepo` 505 file bersih; tidak ada PNG/art/LocalArt/GameplayPrototype/screenshot/build yang terlihat git |

Screenshot lokal (tidak dipush): `prev_interior_desktop_1280x720`, `prev_interior_after125s`, `prev_farm_after_house`, `prev_market_sold`, `prev_market_bought_stool`, `prev_decorating_desktop_1280x720`, `prev_bed_rotated`, `prev_living_after_decor`, `prev_mobile_interior_667x375`, `prev_mobile_decorating_667x375`, `prev_mobile_interior_844x390`, `prev_mobile_decorating_844x390`, `editor_bed_north_calibrated`, `editor_bed_north_vs_west_ghost`.

### 12.4 Tidak diuji
HP nyata dan Safari iOS (founder), gamepad, performa perangkat. Fishing tidak diuji di Web (diuji di Editor + EditMode; kode fishing tidak berubah).

## 13. Founder acceptance & penutupan (27 Nov 2026)

### 13.1 Founder-reported manual playtest — 18/18 lulus
Dilakukan **oleh founder** lewat Cloudflare Preview `aaa1b7ea` (build `M3-06-20261127-1037`), termasuk di HP. Ini laporan founder, bukan pengukuran Claude; model HP, browser dan angka performa tidak dilaporkan (backlog G-1).

| # | Pengujian | Hasil |
|---|---|---|
| 1 | Pemain bisa masuk rumah | lulus |
| 2 | Interior rumah terlihat menarik | lulus |
| 3 | Ukuran furnitur proporsional | lulus |
| 4 | Pemain bisa keluar rumah | lulus |
| 5 | Kamera menyesuaikan ukuran layar | lulus |
| 6 | Decorating Mode berfungsi | lulus |
| 7 | Furnitur dapat ditempatkan | lulus |
| 8 | Furnitur dapat dipindahkan | lulus |
| 9 | Tempat tidur dapat dirotasi | lulus |
| 10 | Furnitur dapat dikembalikan ke tas | lulus |
| 11 | Pintu tidak bisa terhalang furnitur | lulus |
| 12 | Pembelian furnitur di market berhasil | lulus |
| 13 | Coin dan inventory tetap sama saat berpindah scene | lulus |
| 14 | Susunan furnitur tidak hilang saat keluar rumah | lulus |
| 15 | Tanaman tetap tumbuh saat berada di rumah | lulus |
| 16 | Teks dan tombol nyaman digunakan di HP | lulus |
| 17 | Farming dan fishing tetap berfungsi | lulus |
| 18 | Tidak ada crash atau bug yang mengganggu | lulus |

### 13.2 Pengujian otomatis terakhir yang benar-benar dijalankan
EditMode **230/230** (Unity Test Runner via MCP, 27-11-2026, setelah kalibrasi bed dan perubahan HUD; 204 lama + 26 `HousingTests`). Tidak ada PlayMode test assembly (pengujian Play Mode dilakukan dengan skrip via MCP dan Playwright, lihat §7 dan §12.3).

### 13.3 Baseline (disetujui founder)
- **Desain:** Tropical Cozy Cottage 8 × 8 (lantai honey, dinding belakang + jendela, cutaway samping/depan, pintu 2 sel), bed_north skala visual 1,3, aturan sorting/pivot Art Bible M3.6.
- **Responsive housing:** kamera adaptif (area layar bebas, 30 CSS px per sel, follow + clamp), HUD safe-area dengan `CanvasMinScale` 0,62, Decorate/Exit berdampingan.
- **Gameplay:** Living/Decorating Mode, aturan penempatan (layer, sel tercadang, flood fill ke pintu), rotasi hanya bed, Store hanya bila tas muat, Decorating pause jam, furnitur awal sekali per sesi.
- **Harga furnitur:** Stool 25 · Table 50 · Tropical Rug 60 · Indoor Plant 75 · Bookshelf 100 · Tropical Bed 150 · Wooden Wardrobe 150; tidak dijual kembali. Baseline M3.5 (10/18/12, modal 100 + 3 bibit + alat) tetap.

### 13.4 Commit, push & production (disetujui founder)
| Item | Nilai |
|---|---|
| Backup lokal | Folder saudara repo `lifetopia-m3-6-local-backup-20261127`: 829 file lokal yang di-exclude (Art, LocalArt, scene lokal, screenshot, reference) + `Web.asset` lokal; isi HousingPack01 identik (hash). File asli tidak dipindah/dihapus; tidak diunggah |
| Pemeriksaan sebelum commit | stage eksplisit 92 file; tidak ada PNG/Art/LocalArt/GameplayPrototype/screenshot/build; scan GUID 466 file publik → 0 referensi lokal; `Verify-WebBuild -ScanRepo` 505 file bersih; tidak ada path pribadi/email/credential di diff |
| `Web.asset` | versi publik di-stage via `git update-index` = HEAD + `HomeInterior.unity` (tanpa GameplayPrototype dan tanpa perubahan `preloadedAssets`); file kerja lokal tetap |
| Commit fitur | **`61b7e6c`** `feat(m3.6): housing system - starter house interior, decorating mode, furniture market` — push `be2d218..61b7e6c` ke `origin/main`. Commit dokumentasi penutupan menyusul (lihat `git log`) |
| Build ID | **`M3-06-20261127-1037`** (sama dengan preview `aaa1b7ea` yang diuji founder; tidak di-build ulang). `version.json` mencatat `commit be2d218, dirty true` karena build dibuat sebelum commit; isi kode = commit `61b7e6c` |
| Ukuran build | 10,48 MiB (wasm.br 7.349.841 B, data.br 3.530.364 B, framework.js.br 75.577 B, loader.js 26.983 B) |
| Identitas file | MD5 index.html, version.json, loader.js, framework.js.br, wasm.br, data.br: folder build = preview `aaa1b7ea` = production — **identik** (byte mentah dengan `Accept-Encoding: br`) |
| Command | `.\Tools\Deploy-Web.ps1 -Production -SkipBuild -BuildPath "Builds/Web/M3-06-20261127-1037" -ArtworkApproved -ConfirmProduction lifetopia-play` (0 file baru diunggah; 8 sudah ada) |
| Deployment production | **`8c7e39de-a718-4e0f-b2a7-b7302dda2c93`** (https://8c7e39de.lifetopia-play.pages.dev) |
| Rollback target | **`94abb7f9`** (M3.5, build `M3-05-20261126-2211`) — prosedur `WEB-DEPLOYMENT.md` §8 |
| Verifikasi | `version.json` di https://play.lifetopiaworld.io = `M3-06-20261127-1037` v0.3.6; `Verify-WebBuild -Url https://play.lifetopiaworld.io` **lulus**; wasm `application/wasm` + `br`, `no-cache, no-transform`, `nosniff`, `noindex`; path tidak ada → 404 |
| Gameplay production (desktop 1280×720, keyboard + mouse asli) | loading OK, console 0 error (6 warning engine); cangkul/tanam/siram → "Growing"; masuk rumah (E); Decorate → pilih meja → Move → Place ("Moved Wooden Table."); Esc; keluar jalan kaki; Rio's Market terbuka (kategori Farming/Furniture, harga 10/25); fishing di dermaga 5 percobaan (ada "Missed", tas akhirnya Reef Fish ×3, joran tetap, coin tetap) |
| Tidak diuji Claude di production | HP nyata (founder sudah di preview), Safari iOS, gamepad, performa perangkat |

**M3.6 ditutup 27-11-2026.** Handover: `Documentation/HANDOVER.md`. M3.7 belum dimulai.
