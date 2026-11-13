Imports System.Data
Imports Microsoft.Data.SqlClient
Imports System.Windows.Forms

Public Class SupplierForm
    Inherits RoleForm

    Private lblKomp, lblStokKomp, lblProduk, lblStokProduk, lblMenipis As Label
    Private gKomp As DataGridView = NewGrid()
    Private gProduk As DataGridView = NewGrid()
    Private gStokK As DataGridView = NewGrid()
    Private gStokP As DataGridView = NewGrid()

    Public Sub New()
        MyBase.New("Supplier")

        ' ---- Dashboard ----
        Dim fp = AddDashboard()
        lblKomp = AddCard(fp, "Komponen Aktif")
        lblStokKomp = AddCard(fp, "Total Stok Komponen")
        lblMenipis = AddCard(fp, "Komponen Stok Menipis (<20)")
        lblProduk = AddCard(fp, "Jenis Produk Dibuat")
        lblStokProduk = AddCard(fp, "Total Stok Produk")

        ' ---- Data Komponen ----
        Dim t1 = AddTab("Data Komponen")
        t1.Controls.Add(gKomp)
        t1.Controls.Add(ButtonBar(
            Btn("Tambah Komponen", AddressOf KompTambah, 150),
            Btn("Edit", AddressOf KompEdit, 100),
            Btn("Aktif / Nonaktif", AddressOf KompToggle, 150),
            Btn("Hapus", AddressOf KompHapus, 100)))

        ' ---- Produk ----
        Dim t2 = AddTab("Produk")
        t2.Controls.Add(gProduk)
        t2.Controls.Add(ButtonBar(
            Btn("Buat Produk Baru", AddressOf ProdBaru, 150),
            Btn("Produksi Tambahan", AddressOf ProdTambah, 150),
            Btn("Lihat Komposisi", AddressOf ProdKomposisi, 150)))

        ' ---- Stok ----
        Dim t3 = AddTab("Stok Komponen & Produk")
        Dim tl As New TableLayoutPanel With {.Dock = DockStyle.Fill, .ColumnCount = 1, .RowCount = 2}
        tl.RowStyles.Add(New RowStyle(SizeType.Percent, 50))
        tl.RowStyles.Add(New RowStyle(SizeType.Percent, 50))
        tl.Controls.Add(Titled("Stok Komponen", gStokK), 0, 0)
        tl.Controls.Add(Titled("Stok Produk", gStokP), 0, 1)
        t3.Controls.Add(tl)
    End Sub

    Protected Overrides Sub RefreshAll()
        lblKomp.Text = Scalar("SELECT COUNT(*) FROM Components WHERE IsActive=1").ToString()
        lblStokKomp.Text = Scalar("SELECT ISNULL(SUM(Stock),0) FROM Components").ToString()
        lblMenipis.Text = Scalar("SELECT COUNT(*) FROM Components WHERE IsActive=1 AND Stock<20").ToString()
        lblProduk.Text = Scalar("SELECT COUNT(*) FROM Products").ToString()
        lblStokProduk.Text = Scalar("SELECT ISNULL(SUM(Stock),0) FROM Products").ToString()

        gKomp.DataSource = Query("SELECT ComponentId, Code AS Kode, Name AS Nama, Category AS Kategori, Unit AS Satuan, Stock AS Stok, Price AS Harga, " &
                                 "CASE WHEN IsActive=1 THEN 'Aktif' ELSE 'Nonaktif' END AS Status FROM Components ORDER BY Name")
        HideCols(gKomp, "ComponentId") : ColFmt(gKomp, "Harga", "N0")

        gProduk.DataSource = Query("SELECT p.ProductId, p.Code AS Kode, p.Name AS Nama, p.Category AS Kategori, p.Stock AS Stok, p.CreatedAt AS Dibuat, " &
                                   "(SELECT STRING_AGG(c.Name + ' x' + CAST(pc.Qty AS VARCHAR(10)), ', ') FROM ProductComponents pc " &
                                   "JOIN Components c ON c.ComponentId=pc.ComponentId WHERE pc.ProductId=p.ProductId) AS Komponen " &
                                   "FROM Products p ORDER BY p.Name")
        HideCols(gProduk, "ProductId")

        gStokK.DataSource = Query("SELECT Code AS Kode, Name AS Nama, Stock AS Stok, Unit AS Satuan, " &
                                  "CASE WHEN IsActive=0 THEN 'Nonaktif' WHEN Stock<20 THEN 'Menipis' ELSE 'Aman' END AS Kondisi FROM Components ORDER BY Stock")
        gStokP.DataSource = Query("SELECT Code AS Kode, Name AS Nama, Category AS Kategori, Stock AS Stok FROM Products ORDER BY Name")
    End Sub

    ' ================= Komponen =================
    Private Sub KompTambah(s As Object, e As EventArgs)
        Dim v = Prompt("Tambah Komponen", {"Kode", "Nama", "Kategori", "Satuan", "Stok", "Harga"}, {"", "", "", "pcs", "0", "0"})
        If v Is Nothing Then Return
        SaveComp(0, v)
    End Sub

    Private Sub KompEdit(s As Object, e As EventArgs)
        Dim id = SelId(gKomp, "ComponentId") : If id = 0 Then Return
        Dim c = gKomp.CurrentRow.Cells
        Dim v = Prompt("Edit Komponen", {"Kode", "Nama", "Kategori", "Satuan", "Stok", "Harga"},
                       {c("Kode").Value.ToString(), c("Nama").Value.ToString(), c("Kategori").Value.ToString(),
                        c("Satuan").Value.ToString(), c("Stok").Value.ToString(), Convert.ToDecimal(c("Harga").Value).ToString("0")})
        If v Is Nothing Then Return
        SaveComp(id, v)
    End Sub

    Private Sub SaveComp(id As Integer, v() As String)
        Dim stok As Integer, harga As Decimal
        If v(0) = "" OrElse v(1) = "" Then
            Warn("Kode dan nama komponen wajib diisi.") : Return
        End If
        If Not Integer.TryParse(v(4), stok) OrElse Not Decimal.TryParse(v(5), harga) OrElse stok < 0 OrElse harga < 0 Then
            Warn("Stok dan harga harus berupa angka (tidak boleh negatif).") : Return
        End If
        Try
            If id = 0 Then
                Exec("INSERT INTO Components(Code,Name,Category,Unit,Stock,Price) VALUES(@c,@n,@k,@u,@s,@h)",
                     P("@c", v(0)), P("@n", v(1)), P("@k", v(2)), P("@u", If(v(3) = "", "pcs", v(3))), P("@s", stok), P("@h", harga))
            Else
                Exec("UPDATE Components SET Code=@c,Name=@n,Category=@k,Unit=@u,Stock=@s,Price=@h WHERE ComponentId=@id",
                     P("@c", v(0)), P("@n", v(1)), P("@k", v(2)), P("@u", If(v(3) = "", "pcs", v(3))), P("@s", stok), P("@h", harga), P("@id", id))
            End If
        Catch ex As SqlException
            Warn("Gagal menyimpan (kode mungkin sudah dipakai)." & vbCrLf & ex.Message)
        End Try
        SafeRefresh()
    End Sub

    Private Sub KompToggle(s As Object, e As EventArgs)
        Dim id = SelId(gKomp, "ComponentId") : If id = 0 Then Return
        Exec("UPDATE Components SET IsActive = 1 - IsActive WHERE ComponentId=@id", P("@id", id))
        SafeRefresh()
    End Sub

    Private Sub KompHapus(s As Object, e As EventArgs)
        Dim id = SelId(gKomp, "ComponentId") : If id = 0 Then Return
        If Convert.ToInt32(Scalar("SELECT COUNT(*) FROM ProductComponents WHERE ComponentId=@id", P("@id", id))) > 0 Then
            Warn("Komponen ini dipakai oleh produk, sehingga tidak bisa dihapus." & vbCrLf & "Gunakan tombol Aktif / Nonaktif.")
            Return
        End If
        If Not Ask("Hapus komponen ini secara permanen?") Then Return
        Exec("DELETE FROM Components WHERE ComponentId=@id", P("@id", id))
        SafeRefresh()
    End Sub

    ' ================= Produk =================
    Private Sub ProdBaru(s As Object, e As EventArgs)
        Using f As New ProductBuilder()
            If f.ShowDialog(Me) = DialogResult.OK Then SafeRefresh()
        End Using
    End Sub

    Private Sub ProdTambah(s As Object, e As EventArgs)
        Dim id = SelId(gProduk, "ProductId") : If id = 0 Then Return
        Dim v = Prompt("Produksi Tambahan", {"Jumlah unit"}, {"1"})
        If v Is Nothing Then Return
        Dim qty As Integer
        If Not Integer.TryParse(v(0), qty) OrElse qty <= 0 Then
            Warn("Jumlah harus berupa angka lebih dari 0.") : Return
        End If
        Dim lines = Query("SELECT pc.ComponentId, pc.Qty, c.Name FROM ProductComponents pc JOIN Components c ON c.ComponentId=pc.ComponentId WHERE pc.ProductId=@p", P("@p", id))
        Try
            InTx(Sub(cn, tx)
                     For Each r As DataRow In lines.Rows
                         Dim n = TxExec(cn, tx, "UPDATE Components SET Stock=Stock-@x WHERE ComponentId=@c AND Stock>=@x",
                                        P("@x", qty * Convert.ToInt32(r("Qty"))), P("@c", r("ComponentId")))
                         If n = 0 Then Throw New InvalidOperationException("Stok komponen '" & r("Name").ToString() & "' tidak cukup.")
                     Next
                     TxExec(cn, tx, "UPDATE Products SET Stock=Stock+@q WHERE ProductId=@p", P("@q", qty), P("@p", id))
                 End Sub)
            Info(qty & " unit produk berhasil diproduksi.")
        Catch ex As InvalidOperationException
            Warn(ex.Message)
        End Try
        SafeRefresh()
    End Sub

    Private Sub ProdKomposisi(s As Object, e As EventArgs)
        Dim id = SelId(gProduk, "ProductId") : If id = 0 Then Return
        Dim dt = Query("SELECT c.Name, pc.Qty FROM ProductComponents pc JOIN Components c ON c.ComponentId=pc.ComponentId WHERE pc.ProductId=@p ORDER BY c.Name", P("@p", id))
        Dim sb As New System.Text.StringBuilder("Komponen per 1 unit " & SelStr(gProduk, "Nama") & ":" & vbCrLf & vbCrLf)
        For Each r As DataRow In dt.Rows
            sb.AppendLine("- " & r("Name").ToString() & "  x" & r("Qty").ToString())
        Next
        Info(sb.ToString())
    End Sub
End Class
