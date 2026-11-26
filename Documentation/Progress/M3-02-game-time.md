# M3.2 — Game Time & Day/Night Cycle

Tanggal: 26 November 2026 · Basis: commit M3.1 `79cbae8` · Status: **disetujui founder 26-11-2026** (implementasi + pengujian selesai, termasuk Web build).

Ruang lingkup hanya M3.2 (sistem waktu). Farming (M3.3), fishing, market, housing, multiplayer, blockchain, dan akun **tidak** dikerjakan. `lifetopia-platform` dan database tidak disentuh.

---

## 1. Ringkasan hasil

| Kebutuhan | Hasil |
|---|---|
| Hari 1 mulai 08:00; 1 hari = 24 menit aktif; 1 jam game = 60 detik nyata | ✅ `GameClockSettings.Default` |
| 23:59 → 00:00, hari bertambah | ✅ event `DayChanged` sekali per hari |
| Berhenti saat gameplay di-pause, browser kehilangan fokus, inventory/dialog terbuka | ✅ `TimePauseState` (banyak sumber) + `GameClockHost` |
| Tanpa offline progression | ✅ waktu hanya maju lewat `Advance()` per frame; save tidak menyimpan jam dunia nyata |
| Kecepatan bisa diatur di Inspector | ✅ `GameClockConfig` → *Real Seconds Per Game Hour* |
| Konfigurasi, runtime state, pause, dan UI terpisah | ✅ (lihat §2) |
| Event menit/jam/hari/periode, tanpa alokasi per frame | ✅ string waktu di-cache; UI hanya update lewat event |
| API untuk sistem lain | ✅ `IGameClock` (read-only) |
| Day/night: Morning 05–08, Daytime 08–17, Sunset 17–19, Night 19–05, interpolasi halus termasuk tengah malam | ✅ `DayNightPalette` + `DayNightOverlay` |
| Built-In 2D / WebGL, tanpa URP, post-processing, lighting, atau salinan texture | ✅ satu quad + shader multiply kecil |
| Malam tetap terang; UI tidak ikut diwarnai | ✅ tiap channel ≥ 0,55; UI = Screen Space Overlay |
| HUD: Day, waktu 24 jam, periode, ikon placeholder | ✅ prefab `TimeHUD` |
| Alat DEV (set waktu, percepat, +1 jam, ganti hari, sunrise/sunset/malam) | ✅ `TimeDebugTools` (Shift+F1…F8, hanya Editor/Development Build) |
| State bisa diserialisasi + interface restore | ✅ `GameClockData`, `ToData()`, `TryRestore()` |
| FirstPlayable (publik) + GameplayPrototype (lokal) | ✅ keduanya terpasang |
| EditMode tests | ✅ **116/116 lulus** (77 lama + 39 baru) |
| Regresi M1/M2/M3.1 + uji waktu di Play Mode | ✅ (§8) |
| Web build + verifikasi "Hotbar: 1-5" | ✅ build 9,37 MiB, 0 error; "Hotbar: 1-5" benar di Web; 60 FPS desktop (§8.4) |
| HP nyata / gamepad | ❌ belum (V-2, V-3) |

---

## 2. Arsitektur

```
Konfigurasi (aset, Inspector)        Runtime (C# murni, bisa diuji tanpa scene)
──────────────────────────────       ───────────────────────────────────────────
GameClockConfig ─ GameClockSettings  GameTimeRunner
DayNightPalette ─ DayNightKey[]        ├── GameClock (IGameClock)  ⇄ GameClockData (save)
                                       └── TimePauseState (sumber pause)
                       │
Scene ─ GameClockHost (MonoBehaviour)  ◄── WebFocusBridge (FocusLost / FocusGained)
          │ Update: Runner.Tick(Time.deltaTime)   ◄── GameplayInputGate (inventory, dialog)
          │ event MinuteChanged / HourChanged / DayChanged / PeriodChanged / TimeJumped
          ├──► DayNightOverlay  (quad + shader multiply, layer "World" order 32767)
          ├──► TimeHudUI        (HUD: 08:00 · Day 1 · Daytime)
          └──► TimeDebugTools   (DEV only)
          ...nanti: Farming (M3.3), Fishing, NPC, Quest, Market lewat IGameClock
```

