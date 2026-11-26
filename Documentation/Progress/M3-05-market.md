# M3.5 — Market & Economy (NPC Rio, Rio's Market)

Tanggal: 26 November 2026 · Basis: commit `08dea50` (M3.4 ditutup) · Production saat mulai: `5784f2eb` (M3.4) · Status: **M3.5 DITUTUP 26-11-2026** — founder acceptance test 16/16 (founder-reported, Preview + HP nyata), EditMode 204/204; commit/push dan production deploy disetujui founder (detail §12).

Ruang lingkup: import NPC Asset Pack 01, Rio di kios pasar, Market UI (Buy/Sell), sistem ekonomi atomik di atas wallet/inventory yang sudah ada. **Tidak** dikerjakan: Maya/Kai di game, quest, save permanen, backend/database, blockchain/token/NFT, multiplayer, Housing (M3.6).

Sumber dibaca: paket `lifetopia-npc-assets-01` (START-HERE, IMPLEMENTATION-PROMPT, asset-manifest.json, asset-register.csv, QC.txt, preview.png, Reference/approved-concept.png), CLAUDE.md, GDD 2.0, Art Bible, DECISIONS, ROADMAP, BACKLOG, laporan M3.1–M3.4, WEB-DEPLOYMENT.md.

---

## 1. Ringkasan hasil
| Area | Hasil |
|---|---|
| Aset | 17 PNG runtime (12 sprite 4 arah Rio/Maya/Kai, 3 portrait, 2 ikon market) disalin ke `Art/NpcPack01/` (lokal, di-exclude git), checksum identik dengan paket. `Reference/` dan `ReferenceReuse/` **tidak** di-import; ikon seed/fruit/fish/coin memakai sprite yang sudah ada |
| Import | Sprite Single, PPU 64, pivot custom (0.5, 0.125) untuk dunia (= pivot kaki pemain 64,16), bilinear, tanpa mipmap, alpha transparency, clamp; UI/portrait pivot tengah |
| Rio | NPC statis di depan sisi kanan kios (10.05, 1.55), tidak menutup cabang jalur ke kios; collider hanya footprint kaki 0.6×0.3; SortingGroup layer World + sort point pivot (sama dengan pemain); menoleh ke pemain saat Interact, kembali menghadap depan saat market ditutup |
| Maya, Kai | Visual set dibuat (lokal) tetapi **tidak ditempatkan** di game |
| Market UI | "Rio's Market", "Welcome, traveler!", portrait Rio, tab Buy/Sell (ikon + garis indikator), baris item (ikon asli, nama, Owned, harga + ikon coin), kuantitas − / + / Max, Total cost / You receive, Balance after, tombol Purchase xN / Sell xN, Close, teks feedback |
| Ekonomi | Seed beli 10, buah jual 18, ikan jual 12; stok tak terbatas, tanpa biaya, tanpa jam buka; transaksi atomik pada wallet + inventory yang sudah ada |
| Waktu & gerak | Market = menu: gerak/interaksi dunia diblokir dan jam game berhenti (seperti tas); kembali normal setelah ditutup |
| Tes | EditMode **204/204** (176 + 28 baru); Play Mode market lengkap + regresi M3.1/M3.2/M3.3/M3.4 identik; scene publik + sentuh |

