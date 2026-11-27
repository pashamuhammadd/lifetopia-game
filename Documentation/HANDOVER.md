# Lifetopia World — Development Handover

Terakhir diperbarui: **27 November 2026** (penutupan M3.6). Pengembangan dihentikan sementara dan dilanjutkan **Rabu, 2 Desember 2026** lewat sesi Claude Code baru. Penjelasan untuk founder dalam bahasa Indonesia; UI game bahasa Inggris.

---

## 1. Status terbaru
| Item | Nilai |
|---|---|
| Milestone terakhir | **M3.6 Housing System — DITUTUP 27-11-2026** (founder acceptance 18/18) |
| Berikutnya | **M3.7 Quest & Local Save — belum dimulai**, perlu go-ahead founder |
| Production | https://play.lifetopiaworld.io = deployment **`8c7e39de`**, build **`M3-06-20261127-1037`** (v0.3.6, art asli, 10,48 MiB) |
| Rollback target | **`94abb7f9`** (M3.5, build `M3-05-20261126-2211`) |
| Preview terakhir | `aaa1b7ea` (build yang sama, diuji founder) |
| Kode | `main` di GitHub; commit fitur M3.6 `61b7e6c` + commit dokumentasi penutupan sesudahnya (lihat `git log`) |
| Automated tests | **EditMode 230/230** (terakhir dijalankan 27-11-2026). Tidak ada PlayMode test assembly |
| Public Web Build | `M3-06-public-20261127-final` (kode final, placeholder): sukses, 8,83 MiB, verifikasi + smoke test sentuh, 0 error |
| Unity | 6000.3.29f1, 2D Built-In, Web build profile; Unity Pipeline 1.0.1 (CLI + MCP) |

## 2. Milestone selesai
| Gate | Isi | Laporan |
|---|---|---|
| M0 | Fondasi repo, docs, Web smoke | `Progress/M0-foundation.md` |
| M1 | FirstPlayable greybox: gerak, kamera, batas, keyboard + sentuh, interaksi | `Progress/M1-first-playable.md` |
| M2 | Kalibrasi art (avatar, tile, rumah, HUD), ditutup sebagai kalibrasi | `Progress/M2-*.md` |
| M3.1 | Item, inventory 12 slot, hotbar 5, wallet | `Progress/M3-01-inventory-hotbar.md` |
| M3.2 | Jam game, siang/malam, HUD waktu | `Progress/M3-02-game-time.md` |
| M3.2-D | Build/verify/deploy Cloudflare Pages | `Progress/M3-02-D-cloudflare-deployment.md`, `WEB-DEPLOYMENT.md` |
| M3.3 | Farming 6 plot | `Progress/M3-03-farming.md` |
| M3.4 | Fishing, dual-grid terrain, pantai + dermaga | `Progress/M3-04-fishing.md` |
| M3.5 | Rio + Rio's Market, ekonomi atomik | `Progress/M3-05-market.md` |
| M3.6 | Housing: interior, Decorating, furnitur di market, responsive housing | `Progress/M3-06-housing.md` |

## 3. Arsitektur & sistem gameplay
- **Scene:** `GameplayPrototype` (lokal, art asli, scene utama build Local) dan `FirstPlayable` (publik, greybox). `HomeInterior` (tracked, tanpa art) dimuat **additive** di offset (200, 0) saat masuk rumah. Scene desa tetap hidup, jadi hanya ada satu instance tiap service.
- **Host di scene desa (GameSystems/Housing):** `GameplayInputGate` (lock gerak + pause jam per sumber), `PlayerProfileHost` (inventory/hotbar/wallet), `GameClockHost` (jam; `ActivePlaySeconds`), `FarmFieldHost`, `FishingController`, `MarketPanelUI`, `HousingSessionHost` (layout rumah per sesi), `HomeTransitionController`.
- **Pola:** data authored = ScriptableObject (item, crop, market, furniture), state runtime = C# murni (`Inventory`, `Wallet`, `FarmField`, `HouseLayout`…), logika murni dites EditMode, MonoBehaviour tipis. Art lokal tidak dirujuk file publik: `LocalArtBindings`, `ItemIconOverrides`, visual set (`Farm/Fishing/Npc/HousingVisualSet`) hanya di scene lokal.
- **Housing (M3.6):** `HouseLayout` (grid 8×8, layer objek/rug, sel pintu+spawn tercadang, flood fill ke pintu), `HouseDecorService` (place/move/store atomik pada inventory yang sama), `DecorateSession`, `HouseInteriorController`, `HouseRoomView`, `FurnitureView`, `DecoratingController`, `HousingHud` + `CanvasMinScale`, `InteriorCameraFramer` (+ `InteriorCameraMath`). `HouseLayoutData` v1 siap untuk save.
- **Waktu:** bag, dialog, market, Decorating = menu (jam pause). Fishing dan transisi rumah = gerak dikunci, jam tetap jalan. Jam pause saat fokus/tab hilang.

