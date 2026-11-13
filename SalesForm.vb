Imports Microsoft.Data.SqlClient
Imports System.Windows.Forms

Public Class SalesForm
    Inherits RoleForm

    Private lblKatalog, lblMenunggu, lblDiproses, lblOmzet, lblCust As Label
    Private gProd As DataGridView = NewGrid()
    Private gKat As DataGridView = NewGrid()
    Private gCust As DataGridView = NewGrid()
    Private gPesan As DataGridView = NewGrid()
    Private gRiwayat As DataGridView = NewGrid()

    Private Const OrdCols As String =
        "o.OrderId, 'ORD-' + RIGHT('00000' + CAST(o.OrderId AS VARCHAR(10)), 5) AS NoPesanan, u.FullName AS Customer, " &
        "o.OrderDate AS Tanggal, o.Total, o.Status "

    Public Sub New()
        MyBase.New("Sales")

        Dim fp = AddDashboard()
        lblKatalog = AddCard(fp, "Produk di Katalog")
        lblMenunggu = AddCard(fp, "Pesanan Menunggu")
        lblDiproses = AddCard(fp, "Pesanan Diproses")
        lblOmzet = AddCard(fp, "Total Penjualan (Selesai)")
        lblCust = AddCard(fp, "Jumlah Customer")

        Dim t1 = AddTab("Produk dari Supplier")
        t1.Controls.Add(gProd)
        t1.Controls.Add(ButtonBar(Btn("Tambah ke Katalog", AddressOf TambahKatalog, 160)))

        Dim t2 = AddTab("Katalog Penjualan")
        t2.Controls.Add(gKat)
        t2.Controls.Add(ButtonBar(
            Btn("Ubah Harga", AddressOf UbahHarga, 120),
            Btn("Atur Stok", AddressOf AturStok, 120),
            Btn("Tampil / Sembunyi", AddressOf ToggleTampil, 150),
            Btn("Keluarkan dari Katalog", AddressOf HapusKatalog, 180)))

        Dim t3 = AddTab("Customer")
        t3.Controls.Add(gCust)

        Dim t4 = AddTab("Pesanan Customer")
        t4.Controls.Add(gPesan)
        t4.Controls.Add(ButtonBar(
            Btn("Detail", AddressOf DetailPesan, 100),
            Btn("Proses Pesanan", AddressOf ProsesPesan, 140),
            Btn("Batalkan", AddressOf BatalPesan, 100)))

        Dim t5 = AddTab("Riwayat Penjualan")
        t5.Controls.Add(gRiwayat)
        t5.Controls.Add(ButtonBar(
            Btn("Detail", AddressOf DetailRiwayat, 100),
            Btn("Cetak Laporan PDF", AddressOf CetakPdf, 170)))
    End Sub

    Protected Overrides Sub RefreshAll()
        lblKatalog.Text = Scalar("SELECT COUNT(*) FROM Catalog WHERE IsListed=1").ToString()
        lblMenunggu.Text = Scalar("SELECT COUNT(*) FROM Orders WHERE Status='Menunggu'").ToString()
        lblDiproses.Text = Scalar("SELECT COUNT(*) FROM Orders WHERE Status='Diproses'").ToString()
        lblOmzet.Text = Rp(Scalar("SELECT ISNULL(SUM(Total),0) FROM Orders WHERE Status='Selesai'"))
        lblCust.Text = Scalar("SELECT COUNT(*) FROM Users WHERE Role='Customer' AND IsActive=1").ToString()

        gProd.DataSource = Query("SELECT p.ProductId, p.Code AS Kode, p.Name AS Nama, p.Category AS Kategori, p.Stock AS StokProduk, " &
                                 "CASE WHEN k.CatalogId IS NULL THEN 'Belum' ELSE 'Sudah' END AS DiKatalog " &
                                 "FROM Products p LEFT JOIN Catalog k ON k.ProductId=p.ProductId ORDER BY p.Name")
        HideCols(gProd, "ProductId")

        gKat.DataSource = Query("SELECT k.CatalogId, p.Code AS Kode, p.Name AS Nama, k.SellPrice AS HargaJual, k.Stock AS StokKatalog, p.Stock AS StokProduk, " &
                                "CASE WHEN k.IsListed=1 THEN 'Tampil' ELSE 'Disembunyikan' END AS Status " &
                                "FROM Catalog k JOIN Products p ON p.ProductId=k.ProductId ORDER BY p.Name")
        HideCols(gKat, "CatalogId") : ColFmt(gKat, "HargaJual", "N0")

        gCust.DataSource = Query("SELECT u.UserId, u.Username, u.FullName AS Nama, u.Phone AS Telepon, u.Address AS Alamat, " &
                                 "(SELECT COUNT(*) FROM Orders o WHERE o.CustomerId=u.UserId) AS JumlahPesanan " &
                                 "FROM Users u WHERE u.Role='Customer' AND u.IsActive=1 ORDER BY u.FullName")
        HideCols(gCust, "UserId")

        gPesan.DataSource = Query("SELECT " & OrdCols & "FROM Orders o JOIN Users u ON u.UserId=o.CustomerId " &
                                  "WHERE o.Status IN ('Menunggu','Diproses') ORDER BY o.OrderDate")
        HideCols(gPesan, "OrderId") : ColFmt(gPesan, "Total", "N0")

        gRiwayat.DataSource = Query("SELECT " & OrdCols & "FROM Orders o JOIN Users u ON u.UserId=o.CustomerId " &
                                    "WHERE o.Status IN ('Selesai','Dibatalkan') ORDER BY o.OrderDate DESC")
        HideCols(gRiwayat, "OrderId") : ColFmt(gRiwayat, "Total", "N0")
    End Sub

    ' ================= Katalog =================
    Private Sub TambahKatalog(s As Object, e As EventArgs)
        Dim pid = SelId(gProd, "ProductId") : If pid = 0 Then Return
        If Convert.ToInt32(Scalar("SELECT COUNT(*) FROM Catalog WHERE ProductId=@p", P("@p", pid))) > 0 Then
            Warn("Produk ini sudah ada di katalog.") : Return
        End If
        Dim stokProduk = Convert.ToInt32(Scalar("SELECT Stock FROM Products WHERE ProductId=@p", P("@p", pid)))
        Dim v = Prompt("Tambah ke Katalog", {"Harga jual (Rp)", "Stok katalog (maks " & stokProduk & ")"}, {"", "0"})
        If v Is Nothing Then Return
        Dim harga As Decimal, stok As Integer
        If Not Decimal.TryParse(v(0), harga) OrElse harga <= 0 Then
            Warn("Harga jual harus berupa angka lebih dari 0.") : Return
        End If
        If Not Integer.TryParse(v(1), stok) OrElse stok < 0 OrElse stok > stokProduk Then
            Warn("Stok katalog harus antara 0 dan " & stokProduk & ".") : Return
        End If
        Exec("INSERT INTO Catalog(ProductId,SellPrice,Stock) VALUES(@p,@h,@s)", P("@p", pid), P("@h", harga), P("@s", stok))
        SafeRefresh()
    End Sub

    Private Sub UbahHarga(s As Object, e As EventArgs)
        Dim id = SelId(gKat, "CatalogId") : If id = 0 Then Return
        Dim v = Prompt("Ubah Harga Jual", {"Harga jual (Rp)"}, {Convert.ToDecimal(gKat.CurrentRow.Cells("HargaJual").Value).ToString("0")})
        If v Is Nothing Then Return
        Dim harga As Decimal
        If Not Decimal.TryParse(v(0), harga) OrElse harga <= 0 Then
            Warn("Harga jual harus berupa angka lebih dari 0.") : Return
        End If
        Exec("UPDATE Catalog SET SellPrice=@h WHERE CatalogId=@id", P("@h", harga), P("@id", id))
        SafeRefresh()
    End Sub

    Private Sub AturStok(s As Object, e As EventArgs)
        Dim id = SelId(gKat, "CatalogId") : If id = 0 Then Return
        Dim maks = Convert.ToInt32(gKat.CurrentRow.Cells("StokProduk").Value)
        Dim v = Prompt("Atur Stok Katalog", {"Stok baru (maks " & maks & ")"}, {gKat.CurrentRow.Cells("StokKatalog").Value.ToString()})
        If v Is Nothing Then Return
        Dim stok As Integer
        If Not Integer.TryParse(v(0), stok) OrElse stok < 0 OrElse stok > maks Then
            Warn("Stok harus antara 0 dan stok produk yang tersedia (" & maks & ").") : Return
        End If
        Exec("UPDATE Catalog SET Stock=@s WHERE CatalogId=@id", P("@s", stok), P("@id", id))
        SafeRefresh()
    End Sub

    Private Sub ToggleTampil(s As Object, e As EventArgs)
        Dim id = SelId(gKat, "CatalogId") : If id = 0 Then Return
        Exec("UPDATE Catalog SET IsListed = 1 - IsListed WHERE CatalogId=@id", P("@id", id))
        SafeRefresh()
    End Sub

    Private Sub HapusKatalog(s As Object, e As EventArgs)
        Dim id = SelId(gKat, "CatalogId") : If id = 0 Then Return
        If Not Ask("Keluarkan produk ini dari katalog?") Then Return
        Try
            Exec("DELETE FROM Cart WHERE CatalogId=@id", P("@id", id))
            Exec("DELETE FROM Catalog WHERE CatalogId=@id", P("@id", id))
        Catch ex As SqlException
            Warn("Produk sudah pernah dipesan sehingga tidak bisa dihapus dari katalog." & vbCrLf & "Gunakan Tampil / Sembunyi.")
        End Try
        SafeRefresh()
    End Sub

    ' ================= Pesanan =================
    Private Sub DetailPesan(s As Object, e As EventArgs)
        Dim id = SelId(gPesan, "OrderId") : If id = 0 Then Return
        ShowOrderDetail(id)
    End Sub

    Private Sub DetailRiwayat(s As Object, e As EventArgs)
        Dim id = SelId(gRiwayat, "OrderId") : If id = 0 Then Return
        ShowOrderDetail(id)
    End Sub

    Private Sub ProsesPesan(s As Object, e As EventArgs)
        Dim id = SelId(gPesan, "OrderId") : If id = 0 Then Return
        Dim st = SelStr(gPesan, "Status")
        Dim nxt = If(st = "Menunggu", "Diproses", "Selesai")
        If Not Ask("Ubah status pesanan dari '" & st & "' menjadi '" & nxt & "'?") Then Return
        Exec("UPDATE Orders SET Status=@n, ProcessedBy=@u WHERE OrderId=@o AND Status=@old",
             P("@n", nxt), P("@u", Session.UserId), P("@o", id), P("@old", st))
        SafeRefresh()
    End Sub

    Private Sub BatalPesan(s As Object, e As EventArgs)
        Dim id = SelId(gPesan, "OrderId") : If id = 0 Then Return
        If Not Ask("Batalkan pesanan ini? Stok akan dikembalikan.") Then Return
        If CancelOrder(id) Then SafeRefresh()
    End Sub

    Private Sub CetakPdf(s As Object, e As EventArgs)
        CetakLaporan()
    End Sub
End Class
