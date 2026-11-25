# M1 First Playable — laporan progres

Tanggal: 26 November 2026 · Milestone: **M1 FirstPlayable greybox** · Status: **selesai secara lokal, menunggu review founder**. Uji HP nyata masih **gate terbuka**.

Semua visual adalah placeholder sementara (prefix `tmp_` / label `(TEMP)`), bukan art final. Tidak ada quest, inventory, farming, fishing, ekonomi, login, multiplayer, atau blockchain.

## 1. Audit awal (Phase 1)
| Item | Hasil |
| --- | --- |
| M0 | sebelumnya belum di-commit → dengan persetujuan founder di-commit lokal terpisah (`chore(m0): …`), **tidak di-push** |
| Editor / Console | ready, 0 error / 0 warning |
| Input System | project-wide actions = template `InputSystem_Actions` (Interact memakai interaction **Hold**). Template dipertahankan; M1 memakai asset sendiri `LifetopiaControls` |
| Build Profile Web | aktif; ternyata memiliki **Player Settings override sendiri** (setting Web diubah lewat profile, bukan Project Settings global) |
| Temuan M0 | nilai `preloadedAssets` di profile M0 adalah sisa sementara dari proses build Input System; kini kembali `[]` (sama dengan global). Setelah dua build M1 nilainya tetap `[]` |
| Uji M0 yang masih terbuka | HP nyata, Edge/Firefox/Safari, input touch nyata |

## 2. File yang dibuat / diubah
### Baru
```
Assets/_Lifetopia/
├─ Art/Placeholder/            tmp_square_64.png, tmp_circle_64.png (putih 64×64, diwarnai lewat komponen)
├─ Input/                      LifetopiaControls.inputactions (Gameplay: Move, Interact, Pause)
├─ Physics/                    PlayerNoFriction.physicsMaterial2D (friction 0 → tidak lengket di dinding)
├─ Plugins/WebGL/              LifetopiaFocus.jslib (hanya aktif di platform Web)
├─ Prefabs/                    Player, Tree_Placeholder, House_Placeholder, Shop_Placeholder, WelcomeSign_Placeholder
├─ Scenes/FirstPlayable.unity
├─ Scripts/  (assembly Lifetopia.Runtime, 18 file C#)
│  ├─ Core/          GameplayInputGate
│  ├─ Controls/      PlayerInputReader, VirtualJoystick, TouchActionButton, WebFocusBridge
│  ├─ Player/        PlayerMotor2D, PlayerFacing, FacingDirection, MovementMath, PlaceholderCharacterView
│  ├─ CameraSystem/  CameraFollow2D, CameraBoundsMath
│  ├─ World/         WorldBounds, PlayerSpawner
│  ├─ Interaction/   IInteractable, InteractionDetector, DialogueInteractable
│  └─ UI/            DialogueUI, InteractionPromptUI, SafeAreaFitter, TouchControlsVisibility
├─ Tests/EditMode/   Lifetopia.Tests.EditMode (17 test)
└─ Tilemaps/Placeholder/  tmp_tile_grass_a/b, tmp_tile_path, tmp_tile_water
Assets/WebGLTemplates/LifetopiaResponsive/index.html   (Unity mewajibkan lokasi ini untuk template custom)
```
### Diubah (tracked)
| File | Perubahan |
| --- | --- |
| `ProjectSettings/TagManager.asset` | sorting layer: Ground, GroundDetail, Default, World, Overhead |
| `ProjectSettings/GraphicsSettings.asset` | Transparency Sort Mode = Custom Axis (0,1,0). Sisa diff = Unity menulis ulang file ke format serialisasi terbaru (`serializedVersion 13→16`); render pipeline tetap Built-In |
| `Assets/_Lifetopia/Settings/BuildProfiles/Web.asset` | scene list (FirstPlayable, FoundationSmoke), template `PROJECT:LifetopiaResponsive`, `preloadedAssets: []` |

`SampleScene`, `FoundationSmoke`, `InputSystem_Actions`, package, versi Unity, dan render pipeline **tidak diubah**.

