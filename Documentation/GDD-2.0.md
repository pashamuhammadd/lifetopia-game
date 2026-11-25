# Lifetopia World — Game Design Document 2.0

Draft kerja | 25 November 2026 | Nimia Studio

## 01 / Product vision

Lifetopia World adalah cozy life simulation dan social sandbox berlatar desa tropis. Pemain membangun kehidupan virtual melalui aktivitas santai, rumah pribadi, eksplorasi, dan hubungan sosial. Pengembangan ulang memakai Unity 2D dengan visual Chibi Tropical Cozy yang memberi kesan 2.5D, bukan dunia 3D real-time.

![Concept art Chibi Tropical Cozy](reference/chibi_tropical_cozy.png)

Janji pengalaman: “Your little world, your endless possibilities.” Pemain dapat masuk sebentar, melakukan aktivitas yang menyenangkan, memperoleh kemajuan nyata, dan pulang ke rumah virtualnya.

### Keputusan Game Direction (disetujui)

| Aspek | Keputusan |
| --- | --- |
| Visual & dunia | Chibi Tropical Cozy; desa tropis |
| Mode & platform | Prototype single-player → multiplayer; browser PC dan HP sejak awal |
| Rumah & aktivitas | Rumah awal gratis; properti lain dapat dibeli; aktivitas berkembang bertahap |
| Waktu & Web3 | Siklus waktu dalam game; Web3 setelah gameplay stabil |

Status: GDD 2.0, draft kerja untuk validasi founder. Semua angka, nama lokasi/NPC, mekanik rinci, dan target teknis di luar keputusan di atas adalah usulan desain yang dapat diubah setelah playtest.

## 02 / Experience & scope

### Pilar desain

- Cozy: tidak ada hukuman keras, kelaparan, atau kehilangan uang ketika pemain berhenti bermain. Aktivitas harus memberikan feedback yang memuaskan.
- Play: pilihan aktivitas singkat, reward yang jelas, dan progres tanpa grinding berlebihan.
- Connect: identitas Lifetopia yang sama untuk game, website, dan community; pengalaman sosial online diperkenalkan setelah gameplay dasar stabil.
- Create: rumah, pakaian, dan dekorasi sebagai ekspresi pemain, bukan keuntungan kompetitif.
### Target pemain (hipotesis)

Pemain kasual yang menyukai life sim, koleksi, kustomisasi, dan sesi pendek 5–15 menit. Desain mobile-friendly; tidak mengasumsikan pemain memahami Web3. Bahasa UI awal: Inggris; lokalisasi Indonesia setelah fondasi UI siap.

### Batas rilis

| Tahap | Termasuk | Tidak termasuk |
| --- | --- | --- |
| Vertical slice | Satu desa, satu karakter, farming, fishing, toko, rumah awal sederhana, quest, save lokal, build Web. | Login online, multiplayer, properti tambahan, token/NFT. |
| Connected alpha | Lifetopia Account, save server, profil, dekorasi dasar, sinkronisasi community terbatas. | Real-time multiplayer dan marketplace on-chain. |
| Online beta | Shared plaza, avatar pemain, chat/moderasi, kunjungan rumah; server authoritative. | Dunia luas dan ekonomi on-chain sampai tervalidasi. |
| Ekspansi | Properti tambahan, outfit, crafting, event, biome, Web3 opsional. | Tidak ada janji token atau pendapatan bagi pemain. |

Kriteria vertical slice: pemain baru menyelesaikan satu quest dan memperoleh barang/coin pertama tanpa bantuan developer, lalu dapat menutup dan membuka ulang game tanpa kehilangan progres.

## 03 / First village & onboarding

### Lifetopia Village — tata letak awal (usulan)

Map kecil 40 × 40 sel logis, satu scene outdoor; rumah interior scene terpisah. Spawn di plaza; farm dan rumah di barat, market di timur, dock di selatan, forest di utara. Semua rute utama dapat dilewati tanpa melewati collider; uji jarak jalan aktual sebelum memperluas map.

| Lokasi | Tujuan & interaksi |
| --- | --- |
| Town Plaza | Spawn, papan quest, NPC penyambut; kelak titik kumpul multiplayer. |
| Starter House | Rumah gratis; masuk/keluar scene, tempat istirahat; dekorasi terbatas pada alpha. |
| Farm | 6 petak awal; tanam, siram, panen. |
| Market | Beli bibit, jual panen/ikan, lihat saldo. |
| Fishing Dock | Spot memancing; minigame singkat. |
| Forest | Area eksplorasi visual; gathering baru setelah slice stabil. |

### NPC usulan

