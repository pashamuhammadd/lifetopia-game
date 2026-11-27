# Lifetopia World — Art Bible 1.0

Draft kerja | 25 November 2026 | Nimia Studio

## 01 / Visual north star

Lifetopia World menggunakan Chibi Tropical Cozy: ilustrasi 2D yang terasa bervolume melalui shading, bentuk membulat, dan sudut kamera tetap. Dunia hangat, santai, cerah, dan ramah pemain. Semua aset harus terasa berasal dari satu dunia.

![Concept art Chibi Tropical Cozy](reference/chibi_tropical_cozy.png)

Concept art pilihan founder: Chibi Tropical Cozy. Ini referensi suasana, palet, proporsi dan bentuk, bukan screenshot aktual atau sprite siap pakai. Perspektif ilustrasi promosi yang menampilkan cakrawala harus disederhanakan untuk map gameplay.

### Pilar visual

- Tropical: kayu, atap jerami, pohon buah, palem, pasir, air turquoise, bunga hibiscus.
- Cozy: bentuk lembut, ekspresi bersahabat, detail kecil yang memberi kehidupan.
- Premium & readable: siluet jelas, warna terkontrol, detail tidak mengganggu jalur pemain.
Status: Art Bible 1.0, draft standar produksi. Angka ukuran, warna, dan pipeline di halaman berikut adalah usulan yang dikunci setelah satu sampel scene lulus di Unity.

## 02 / Color, light & shape

### Palet produksi awal

- Leaf #74B85B: Rumput dan daun
- Deep leaf #377F55: Bayangan vegetasi
- Lagoon #55C6C8: Air dan aksen
- Sand #EBD4A5: Jalan/pasir
- Wood #A8754D: Kayu
- Coral #EE816C: Bunga dan accent
- Cream #FFF3D8: UI dan highlight
- Ink #604A3C: Outline hangat

### Pencahayaan dan material

Cahaya utama dari kiri atas layar, highlight di sisi atas/kiri, bayangan ke kanan bawah. Baked soft shading dalam sprite; maksimal 2–3 tingkat tone per material utama. Contact shadow lokal lembut; hindari bayangan panjang yang tertanam di sprite bangunan. Tambahkan shadow terpisah bila diperlukan.

### Shape language

Gunakan sudut membulat, atap melengkung halus, daun lebar, kayu dan kain yang sederhana. Outline warna gelap hangat, bukan hitam pekat; tipis dan selektif. Pada aset karakter 128 px, target outline tampak 1–2 px, sesuaikan skala objek.

### Do / Don’t

| Do | Don’t |
| --- | --- |
| Warna kaya tetapi lembut, area fokus terang. | Neon, hitam pekat, outline tebal seragam. |
| Bentuk mudah dibaca dari jauh. | Tekstur realistis, noise berlebihan, detail mikro. |
| Satu arah pencahayaan konsisten. | Campuran perspektif, cahaya dan style dari berbagai pack. |
| Variasi kecil untuk alam. | Mengulang objek besar dengan pola terlihat jelas. |

## 03 / Camera, scale & sorting

### Perspektif gameplay

Unity 2D orthographic camera menghadap bidang XY secara tegak lurus. Tampilan 2.5D dibuat dari sprite yang digambar oblique top-down, dengan perkiraan visual sudut pandang 40° dari horizontal. Ini bukan kamera 3D miring dan bukan grid diamond isometric. Tile tanah persegi, tanpa horizon/vanishing point. Semua objek tampak bagian depan dan atas secara konsisten.

| Parameter | Standar awal |
| --- | --- |
| Grid & PPU | 1 sel = 1 Unity unit; terrain tile 64 × 64 px; 64 pixels per unit untuk seluruh sprite dunia. |
| Karakter | Canvas 128 × 128 px; tinggi gambar karakter ±96–112 px (1,5–1,75 unit), titik kaki (64,16) dari kiri bawah. |
| Bangunan kecil | Footprint 3 × 3 atau 4 × 3 sel; art visual dapat melampaui footprint. Collider hanya area dasar, pintu tetap dapat diakses. |
| Pohon | Footprint 1 × 1 sel; sprite contoh 128 × 192 px; pivot dasar batang, canopy menutupi karakter di belakang. |
| Props | 1–2 sel footprint; canvas umum 64/128 px dengan padding sesuai siluet. |
| Kamera | Orthographic size awal 5,5; uji zoom dan aspect 16:9 / layar HP lebar. |