Prinsip yang dipakai:

- **Jam hanya bergerak jika diberi waktu.** `GameClock` tidak membaca `Time.deltaTime` sendiri. `GameClockHost` yang memanggil `Tick(delta)` setiap frame. Akibatnya "pause" cukup berarti *tidak memanggil Advance*, dan tidak ada cara bagi waktu untuk maju saat game tertutup (tidak ada offline progression).
- **Pause berdasarkan sumber.** Fokus browser, gate inventory/dialog, dan pause DEV masing-masing mendaftar sebagai sumber pause. Waktu baru berjalan lagi setelah **semua** sumber melepas, jadi menutup tas tidak akan menjalankan jam bila tab browser masih tersembunyi.
- **Langkah per frame dibatasi** (`maxStepSeconds` = 0,5 detik). Bila game tersendat 30 detik, waktu game hanya maju 0,5 menit, bukan 30.
- **`Time.timeScale = 0` juga menghentikan jam**, karena host memakai `Time.deltaTime` (yang ikut skala waktu).
- **Event-driven.** HUD dan overlay hanya diperbarui saat menit game berganti (sekali per detik nyata), tidak setiap frame. String waktu "HH:mm" dibuat sekali per menit-of-day lalu disimpan di cache.

---

## 3. File baru dan perubahan

### 3.1 Publik

Skrip baru `Assets/_Lifetopia/Scripts/TimeSystem/`:

| File | Isi |
|---|---|
| `TimeOfDayPeriod.cs` | enum Morning / Daytime / Sunset / Night |
| `GameClockSettings.cs` | aturan jam (start, kecepatan, max step, batas periode) + `Validate` + `PeriodAt` |
| `GameClockConfig.cs` | ScriptableObject pembungkus settings (Inspector) |
| `GameClockData.cs` | DTO save berversi |
| `IGameClock.cs` | API read-only + event untuk sistem lain |
| `GameClock.cs` | jam runtime: Advance, AdvanceGameMinutes, SetTime, ToData, TryRestore |
| `TimePauseState.cs` | `TimePauseState` (sumber pause) + `GameTimeRunner` (jam + pause + kecepatan) |
| `GameClockHost.cs` | komponen scene: tick per frame, fokus, gate, API pause, capture/restore |
| `ClockText.cs` | teks "08:00", "Day 1", nama periode (cache) |
| `DayNightPalette.cs` | `DayNightKey`, `DayNightMath` (interpolasi melingkar), ScriptableObject palet |
| `DayNightOverlay.cs` | quad seukuran kamera yang mewarnai dunia |

Lainnya (baru):
- `Scripts/UI/Time/TimeHudUI.cs`
- `Scripts/DevTools/TimeDebugTools.cs`
- `Shaders/DayNightMultiply.shader`
- `Materials/DayNightOverlay.mat`
- `Data/Time/GameClockConfig.asset`, `Data/Time/DayNightPalette.asset`
- `Prefabs/UI/TimeHUD.prefab`
- `Tests/EditMode/GameTimeTests.cs`
- meta folder: `Scripts/TimeSystem.meta`, `Scripts/UI/Time.meta`, `Shaders.meta`, `Materials.meta`, `Data/Time.meta`

Diubah:

| File | Perubahan |
|---|---|
| `Scripts/Controls/WebFocusBridge.cs` | event baru `FocusGained` + `NotifyFocusGained()`; perilaku `FocusLost` lama tidak berubah |
| `Plugins/WebGL/LifetopiaFocus.jslib` | callback baru saat tab terlihat lagi / window focus / pageshow |
| `Scripts/DevTools/InventoryDebugTools.cs` | mengabaikan tombol F bila Shift ditekan (Shift+F milik alat DEV waktu) |
| `Scenes/FirstPlayable.unity` | `GameClockHost`, `DayNightOverlay`, `TimeDebugTools` di GameSystems; instance `TimeHUD` |

### 3.2 Lokal saja (tidak dipublikasikan)
- `Scenes/GameplayPrototype.unity`: perubahan yang sama seperti FirstPlayable (scene berbasis art lokal).
- `Settings/BuildProfiles/Web.asset`: tetap lokal (scene pertama GameplayPrototype).
- `Documentation/Progress/M3-02-screenshots-local/`: screenshot (berisi art lokal), dikecualikan lewat `.git/info/exclude`.