Maya (penyambut dan tutorial), Rio (pedagang), Kai (nelayan). Nama dan desain belum final. Dialog maksimal 2–3 kalimat tiap langkah; tombol lewati tersedia.

### Lima hingga delapan menit pertama

| Urutan | Pengalaman pemain |
| --- | --- |
| 0–1 menit | Masuk, lihat avatar, bergerak dengan keyboard/touch, temui Maya. |
| 1–2 menit | Terima rumah awal, 100 coin, 3 bibit dan alat; tanam dan siram bibit. |
| 2–4 menit | Ke dock, tangkap ikan; lihat reward dan inventory. |
| 4–6 menit | Panen tanaman siap, jual ke Rio, beli bibit baru. |
| 6–8 menit | Klaim quest awal, pulang, save; ajak pemain kembali untuk kegiatan berikutnya. |

Tutorial tidak memerlukan wallet, login, ataupun koneksi multiplayer. Jika pemain gagal memancing, retry langsung tersedia.

## 04 / Core mechanics

### Core loop

Eksplorasi → pilih aktivitas → lakukan aksi → peroleh item/coin → gunakan untuk menambah koleksi atau rumah → buka tujuan baru. Pemain bebas memilih aktivitas setelah tutorial.

| Sistem | Aturan prototype (usulan) |
| --- | --- |
| Movement | WASD/arrows; joystick virtual kiri di HP. Gerak bebas, 4 arah animasi, diagonal dinormalisasi; kamera mengikuti pemain. Interaksi E atau tombol konteks. |
| Farming | Pilih bibit → tanam pada petak kosong → siram sekali → tumbuh selama 120 detik waktu bermain → panen 1 tanaman. Status: Empty, Planted, Watered, Ready. Tidak layu. |
| Fishing | Tekan interaksi di dock; indikator bergerak, tekan saat masuk zona target. Maks. 8 detik per percobaan; jika gagal tidak kehilangan item. |
| Inventory | 12 slot stack, maksimal 99 per slot; alat dasar non-consumable di hotbar. Jika penuh, reward ditolak tanpa menghapus sumber reward; beri pesan. |
| Shop | Beli/jual per unit, konfirmasi kuantitas dan saldo; jangan izinkan saldo negatif atau barang fiktif. |
| Quest | Tutorial satu kali: panen 1, tangkap 1, jual 1 item; hadiah 30 coin setelah semua selesai, tidak dapat diklaim dua kali. |
| Housing | Rumah gratis dari awal, interior dasar; properti tambahan hanya setelah sistem rumah stabil. |
| Waktu | 1 hari game = 24 menit aktif (1 jam game/menit nyata). Mulai pukul 08:00. Pause saat menu pause/halaman tidak aktif; tidak ada progres offline pada slice. |

### Feedback & aksesibilitas

Setiap aksi: indikator fokus objek, animasi pendek, suara lembut, floating reward, serta teks/status. Jangan mengandalkan warna saja. Audio dapat dimatikan; kurangi motion dan efek partikel jika perangkat lambat.

## 05 / Economy, persistence & social

### Ekonomi awal — balancing usulan, bukan harga final

| Item / aksi | Nilai | Keterangan |
| --- | --- | --- |
| Saldo awal | 100 coin | Ditambah 3 bibit gratis dan alat dasar. |
| Bibit tropis | 10 coin | Satu hasil panen per bibit. |
| Hasil panen | 18 coin | Margin 8 coin jika bibit dibeli. |
| Ikan umum | 12 coin | Reward fishing; tidak ada biaya percobaan. |
| Quest tutorial | 30 coin | Satu kali, setelah seluruh tujuan. |
| Dekorasi dasar | 50–150 coin | Target sink awal di connected alpha. |
| Properti tambahan | TBD | Setelah telemetry ekonomi dan housing stabil. |

Contoh: setelah memanen 3 bibit gratis dan menjual 1 ikan, saldo awal menjadi 166 coin sebelum hadiah quest. Setelah klaim: 196 coin. Dengan pembelian 3 bibit berikutnya, saldo 166 coin. Validasi tempo reward melalui playtest; hindari eksploitasi duplikasi.

### Save dan integritas progres

Prototype: save lokal berversi berisi posisi/scene, inventory, coin, quest, petak farm, jam game, dan flag tutorial. Simpan setelah transaksi, panen, quest, perpindahan scene, dan berkala. Uji reload, penyimpanan browser diblokir, dan data rusak; tampilkan pesan recovery/reset. Save lokal tidak dianggap sumber kebenaran untuk ekonomi online.

### Connected alpha & multiplayer