### Layer dan depth

Ground → ground detail → world objects & player (Y-sort) → overhead khusus → UI. Semua objek yang perlu saling menutupi berada pada layer depth yang sama, sort axis (0,1,0), Sprite Sort Point Pivot, SortingGroup untuk prefab multi-sprite. Pivot di kaki/footprint dasar. Jangan gunakan collider sebesar gambar atap atau canopy.

### Acceptance test

Karakter berjalan di depan/belakang pohon dan rumah tanpa pop/lompatan sorting. Ground tile tidak memiliki seam pada 3 × 3 pengulangan dan semua jalan menyambung. Ukuran pintu masuk masuk akal terhadap karakter.

## 04 / Characters & animation

### Desain karakter

Proporsi 2–2,5 kepala; wajah besar, badan kecil, tangan dan kaki jelas; ekspresi hangat. Satu avatar awal dengan pakaian tropis sederhana. NPC Maya, Rio dan Kai berbeda siluet, warna pakaian, rambut, serta aksesori. (M3.5: desain Rio, Maya dan Kai disetujui founder; sprite runtime 128 × 128 px, PPU 64, pivot kaki (0.5, 0.125) sama dengan pemain, 4 arah statis D/L/R/U + portrait 256 × 256; Y-sort lewat SortingGroup layer World seperti pemain. Rio lulus founder acceptance M3.5: ukuran sesuai pemain, posisi di depan kios.) Gender dan variasi tubuh/pakaian ditambah setelah base style konsisten.

| Animasi | Frame / arah | Standar |
| --- | --- | --- |
| Idle | 4 × 4 | 4 frame per arah, ±4 fps; napas ringan. |
| Walk | 6 × 4 | 6 frame per arah, ±8–10 fps; kontak kaki konsisten. |
| Water | 4 × 4 | 4 frame per arah; selesai kembali idle. |
| Fish | 6 × 1 | 6 frame arah dock awal; arah lain saat dibutuhkan. |
| Harvest | 4 × 4 | 4 frame per arah; efek panen terpisah. |

Arah D (down/depan), L (left), R (right), U (up/belakang). Sprite sheet per animasi: kolom = waktu, baris = arah D/L/R/U; ukuran sel 128 × 128 px; koordinat pivot sama. Untuk fishing hanya arah yang dipakai spot. Left/right boleh mirror hanya jika desain simetris; aksesori asimetris perlu frame sendiri.

### Ekspor dan modularitas

PNG RGBA, background transparan, tanpa teks atau label pada sheet. Jangan resize setiap frame secara independen. Simpan master base avatar; modular outfit tahap berikut memakai pivot dan frame index sama, dengan layer rambut, badan, pakaian dan aksesori. Untuk prototype outfit boleh dibake untuk mengurangi kompleksitas.

### QC animasi

Periksa onion-skin, wajah/warna tidak berubah, tidak ada jitter pivot, langkah loop mulus, tidak ada bayangan melayang, dan tidak ada frame cacat. AI image generation tidak menjamin frame konsisten; perbaikan manual/prosedural diperbolehkan sebelum approve.

## 05 / Terrain, environment & items

### Terrain starter tiles

Rumput, pasir/jalan, tanah kebun, air dangkal dan air dalam. Gunakan tile 64 × 64 px; ground top surface rata dan berulang, tanpa perspective distortion. Transisi terrain memakai **dual-grid** (keputusan founder M3.4, 26 Nov 2026; menggantikan bitmask 4 arah N/E/S/W yang lama): tilemap visual digeser setengah sel (-0.5, -0.5) sehingga satu tile digambar per titik sudut grid, dengan corner mask NW=1, NE=2, SE=4, SW=8 (16 tile, indeks 0-15). Tilemap logis dan collider tidak berubah. Shoreline pasir-air memakai set terpisah dengan mask yang sama (0 = pasir penuh, 15 = air); pasir = tekstur jalan, jadi rumput tidak pernah bersentuhan langsung dengan air; mask diagonal 5 dan 10 dihindari dalam level design. Lihat `Documentation/Progress/M3-04-fishing.md`.