---

## 4. Penjelasan setiap script C#

**`GameClockSettings`** (struct `[Serializable]`)
- Isi aturan jam: `startDay`, `startMinuteOfDay` (480 = 08:00), `realSecondsPerGameHour` (60), `maxStepSeconds` (0,5), dan menit awal tiap periode.
- `Validate(out error)` menolak nilai mustahil: kecepatan 0/negatif/NaN, menit di luar 0..1439, hari < 1, atau periode yang tidak berurutan.
- `PeriodAt(menit)` menentukan periode; Night "melingkar" melewati tengah malam (19:00–04:59).
- Struct dipakai (bukan class) supaya mudah disalin, dibandingkan, dan diuji.

**`GameClockConfig`** (ScriptableObject) — aset Inspector yang menyimpan satu `GameClockSettings`. Bila nilainya tidak valid, Unity menampilkan warning dan jam memakai default GDD. Data tidak diubah saat runtime.

**`GameClock`** — jam runtime.
- `Advance(realSeconds, speed)` dipanggil setiap frame. Nilai 0/negatif/NaN/∞ diabaikan, dan setiap langkah dibatasi `maxStepSeconds`.
- Menit dihitung dalam `double` agar pecahan tidak hilang.
- Event dipancarkan maksimal sekali per panggilan: `MinuteChanged`, `HourChanged(jam)`, `PeriodChanged(periode)`, ditambah `DayChanged(hari)` untuk **setiap** hari yang terlewati. Bila nanti dipercepat jauh, farming tetap menerima tiap hari.
- `SetTime` (DEV) dan `TryRestore` (save) melompat langsung tanpa memancarkan `DayChanged`; yang dipancarkan adalah `TimeJumped`.
- `TotalMinutes` adalah total menit game sejak hari 1 pukul 00:00, cocok untuk mengukur durasi.
- `ActivePlaySeconds` adalah detik nyata selama game benar-benar berjalan (tanpa waktu pause).

**`TimePauseState`** — himpunan "siapa yang sedang mem-pause". `Pause(source)` dan `Resume(source)`; jam hanya berjalan bila himpunannya kosong. Sumber yang sama dihitung sekali, dan `null` diabaikan.

**`GameTimeRunner`** — gabungan `GameClock` + `TimePauseState` + `Speed` (pengali DEV, 0..1000). `Tick(delta)` = "jika tidak di-pause, majukan jam". Karena C# murni, seluruh logika pause bisa diuji di EditMode.

**`GameClockHost`** (MonoBehaviour)
- Membuat runner dari config. Bila config kosong atau tidak valid, memakai default dan menulis warning.
- `Update()` → `Runner.Tick(Time.deltaTime)`.
- Pause saat `WebFocusBridge.FocusLost` atau `OnApplicationFocus(false)` / `OnApplicationPause(true)`; resume saat fokus kembali.
- Pause saat `GameplayInputGate` terblokir (inventory atau dialog terbuka).
- API publik: `Clock` (IGameClock), `PauseClock(src)`, `ResumeClock(src)`, `CaptureState()`, `TryRestoreState(data)`.
- `[DefaultExecutionOrder(-50)]` membuat jam diperbarui sebelum skrip gameplay biasa di frame yang sama.

**`ClockText`** — "HH:mm" 24 jam (`08:00`, `19:30`), "Day N", dan nama periode dalam bahasa Inggris. String waktu dibuat sekali lalu disimpan di larik cache berisi 1.440 elemen.

**`DayNightPalette` / `DayNightMath`**
- Palet berisi pasangan (menit, warna).
- `DayNightMath.Evaluate` mencari key sebelum dan sesudah secara melingkar. Contohnya, antara 23:00 dan 03:00 warna bergeser mulus melewati 00:00. Interpolasinya `SmoothStep` agar perubahan terasa lembut.
- `strength` (0..1) bisa mengurangi efek secara global (aksesibilitas).
- Palet default:

| Waktu | Warna (sRGB) | Kesan |
|---|---|---|
| 03:00 | 0,60 / 0,64 / 0,86 | malam dalam |
| 05:00 | 0,66 / 0,68 / 0,86 | awal pagi |
| 06:30 | 1,00 / 0,88 / 0,82 | matahari terbit hangat |
| 08:00 – 16:30 | putih | siang, tanpa tint |
| 17:45 | 1,00 / 0,84 / 0,66 | senja keemasan |
| 18:45 | 0,92 / 0,72 / 0,72 | senja merah muda |
| 19:45 – 23:00 | ±0,60–0,64 / 0,64–0,66 / 0,86–0,88 | malam biru lembut |

**`DayNightOverlay`**
- Saat Start, membuat satu quad sebagai anak kamera, berukuran 1,2× pandangan kamera dan disesuaikan ulang hanya bila ukuran/rasio kamera berubah.
- Renderer: sorting layer "World", order 32767 (paling akhir di dunia), material `DayNightOverlay` dengan shader `Lifetopia/DayNightMultiply` (`Blend DstColor Zero`: warna dunia × warna tint).
- Warna disimpan di vertex color, sehingga tidak ada instance material dan tidak ada texture.
- Diperbarui hanya pada event `MinuteChanged`.
- **Catatan penting:** proyek memakai *Linear color space*, sedangkan vertex color tidak dikonversi otomatis oleh Unity. Karena itu warna palet (sRGB) dikonversi ke linear lebih dulu. Tanpa konversi ini, efek malam hanya sekitar 20% (bug ini ditemukan dan diperbaiki saat pengujian).

**`TimeHudUI`** — berlangganan `MinuteChanged` dan `PeriodChanged`, lalu menampilkan "08:00", "Day 1", dan "Daytime". Ikon periode diambil dari `periodSprites` bila diisi; bila kosong, dipakai lingkaran placeholder berwarna Art Bible (Morning coral, Daytime sand, Sunset wood, Night lagoon).

**`TimeDebugTools`** — hanya aktif di Editor dan Development Build (`#if`):

| Tombol | Fungsi |
|---|---|
| Shift+F1 | 06:00 (sunrise) |
| Shift+F2 | 12:00 |
| Shift+F3 | 18:00 (sunset) |
| Shift+F4 | 22:00 (night) |
| Shift+F5 | 23:58 (uji pergantian hari) |
| Shift+F6 | +1 jam |
| Shift+F7 | kecepatan ×1 / ×10 / ×60 |
| Shift+F8 | pause/resume DEV |

**`WebFocusBridge`** (diubah) — selain `FocusLost`, kini ada `FocusGained`. Di build Web, jslib memanggilnya saat tab terlihat lagi (`visibilitychange`), window mendapat `focus`, atau `pageshow`.

---

## 5. Konfigurasi lewat Inspector

1. **Kecepatan waktu:** pilih `Assets/_Lifetopia/Data/Time/GameClockConfig.asset`, lalu ubah **Real Seconds Per Game Hour**.

   | Nilai | Hasil |
   |---|---|
   | 60 (default GDD) | 1 hari = 24 menit |
   | 30 | 2× lebih cepat (1 hari = 12 menit) |
   | 120 | 2× lebih lambat |

   Rentang yang valid adalah 0,1–3600.
2. **Jam mulai:** *Start Minute Of Day* (menit dari tengah malam; 480 = 08:00, 360 = 06:00) dan *Start Day*.
3. **Batas periode:** *Morning/Daytime/Sunset/Night Start Minute* (harus berurutan).
4. **Warna siang-malam:** `Data/Time/DayNightPalette.asset`. Tambah atau ubah key (menit + warna). Gunakan *Strength* untuk melemahkan efek. Jaga warna malam tetap ≥ ±0,55 per channel; test `DefaultPalette_NightStaysReadable` memeriksa palet default.
5. **Di scene (`GameSystems`):**
   - `GameClockHost`: Config, Gate (`GameplayInputGate`), *Pause While Modal Open*, *Pause On Focus Loss*.
   - `DayNightOverlay`: Clock Host, Palette, Overlay Material (`Materials/DayNightOverlay.mat`), Target Camera (Main Camera).
   - `TimeDebugTools`: Clock Host.