## 3. Struktur scene `FirstPlayable`
```
GameSystems            GameplayInputGate, PlayerSpawner
WorldBounds            EdgeCollider2D + WorldBounds (32×22 unit, pusat 0,0)
Grid                   Tilemap: Ground (layer Ground), Water (Ground, order 1, collider), Path (GroundDetail)
Trees                  17× Tree_Placeholder (prefab instance)
House_Placeholder      footprint 3×3, root (-9,2)
Shop_Placeholder       footprint 4×3, root (9,2)
WelcomeSign_Placeholder  DialogueInteractable (contoh interaksi)
Player                 prefab instance, spawn (0,-2)
SpawnPoint             titik spawn (pindahkan di Scene view)
Main Camera            orthographic size 5.5, CameraFollow2D
EventSystem            InputSystemUIInputModule (wajib karena project hanya memakai Input System)
GameplayUI             Canvas 1280×720 scale-with-screen-size, DialogueUI, InteractionPromptUI, TouchControlsVisibility
└─ SafeArea            SafeAreaFitter
   ├─ KeyboardHint     "Move: WASD / Arrow keys    Interact: E"
   ├─ TouchControls    Joystick (+Handle), InteractButton
   ├─ InteractionPrompt
   └─ DialoguePanel    Speaker, Body, ContinueHint
```
Skala sesuai Art Bible/Technical Architecture: 1 tile = 1 unit = 64 px, sprite 64 PPU, Bilinear, mipmap off, kamera ortho 5.5. Tinggi karakter placeholder ±1.5 unit (≈96 px).

### Pola prefab dunia (penting untuk M2)
Root GameObject diletakkan di **titik kaki / dasar footprint** dan membawa `SortingGroup` (layer `World`) + collider **hanya di area dasar**. Semua visual adalah child. Di M2 cukup ganti child visual dengan sprite asli; posisi root, collider, dan sorting tetap.

Player prefab:
```
Player (Rigidbody2D dynamic, gravity 0, freeze rotation, interpolate; CircleCollider2D kaki r=0.28;
        SortingGroup World; PlayerInputReader; PlayerMotor2D; PlayerFacing; PlaceholderCharacterView)
├─ Shadow
├─ Body (Legs, Torso, Head, FacingMarker)   ← bob saat berjalan
├─ Label_TEMP  (teks debug "IDLE D"/"WALK R")
└─ InteractionSensor (CircleCollider2D trigger r=1.1; InteractionDetector)
```

## 4. Penjelasan script C#
| Script | Tanggung jawab | Konsep yang dipelajari |
| --- | --- | --- |
| `PlayerInputReader` | Satu-satunya pembaca input gameplay. Menggabungkan keyboard (Input System) dan joystick; joystick menang saat dipegang. Menyediakan `Move` dan event `InteractPerformed` / `UiSubmitPerformed` / `CancelPerformed`. Melepas semua input saat fokus hilang. | *event* C# (`event Action`), `InputActionReference` |
| `MovementMath` | Fungsi murni: normalisasi diagonal (`ClampInput`) dan pemilihan arah tanpa flicker (`ResolveFacing`). | kelas `static` yang bisa di-unit-test |
| `PlayerMotor2D` | Menggerakkan Rigidbody2D di `FixedUpdate` dengan akselerasi/deselerasi; `Teleport` untuk spawn. | `FixedUpdate` (langkah fisika) vs `Update`; interpolation |
| `PlayerFacing` | Menyimpan arah (Down/Left/Right/Up) dan status Idle/Walking; memberi event `StateChanged`. | pemisahan *state* dari *visual* |
| `FacingDirection` | Enum arah; urutan sama dengan baris sprite sheet D,L,R,U (DECISIONS.md). | `enum` |
| `PlaceholderCharacterView` | Visual sementara: marker arah di kepala + bob saat berjalan + label debug. **Diganti di M2.** | komponen visual yang bisa ditukar |
| `CameraFollow2D` | Kamera mengikuti target dengan `SmoothDamp` di `LateUpdate`, lalu di-clamp ke batas map. | urutan update: `LateUpdate` setelah gerakan → tanpa jitter |
| `CameraBoundsMath` | Matematika clamp kamera (setengah ukuran view = orthoSize × aspect). | |
| `WorldBounds` | Satu sumber ukuran map: membangun EdgeCollider2D dan dipakai kamera. Gizmo hijau di Scene view. | `OnValidate`, `OnDrawGizmos` |
| `PlayerSpawner` | Menaruh player di SpawnPoint saat Start dan *snap* kamera. | |
| `IInteractable` | Kontrak objek yang bisa diinteraksi (prompt, bisa/tidak, posisi, aksi). | `interface` |
| `InteractionDetector` | Di sensor trigger player; memilih target terdekat; memanggil `Interact` saat tombol interaksi. | `OnTriggerEnter2D/Exit2D` |
| `DialogueInteractable` | Contoh: papan yang membuka dialog. | implementasi interface |
| `DialogueUI` | Panel dialog modal; memblokir gameplay lewat `GameplayInputGate`; E/tap = lanjut, Escape = tutup. | UI modal |
| `GameplayInputGate` | Penghitung "siapa yang sedang memblokir gameplay". | `HashSet` |
| `InteractionPromptUI` | Menampilkan "[E]  Read Welcome Board" (di mode sentuh tanpa "[E]") dan meredupkan tombol Interact bila tidak ada target. | |
| `VirtualJoystick` | Joystick UGUI (pointer event → sentuh & mouse), reset saat dilepas/disable. | `IPointerDownHandler`, `IDragHandler`, `IPointerUpHandler` |
| `TouchActionButton` | Tombol sentuh yang aktif saat ditekan (bukan saat dilepas). | |
| `TouchControlsVisibility` | Auto: tampil di platform mobile, URL `?touch=1`, atau setelah sentuhan nyata pertama. | |
| `SafeAreaFitter` | Menyesuaikan area UI dengan `Screen.safeArea` (notch). | |
| `WebFocusBridge` + `LifetopiaFocus.jslib` | Saat tab disembunyikan / blur / pagehide, C# diberi tahu agar tombol yang "tertahan" dilepas. | interop JavaScript ↔ C# |