### Aset dunia dan hierarki visual

| Kategori | Standar desain |
| --- | --- |
| Rumah | Kayu hangat, atap jerami, pintu turquoise, jendela sederhana, porch; entrance jelas. |
| Market | Kios dengan kanopi coral/cream, meja dan peti buah; tidak perlu interior pada slice. |
| Vegetasi | Palem, pohon buah, semak, hibiscus; variasi siluet dan warna senada. |
| Water/dock | Air turquoise, riak kecil, batu tepi, dock papan kayu dengan rope posts. |
| Farm | Petak kosong, ditanam, basah, siap panen; visual status dan ikon interaksi tidak ambigu. |
| Item | Bibit, hasil panen, ikan, coin, alat. Ikon 128 × 128 px dengan padding 12 px, siluet tegas. |
| VFX | Splash, sparkles panen, coin pop; frame kecil dan efek singkat, hindari particle berlebihan. |

### Interior & housing

Scene rumah terpisah dengan kamera/perspektif sama; lantai grid persegi, dinding belakang/samping terpotong agar terlihat. Furnitur satu pivot dasar dan footprint terdefinisi. Rotasi furnitur 90° hanya dengan sprite varian yang benar, bukan memutar sprite perspektif begitu saja. Dekorasi bebas dan properti baru masuk setelah vertical slice.

**Kalibrasi M3.6 (Housing Asset Pack 01, Tropical Cozy Cottage — disetujui founder 27-11-2026, acceptance 18/18; baseline):**

| Aturan | Nilai |
|---|---|
| Grid interior | 8 × 8 sel logis, 1 unit = 1 sel = 64 px; origin sudut barat-daya lantai |
| Import | Sprite Single, 64 PPU, Bilinear, Clamp, tanpa mipmap, tanpa kompresi (kalibrasi), Full Rect, pivot dari manifest |
| Pivot furnitur | 8 px di atas tepi bawah PNG = dasar visual (padding bawah terukur 8 px di semua sprite furnitur/dekor); anchor = depan-tengah footprint. Rug: pivot tengah, anchor tengah footprint |
| Footprint | Dari data (`FurnitureDefinition`), bukan dari ukuran PNG; collider = footprint dikurangi 0,06 tiap sisi |
| Sorting | Lantai `Ground`; dinding belakang/samping/jendela `GroundDetail`; rug `GroundDetail` order 5; furnitur `World` (SortingGroup, sumbu Y custom, sama dengan pemain); dinding depan rendah `World` pivot; ghost/penanda Decorating `Overhead` |
| Dinding | Back wall 96 px (1,5 u) + cap kiri/kanan; strip samping 17 px diletakkan tepat di luar lantai; front cutaway 17 px dengan bukaan pintu 2 sel |
| Arah | Hanya bed memakai varian west (bed_north 1×2 ↔ bed_west 2×1). Varian west lain cadangan sampai dikalibrasi; tidak ada sprite back/east |
| Kalibrasi skala (founder 27-11-2026) | `bed_north` memakai skala visual **seragam 1,3** (X = Y, di sekitar pivot) agar sebanding dengan `bed_west` dan karakter; PNG/PPU/pivot/footprint/collider tidak berubah. Aturan umum: skala hanya seragam, diukur dari fitur yang sama antar varian, dicatat per sprite di `HousingVisualSet`. `table_west` masih terlalu kecil (cadangan, T-58) |
| Publik | Interior publik memakai blok greybox berlabel "(TEMP)" yang dibuat di kode; tidak ada PNG |

## 06 / UI, UX & mobile

### Visual interface