Gunakan stable user ID dari Lifetopia Account. Save server menjadi authoritative; migrasi save lokal hanya melalui kebijakan terkontrol, tanpa mengimpor coin/item tidak tervalidasi. Backend memvalidasi ownership, reward, transaksi dan idempotency. Multiplayer dimulai dari plaza kecil dan chat; moderasi, mute, report, rate limiting serta privasi harus siap sebelum rilis publik.

### Monetisasi dan Web3

Usulan: kosmetik, furnitur dan dekorasi opsional; hindari pay-to-win dan loot box berbayar. Web3 baru setelah retensi, keamanan, dan ekonomi terbukti; wallet opsional, kepemilikan digital transparan, tidak menjanjikan profit.

## 06 / Engineering & integration

### Arsitektur kerja

Pertahankan lifetopia-platform (Next.js/Turborepo) untuk website, community, grants, dan shared identity. Usulkan repo terpisah lifetopia-game untuk Unity source, prefab, scene, test dan aset. Game dipublikasikan ke play.lifetopiaworld.io melalui web host yang mendukung konfigurasi build Unity Web. Jangan pindahkan akun atau mengubah database produksi sebelum audit.

| Lapisan | Keputusan teknis awal |
| --- | --- |
| Client | Unity 6 LTS yang kompatibel, template 2D, C#, SpriteRenderer, Tilemap, orthographic camera. Unity version dikunci setelah smoke test. |
| Input & UI | Unity Input System; keyboard + touch; UI responsive dengan safe area; landscape utama. Portrait menampilkan arahan rotasi pada slice (usulan). |
| Data | ScriptableObject untuk definisi item/quest/crop; runtime state terpisah; service interface untuk LocalSave dan RemoteSave. |
| Web integration | Web shell menangani login akun yang sudah ada; bridge JavaScript–C# mengirim session/credential pendek umur melalui protokol yang diaudit. |
| Backend | API memvalidasi sesi dan akses user ID; RLS sesuai skema nyata; tidak ada service role key atau secret di Unity build. |
| Build & repo | Git, .gitignore Unity, .meta wajib; Git LFS bila perlu; Web build artifacts tidak masuk source repo. |

### Batas integrasi

Sesi login lintas subdomain, mekanisme token exchange, CORS, cookie policy, revocation, dan skema Supabase harus diperiksa berdasarkan source yang aktual. Jangan menganggap session otomatis terbagi antar subdomain. Uji browser iOS/Android; dukungan build Web dan memory bergantung engine/browser/perangkat.

### Pembelajaran Unity

Setiap milestone Claude Code wajib menjelaskan struktur scene, script, prefab, cara edit Inspector dan test manual. Founder menjalankan scene, memeriksa console, dan menyetujui perubahan sebelum merge.

## 07 / Milestones & release gates

| ID / milestone | Definition of done |
| --- | --- |
| M0 Foundation | Repo, Unity project, Git ignore, CLAUDE.md; build kosong berhasil pada PC dan satu HP target. |
| M1 Movement | Scene greybox, karakter 4 arah, camera follow, collider dan kontrol touch/keyboard lulus. |
| M2 Visual sample | Satu rumah, pohon, terrain, pemain, HUD masuk Unity; scale, pivot, sorting, dan readability cocok dengan Art Bible. |
| M3 Gameplay | Farming, fishing, inventory, market, quest dan local save berfungsi dalam satu loop. |
| M4 Vertical slice | Pengujian ulang build Web; 5–8 menit onboarding dapat selesai; minimal 5 playtester mencoba tanpa panduan langsung (target usulan). |
| M5 Connected alpha | Audit auth/API, save authoritative, account reuse, recovery dan privacy. |
| M6 Online beta | Multiplayer plaza, reconnect, moderation, load test, server validation; tidak ada duplikasi reward. |

### Target kualitas sementara

30 FPS stabil pada HP target dan 60 FPS PC target (target, bukan jaminan), initial compressed download ≤ 25 MB jika memungkinkan; profiling actual memory dan waktu muat. Uji Chrome/Edge PC, Chrome Android, Safari iOS; catat perangkat, browser, FPS dan error. Target awal perlu direvisi berdasarkan baseline.

### Risiko & keputusan terbuka

Performa Web di HP; konsistensi sprite AI; scope creep; kompatibilitas autentikasi; perubahan milestone grant sebelumnya. Audit kontrak grant dan repository sebelum mengubah rilis publik. Validasi orientasi, panjang hari, ekonomi dan nama desa/NPC melalui prototype.

### Sumber dan status

Keputusan founder 25 November 2026; concept art Chibi Tropical Cozy; README lifetopia-platform: https://github.com/pashamuhammadd/lifetopia-platform/blob/main/README.md (dibaca pada pembahasan sebelumnya). Belum ada audit kode lengkap, perubahan repo, atau pengujian engine.