## 5. Mengubah konfigurasi lewat Inspector
| Ingin mengubah | Pilih objek → komponen → field |
| --- | --- |
| Kecepatan jalan | Player → PlayerMotor2D → **Move Speed** (default 4 tile/detik), Acceleration, Deceleration |
| Kehalusan kamera | Main Camera → CameraFollow2D → **Smooth Time** (0 = terkunci), Offset |
| Zoom kamera | Main Camera → Camera → **Size** (5.5) |
| Ukuran map | WorldBounds → **Size** (gizmo hijau ikut berubah) |
| Titik spawn | geser objek **SpawnPoint** di Scene view |
| Teks papan | WelcomeSign_Placeholder → DialogueInteractable → Speaker Name, Prompt Text, **Lines** |
| Jangkauan interaksi | Player/InteractionSensor → CircleCollider2D → **Radius** |
| Kontrol sentuh | GameplayUI → TouchControlsVisibility → **Mode** (Auto / AlwaysShow / AlwaysHide) |
| Dead zone joystick | GameplayUI/SafeArea/TouchControls/Joystick → VirtualJoystick → Dead Zone |
| Label debug "IDLE D" | nonaktifkan Player/Label_TEMP |
| Template Web | **File > Build Profiles > Web > Player Settings** (bukan Project Settings global) |

Catatan: field yang menunjuk objek scene (joystick, gate, dialog) disimpan sebagai *override* di instance prefab dalam scene — wajar karena prefab tidak boleh menyimpan referensi ke objek scene.

## 6. Hasil pengujian (benar-benar dijalankan)
### 6.1 Kompilasi & unit test
- Kompilasi: 0 error, 0 warning (setiap perubahan).
- EditMode test **17/17 Passed** (normalisasi diagonal, facing tanpa flicker, clamp kamera, gate) — dijalankan dua kali (sebelum & sesudah perbaikan).