## 4. Keputusan desain penting (detail: `DECISIONS.md`)
- Ekonomi baseline: bibit beli 10, buah jual 18, ikan jual 12; modal 100 coin + 3 bibit + cangkul/penyiram/joran; maks 99 per transaksi.
- Furnitur baseline: Stool 25, Table 50, Tropical Rug 60, Indoor Plant 75, Bookshelf 100, Tropical Bed 150, Wooden Wardrobe 150; tidak dijual kembali.
- Fishing baseline (zona 28 %, marker 0,75 siklus/s, bite 1,2–2,0 s, batas 8 s); jam tetap jalan saat fishing.
- Farming: tumbuh 120 s `ActivePlaySeconds`; ukuran visual tahap 0,68/0,81/0,78/1,0; slot hotbar bernilai 0 tetap tersimpan.
- Housing: Tropical Cozy Cottage 8×8, masuk via Interact di pintu, keluar lewat bukaan pintu/tombol Exit, furnitur awal bed/table/stool sekali per sesi, rotasi hanya bed (sprite west asli), bed_north skala visual seragam 1,3, Decorating pause jam, kamera adaptif 30 CSS px per sel, HUD interior min 0,62 CSS px per reference px.
- Art: boleh dipublikasikan di Web build Cloudflare; PNG/master/backup tidak pernah ke GitHub.
- Belum ada penyimpanan permanen: reload browser me-reset sesi.

## 5. Repository publik & artwork lokal
- Repo GitHub publik (bukan berarti open source; lisensi belum diputuskan, G-3).
- File lokal (tidak pernah di-commit) diatur lewat `.git/info/exclude`: `Assets/_Lifetopia/Art/**` (A0, GameplayPack01, NpcPack01, HousingPack01, dll.), `Assets/_Lifetopia/Data/LocalArt/**`, `Data/Characters/`, `Prefabs/A0/`, `Tilemaps/A0/`, `Scenes/GameplayPrototype.unity`, `Scenes/ArtCalibration.unity`, `Documentation/reference/`, `Documentation/Progress/*-screenshots*/`, `.playwright-mcp/`. `Builds/` di-ignore lewat `.gitignore`.
- **`Assets/_Lifetopia/Settings/BuildProfiles/Web.asset`:** versi lokal (ada GameplayPrototype + `preloadedAssets` yang berubah-ubah, T-2) **tidak pernah di-stage**. Versi publik di Git = FirstPlayable, FoundationSmoke, HomeInterior. Bila perlu memperbarui versi publik: buat blob dari `git show HEAD:<path>` + perubahan, `git hash-object -w`, lalu `git update-index --cacheinfo` (cara yang dipakai di M3.6).
- Sebelum setiap commit: stage eksplisit (tanpa `git add -A`), scan GUID (file publik tidak boleh merujuk GUID asset lokal; terakhir 466 file → 0), `.\Tools\Verify-WebBuild.ps1 -ScanRepo` (terakhir 505 file → bersih).

### Lokasi aset lokal (relatif terhadap folder induk repo)
| Isi | Lokasi |
|---|---|
| Paket aset asli dari founder | folder saudara repo: `lifetopia-m2-a0-static`, `lifetopia-m2-a0-02-animation`, `lifetopia-gameplay-assets-01-ab`, `lifetopia-gameplay-assets-01-cde`, `lifetopia-npc-assets-01`, `lifetopia-housing-assets-01`, `lifetopia-artwork` |
| Backup | `lifetopia-m2-local-art-backup`, `lifetopia-m3-3-scene-backup-20261126-1600`, `lifetopia-m3-4-backup-20261126-1823`, **`lifetopia-m3-6-local-backup-20261127`** (semua file lokal yang di-exclude + `Web.asset` lokal, 830 file, hash HousingPack01 identik) |
| Build Web | `Builds/Web/<BuildName>/` di dalam repo (di-ignore) |

## 6. Prosedur
Semua dari root repo, Windows PowerShell. Detail: `WEB-DEPLOYMENT.md`.
- **RAM 8 GB:** Unity Editor **ditutup** sebelum batch build; satu Unity pada satu waktu. Jalankan build sebagai proses Windows terpisah agar tidak dihentikan pembersih background Claude Code:
  `Start-Process powershell -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-Command',"cd <repo>; .\Tools\Build-Web.ps1 -Variant Local *> <log>" -WindowStyle Hidden` lalu pantau log. Skrip tetap menolak bila RAM bebas < 1,5 GB. Local ±9 menit, Public ±12 menit.