Panel cream dengan outline cokelat lembut, sudut membulat, accent coral dan lagoon; ikon berisi bentuk besar dan shading ringan. Gunakan teks gelap dengan kontras memadai. Judul ramah dan rounded, body sans-serif yang terbaca; usulan font Fredoka + Nunito Sans (verifikasi lisensi sebelum bundling).

| Komponen | Layout & behavior |
| --- | --- |
| HUD | Atas kiri: avatar, jam/hari; atas kanan: coin, settings. Quest ringkas dapat collapse. |
| Hotbar | Bawah tengah PC; 4 tool shortcuts. Mobile geser ke area bawah tengah/kanan tanpa bertabrakan joystick. |
| Controls | Joystick kiri bawah, tombol aksi kontekstual kanan bawah; area sentuh ≥44 × 44 CSS px ekuivalen. |
| Inventory/shop | Modal responsif; jumlah stack dan saldo jelas; konfirmasi transaksi; close mudah. |
| Dialogue | Panel bawah dengan nama NPC, potret opsional, tombol lanjut/lewati. |
| Feedback | Ikon + teks: ready, insufficient coins, inventory full, saved; tidak hanya mengandalkan warna. |

### Responsive baseline

Reference canvas 1280 × 720, scale with screen size; uji 16:9 desktop, 19.5:9/20:9 HP landscape dan safe area notch. Orientasi portrait pada prototype menampilkan instruksi rotasi yang jelas (usulan). Jangan menempatkan tombol permanen di area gameplay penting.

### Aksesibilitas

Kontras teks normal target WCAG 4.5:1, label ikon, ukuran font yang terbaca di HP, mute musik/SFX, reduce motion, tidak ada flash cepat. UI text berupa komponen teks, bukan baked text di PNG. Uji keyboard dan touch secara terpisah.

### Audio mood (arah awal)

Musik akustik tropis yang ringan, ambience air dan burung, efek interaksi lembut. Gunakan aset original atau lisensi yang jelas; audio tidak wajib untuk memahami status gameplay.

## 07 / Production pipeline & starter manifest

### Folder dan penamaan

Assets/Art/Lifetopia/{Characters,Terrain,Buildings,Props,Items,UI,VFX}; simpan master di luar folder runtime bila besar. Gunakan lower_snake_case: char_player_walk_d_v01.png, terrain_grass_01_v01.png, bld_starter_house_v01.png. Sprite sheet multi-arah: char_player_walk_4dir_v01.png.

| Batch | Isi minimum untuk gate |
| --- | --- |
| A0 Calibration | 1 avatar idle/walk down, 1 grass tile, 1 path tile, 1 palm, 1 house, 1 HUD sample. Jangan produksi massal sebelum lulus. |
| A1 Terrain | Grass variants, path/grass transitions, water/shore, tilled soil, dock. |
| A2 Player/NPC | Player idle/walk/action sheets; 3 NPC idle + portrait bila perlu. |
| A3 Environment | Starter house, market stall, palm, fruit tree, shrubs, flowers, rocks, fences, props. |
| A4 Gameplay | Crop 4 state visuals, 1 fish, 1 seed, harvest item, coin, watering can, rod, quest UI. |
| A5 Polish | Interior starter, VFX, sound cues, HUD panels, inventory/shop/quest icons. |

### Unity import / QC

Sprite (2D and UI), PPU 64 untuk world, filter Bilinear (bukan Point), mipmaps off untuk baseline 2D, alpha transparency on; sprite atlas dengan padding ≥4 px, max atlas 2048 awal, uji kompresi platform agar tepi bersih. UI icons bebas PPU sesuai layout. Sprite sheet slicing grid dan pivot custom. Jangan gunakan gambar konsep utuh sebagai tilemap.

Checklist approve: transparansi bersih; canvas/pivot benar; style/palette/scale cocok; tidak ada watermark/teks; tile seam lolos; animasi stabil; sorting/collider lolos; profil memory/build Web; catat sumber/lisensi, prompt, versi dan status approval. ChatGPT membuat art; Claude Code mengimpor, menguji dan memperbaiki teknis; founder menyetujui. Belum ada aset game-ready atau pengujian Unity dalam dokumen ini.