### 6.2 Play Mode Editor (skenario otomatis per-frame, input disuntik lewat Input System)
| Uji | Hasil terukur | Status |
| --- | --- | --- |
| W / S / A / D | |v| = 4.00, facing Up/Down/Left/Right, walking=true | ✅ |
| Arrow keys | |v| = 4.00, arah benar | ✅ |
| Diagonal W+D / S+A | v = (±2.83, ±2.83), **|v| = 4.00** (tanpa normalisasi akan 5.66) | ✅ |
| Facing saat diagonal | tetap pada arah sebelumnya yang sesuai (tidak flicker) | ✅ |
| Lepas tombol | |v| = 0, idle | ✅ |
| Collision rumah | berhenti y = 1.52 (prediksi 1.52) | ✅ |
| Collision air / pond | y = −7.92, x = 9.72 (sesuai prediksi) | ✅ |
| Collision batang pohon | y = −5.10 (prediksi −5.11) | ✅ |
| Batas dunia N/W/sudut SW | 10.52 / −15.72 / (−15.72, −7.92) | ✅ |
| Kamera di tepi map | tepi view tepat −16.00 / 16.00 / −11.00 / 11.00 | ✅ |
| Geser sepanjang dinding | tetap meluncur (vx 2.83), tidak lengket | ✅ |
| Prompt | muncul dekat papan, hilang saat jauh/dialog terbuka | ✅ |
| Dialog | E buka → E baris 2 → E tutup; Escape menutup; tahan D saat dialog → player diam | ✅ |
| Focus hilang (jalur browser & `OnApplicationFocus`) | D ditahan → |v| langsung 0 | ✅ |
| Joystick (pointer UGUI) | penuh → |v| 4.00; setengah → move 0.51, |v| 2.04; dilepas → 0 | ✅ |
| Joystick vs keyboard | joystick menang saat dipegang; setelah dilepas keyboard mengambil alih | ✅ |
| Tombol Interact sentuh | membuka dialog; saat dialog = submit | ✅ |
| Y-sort | player tertutup rumah/pohon saat di belakang, di depan saat di bawah | ✅ (screenshot) |
| Regression setelah perbaikan | 8/8 | ✅ |

### 6.3 Web build lokal (HTTP 127.0.0.1, Playwright Chromium 157, desktop Windows)
| Uji | Hasil |
| --- | --- |
| Build | Succeeded, 0 error, 0 warning |
| Muat halaman | WebGL 2.0, console browser 0 error (sebelum perbaikan: 1× favicon 404 → diperbaiki) |
| Keyboard D 530 ms | dunia bergeser 2.02 unit (≈4 u/s termasuk akselerasi) |
| Diagonal W+A | gerak diagonal normal |
| Blur + visibilitychange saat S masih ditekan | player berhenti; dua frame 0.8 s kemudian identik kecuali 1 px ekor smoothing kamera |
| Interaksi | prompt → dialog baris 1 → (tahan A: player diam) → baris 2 → tutup; Escape menutup |
| Viewport 844×390, 915×412, 1920×1080 | canvas = viewport, **tanpa scroll** (masalah M0 teratasi) |
| Joystick drag mouse 844×390 | player berjalan; setelah dilepas dua frame 600 ms identik (0 byte beda) |

### 6.4 Kegagalan / kejadian yang dicatat apa adanya
1. **Bug ditemukan & diperbaiki:** di build Web, kontrol sentuh tampil di desktop karena Unity Web mendaftarkan device Touchscreen walau browser tanpa sentuhan (`maxTouchPoints 0`). Deteksi diubah (lihat `TouchControlsVisibility`). Setelah rebuild diverifikasi ulang (lihat 6.5).
2. Prompt di HP menampilkan "[E]" → kini tanpa "[E]" saat kontrol sentuh tampil.
3. `runtime simulate_key` dari Unity CLI tidak menggerakkan player; penyebab: setting Editor `PointersAndKeyboardsRespectGameViewFocus`. Untuk test, setting di memori diubah sementara lalu **dikembalikan**; tidak ada file setting yang tersimpan.
4. Satu percobaan Playwright pertama menghasilkan jarak ~13 unit untuk 1 s tombol D; **tidak dapat direproduksi**. Pengukuran terkontrol berikutnya normal. Penekanan tombol pertama setelah klik di Playwright juga terkadang tidak tepat durasinya → test diberi "pemanasan" tombol.
5. Satu screenshot Playwright kosong (sesi browser ter-reset) → hasil tersebut **dibuang**, test diulang.
6. Capture Game view Editor sedikit lonjong (rasio jendela Game view); di browser lingkaran bulat sempurna.
7. Label debug "IDLE D" dapat bertumpuk dengan "HOUSE (TEMP)" saat player tepat di belakang rumah (hanya label sementara).

### 6.5 Build final (setelah perbaikan)
| Uji | Hasil |
| --- | --- |
| Rebuild | Succeeded, 0 error, 0 warning |
| Console browser | **0 error** (favicon 404 hilang); 6 warning engine `getInternalformatParameter` (sama seperti M0) |
| Desktop 1280×720 | kontrol sentuh **tersembunyi**, hint keyboard tampil, prompt `[E]  Read Welcome Board` |
| 844×390 + `?touch=1` | joystick + tombol Interact tampil, prompt tanpa "[E]", tap Interact membuka dialog |
| Catatan | teks dialog pada 844×390 berukuran ±15 px CSS — perlu dinilai keterbacaannya di HP nyata |