6. **HUD:** instance `TimeHUD` di `GameplayUI/SafeArea` (setelah `InventoryHUD`), field *Clock Host*. Untuk ikon final, isi *Period Sprites* (urutan Morning, Daytime, Sunset, Night).

Posisi HUD: kanan atas, di kiri coin counter (210×64 pada referensi 1280×720). Tidak bertumpuk dengan coin, Bag, hint keyboard, toast, hotbar, joystick, maupun tombol Interact.

---

## 6. Screenshot aktual (lokal)

Disimpan di `Documentation/Progress/M3-02-screenshots-local/` (dikecualikan dari Git karena memuat art lokal):

| File | Isi |
|---|---|
| `editor_00_start_0805.png` | Awal permainan (HUD 08:09 · Day 1 · Daytime) |
| `editor_01_morning_0630.png` | Morning 06:30–06:35: rona hangat |
| `editor_02_daytime_1200.png` | Daytime 12:00: tanpa tint |
| `editor_03_sunset_1800.png` | Sunset 18:00: keemasan |
| `editor_04_night_2200.png` | Night 22:00: biru lembut, karakter dan objek tetap jelas |
| `editor_05_firstplayable_dusk_1930.png` | FirstPlayable (publik, greybox) 19:30 |
| `web_*` / `mobile_*` | lihat §8.4 |

Di semua screenshot, UI (hint, jam, coin, Bag, hotbar) tetap berwarna asli; hanya dunia yang diwarnai.

---

## 7. Cara pakai clock untuk Farming (M3.3)

```csharp
using Lifetopia.TimeSystem;

public sealed class CropPlot : MonoBehaviour
{
    [SerializeField] private GameClockHost clockHost;
    private IGameClock _clock;
    private long _plantedAtMinute;   // simpan di save, bersama state petak

    private void Start()
    {
        _clock = clockHost.Clock;
        _clock.DayChanged += OnNewDay;   // contoh: pertumbuhan harian / reset "sudah disiram"
    }

    private void OnDestroy() => _clock.DayChanged -= OnNewDay;

    public void Plant() => _plantedAtMinute = _clock.TotalMinutes;

    public bool IsReady(long growMinutes) => _clock.TotalMinutes - _plantedAtMinute >= growMinutes;

    private void OnNewDay(int day) { /* naikkan tahap tumbuh jika disiram kemarin */ }
}
```

Aturan:
- **Selalu pakai `IGameClock.TotalMinutes` atau `DayChanged`**, jangan `DateTime.Now` atau `Time.time`. Waktu dunia nyata dilarang untuk pertumbuhan tanaman, sehingga tidak ada progres saat game tertutup.
- **Simpan angka, bukan waktu nyata.** Di save, simpan `TotalMinutes` saat tanam (long) atau tahap pertumbuhan; jam sendiri disimpan lewat `GameClockData`.
- **Jangan polling setiap frame.** Gunakan `MinuteChanged` / `HourChanged` / `DayChanged`.
- **`DayChanged` dipancarkan per hari terlewati**, jadi lompatan beberapa hari tetap memberi setiap hari ke farming. `TimeJumped` (alat DEV / restore save) **tidak** menghitung sebagai hari berlalu; setelah load, hitung ulang state dari angka yang disimpan.
- **Bila farming perlu menghentikan waktu** (misal cutscene), panggil `clockHost.PauseClock(this)` dan pastikan `ResumeClock(this)` dipanggil kembali.

Pemulihan saat Local Save & Load dikerjakan:

```csharp
GameClockData data = clockHost.CaptureState();        // saat menyimpan
if (!clockHost.TryRestoreState(loaded, out string err)) // saat memuat
    Debug.LogWarning(err); // data rusak → jam tidak berubah, tampilkan pesan recovery
```

---

## 8. Hasil pengujian (aktual)

### 8.1 EditMode
**116 test, 116 lulus, 0 gagal.** Rinciannya 77 test lama (semua tetap lulus) dan 39 test baru di `GameTimeTests.cs`:

| Kebutuhan | Test |
|---|---|
| Awal Day 1 08:00 | `StartsOnDay1_At0800_Daytime` |
| Konversi waktu nyata → game | `SixtyRealSeconds_IsOneGameHour_AndOneDayIs24Minutes`, `ConfigurableSpeed_TwoRealSecondsPerHour`, `LongFrame_IsClampedToMaxStep`, `InvalidDelta_IsIgnored(×4)` |
| Rollover tengah malam | `MidnightRollover_GoesTo0000_AndNextDay` |
| Penambahan hari | `DayCounter_RaisesOncePerDayPassed` |
| Event | `MinuteAndHourEvents_FireOncePerChange` |
| Pause/resume | `PauseAndResume` |
| Browser tidak aktif | `NoTimePasses_WhileBrowserIsInactive` |
| Banyak sumber pause | `SeveralPauseSources_AllMustResume` |
| Kecepatan DEV | `Speed_MultipliesGameTime_NotActivePlayTime` |
| Perubahan periode | `PeriodBoundaries(×10)`, `PeriodChanged_FiresWhenCrossingSunset` |
| Interpolasi warna + tengah malam | `KeyTimes_ReturnKeyColours`, `BetweenKeys_IsHalfwayAtMidpoint`, `AcrossMidnight_InterpolatesSmoothly`, `Strength_BlendsTowardWhite`, `SingleOrEmptyPalette_IsSafe`, `DefaultPalette_NightStaysReadable_AndDayIsUntinted` |
| Konfigurasi tidak valid | `DefaultSettings_AreValid`, `InvalidSettings_AreRejected_AndClockFallsBackToDefaults` (8 kasus) |
| Serialisasi/restore | `State_RoundTripsThroughJson`, `InvalidState_IsRejected_AndChangesNothing`, `SaveData_HasNoRealWorldTimestamp` |
| Teks | `ClockText_Is24Hour`, `SetTime_RejectsInvalidValues` |

### 8.2 Play Mode — sistem waktu (GameplayPrototype, input disuntikkan)

| Skenario | Hasil aktual |
|---|---|
| Berjalan 3 s | +3,01 menit game dalam 3,01 s |
| Tas terbuka 3 s | **+0,00** (paused, 1 sumber); setelah ditutup +2,00 dalam 2,01 s |
| Dialog terbuka 3 s | **+0,00**; setelah Esc +2,01 dalam 2,01 s |
| Focus lost 3 s | **+0,00**; focus gained +2,01 dalam 2,00 s |
| `Time.timeScale = 0` 2 s | **+0,00**; kembali 1 → +2,04 dalam 2,00 s |
| Shift+F5 (23:58) lalu tunggu 2,5 s | **Day 2 00:00 Night**; HUD "00:00 · Day 2 · Night" |
| Shift+F6 | 00:00 → 01:00 |
| Shift+F7 ×10 selama 2 s | +20,26 menit dalam 2,02 s |
| Shift+F8 pause DEV 2 s | +0,00; resume +2,02 dalam 2,02 s |
| Shift+F1 / F3 / F4 | 06:00 Morning / 18:00 Sunset / 22:00 Night; tint berubah sesuai; **benih tetap 3** (tombol inventory tidak ikut terpicu) |
| Berjalan malam (D) | kecepatan 4,00 u/s, bergerak normal |
| Kecepatan jam awal (Editor) | 11,52 menit dalam 11,83 s (rasio 0,975). Selisih berasal dari frame yang tersendat saat screenshot (dipotong `maxStepSeconds`), sesuai desain |

### 8.3 Regresi M1/M2/M3.1
- Harness 113 langkah dari M3.1 dijalankan ulang di GameplayPrototype dengan jam berjalan. **Hasil identik dengan M3.1**:
  - gerak 4,00 u/s, animasi walk/idle;
  - tabrakan rumah (y 2,12) dan air (y −7,92);
  - hotbar 1–5 dan numpad; inventory B/I/Esc, mouse (klik + drag), sentuh (tap + drag + backdrop menahan joystick);
  - tas penuh dengan pesan; benih habis mengosongkan hotbar;
  - tas tidak bisa dibuka saat dialog; prompt dan E diblokir saat tas terbuka; focus lost tetap aman.
- FirstPlayable (publik): jam, HUD, dan tint berjalan (screenshot 19:30).
- Y-sorting tidak berubah: overlay digambar setelah semua objek World, sehingga urutan antar objek tetap.