- **Build:** `.\Tools\Build-Web.ps1 -Variant Local` (art asli) / `-Variant Public` (placeholder, termasuk `PublicExtraScenes`). Verifikasi: `.\Tools\Verify-WebBuild.ps1 -BuildPath Builds/Web/<Name>`.
- **Preview** (perlu persetujuan founder per deploy): `.\Tools\Deploy-Web.ps1 -SkipBuild -BuildPath Builds/Web/<Name> -ArtworkApproved` → cek production ID tidak berubah.
- **Production** (perlu persetujuan founder, build yang sama dengan preview yang diuji): bandingkan MD5 file build vs preview (`curl -H "Accept-Encoding: br"`), lalu `.\Tools\Deploy-Web.ps1 -Production -SkipBuild -BuildPath Builds/Web/<Name> -ArtworkApproved -ConfirmProduction lifetopia-play`, lalu `Verify-WebBuild -Url https://play.lifetopiaworld.io` + `version.json`.
- **Rollback** (hanya dengan persetujuan): dashboard Cloudflare → deployment `94abb7f9` → "Rollback to this deployment", atau deploy ulang folder build M3.5 (`WEB-DEPLOYMENT.md` §8).
- **Uji lokal build:** server statis kecil yang mengirim `Content-Encoding: br` untuk `*.br` (Python `http.server` subclass); jangan `file://`.
- **Unity Editor + MCP:** buka Editor dengan `Unity.exe -projectPath <repo>`; MCP siap ±40 s. Injeksi keyboard di Play Mode perlu `editorInputBehaviorInPlayMode = AllDeviceInputAlwaysGoesToGameView` (kembalikan setelahnya) dan jendela Unity fokus; jam berhenti saat Editor tidak fokus (T-31). Uji keyboard/waktu paling andal di browser (Playwright).

## 7. Kendala & backlog (lengkap: `BACKLOG.md`)
- **G-1:** HP nyata sudah dicoba founder per milestone, tapi model/browser/FPS/memori/load time belum diukur; Safari iOS belum.
- **V-3 / T-47 / T-55 / T-64:** gamepad belum diuji.
- **T-41 / T-52 / T-57 / T-29 / T-16:** save lokal (profile, coin, jam, farm, rumah) — inti M3.7.
- **T-58 / T-59 / T-60:** `table_west` terlalu kecil; varian west lain dan dekor cadangan belum aktif/berharga.
- **T-62:** bed dekoratif (belum ada tidur/skip hari).
- **T-1 / T-23 / T-43:** atlas + kompresi tekstur (ukuran build 10,5 MiB).
- **T-2:** `Web.asset` `preloadedAssets` berubah tiap build (alasan versi lokal tidak di-commit).
- **T-18 / T-65:** belum ada PlayMode test assembly.
- **T-45 / T-54 / T-63:** SFX; **T-42 / T-46 / T-53:** animasi aksi/NPC.

## 8. Rekomendasi scope M3.7 Quest & Local Save (usulan, menunggu founder)
1. **Local save dulu** (risiko terbesar, prasyarat quest): satu `SaveData` berversi yang menggabungkan `PlayerProfileData`, `GameClockData`, `FarmFieldData`, `HouseLayoutData` (semua sudah ada `ToData`/`TryRestore` dengan validasi menyeluruh). Penyimpanan Web lewat `PlayerPrefs` (IndexedDB) — uji reload, data rusak, storage diblokir/kuota, tab tersembunyi. Auto-save pada event aman (keluar rumah, setelah transaksi, interval), tombol "New Game"/reset eksplisit, starter kit hanya bila belum ada save (T-16).
2. **Quest tutorial pendek** (GDD: tutorial quest + one-time reward): rantai 4–6 langkah (bicara ke papan/Rio → cangkul → tanam → siram → panen → jual → beli furnitur → letakkan di rumah), hadiah sekali (flag di save), UI quest kecil yang tidak menutupi HUD HP.
3. **Di luar scope:** cloud save/akun (M5), multiplayer, Web3, harga/ekonomi baru tanpa persetujuan, properti tambahan.
4. Tes: EditMode untuk serialisasi/migrasi/penolakan data rusak/quest state; Play Mode reload di Web build.

## 9. Cara melanjutkan (sesi Claude Code baru)
1. Baca `CLAUDE.md`, **file ini**, lalu `DECISIONS.md`, `DEVELOPMENT-ROADMAP.md`, `BACKLOG.md`, laporan milestone terakhir (`Progress/M3-06-housing.md`), `WEB-DEPLOYMENT.md`.
2. Jalankan `git status`, `git log --oneline -5`; pastikan hanya `Web.asset` lokal yang berubah (memang lokal).
3. Cek Unity (Editor tertutup bila tidak dipakai) dan RAM.
4. Konfirmasi production masih `M3-06-20261127-1037` (`https://play.lifetopiaworld.io/version.json`).
5. **Jangan mulai M3.7** sebelum founder memberi instruksi dan (bila ada) paket aset/prompt baru. Sampaikan rencana singkat + milestone saat mulai.
6. Aturan tetap: tidak commit/push/deploy tanpa persetujuan eksplisit; tidak mengubah `lifetopia-platform`, database produksi, Web3, atau multiplayer; artwork original tidak ke GitHub.