## 7. Ukuran Web build
| File (Brotli) | Ukuran |
| --- | --- |
| `FirstPlayable.wasm.br` | 7.28 MB |
| `FirstPlayable.data.br` | 1.71 MB |
| `FirstPlayable.framework.js.br` | 75 KB |
| `FirstPlayable.loader.js` | 27 KB |
| **Total (BuildReport)** | **9.09 MB** (M0 smoke: ≈8.7 MB) |

Waktu build: ±14.5 menit (pertama), ±11.4 menit (rebuild). Output di `Builds/Web/FirstPlayable` (di-ignore Git). Masih jauh di bawah target draft 25 MB; angka akan berubah setelah art M2.

**Known issue (tidak berbahaya):** proses build Input System sementara menambahkan input actions project-wide ke `preloadedAssets`; karena profile Web punya salinan Player Settings sendiri, nilai sementara itu bisa ikut tersimpan di `Web.asset`. Diff baris ini dapat muncul/hilang antar build; tidak memengaruhi gameplay.

## 8. Belum diuji / keterbatasan
- **HP nyata (Android Chrome, iOS Safari): BELUM diuji.** Touch multi-jari, safe area notch, performa, dan rotasi hanya bisa dipastikan di perangkat.
- Input sentuh hanya diuji lewat jalur pointer (mouse) yang sama dengan sentuhan di UGUI; event touch asli browser belum.
- Browser Edge/Firefox/Safari desktop belum.
- FPS/memori nyata belum diukur (scene greybox kosong; angka di otomasi browser tidak representatif).
- Portrait: belum ada pesan "rotate device" (usulan di Art Bible) — direkomendasikan untuk M2/M3.
- Teks memakai font Legacy bawaan Unity; font final (Fredoka/Nunito, cek lisensi) dan TextMeshPro diputuskan di M2.
- Gamepad sudah dipetakan (stick kiri, South, Start) tetapi tidak diuji.

## 9. Langkah uji manual untuk founder
1. **Editor:** buka `Assets/_Lifetopia/Scenes/FirstPlayable`, tekan Play, **klik di Game view** (agar keyboard masuk), lalu:
   - WASD dan panah: player bergerak 4 arah, label berganti `WALK U/D/L/R`, diagonal tidak lebih cepat.
   - Tabrak rumah, toko, pohon (batang), air, dan tepi map. Kamera tidak boleh memperlihatkan area hijau tua di luar map.
   - Berjalan ke belakang rumah/pohon: player tertutup; di depan: terlihat.
   - Dekati papan INFO → prompt, tekan **E** (lanjut), **E** (tutup), coba **Esc**.
   - Klik jendela lain saat menahan D → kembali: player tidak boleh berjalan sendiri.
   - GameplayUI → TouchControlsVisibility → Mode = AlwaysShow, uji joystick dengan mouse.
2. **Web lokal:** setelah build, jalankan server lokal (minta Claude) dan buka `http://127.0.0.1:<port>/`; tambahkan `?touch=1` untuk memaksa kontrol sentuh di desktop.
3. **HP nyata:** butuh server yang terlihat di jaringan WiFi lokal (bukan deploy). Minta Claude memandu langkah yang aman; catat perangkat, browser, apakah joystick/tombol/notch/rotasi berfungsi, dan kelancaran.

## 10. Rekomendasi M2 (Art calibration)
1. Batch A0 sesuai ASSET-MASTER-LIST: 1 avatar idle/walk (D dulu), grass + path tile, palem, rumah, sampel HUD — **founder menyetujui hasil sebelum produksi massal**.
2. Ganti `PlaceholderCharacterView` dengan view berbasis Animator (parameter Facing int + IsWalking bool dari `PlayerFacing`); pivot kaki (0.5, 0.125) sesuai DECISIONS.
3. Uji seam tile 3×3, foot anchor, Y-sort dengan sprite asli, keterbacaan di layar HP.
4. Putuskan font & TextMeshPro; pesan rotate untuk portrait; jadikan GameplayUI prefab bila scene kedua dibuat.
5. Pertimbangkan apakah project-wide input actions perlu diarahkan ke `LifetopiaControls` (template tetap dipertahankan sampai disetujui).
6. Uji HP nyata M0/M1 sebelum art ditambahkan agar baseline performa tercatat.