### 8.4 Web build dan verifikasi "Hotbar: 1-5"
Build profile `Web` lokal (scene pertama GameplayPrototype), non-development, lewat **batchmode dengan Unity Editor ditutup** (rekomendasi T-22). Output `Builds/Web/M3-02` (di-ignore Git).

| Ukuran / waktu | Nilai |
|---|---|
| Hasil | **Build Finished, Result: Success**; 0 error, 0 warning compiler |
| Total | 9.821.646 byte (9,37 MiB); M3.1 sebelumnya 9.764.600 byte (+57 KB) |
| `M3-02.data.br` | 2.443.552 byte |
| `M3-02.wasm.br` | 7.271.209 byte |
| `M3-02.framework.js.br` | 75.452 byte |
| `M3-02.loader.js` | 26.983 byte |
| Waktu build | **13,6 menit** (10:32–10:45); M3.1 dengan Editor terbuka 22,7 menit |

Catatan memori (aktual):
- Percobaan build pertama dihentikan oleh sistem karena memori kritis (RAM bebas ±0,5 GB saat kompilasi IL2CPP, dengan Chrome dan VS Code masih terbuka).
- Percobaan kedua dimulai dengan RAM bebas 1,8 GB dan berhasil. RAM bebas sempat turun ke ±0,28 GB saat link WASM.

**Desktop 1280×720 (Chromium, input sungguhan ke canvas):**

| Uji | Hasil |
|---|---|
| Load (file sudah di cache browser) | 1,5 s |
| **V-1: hint keyboard** | **"Hotbar: 1-5" tampil benar**; perbaikan M3.1 terverifikasi di Web |
| HUD jam | "08:10 · Day 1 · Daytime" di kiri coin, tidak bertumpuk |
| Jam berjalan | 08:10 → 08:15 dalam 5 s |
| Tas terbuka 5 s | 08:15 → 08:15 (berhenti) |
| `window blur` 5 s (jalur jslib baru) | 08:16 → 08:16 (berhenti) |
| `window focus` lalu 3 s | 08:19 (berjalan lagi) |
| FPS 5 s | **60,0**, 0 frame > 33 ms (maks 17,3 ms) |
| Console | **0 error**, 6 warning engine `getInternalformatParameter` (sama seperti M2/M3.1) |

**Emulasi HP 844×390 (bukan HP nyata):**
- HUD jam, coin, Bag, hotbar, joystick, dan Interact tidak bertumpuk.
- 60 FPS (GPU laptop), 0 error.

Tidak diuji di Web:
- Warna morning, sunset, dan night: build rilis tidak memiliki tombol DEV, dan menunggu sampai malam di Web butuh ±11 menit nyata. Visual tiap periode sudah diuji di Editor (§6).
- Tab tersembunyi sungguhan (`visibilitychange`): yang diuji hanya event `blur`/`focus` window.

### 8.5 Console
- Editor setelah pengujian: **0 error, 0 warning**.
- Dua log dari tahap sebelumnya ("missing references" / "no GameClockConfig") muncul saat bug wiring di §9 belum diperbaiki; keduanya hilang setelah perbaikan.

### 8.6 Repo publik
Pemindai GUID terhadap file yang bisa dipublikasikan (`git ls-files -co --exclude-standard`): 298 file, 50 YAML, **0 referensi tak terselesaikan**. FirstPlayable, TimeHUD, data, material, dan shader tidak bergantung pada art lokal.

### 8.7 Tidak diuji
- HP nyata (V-2 / G-1) dan gamepad (V-3).
- Development Build Web (tombol DEV di browser).
- Peralihan sungguhan antar tab browser (yang diuji di Web hanya `blur`/`focus` window, lihat §8.4).

---

## 9. Kendala dan technical debt

Bug yang ditemukan dan diperbaiki selama M3.2:
1. **Tint terlalu lemah di Linear color space.** Vertex color tidak dikonversi sRGB→linear, sehingga malam hanya sekitar 20% lebih gelap. Diperbaiki di `DayNightOverlay.Refresh` (konversi `.linear`).
2. **Referensi config/palette kosong di scene.** Skrip wiring memuat aset sebelum `OpenScene`; membuka scene (mode Single) membongkar aset yang tidak direferensikan, sehingga referensi tersimpan `null`. Diperbaiki dengan memuat aset setelah scene dibuka, lalu diverifikasi langsung pada file scene. Komponen menulis error yang jelas di Console bila referensi kosong.
3. **Test aritmetika:** langkah 0,1 detik tidak eksak dalam biner (10 × 0,1 ≠ 1,0). Test memakai 0,125 detik.