## 2. Arsitektur (sistem lama dipakai ulang, tidak ada sistem paralel)
- `Scripts/Economy/MarketConfig.cs` (ScriptableObject): nama toko, sapaan, daftar item Buy dan Sell, `maxQuantityPerTrade` (99, maks 999). **Harga satuan tidak disimpan di sini**: diambil dari `ItemDefinition.BuyPrice/SellPrice` di asset item (satu sumber kebenaran; bisa diubah di Inspector). `Validate()` menolak item kosong, duplikat, harga 0, dan alat di daftar Sell.
- `Scripts/Economy/MarketService.cs` (C# murni, bisa dites): `Quote`, `Buy`, `Sell`, `MaxQuantity`, event `Traded`, `TradeCount`. Memakai `Wallet` dan `Inventory` milik `PlayerProfile`.
- `Scripts/Economy/MarketText.cs`: semua teks bahasa Inggris.
- `Scripts/Npc/NpcVisualSet.cs` (sprite D/L/R/U + portrait), `NpcView.cs` (art bila ada, placeholder bila tidak; `FaceTowards`), `MerchantNpc.cs` (IInteractable → buka market).
- `Scripts/UI/Market/MarketPanelUI.cs`, `MarketItemRowView.cs` (ikon lewat `ItemSlotView` yang sudah ada → override ikon lokal otomatis).
- `PlayerInputReader.NavigateInput` (baru, kecil): arah WASD/panah/stick yang tetap terbaca saat gerak dikunci — dipakai navigasi market; tidak pernah menggerakkan pemain.

**Penjelasan untuk founder:** "atomik" berarti transaksi dicek lengkap dulu (item terdaftar, jumlah 1–99, coin cukup, ruang tas termasuk stack sebagian, barang cukup, batas coin 9.999.999, total dihitung 64-bit agar tidak overflow). Baru setelah semua lolos, coin dan item diubah. Bila langkah kedua gagal tak terduga, langkah pertama dibatalkan (rollback). Hasilnya: coin dan item berubah bersama, atau tidak berubah sama sekali.

## 3. Aturan transaksi
| Kasus | Perilaku |
|---|---|
| Beli | coin −(10 × jumlah), bibit +jumlah; tumpuk ke stack yang ada dulu |
| Jual | buah +18 / ikan +12 per unit; diambil dari stack mana pun |
| Coin kurang | ditolak, "Not enough coins.", tombol nonaktif, tidak ada perubahan |
| Tas penuh (termasuk stack sebagian) | ditolak, "Inventory full. Make room for … first." |
| Barang tidak cukup | ditolak, "You don't have enough …" |
| Jumlah 0 / negatif / > 99 / int.MaxValue | ditolak (InvalidQuantity) |
| Batas coin tercapai | jual ditolak (CoinLimitReached) |
| Item tidak terdaftar (jual bibit, jual alat, beli buah) | ditolak (NotInCatalog) |
| Double submission | satu konfirmasi per frame + cooldown 0,25 s (real time) + penekanan yang membuka market tidak pernah mengonfirmasi + service menolak panggilan re-entrant (Busy) |
| Hotbar | tidak disentuh langsung; mengikuti event inventory. Bibit 0 tetap di slot (redup), aktif lagi setelah beli (perilaku M3.3) |
| Debug | tidak ada pemberian coin/item di build: debug tools hanya `UNITY_EDITOR || DEVELOPMENT_BUILD`, Web build `m_Development: 0` |

## 4. Kontrol
| Input | Aksi |
|---|---|
| E / tombol sentuh Interact dekat Rio | buka market |
| Mouse / sentuh | tab Buy/Sell, pilih baris, − / + / Max, Purchase/Sell, Close (X) |
| Keyboard | W/S atau ↑/↓ pilih item · A/D atau ←/→ kuantitas · Tab ganti tab · E konfirmasi · Esc tutup |
| Saat market terbuka | B/I (tas) diabaikan; joystick tertutup backdrop |

## 5. Konfigurasi (Inspector)
- `Data/Economy/MarketConfig_Rio.asset`: Buy = `seed_tropical`; Sell = `crop_tropical_fruit`, `fish_reef`; max per transaksi 99.
- Harga: `Data/Items/item_seed_tropical.asset` (Buy Price 10), `item_crop_tropical_fruit.asset` (Sell Price 18), `item_fish_reef.asset` (Sell Price 12) — nilai ini sudah ada sejak M3.1, tidak diubah.
- `MarketPanelUI`: `submitCooldown` 0,25 s.
- Modal awal 100 coin + 3 bibit + 3 alat: `Data/Items/StarterProfile.asset` (tidak diubah).

## 6. Publik / lokal
| | Publik (`FirstPlayable`, tracked) | Lokal (`GameplayPrototype`, tidak dipush) |
|---|---|---|
| Rio | prefab `Prefabs/World/Npc/Npc_Rio.prefab` dengan placeholder berlabel "Rio (TEMP)" | prefab yang sama + `NpcVisualSet_Rio` (lokal) |
| Market | prefab `Prefabs/UI/MarketPanel.prefab` (inisial "R", koin kuning, label item) | sama + `LocalArtBindings`: portrait, ikon coin, ikon tab Buy/Sell, ikon close |
| Data | `Data/Economy/MarketConfig_Rio.asset` | `Data/LocalArt/NpcVisualSet_{Rio,Maya,Kai}.asset` |
GUID scan: 430 file publik, 0 referensi tak ter-resolve (tidak ada GUID art lokal di file publik).

## 7. Hasil pengujian (aktual)
### 7.1 EditMode — **204/204 lulus** (176 + 28 baru di `MarketTests`)
Harga 10/18/12 dari data; modal awal 100 + 3 bibit + alat; beli 1 & banyak; saldo pas sampai 0; saldo kurang; tas penuh termasuk stack sebagian; restock hotbar bibit 0; jual 1 & banyak & dari beberapa stack; barang kurang; batas coin; jumlah 0/−1/−999/100/int.Max/int.Min; total 64-bit; re-entrancy (event Changed mencoba beli lagi → Busy, hanya 1 transaksi); event `Traded` sekali per sukses; end-to-end 100 → 136; validasi config; arah hadap NPC (5 kasus).
Satu test sempat gagal karena asumsi test saya salah (mengharapkan `Hotbar.SlotChanged`, padahal HotbarUI redraw lewat `Inventory.Changed`); test diperbaiki, kode tidak diubah.

### 7.2 Play Mode — GameplayPrototype (art lokal), input keyboard disuntikkan
| Uji | Hasil |
|---|---|
| Rio dari plaza | jalan dari (2, 0.5) ke kios; prompt "[E] Shop at Rio's Market" muncul dekat Rio |
| Collision | dari kiri berhenti x 9,47; dari bawah berhenti y 1,07 (footprint kaki saja) |
| Y-sort | pemain di bawah Rio tampil di depan; pemain di belakang Rio tertutup Rio |
| Buka (E) | market terbuka, Rio menghadap Left (ke pemain), gate aktif, jam pause, hotbar disembunyikan |
| Tahan D saat terbuka | pemain tidak bergerak; kuantitas +1 sekali |
| Beli x2 (E) | 100 → 80, bibit 3 → 5, "Bought 2 Tropical Seeds for 20 coins." |
| Burst satu frame (E + klik tombol + 2× Submit) | **+1 transaksi** |
| 3 klik dalam 0,1 s | **+1 transaksi** (cooldown); klik setelah cooldown +1 |
| Sell tanpa barang | "You don't have enough Tropical Fruit.", tidak ada perubahan |
| End-to-end (setup test: coin 100, 3 buah + 1 ikan) | jual 3 buah → 154, jual 1 ikan → 166, beli 3 bibit → **136** |
| B saat market terbuka | tas tetap tertutup |
| Fokus hilang/kembali | market tetap terbuka, jam tetap pause; tidak ada transaksi |
| Esc | tertutup; jam jalan lagi (+1,01 s per 1 s), hotbar kembali, Rio menghadap Down, pemain bisa berjalan |
| Tas terbuka lalu E dekat Rio | market tidak terbuka |
| Coin 5 | "Not enough coins.", Purchase ditolak |
| Tas penuh (bibit 99 + driftwood) | "Inventory full…", Purchase ditolak, coin tetap |
| Bibit 0 → beli 1 | slot hotbar bibit tetap ada (redup), setelah beli: available, count 1 |
| Saat fishing | `CanInteract` dan `Open()` = False |
| Malam 21:00 | market & Rio terbaca |
| Responsif 844×390 (Game view) | panel muat, tombol besar, joystick/Interact tertutup backdrop |
Catatan: memberi barang/coin pada skenario hanya setup test di Editor (Play Mode), bukan fitur build.

### 7.3 Play Mode — FirstPlayable (publik, placeholder)
Rio placeholder tampil (art tidak dipakai); tombol sentuh **Interact** membuka market, Rio menghadap Left; ketuk + + → qty 3; ketuk Purchase → coin 100 → 70, "Bought 3 Tropical Seeds for 30 coins."; ketuk Close → tertutup.

### 7.4 Regresi
| Milestone | Hasil |
|---|---|
| M3.1 (45 langkah) | gerak WASD, tabrakan rumah/air, hotbar 1–5/numpad, tas B/I/Esc, drag mouse & sentuh, joystick, tas penuh, dialog papan — identik |
| M3.2 waktu | tas/dialog pause, fokus, timeScale 0, ×10, dev pause, siang/malam & tint — identik |
| M3.3 farming (sesi baru) | till → tanam → siram → 4 fase (0,68 / 0,81 / 0,78 / 1,00) → panen; bibit 0 redup; 6 plot — identik |
| M3.4 fishing | dermaga (y −11,91, x ±0,71), semua hasil, timeout 8,01 s, fokus pause, spam +1, tas penuh, malam; jam & tanaman tetap berjalan saat fishing (+2,58 s) — identik |
| Console | 0 error, 0 warning |

## 8. Keputusan & risiko (untuk review founder)
- **Harga dari item asset** (bukan disalin ke config market) supaya hanya ada satu angka per item. Buy Price bibit, Sell Price buah/ikan sudah 10/18/12 sejak M3.1.
- **Rio di depan kios** (bukan di belakang meja): di belakang meja hanya ubun-ubun yang terlihat karena meja kios tinggi.
- **Langsung ke market** saat Interact (sapaan ditampilkan di header), tanpa dialog terpisah.
- Market memakai pause waktu seperti tas (usulan paket; sesuai instruksi founder).
- Refresh/reload browser masih me-reset sesi (coin, tas, tanaman) — save permanen bukan scope M3.5 (T-41).
- Ikon item/NPC: sprite di-import tanpa kompresi untuk kalibrasi (seperti Pack 01, T-43).

## 9. Backlog baru
T-52 save lokal progres termasuk coin/transaksi (gabung T-41) · T-53 animasi Rio (idle breathing opsional) & animasi transaksi · T-54 SFX market (beli/jual/gagal) · T-55 gamepad untuk market (V-3) · T-56 Maya/Kai di game (milestone lain) · G-1 metrik perangkat nyata & Safari iOS.

## 10. Web build & preview
| Item | Hasil |
|---|---|
| Build Local | **`M3-05-20261126-2211`** — `Tools/Build-Web.ps1 -Variant Local`, Unity Editor ditutup; exit 0; 9:05; RAM bebas 3,11 → terendah 0,54 GB; 10,17 MiB; verifikasi lulus; `version.json` v0.3.5 M3.5 `local-art` (commit dasar `08dea50`, dirty — belum di-commit) |
| Preview (disetujui founder) | `Deploy-Web.ps1 -SkipBuild -BuildPath Builds/Web/M3-05-20261126-2211 -ArtworkApproved` → deployment **`ad464534`**: https://ad464534.lifetopia-play.pages.dev · alias https://preview.lifetopia-play.pages.dev |
| HTTP | 200 untuk index/data/framework/wasm/loader, wasm `application/wasm` + `br`, `no-cache, no-transform`, path tidak ada → 404. Alias `preview.` sempat masih M3.4 pada cek pertama (propagasi), ±30 s kemudian `version.json` = `M3-05-20261126-2211` |
| Production | tidak berubah: skrip `5784f2eb` sebelum & sesudah; `wrangler pages deployment list` production terbaru `5784f2eb`; `play.lifetopiaworld.io/version.json` = `M3-04-20261126-2103` |
| Desktop 1280×720 (browser otomatis, input asli) | 60,3 FPS, 0 error (6 warning engine yang dikenal). Jalan W/D ke Rio, prompt muncul, **E** membuka market; **klik mouse** + + Purchase → coin 100 → 70, bibit 3 → 6, "Bought 3 Tropical Seeds for 30 coins."; klik tab Sell → buah/ikan Owned 0, tombol nonaktif + alasan; Esc menutup |
| Emulasi HP 844×390 (touch) | 51 FPS, 0 error; **tap tombol Interact** membuka market; tap + lalu **tap Purchase** → 100 → 80, bibit 3 → 5; tap tab Sell bekerja; panel muat di layar |
| Tidak diuji | HP nyata, Safari iOS, gamepad, build Public (tidak dibuat ulang untuk M3.5 demi RAM/waktu; scene publik diuji di Editor Play Mode, kode publik terkompilasi di Editor tanpa error, GUID scan bersih) |

Screenshot lokal (`Documentation/Progress/M3-05-screenshots-local/`, tidak dipush): `npc_rio_stall_front`, `npc_rio_stall_night`, `npc_ysort_player_front`, `npc_ysort_player_behind`, `market_buy_desktop`, `market_buy_qty2_desktop`, `market_buy_success`, `market_sell_desktop`, `market_sell_success`, `market_fail_not_enough_coins`, `market_fail_inventory_full`, `hotbar_seed_zero_before`, `hotbar_seed_restocked`, `market_night`, `market_buy_mobile_844x390_editor`, `public_market_placeholder`, `web_rio_prompt`, `web_market_open`, `web_market_buy_desktop`, `web_market_sell_desktop`, `web_bag_after_buy`, `web_mobile_rio_prompt`, `web_market_mobile_open`, `web_market_mobile_buy_success`, `web_market_mobile_sell`.

## 11. Rekomendasi berikutnya
Founder acceptance test di preview (desktop + HP nyata): beli/jual, tas penuh, coin kurang, siklus farming → jual → beli bibit → tanam. Setelah disetujui: commit + push M3.5, lalu (dengan persetujuan terpisah) production. M3.6 Housing belum dimulai.

## 12. Founder acceptance & penutupan (26 Nov 2026)

### 12.1 Founder-reported manual playtest — 16/16 lulus
Dilakukan **oleh founder** lewat Cloudflare Preview (`ad464534`, build `M3-05-20261126-2211`), termasuk di **HP nyata**. Ini laporan founder, bukan pengukuran Claude; model HP, browser dan angka performa tidak dilaporkan (tetap backlog G-1).

| # | Pengujian | Hasil |
|---|---|---|
| 1 | Desain NPC Rio terlihat menarik | lulus |
| 2 | Ukuran Rio sesuai dengan karakter pemain | lulus |
| 3 | Posisi Rio di depan kios sesuai | lulus |
| 4 | Interaksi dengan Rio berfungsi | lulus |
| 5 | Tampilan Market UI menarik | lulus |
| 6 | Tab Buy dan Sell berfungsi | lulus |
| 7 | Pembelian bibit berhasil | lulus |
| 8 | Penjualan hasil panen berhasil | lulus |
| 9 | Penjualan ikan berhasil | lulus |
| 10 | Jumlah dan saldo coin akurat | lulus |
| 11 | Transaksi dengan saldo tidak cukup ditolak | lulus |
| 12 | Bibit kembali aktif di hotbar setelah dibeli | lulus |
| 13 | Jam berhenti saat market terbuka | lulus |
| 14 | Market nyaman digunakan melalui HP | lulus |
| 15 | Farming dan fishing tetap berfungsi | lulus |
| 16 | Tidak ada crash atau bug yang mengganggu | lulus |

### 12.2 Pengujian aktual oleh Claude (ringkasan)
EditMode **204/204**; Play Mode market lengkap (keyboard, burst/double submit, E2E 100 → 136, coin kurang, tas penuh, restock hotbar, tidak overlap tas/fishing, fokus, malam); regresi M3.1–M3.4 identik; scene publik + sentuh; Web preview desktop (klik mouse asli) dan emulasi HP 844×390 (tap asli). Rincian §7 dan §10.
**Tidak diuji oleh Claude:** HP nyata (hanya laporan founder), Safari iOS, gamepad, pengukuran performa perangkat, **Public Web Build M3.5** (tidak dibuat; lihat §12.5).

### 12.3 Baseline ekonomi (disetujui)
seed_tropical beli **10** · crop_tropical_fruit jual **18** · fish_reef jual **12** (asset item) · modal awal 100 coin + 3 bibit + cangkul/penyiram/joran · maks 99 per transaksi · cooldown konfirmasi 0,25 s · stok tak terbatas, tanpa biaya/jam buka. Perubahan berikutnya butuh persetujuan founder + playtest.

### 12.4 Dipertahankan
Rio di depan kios; Interact langsung membuka market; sapaan "Welcome, traveler!" di header; market menghentikan jam seperti tas; inventory, hotbar, coin, farming, fishing dan game time tidak diubah semantiknya.

### 12.5 Kompatibilitas repository publik
GUID scan file publik: 0 referensi tak ter-resolve (tidak ada GUID art lokal). Scene publik `FirstPlayable` diuji di Editor Play Mode (placeholder Rio + market, sentuh). Kode terkompilasi tanpa error di Editor. **Public Web Build terbaru belum dibuat/diuji untuk M3.5** — Public build terakhir yang diuji adalah `M3-04-20261126-1844` (M3.4).

### 12.6 Commit, push & production
(diisi setelah commit dan deploy)
