# LgProduksi

Sistem Manajemen Produksi dan Penjualan Produk Elektronik berbasis desktop (VB.NET dan SQL Server).
Dibuat sebagai proyek Praktik Kerja Lapangan (PKL). Data produk (TV, Monitor, dan komponennya) adalah **data simulasi**.

## Fitur

| Peran | Fitur utama |
|---|---|
| **Supplier** | Dashboard, kelola komponen (tambah, edit, nonaktifkan, hapus), buat produk dari komponen, produksi tambahan, lihat stok komponen dan produk |
| **Sales** | Dashboard, lihat produk supplier, katalog penjualan (harga jual dan stok), lihat customer, proses atau batalkan pesanan, riwayat penjualan, laporan penjualan PDF |
| **Customer** | Dashboard, katalog dan pencarian, keranjang, checkout, status pesanan, riwayat pembelian |

## Teknologi

- VB.NET, Windows Forms, .NET 8
- SQL Server Express dan SQL Server Management Studio (SSMS)
- Paket NuGet: `Microsoft.Data.SqlClient`, `QuestPDF`

## Cara menjalankan

1. Buka `database.sql` di SSMS lalu jalankan (Execute). Skrip ini membuat database `LgProduksiDB`, semua tabel, dan data contoh.
2. Buka `LgProduksi.sln` di Visual Studio (workload ".NET desktop development").
3. Sesuaikan nama server pada `ConnStr` di `Db.vb`, misalnya `Server=localhost\SQLEXPRESS;...`.
4. Tekan **F5**. Paket NuGet akan dipulihkan otomatis saat build.

## Akun demo

| Username | Password | Peran |
|---|---|---|
| `supplier` | `supplier123` | Supplier |
| `sales` | `sales123` | Sales |
| `customer` | `customer123` | Customer |

Akun ini hanya untuk percobaan. Ganti atau hapus bila aplikasi dipakai di luar simulasi.

## Struktur berkas

| Berkas | Fungsi |
|---|---|
| `Program.vb` | Titik masuk aplikasi |
| `Db.vb` | Koneksi database dan fungsi dasar (Query, Exec, Scalar, transaksi) |
| `Session.vb` | Data pengguna yang sedang login dan hash password (SHA-256) |
| `UI.vb` | Pembantu tampilan (tabel, tombol, dialog input) |
| `LoginForm.vb` | Form login dan registrasi customer |
| `RoleForm.vb` | Kerangka halaman utama (header, tab, logout) |
| `SupplierForm.vb`, `ProductBuilder.vb` | Halaman Supplier dan pembuatan produk |
| `SalesForm.vb`, `SalesReport.vb` | Halaman Sales dan laporan PDF |
| `CustomerForm.vb` | Halaman Customer |
| `OrderHelper.vb` | Detail dan pembatalan pesanan |
| `database.sql` | Skrip database dan data contoh |

## Catatan

- Laporan PDF memakai QuestPDF dengan lisensi Community. Periksa ketentuannya di https://questpdf.com/license/ sebelum dipakai secara komersial.
- Nama dan logo LG hanya dipakai sebagai konteks simulasi, bukan produk resmi LG Electronics.