| ID | Pri | Item |
|---|---|---|
| T-25 | P2 | Tint dunia satu warna untuk seluruh layar; belum ada lampu malam lokal (lentera/jendela). Bila diperlukan, tambahkan sprite "glow" sederhana (additive) di M berikutnya, bukan lighting real-time |
| T-26 | P2 | Overlay ikut mewarnai background kamera di luar peta (tidak terlihat karena kamera dibatasi peta) |
| T-27 | P2 | `TimeDebugTools.OnGUI` membuat string tiap frame (DEV only); tidak ada di build rilis |
| T-28 | P1 | Belum ada pause menu sungguhan; API `PauseClock/ResumeClock` sudah siap |
| T-29 | P1 | Local save: simpan/muat `GameClockData` bersama `PlayerProfileData`; jangan beri progres saat load |
| T-30 | P2 | Ikon periode masih placeholder lingkaran berwarna (perlu art: matahari terbit, matahari, senja, bulan) |
| T-31 | P2 | Editor: `OnApplicationFocus(false)` saat Editor tidak difokuskan ikut mem-pause jam di Play Mode (perilaku benar, tapi perlu diketahui saat menguji) |

Kendala proses:
- RAM 8 GB: dengan Editor terbuka, RAM bebas sempat hanya ±145 MB. Web build dijalankan lewat **batchmode setelah Editor ditutup**. Percobaan pertama tetap dihentikan sistem karena memori kritis; percobaan kedua berhasil dalam 13,6 menit (lihat §8.4). Unity Editor dibiarkan tertutup setelah build.
- Domain reload memutus koneksi MCP beberapa kali; setiap perintah diulang setelah `serverReachable = true`.

---

## 10. Ikon yang perlu diproduksi Game Artist
Ukuran sekitar 64×64 px (tampil ±38 px), siluet jelas, gaya Chibi Tropical Cozy, PNG alpha, tanpa teks:
- Morning (matahari terbit di atas laut)
- Daytime (matahari)
- Sunset (matahari setengah terbenam, keemasan)
- Night (bulan sabit + bintang)

Pasang lewat field *Period Sprites* di `TimeHUD`, tanpa perubahan kode.

---

## 11. Langkah reproduksi manual (founder)

1. Buka `FirstPlayable.unity` (publik) atau `GameplayPrototype.unity` (lokal), lalu tekan **Play**. HUD kanan atas menampilkan **08:00 · Day 1 · Daytime**, dan menit bertambah setiap detik.
2. Tekan **B**: tas terbuka dan menit **berhenti**. Tutup tas: menit berjalan lagi. Hal yang sama berlaku untuk dialog papan INFO.
3. Klik jendela lain (Editor kehilangan fokus): jam berhenti. Klik kembali Game view: jam berjalan.
4. **Shift+F1**: 06:00 pagi hangat. **Shift+F2**: 12:00 siang. **Shift+F3**: 18:00 senja. **Shift+F4**: 22:00 malam.
5. **Shift+F5**: 23:58. Tunggu ±2 detik, maka HUD berganti ke **Day 2 · 00:00**.
6. **Shift+F7**: kecepatan ×10 / ×60 / ×1. **Shift+F6**: +1 jam. **Shift+F8**: pause DEV.
7. Ubah kecepatan: `Data/Time/GameClockConfig` → *Real Seconds Per Game Hour* = 30, lalu Play: menit bertambah 2 per detik.

---

## 12. Usulan M3.3 (butuh persetujuan)
Farming dasar memakai `IGameClock`:
- petak tanah (tilled → planted → watered → grown);
- aksi hotbar (Hoe, Seeds, Watering Can);
- pertumbuhan per `DayChanged` atau `TotalMinutes`;
- panen ke inventory (gagal bila tas penuh, tanaman tetap ada);
- EditMode test state tanaman + regresi.
