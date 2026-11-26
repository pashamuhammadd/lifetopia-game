# M3.1 — Item, Inventory & Hotbar

Tanggal: 26 November 2026 · Basis: commit M2 `c7b4b3f` · Status: **disetujui founder 26 November 2026** (finalisasi; commit menunggu review daftar file). Tugas tertunda: lihat §14.

Ruang lingkup hanya M3.1. Tidak ada game time, farming, fishing, market, housing, multiplayer, blockchain, login, database, atau mata uang sungguhan. `lifetopia-platform` tidak disentuh.

---

## 1. Ringkasan hasil

| Kebutuhan | Hasil |
|---|---|
| Item ScriptableObject (ID stabil, nama, deskripsi, kategori, ikon, max stack, stackable, harga jual/beli) | ✅ `ItemDefinition` + 8 aset di `Data/Items` |
| 6 kategori awal | ✅ Tool, Seed, Crop, Fish, Material, Furniture |
| Inventory 12 slot, stack 99, Add/Remove/Has/Count/Move/Swap/Merge/CanAdd | ✅ `Inventory` (C# murni, operasi all-or-nothing) |
| Tidak negatif, tidak overflow, tidak duplikasi, tidak hilang saat gagal | ✅ diuji (termasuk 2.000 operasi acak) |
| Tas penuh → pesan, item tidak dihapus diam-diam | ✅ toast "Your bag is full. … was not added." |
| UI berbasis event, bukan polling | ✅ UI hanya refresh dari event `SlotChanged`/`Changed`/`SelectionChanged` |
| Hotbar 5 slot, keyboard 1–5 + touch | ✅ `Hotbar` (referensi item, tanpa jumlah sendiri) + `HotbarUI` |
| 3 alat awal, tidak habis dipakai | ✅ Hoe, Watering Can, Fishing Rod (`ConsumedOnUse = false`) |
| Starter: 100 coin, 3 Tropical Seeds, 3 alat; sekali per sesi baru | ✅ `StarterProfile` + flag `StarterGranted` (ikut tersimpan) |
| State bisa diserialisasi | ✅ `PlayerProfileData` (JsonUtility), divalidasi saat dibaca |
| Panel inventory 4×3, detail, close; hotbar bawah; coin counter | ✅ prefab `InventoryHUD` |
| Buka: B / I / tombol Bag; blokir gerak & interaksi saat terbuka | ✅ via `GameplayInputGate` + backdrop |
| Tema Chibi Tropical Cozy, warna Art Bible, teks Inggris | ✅ palet Art Bible §02, sudut membulat & bayangan lembut dibuat lewat kode |
| Placeholder ikon yang mudah diganti | ✅ tile berwarna per kategori + label pendek; ganti dengan mengisi field **Icon** |
| Scene uji | ✅ `GameplayPrototype.unity` (lokal, berbasis ArtCalibration) + `FirstPlayable.unity` (publik, greybox) |
| Alat uji khusus developer | ✅ `InventoryDebugTools` (hanya Editor / Development Build) |
| EditMode tests | ✅ **77/77 lulus** (27 lama + 50 baru) |
| Regresi Play Mode M1/M2 + keyboard/mouse/touch | ✅ lihat §8 |
| Web build lokal | ✅ berhasil (9,31 MiB, 0 error); diuji desktop + emulasi HP; 1 bug font ditemukan dan diperbaiki (§8.4) |
| Uji HP nyata | ❌ **belum dilakukan**: pending V-2 / gate G-1, wajib sebelum rilis publik |
| Uji gamepad | ❌ **belum dilakukan**: pending V-3 (tidak ada perangkat) |

---

## 2. Arsitektur

```
Data (ScriptableObject, read-only saat runtime)        Runtime state (C# murni, bisa diserialisasi)
─────────────────────────────────────────────          ───────────────────────────────────────────
ItemDefinition ×8  ──┐                                  PlayerProfile
ItemDatabase  (IItemCatalog: ID → definisi)             ├── Inventory (12 slot: item + qty)
StarterProfile (100 coin + item + slot hotbar)          ├── Hotbar (5 referensi item + slot terpilih)
                     │                                  ├── Wallet (coin)
                     ▼                                  └── StarterGranted
Scene ─ PlayerProfileHost (MonoBehaviour) ───────────►  ToData() / TryFromData() ⇄ PlayerProfileData (DTO save)
          │  event Notification(string)
          ▼
UI  ─ InventoryPanelUI · HotbarUI · CoinCounterUI · HudToastUI · ItemSlotView
          ▲                                   ▲
Input ─ PlayerInputReader (Inventory, HotbarSlot)   GameplayInputGate (blokir gerak/interaksi)
```

Prinsip yang dipakai:

- **Definisi dipisah dari state.** `ItemDefinition` adalah "kartu katalog": satu aset per jenis barang dan tidak pernah diubah saat game berjalan. Yang berubah hanyalah `Inventory` (berapa banyak, di slot mana).
- **Inventory dan Hotbar ditulis sebagai C# biasa, bukan MonoBehaviour.** Karena tidak butuh scene, keduanya bisa diuji dengan EditMode test dalam milidetik, dan nanti bisa disimpan atau dimuat tanpa tergantung GameObject.
- **Semua operasi bersifat all-or-nothing.** Setiap fungsi memeriksa dulu apakah operasinya mungkin, baru mengubah data. Jika gagal, ia mengembalikan `InventoryResult` (mis. `InventoryFull`) dan tidak ada data yang berubah.
- **Hotbar hanya menyimpan referensi.** Jumlah barang selalu dibaca langsung dari inventory, jadi angka di hotbar dan di tas tidak mungkin berbeda.
- **UI digerakkan oleh event.** Inventory memancarkan `SlotChanged(i)` hanya untuk slot yang benar-benar berubah, lalu `Changed` satu kali. UI memperbarui slot itu saja; tidak ada pengecekan seluruh tas setiap frame.

---

## 3. File baru dan perubahan

### 3.1 Publik (aman untuk repo — tidak merujuk art lokal)

Skrip baru (`Assets/_Lifetopia/Scripts/`):

| File | Isi |
|---|---|
| `Items/ItemCategory.cs` | enum 6 kategori |
| `Items/ItemDefinition.cs` | ScriptableObject definisi item |
| `Items/ItemDatabase.cs` | daftar semua item + `IItemCatalog` (cari item dari ID) + validasi data |
| `Inventory/InventoryResult.cs` | kode hasil operasi (Success, InventoryFull, …) |
| `Inventory/Inventory.cs` | inventory 12 slot + `InventorySlot` |
| `Inventory/Hotbar.cs` | hotbar 5 referensi |
| `Inventory/Wallet.cs` | coin lokal |
| `Inventory/StarterProfile.cs` | ScriptableObject paket awal |
| `Inventory/PlayerProfile.cs` | gabungan inventory + hotbar + wallet; starter sekali; konversi save |
| `Inventory/PlayerProfileData.cs` | DTO save berversi |
| `Inventory/PlayerProfileHost.cs` | komponen scene pemilik profil + event pesan |
| `Inventory/InventoryMessages.cs` | teks pesan bahasa Inggris |
| `UI/Inventory/UiPalette.cs` | warna Art Bible + warna placeholder per kategori |
| `UI/Inventory/RoundedSprites.cs` | membuat sprite sudut membulat/bayangan lembut lewat kode |
| `UI/Inventory/RoundedImage.cs` | memasang sprite itu ke `Image` saat runtime |
| `UI/Inventory/ItemSlotView.cs` | tampilan satu slot + klik/drag/drop |
| `UI/Inventory/InventoryPanelUI.cs` | jendela Backpack |
| `UI/Inventory/HotbarUI.cs` | hotbar di layar |
| `UI/Inventory/CoinCounterUI.cs` | penghitung coin |
| `UI/Inventory/HudToastUI.cs` | pesan singkat |
| `DevTools/InventoryDebugTools.cs` | tombol F uji (Editor/Development Build saja) |
| `AssemblyInfo.cs` | `InternalsVisibleTo` untuk test |

Aset baru:

- `Data/Items/item_*.asset` ×8, `ItemDatabase.asset`, `StarterProfile.asset`
- `Prefabs/UI/InventoryHUD.prefab`
- meta folder `Data.meta`, `Data/Items.meta`, `Prefabs/UI.meta`, `Scripts/Items.meta`, `Scripts/Inventory.meta`, `Scripts/UI/Inventory.meta`, `Scripts/DevTools.meta`

Test baru: `Tests/EditMode/InventoryTests.cs`, `HotbarAndProfileTests.cs`, `ItemDataTests.cs`.

Diubah:

| File | Perubahan |
|---|---|
| `Input/LifetopiaControls.inputactions` | aksi baru `Inventory` (B, I, Gamepad North) dan `HotbarSlot` (1–5, Numpad 1–5); murni tambahan |
| `Scripts/Controls/PlayerInputReader.cs` | event `InventoryTogglePerformed`, `HotbarSlotPerformed(int)` |
| `Scripts/Core/GameplayInputGate.cs` | `IsBlockedExcept(source)` |
| `Prefabs/Player.prefab` | referensi dua aksi baru |
| `Scenes/FirstPlayable.unity` | `PlayerProfileHost` + `InventoryDebugTools` di GameSystems, instance `InventoryHUD`, prompt interaksi naik ke y = 124, hint keyboard diperbarui |
| `Tests/EditMode/Lifetopia.Tests.EditMode.asmdef` | referensi `UnityEngine.UI` |

### 3.2 Lokal saja (tidak dipublikasikan)

| File | Alasan |
|---|---|
| `Scenes/GameplayPrototype.unity` | salinan ArtCalibration → memakai art A0 |
| `Prefabs/A0/Player_A0.prefab` (diubah: referensi dua aksi baru) | folder A0 sudah lokal-only |
| `Settings/BuildProfiles/Web.asset` | scene pertama = GameplayPrototype (lokal); cadangan versi M2 disimpan lokal di luar repo |
| `Documentation/Progress/M3-01-screenshots-local/` | screenshot berisi art lokal |

`.git/info/exclude` (aturan lokal, tidak ikut repo) disesuaikan: `Data/` kini hanya mengecualikan `Data/Characters/` (animasi yang merujuk art), sehingga `Data/Items` bisa dipublikasikan; `GameplayPrototype.unity` dan folder screenshot ditambahkan.

---

## 4. Penjelasan setiap script C#

**`ItemDefinition`** — ScriptableObject adalah file data yang dibuat dan diedit di Inspector, mirip "formulir" yang disimpan sebagai aset. Field-nya `private` + `[SerializeField]` agar bisa diisi di Inspector tetapi tidak bisa diubah oleh kode game; kode hanya membaca lewat properti read-only (`ItemId`, `DisplayName`, …). `MaxStack` otomatis 1 untuk item non-stackable dan dibatasi maksimal 99. `ConsumedOnUse` bernilai `false` untuk kategori Tool, jadi alat tidak akan pernah habis. `OnValidate()` dijalankan Unity setiap kali nilai diubah di Inspector; ia merapikan angka yang tidak masuk akal.

**`ItemDatabase`** — daftar semua `ItemDefinition`. Save game hanya menyimpan ID (teks), bukan referensi objek; database inilah yang menerjemahkan ID kembali menjadi definisi (`TryGet`). `Validate()` dipakai oleh test untuk mendeteksi ID kosong, ganda, atau format salah.

**`Inventory`** — larik 12 `InventorySlot` (item + jumlah).
- `AddItem` mengisi stack yang sudah ada dulu, baru slot kosong.
- `RemoveItem` mengambil dari slot paling belakang.
- `MoveSlot` memindahkan ke slot kosong atau menggabungkan bila semuanya muat.
- `SwapSlot` menukar dua slot.
- `MergeStack` memindahkan sebanyak yang muat dan sisanya tetap di slot asal.

Semua fungsi mengembalikan `InventoryResult`. Kapasitas dihitung dengan `long` agar `int.MaxValue` pun tidak menyebabkan overflow.

**`Hotbar`** — 5 referensi `ItemDefinition`.
- `Assign` hanya menerima item yang memang ada di tas, dan sebuah item hanya boleh berada di satu slot (bila dipasang ulang, ia pindah).
- Hotbar berlangganan event `Inventory.Changed`: bila jumlah sebuah item menjadi 0, slotnya dikosongkan otomatis.

**`Wallet`** — coin lokal, tidak pernah negatif, dengan batas maksimum 9.999.999.

**`StarterProfile`** — ScriptableObject paket awal: 100 coin, Hoe/Watering Can/Fishing Rod (slot hotbar 1–3), 3 Tropical Seeds (slot 4).

**`PlayerProfile`**
- `GrantStarterKit` mencoba paketnya dulu pada salinan inventory (*dry run*), dan baru memberikannya bila semuanya muat. Setelah itu `StarterGranted = true`, sehingga paket tidak bisa diberikan dua kali.
- `ToData()` membuat snapshot untuk save.
- `TryFromData()` menolak **seluruh** save bila ada yang janggal (ID tak dikenal, jumlah 0/negatif/lebih dari stack, hotbar ganda, versi lebih baru, coin negatif), alih-alih memuat sebagian.

**`PlayerProfileData` / `SlotData`** — kelas `[Serializable]` berisi angka dan string saja; kompatibel dengan `JsonUtility`. Ada `version = 1` untuk migrasi di masa depan.

**`PlayerProfileHost`** — satu-satunya MonoBehaviour di sisi data. Profil dibuat sekali saat pertama dibutuhkan (`Awake` atau akses pertama), dan paket awal diberikan saat itu. Kode gameplay nanti memakai `GiveItem`/`TakeItem`, yang otomatis memancarkan `Notification` bila gagal.

**`InventoryMessages`** — memetakan `InventoryResult` ke kalimat bahasa Inggris untuk pemain.

**`PlayerInputReader`** (diubah) — tetap menjadi satu-satunya pembaca input. Event `InventoryTogglePerformed` dipancarkan bahkan saat gameplay diblokir (karena UI-lah yang memutuskan). `HotbarSlotPerformed(index)` memakai urutan binding: binding ke-n berarti slot n % 5, sehingga tombol 1–5 dan Numpad 1–5 sama-sama berfungsi.

**`GameplayInputGate.IsBlockedExcept`** (baru) — memungkinkan tas dan hotbar bertanya "apakah ada modal *lain* yang terbuka, misalnya dialog?".

**`ItemSlotView`** — tampilan satu slot. Komponen ini tidak menyimpan state game; pemiliknya memanggil `Show(item, jumlah)`. Ia mengimplementasikan interface EventSystem (`IPointerClickHandler`, `IBeginDragHandler`, `IDropHandler`, …), sehingga mouse dan sentuhan bekerja dengan kode yang sama. Bila item tidak punya ikon, ia menampilkan tile berwarna kategori dengan label pendek ("HOE", "WC", "TS").

**`InventoryPanelUI`**
- Membuka dan menutup jendela, memblokir gate, berlangganan Escape selama terbuka, memilih slot, menampilkan detail, dan menangani drag & drop (item sama → `MergeStack`, lainnya → `SwapSlot`).
- Tidak mau terbuka bila dialog sedang terbuka.

**`HotbarUI`**
- Saat tas tertutup, 1–5 atau tap berfungsi memilih slot.
- Saat tas terbuka dan ada item terpilih, 1–5 atau tap memasang item itu di slot tersebut; menekan slot yang sama sekali lagi melepasnya.
- Men-drag slot tas ke slot hotbar juga memasang item.
- Diabaikan saat dialog terbuka.

**`CoinCounterUI`**, **`HudToastUI`** — penghitung coin dan pesan singkat. Toast mematikan komponennya sendiri saat tidak ada pesan, sehingga `Update` tidak berjalan sia-sia.

**`UiPalette`**, **`RoundedSprites`**, **`RoundedImage`** — warna Art Bible dan sprite sudut membulat yang dibuat **lewat kode** saat runtime (satu kali per radius, lalu disimpan di cache). Hasilnya tidak ada file PNG UI baru. Bila nanti ada art UI final, isi sprite pada `Image`; `RoundedImage` akan membiarkannya.

**`InventoryDebugTools`** — isinya dibungkus `#if UNITY_EDITOR || DEVELOPMENT_BUILD`, jadi di build rilis komponennya kosong (bukan fitur pemain). Tombol:
- F1 +1 Seeds, F2 −1 Seeds
- F3 +1 Fish
- F4 +10 Material, F6 +99 Material
- F7 +1 Furniture
- F8 +10 coin, F9 −10 coin
- F10 sembunyikan bantuan

Tombol F bentrok dengan shortcut browser, jadi gunakan di Editor saja.

---

## 5. Struktur ScriptableObject dan data awal

| ID (stabil) | Nama | Kategori | Stack | Jual | Beli |
|---|---|---|---|---|---|
| `tool_hoe` | Hoe | Tool | tidak (1) | – | – |
| `tool_watering_can` | Watering Can | Tool | tidak (1) | – | – |
| `tool_fishing_rod` | Fishing Rod | Tool | tidak (1) | – | – |
| `seed_tropical` | Tropical Seeds | Seed | 99 | 5 | 10 |
| `crop_tropical_fruit` | Tropical Fruit | Crop | 99 | 18 | – |
| `fish_reef` | Reef Fish | Fish | 99 | 12 | – |
| `material_driftwood` | Driftwood | Material | 99 | 2 | – |
| `furniture_wooden_stool` | Wooden Stool | Furniture | tidak (1) | 25 | 50 |

Harga beli benih (10), harga jual panen (18), dan harga jual ikan (12) diambil dari tabel ekonomi GDD. Nilai lainnya (jual benih 5, driftwood 2, stool 50/25) adalah **angka sementara** untuk pengujian kategori; ekonomi tetap berstatus draft (DECISIONS "Still open"). Crop, Fish, Material, dan Furniture belum bisa didapat pemain secara normal. Di M3.1 item-item itu hanya muncul lewat alat developer.

`StarterProfile`: `startingCoins = 100`; items = Hoe ×1 → slot 0, Watering Can ×1 → slot 1, Fishing Rod ×1 → slot 2, Tropical Seeds ×3 → slot 3.

---

## 6. Konfigurasi di Inspector

**Scene (`GameSystems`):**
- `PlayerProfileHost`: Item Database = `ItemDatabase`, Starter Profile = `StarterProfile`.
- `InventoryDebugTools`: Host = `PlayerProfileHost`; Seed, Fish, Material, Furniture = aset item yang sesuai.

**Instance `InventoryHUD`** (child dari `GameplayUI/SafeArea`, tepat setelah `TouchControls`):
- `InventoryPanelUI` dan `HotbarUI`: isi **Host**, **Input** (`PlayerInputReader` pada Player), dan **Gate** (`GameplayInputGate` pada GameSystems).
- `CoinCounter` → `CoinCounterUI.Host`; `Toast` → `HudToastUI.Host`.
- Referensi internal prefab (slot, label detail, tombol) sudah terisi di prefab. Jangan diubah kecuali menata ulang layout.

**Player:** `PlayerInputReader` → *Inventory Action* = `Gameplay/Inventory`, *Hotbar Slot Action* = `Gameplay/HotbarSlot`, *Hotbar Slot Count* = 5.

**Urutan gambar UI** (atas ke bawah di Hierarchy = belakang ke depan):
1. `TouchControls`
2. `InventoryHUD`, berisi: InventoryWindow (backdrop + panel), CoinCounter, BagButton, Hotbar, Toast, DragGhost
3. `InteractionPrompt`
4. `DialoguePanel`

Backdrop berada di atas joystick, sehingga sentuhan pada joystick tertahan saat tas terbuka. Hotbar berada di atas backdrop supaya tetap bisa di-tap untuk memasang item.

**Layout** (referensi 1280×720, match 0.5):

| Elemen | Posisi | Ukuran |
|---|---|---|
| Hotbar | tengah bawah, x 424–856, y 14–110 | 438×96 |
| Joystick | x 50–290 | – |
| Tombol interaksi | x 1055–1205 | – |
| Coin | kanan atas | 190×58 |
| Bag | kanan atas, di bawah coin | 104×104 |
| Toast | tengah atas, y −100 | – |
| Prompt interaksi | dinaikkan ke y = 124 | – |

Hotbar tidak bertumpuk dengan joystick maupun tombol interaksi. Prompt dinaikkan agar tidak menyentuh hotbar.

---

## 7. Cara membuat item baru

1. Di Project window: klik kanan `Assets/_Lifetopia/Data/Items` → **Create → Lifetopia → Items → Item Definition**. Beri nama file `item_<id>.asset`.
2. Isi **Item Id** dengan huruf kecil dan garis bawah, misalnya `fish_parrotfish`. **Jangan pernah mengganti ID setelah rilis**, karena save memakai ID ini.
3. Isi Display Name, Description, Category, Stackable / Max Stack Size, Sell Price, Buy Price. Icon boleh dikosongkan; placeholder akan muncul otomatis.
4. Buka `ItemDatabase.asset` → tambahkan item ke daftar **Items**.
5. Jalankan **Window → General → Test Runner → EditMode → Run All**. `ItemDataTests` akan gagal bila ID ganda, format salah, deskripsi kosong, atau item belum dimasukkan ke database.
6. Untuk memasang ikon final: seret sprite (128×128, padding 12 px sesuai Art Bible) ke field **Icon**.

---

## 8. Hasil pengujian (aktual)

### 8.1 EditMode — Unity Test Runner via MCP
**77 test, 77 lulus, 0 gagal.** Rinciannya: 27 test lama (kamera, gate, gerak, animasi) ditambah 50 test baru.

| Kebutuhan | Test |
|---|---|
| Add & remove | `AddAndRemove_UpdateCounts`, `RemoveAcrossStacks_TakesExactAmount`, `Add_TopsUpExistingStackBeforeUsingEmptySlot` |
| Stack 99 | `StackLimit_Is99_AndOverflowGoesToNextSlot` |
| Inventory penuh | `FullInventory_RejectsWholeAdd_AndChangesNothing`, `HugeQuantity_DoesNotOverflow` |
| Jumlah tidak valid | `InvalidQuantity_IsRejected(0/−1/int.MinValue)`, `NullItem_IsRejected`, `RemoveMoreThanOwned_FailsAndKeepsItems` |
| Non-stackable | `NonStackable_UsesOneSlotEach`, `Tools_AreNonStackable_AndNeverConsumed` |
| Move & swap | `MoveSlot_ToEmpty_MovesWholeStack`, `MoveSlot_OntoOtherItem_FailsWithoutChange`, `MoveSlot_OntoSameItem_MergesOnlyWhenEverythingFits`, `SwapSlot_ExchangesContents` |
| Merge | `MergeStack_MovesWhatFits_KeepsRemainder`, `MergeStack_RejectsDifferentItemsAndEmptyTarget` |
| Pencegahan duplikasi | `NoDuplication_AcrossManyMovesSwapsAndMerges` (2.000 operasi acak, total per item tetap), `Count_IsReadFromInventory_NotDuplicated`, `Item_IsOnAtMostOneSlot`, `SaveData_WithImpossibleValues_IsRejectedWhole` |
| Konsistensi hotbar | `StaysConsistent_WhenItemIsMovedInInventory`, `ClearsSlot_WhenItemRunsOut`, `Assign_RequiresItemInInventory`, `ClearSlot_RemovesReferenceOnly`, `Select_…` |
| Starter state | `StarterKit_Gives100Coins_3Seeds_3Tools_OnHotbar`, `StarterKit_IsGrantedOnlyOnce`, `StarterKit_NotGrantedAgain_AfterSaveRoundTrip`, `StarterKit_ThatDoesNotFit_GrantsNothing`, `StarterAsset_MatchesGdd` |
| Save/serialisasi | `SaveData_RoundTripsThroughJson` |
| Event | `Events_FireOnlyForChangedSlots_AndNotOnFailure` |
| Data | `Database_HasValidUniqueIds`, `Database_ContainsEveryItemAsset`, `ItemIdFormat(...)`, `PlaceholderLabel(...)` |
| Wallet | `Wallet_NeverNegative_NoOverflow` |

### 8.2 Regresi Play Mode di Editor (input disuntikkan lewat Input System)

Dijalankan di **GameplayPrototype** (art lokal) dan **FirstPlayable** (greybox publik), masing-masing 113 langkah. Hasil inventory identik di kedua scene.

| Skenario | Hasil |
|---|---|
| Starter: coin 100, hotbar [Hoe, Can, Rod, Seeds, –], seeds 3 | ✅ |
| Jalan W/D 4,00 u/s, animasi walk/idle, berhenti → idle (M2) | ✅ |
| Tabrakan rumah (y 2,12 di GameplayPrototype), air (y −7,92) | ✅ sama dengan M2 |
| Tombol 2 → slot 2; Numpad 4 → slot 4 | ✅ |
| B buka → gate terblokir; tahan D 0,5 s → posisi tetap, kecepatan 0 | ✅ |
| Saat tas terbuka: 5 → Hoe pindah ke slot 5; 5 lagi → dilepas; 1 → kembali | ✅ |
| B tutup, I buka, Esc tutup; D setelah tutup → bergerak lagi | ✅ |
| Mouse: klik Bag → buka; klik slot 4 (seeds) → detail; klik hotbar 5 → seeds pindah ke slot 5 | ✅ |
| Mouse drag slot 4 → slot 10 (seeds pindah, jumlah 3 tetap) | ✅ |
| Mouse klik × → tutup | ✅ |
| Touch (Touchscreen virtual): tap hotbar 3 → pilih; tap Bag → buka | ✅ |
| Touch pada joystick saat tas terbuka → **tidak bergerak** (tertahan backdrop) | ✅ |
| Touch drag slot tas → hotbar 4 (seeds pindah dari 5 ke 4, tidak ganda) | ✅ |
| Tap × → tutup; joystick setelah tutup → bergerak 4,00 u/s, dilepas → berhenti | ✅ |
| Tas penuh: 8×(+99 Driftwood) = 792; tambah lagi → toast "Your bag is full. Driftwood was not added.", total tetap 792 | ✅ |
| Ikan saat penuh → "Your bag is full. Reef Fish was not added." | ✅ |
| Seeds dikurangi sampai 0 → slot hotbar seeds kosong otomatis; kurangi lagi → "You don't have enough Tropical Seeds." | ✅ |
| Dialog terbuka → B tidak membuka tas, tombol 3 tidak mengganti pilihan | ✅ |
| Tas terbuka di dekat papan INFO → prompt interaksi hilang, E tidak membuka dialog; tas ditutup → E membuka dialog | ✅ |
| Focus lost saat tas terbuka → input dilepas (resets = 1), tas tetap konsisten | ✅ |

Catatan jujur tentang metode:
- Touch diuji dengan perangkat **Touchscreen virtual** dari Input System di Editor, bukan jari sungguhan.
- Satu langkah skrip awal ("E saat tas terbuka") sempat salah urut karena dialog terbuka ulang. Langkah itu diuji ulang secara terpisah, dan hasilnya tercantum di tabel.
- Animasi di FirstPlayable memakai view greybox M1 (berbasis intent, bukan kecepatan). Itu perilaku M1 yang ada, bukan regresi.

### 8.3 Console
- Setelah semua pengujian: **0 error, 0 warning** dari kode proyek; kompilasi bersih.
- Ada 2 error yang sempat tercatat dari paket **Unity Pipeline** (`ObjectDisposedException: HttpListenerResponse`). Penyebabnya: permintaan screenshot MCP terputus saat domain reload ketika masuk Play Mode. Error ini bukan dari kode game dan hilang setelah Console dibersihkan saat reload berikutnya.

### 8.4 Web build lokal
Build profile `Web` lokal (scene pertama: GameplayPrototype), non-development, output `Builds/Web/M3-01` (folder `Builds/` di-ignore Git).

| Ukuran | Nilai |
|---|---|
| Hasil | **Build Succeeded**, 0 error, 0 warning (BuildReport) |
| Total | 9.764.600 byte (9,31 MiB) |
| `M3-01.data.br` | 2.416.772 byte |
| `M3-01.wasm.br` | 7.241.005 byte |
| `M3-01.framework.js.br` | 75.390 byte |
| `M3-01.loader.js` | 26.983 byte |
| Waktu build | **22,7 menit**, sebagian besar di link/optimasi WASM (`wasm-opt`) |

Dibanding M2 (9,73 MB), ukurannya praktis sama; sistem inventory tidak menambah ukuran secara berarti.

**Kendala memori (aktual):**
- Laptop memiliki 8 GB RAM. Selama build, memori bebas hanya 0,2–0,6 GB, karena Unity Editor, VS Code, dan proses lain tetap berjalan. Akibatnya tahap link WASM sangat lambat, walau build tetap berhasil.
- Pemuatan pertama di Chromium (Playwright) tertahan di 90% (kompilasi WASM) lebih dari 8 menit, lalu *reload* pun timeout. Setelah browser ditutup dan dibuka ulang, game termuat dalam ±1,5–2 detik (file sudah tersimpan di cache browser).
- Kesimpulannya, kemacetan itu terjadi karena tekanan memori pada laptop uji, bukan error build (Console 0 error). Untuk uji Web berikutnya, sebaiknya tutup Unity Editor terlebih dahulu.

**Desktop 1280×720 (Chromium, input keyboard/mouse sungguhan ke canvas):**

| Uji | Hasil |
|---|---|
| Starter HUD (coin 100, Bag, hotbar HOE/WC/FR/TS 3) | ✅ |
| Tombol 2 → sorotan pindah ke slot 2 | ✅ |
| B → Backpack terbuka (detail Hoe); tahan D 0,6 s → dunia tidak bergeser | ✅ |
| Esc → tertutup; tahan D → pemain berjalan ke kanan (walk) | ✅ |
| Klik Bag → buka; klik TS → detail Tropical Seeds; klik hotbar 5 → TS pindah ke slot 5 | ✅ |
| Drag TS slot 4 → slot 10 (ghost terlihat saat drag; jumlah 3 tetap; hotbar tetap konsisten) | ✅ |
| Klik × → tertutup | ✅ |
| FPS 5 s tas tertutup | **60,0**, 0 frame > 33 ms (maks 17,5 ms) |
| FPS 5 s tas terbuka | **59,2**, 3 frame > 33,4 ms (maks 33,6 ms) |
| Console browser | **0 error**, 6 warning engine `getInternalformatParameter` (sama seperti M2) |

**Emulasi HP 844×390, DPR 2, touch via CDP** (bukan HP nyata):

| Uji | Hasil |
|---|---|
| Tap hotbar 3 → terpilih | ✅ |
| Tap Bag → terbuka | ✅ |
| Gesek joystick saat tas terbuka → pemain & handle joystick tidak bergerak | ✅ |
| Tap TS → detail; drag TS ke hotbar 5 → referensi pindah dari slot 4 ke 5 | ✅ |
| Tap × → tertutup; joystick → pemain berjalan | ✅ |
| Panel 760×440 ref → ±454×263 CSS px, muat di 390 px tinggi; tidak menutupi tombol Interact | ✅ |
| Ukuran sentuh: Bag ±62 CSS px, slot hotbar ±45 CSS px, slot tas ±50 CSS px (≥ 44) | ✅ (slot hotbar di batas minimum) |
| FPS 5 s | 60,0 (GPU laptop; **bukan** angka HP) |
| Portrait 390×844 → overlay "Please rotate your device" tampil | ✅ |

Catatan jujur:
- Pada percobaan touch pertama, koordinat tap untuk tombol Bag yang saya hitung keliru (anchor kanan atas dihitung relatif ke tengah layar), sehingga tap meleset dari tombol. Setelah koordinat diperbaiki, skenario dijalankan ulang dan hasil di atas berasal dari run kedua.

**Bug yang ditemukan di Web dan sudah diperbaiki:**
- Font bawaan `LegacyRuntime` di build Web tidak memiliki glyph en dash "–". Akibatnya hint "Hotbar: 1–5" tampil sebagai "**Hotbar: 15**". Di Editor bug ini tidak terlihat karena ada font fallback dari sistem.
- Perbaikan: teks diganti ke tanda hubung ASCII "1-5" di kedua scene dan di `InventoryPanelUI`. EditMode test 77/77 lulus setelah perbaikan.
- **Web build belum diulang** setelah perbaikan ini, karena build berikutnya butuh ±20 menit dengan RAM yang terbatas. Verifikasi visual perbaikan ini di Web masih tertunda (lihat langkah manual §13).

### 8.5 Repo publik tetap bisa di-build tanpa art lokal
Pemindai GUID dijalankan hanya terhadap file yang akan dipublikasikan (`git ls-files -co --exclude-standard`) ditambah paket:
- 254 file publik, 46 YAML dipindai.
- **0 referensi GUID yang tidak terselesaikan.**

`FirstPlayable`, `Player.prefab`, `InventoryHUD.prefab`, dan `Data/Items/*` tidak merujuk art, `Data/Characters`, `Prefabs/A0`, `Tilemaps/A0`, maupun `GameplayPrototype`.

### 8.6 Tidak diuji
- **HP nyata** (Android Chrome / iOS Safari): belum. Gate G-1 tetap wajib sebelum rilis publik.
- Gamepad (tombol North untuk tas): binding ada, tetapi tidak diuji karena tidak ada gamepad.
- Development Build untuk Web (tombol DEV di browser) tidak dibuat; alat DEV diuji di Editor.
- PlayMode test assembly otomatis (T-6/T-7) belum dibuat; regresi masih memakai skrip Editor.

---

## 9. Screenshot aktual Game View

Disimpan **lokal** di `Documentation/Progress/M3-01-screenshots-local/` (dikecualikan dari Git karena memuat art A0 yang belum berizin distribusi):

| File | Isi |
|---|---|
| `editor_01_hud_start_1280x720.png` | Editor, GameplayPrototype: HUD awal (sebelum perbaikan padding hotbar) |
| `editor_02_inventory_open_1280x720.png` | Editor: Backpack terbuka, Tropical Seeds terpilih, semua kategori placeholder |
| `editor_05_firstplayable_greybox_1280x720.png` | Editor, FirstPlayable (publik, greybox) dengan HUD yang sama |
| `web_01` … `web_12` | Web build desktop: start, pilih slot 2, buka dengan B, tahan D saat terbuka, Esc, berjalan, klik Bag, pilih seeds, pasang ke hotbar 5, drag (ghost), drop, tutup |
| `mobile_01` … `mobile_09` | Emulasi HP 844×390: start, tap hotbar, tap Bag, joystick saat terbuka, tap seeds, drag ke hotbar, drop, tutup, joystick |
| `mobile_10_portrait_overlay.png` | Overlay rotasi pada 390×844 |

Screenshot web menampilkan bug "Hotbar: 15" yang sudah diperbaiki di Editor (lihat §8.4).

---

## 10. Kendala dan technical debt

| ID | Pri | Item |
|---|---|---|
| T-11 | P1 | Font masih `LegacyRuntime` (Arial bawaan). Art Bible mengusulkan Fredoka + Nunito Sans; butuh cek lisensi dan migrasi ke TextMeshPro (terkait T-9) |
| T-12 | P2 | Sprite sudut membulat dibuat saat runtime; di Edit Mode panel tampil kotak. Ganti dengan sprite UI 9-slice final saat tersedia |
| T-13 | P2 | Alat DEV memakai tombol F (bentrok dengan browser). Bila perlu uji di Web, buat panel DEV on-screen untuk Development Build |
| T-14 | P2 | Belum ada pemilih jumlah (split stack) dan belum ada fitur buang item; sengaja di luar M3.1 |
| T-15 | P2 | Hotbar memilih slot, tetapi belum ada aksi "use" (farming/fishing = M3.2+) |
| T-16 | P1 | `PlayerProfileHost` selalu membuat profil baru per sesi; local save (milestone berikut) perlu memuat `PlayerProfileData` dan hanya memberi starter bila belum ada save |
| T-17 | P2 | Navigasi UI dengan gamepad/Tab belum ada (mouse, touch, dan hotkey sudah) |
| T-18 | P2 | Regresi Play Mode masih memakai skrip Editor ad-hoc; pindahkan ke PlayMode test assembly (lanjutan T-6/T-7) |
| T-19 | P2 | `Data/Characters` tetap lokal (merujuk art A0); `Data/` kini sebagian publik, jadi perhatikan aturan `.git/info/exclude` |

Kendala proses:
- Domain reload saat masuk Play Mode beberapa kali memutus koneksi MCP selama beberapa detik. Solusinya menunggu `serverReachable = true` lalu mengulang perintah.
- Menunggu di dalam `script_eval` membekukan main thread Unity, sehingga pemantauan test harus dilakukan dengan polling terpisah.
- Memori: pekerjaan dijalankan berurutan (satu Editor, satu build, satu browser). Meski begitu, RAM bebas hanya 0,2–0,6 GB dari 8 GB. Build Web memakan 22,7 menit, dan pemuatan pertama di browser sempat macet (§8.4). Rekomendasi: tutup Unity Editor dan aplikasi lain saat build/uji Web, atau jalankan build Web lewat batchmode ketika Editor tertutup.

| ID | Pri | Item tambahan |
|---|---|---|
| T-20 | P2 | Saat tas terbuka, tombol Interact di layar sentuh tampil "aktif" (perilaku modal M1 untuk dialog), walau tap-nya tertahan backdrop; sembunyikan atau redupkan saat modal bukan dialog |
| T-21 | P1 | Gunakan hanya karakter yang ada di font Web (ASCII) sampai font final (T-11) dipasang; ulangi Web build untuk memastikan perbaikan "1-5" |

---

## 11. Ikon yang perlu diproduksi Game Artist

Spesifikasi Art Bible §05: **128×128 px, padding 12 px, siluet tegas**, PNG dengan alpha, gaya Chibi Tropical Cozy, tanpa teks baked.

| ID | Ikon | Prioritas |
|---|---|---|
| `tool_hoe` | Hoe (cangkul) | P1 |
| `tool_watering_can` | Watering Can | P1 |
| `tool_fishing_rod` | Fishing Rod (bambu) | P1 |
| `seed_tropical` | Tropical Seeds (kantong benih) | P1 |
| `crop_tropical_fruit` | Tropical Fruit (hasil panen) | P1 |
| `fish_reef` | Reef Fish | P1 |
| `material_driftwood` | Driftwood | P2 |
| `furniture_wooden_stool` | Wooden Stool | P2 |
| UI | Coin (kecil, ~48 px), ikon Backpack untuk tombol Bag, ikon × (close) | P1 |
| UI | Panel 9-slice cream/kayu (frame, slot, tombol) sebagai pengganti sprite runtime | P2 |

Sebelum diimpor: cek sumber/hak, alpha bersih, ukuran kanvas konsisten, lalu catat di `ASSET-REGISTER.csv`. Ikon bisa langsung dipasang ke field **Icon** pada aset item tanpa perubahan kode.

---

## 12. Usulan M3.2 (butuh persetujuan)

**Farming dasar + aksi hotbar.**
1. Petak tanah (tilemap) dengan state `untilled → tilled → planted → watered → grown`, disimpan sebagai runtime state yang bisa diserialisasi.
2. "Use" item terpilih di hotbar lewat tombol aksi kontekstual (tombol E / interact yang sama, dengan target petak di depan pemain):
   - Hoe mencangkul.
   - Seeds menanam (mengurangi 1 benih lewat `Inventory.RemoveItem`).
   - Watering Can menyiram.
3. Panen memberi `crop_tropical_fruit` lewat `PlayerProfileHost.GiveItem`. Bila tas penuh, panen ditolak dan tanaman tetap ada (sesuai GDD).
4. Pertumbuhan sementara berbasis timer sederhana, karena game time adalah milestone terpisah.
5. EditMode test untuk state tanaman dan transaksi item; regresi Play Mode.

Alternatif bila ingin save lebih dulu: **M3.2 = Local save** (`PlayerProfileData` → PlayerPrefs/IndexedDB Web), termasuk uji reload, data rusak, dan storage diblokir.

---

## 13. Langkah reproduksi manual (untuk founder)

1. Buka `Assets/_Lifetopia/Scenes/GameplayPrototype.unity` (lokal) atau `FirstPlayable.unity` (publik) → tekan **Play**.
2. Lihat koin **100** di kanan atas dan hotbar HOE / WC / FR / TS(3) di bawah.
3. Tekan **1–5**: sorotan hijau berpindah.
4. Tekan **B** (atau **I**, atau klik **Bag**): jendela Backpack terbuka. Coba W/A/S/D; karakter tidak bergerak.
5. Klik **TS** (Tropical Seeds): detail muncul. Tekan **5**: TS pindah ke slot hotbar 5. Tekan **5** lagi: dilepas.
6. Drag slot ke slot lain untuk memindah. Drag dua stack item yang sama untuk menggabung.
7. Tutup dengan **×**, **B**, atau **Esc**.
8. Uji tas penuh: tekan **F6** delapan kali, lalu sekali lagi. Muncul pesan "Your bag is full…" dan jumlah tidak berubah.
9. Tekan **F2** tiga kali: seeds habis dan slot hotbar TS kosong otomatis.
10. Dekati papan **INFO**, tekan **E** (dialog), lalu tekan **B**: tas tidak terbuka selama dialog.

---

## 14. Finalisasi dan tugas tertunda (26 November 2026)

Founder menyetujui hasil M3.1. Tugas berikut sengaja **belum** dikerjakan dan dicatat di `BACKLOG.md`:

| ID | Tugas | Kapan |
|---|---|---|
| V-1 | Verifikasi di Web bahwa hint kini tampil "Hotbar: 1-5", bukan "Hotbar: 15" (perbaikan font hanya diverifikasi di Editor; Web build sengaja tidak diulang hanya untuk teks ini, atas keputusan founder) | Web build milestone berikutnya |
| V-2 | Uji HP nyata (Android Chrome + iOS Safari): tap, drag & drop, backdrop di atas joystick, ukuran sentuh, FPS, memori | Sebelum rilis publik (gate G-1) |
| V-3 | Uji gamepad: tombol North membuka/menutup tas; gerak terblokir saat tas terbuka | Saat perangkat tersedia |
| T-22–T-24 | Keterbatasan RAM 8 GB dan optimasi build Web (build sekali per milestone dengan Editor tertutup atau batchmode; evaluasi stripping/kompresi dengan persetujuan) | Berkelanjutan |

Pemeriksaan repo publik saat finalisasi:
- Pemindai GUID pada file yang bisa dipublikasikan (`git ls-files -co --exclude-standard`) menemukan 0 referensi yang tidak terselesaikan.
- Tidak ada PNG, screenshot, backup, build, atau scene art-based di daftar file commit.

Prosedur commit sama dengan M2: stage file satu per satu, lalu verifikasi daftar yang ter-stage.

