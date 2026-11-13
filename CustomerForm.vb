Imports System.Data
Imports System.Drawing
Imports System.Windows.Forms

Public Class CustomerForm
    Inherits RoleForm

    Private lblCart, lblAktif, lblTotalBeli As Label
    Private lblTotal As New Label With {.AutoSize = True, .Margin = New Padding(20, 8, 0, 0), .Font = New Font("Segoe UI", 10, FontStyle.Bold)}
    Private txtCari As New TextBox With {.Width = 220, .Margin = New Padding(3, 5, 3, 3)}
    Private gKat As DataGridView = NewGrid()
    Private gCart As DataGridView = NewGrid()
    Private gPesan As DataGridView = NewGrid()
    Private gRiwayat As DataGridView = NewGrid()

    Private Const OrdCols As String =
        "o.OrderId, 'ORD-' + RIGHT('00000' + CAST(o.OrderId AS VARCHAR(10)), 5) AS NoPesanan, " &
        "o.OrderDate AS Tanggal, o.Total, o.Status "

    Public Sub New()
        MyBase.New("Customer")

        Dim fp = AddDashboard()
        lblCart = AddCard(fp, "Item di Keranjang")
        lblAktif = AddCard(fp, "Pesanan Aktif")
        lblTotalBeli = AddCard(fp, "Total Pembelian (Selesai)")

        Dim t1 = AddTab("Katalog Produk")
        t1.Controls.Add(gKat)
        t1.Controls.Add(ButtonBar(
            New Label With {.Text = "Cari:", .AutoSize = True, .Margin = New Padding(3, 9, 0, 0)},
            txtCari,
            Btn("Cari", AddressOf Cari, 70),
            Btn("Detail Produk", AddressOf DetailProduk, 120),
            Btn("Tambah ke Keranjang", AddressOf KeKeranjang, 170)))
        AddHandler txtCari.KeyDown, Sub(s, e)
                                        If e.KeyCode = Keys.Enter Then SafeRefresh()
                                    End Sub

        Dim t2 = AddTab("Keranjang")
        t2.Controls.Add(gCart)
        t2.Controls.Add(ButtonBar(
            Btn("Ubah Jumlah", AddressOf UbahJumlah, 120),
            Btn("Hapus dari Keranjang", AddressOf HapusCart, 170),
            Btn("Checkout", AddressOf Checkout, 110),
            lblTotal))

        Dim t3 = AddTab("Status Pesanan")
        t3.Controls.Add(gPesan)
        t3.Controls.Add(ButtonBar(
            Btn("Detail", AddressOf DetailPesan, 100),
            Btn("Batalkan Pesanan", AddressOf BatalPesan, 150)))

        Dim t4 = AddTab("Riwayat Pembelian")
        t4.Controls.Add(gRiwayat)
        t4.Controls.Add(ButtonBar(Btn("Detail", AddressOf DetailRiwayat, 100)))
    End Sub

    Protected Overrides Sub RefreshAll()
        lblCart.Text = Scalar("SELECT COUNT(*) FROM Cart WHERE CustomerId=@u", P("@u", Session.UserId)).ToString()
        lblAktif.Text = Scalar("SELECT COUNT(*) FROM Orders WHERE CustomerId=@u AND Status IN ('Menunggu','Diproses')", P("@u", Session.UserId)).ToString()
        lblTotalBeli.Text = Rp(Scalar("SELECT ISNULL(SUM(Total),0) FROM Orders WHERE CustomerId=@u AND Status='Selesai'", P("@u", Session.UserId)))

        gKat.DataSource = Query("SELECT k.CatalogId, p.Code AS Kode, p.Name AS Nama, p.Category AS Kategori, k.SellPrice AS Harga, k.Stock AS Stok " &
                                "FROM Catalog k JOIN Products p ON p.ProductId=k.ProductId " &
                                "WHERE k.IsListed=1 AND (p.Name LIKE @q OR p.Category LIKE @q OR p.Code LIKE @q) ORDER BY p.Name",
                                P("@q", "%" & txtCari.Text.Trim() & "%"))
        HideCols(gKat, "CatalogId") : ColFmt(gKat, "Harga", "N0")

        gCart.DataSource = Query("SELECT c.CartId, p.Name AS Produk, k.SellPrice AS Harga, c.Qty AS Jumlah, k.SellPrice * c.Qty AS Subtotal " &
                                 "FROM Cart c JOIN Catalog k ON k.CatalogId=c.CatalogId JOIN Products p ON p.ProductId=k.ProductId " &
                                 "WHERE c.CustomerId=@u ORDER BY p.Name", P("@u", Session.UserId))
        HideCols(gCart, "CartId") : ColFmt(gCart, "Harga", "N0") : ColFmt(gCart, "Subtotal", "N0")
        lblTotal.Text = "Total: " & Rp(Scalar("SELECT ISNULL(SUM(k.SellPrice*c.Qty),0) FROM Cart c JOIN Catalog k ON k.CatalogId=c.CatalogId WHERE c.CustomerId=@u", P("@u", Session.UserId)))

        gPesan.DataSource = Query("SELECT " & OrdCols & "FROM Orders o WHERE o.CustomerId=@u AND o.Status IN ('Menunggu','Diproses') ORDER BY o.OrderDate DESC", P("@u", Session.UserId))
        HideCols(gPesan, "OrderId") : ColFmt(gPesan, "Total", "N0")

        gRiwayat.DataSource = Query("SELECT " & OrdCols & "FROM Orders o WHERE o.CustomerId=@u AND o.Status='Selesai' ORDER BY o.OrderDate DESC", P("@u", Session.UserId))
        HideCols(gRiwayat, "OrderId") : ColFmt(gRiwayat, "Total", "N0")
    End Sub

    ' ================= Katalog =================
    Private Sub Cari(s As Object, e As EventArgs)
        SafeRefresh()
    End Sub

    Private Sub DetailProduk(s As Object, e As EventArgs)
        Dim id = SelId(gKat, "CatalogId") : If id = 0 Then Return
        Dim h = Query("SELECT p.ProductId, p.Name, p.Category, p.Description, k.SellPrice, k.Stock FROM Catalog k JOIN Products p ON p.ProductId=k.ProductId WHERE k.CatalogId=@k", P("@k", id))
        If h.Rows.Count = 0 Then Return
        Dim r = h.Rows(0)
        Dim comps = Query("SELECT c.Name, pc.Qty FROM ProductComponents pc JOIN Components c ON c.ComponentId=pc.ComponentId WHERE pc.ProductId=@p ORDER BY c.Name", P("@p", r("ProductId")))
        Dim sb As New System.Text.StringBuilder()
        sb.AppendLine(r("Name").ToString())
        sb.AppendLine("Kategori : " & r("Category").ToString())
        sb.AppendLine("Harga    : " & Rp(r("SellPrice")))
        sb.AppendLine("Stok     : " & r("Stock").ToString())
        sb.AppendLine()
        sb.AppendLine(r("Description").ToString())
        sb.AppendLine()
        sb.AppendLine("Komponen utama:")
        For Each c As DataRow In comps.Rows
            sb.AppendLine("- " & c("Name").ToString() & " x" & c("Qty").ToString())
        Next
        Info(sb.ToString())
    End Sub

    Private Sub KeKeranjang(s As Object, e As EventArgs)
        Dim id = SelId(gKat, "CatalogId") : If id = 0 Then Return
        Dim stok = Convert.ToInt32(gKat.CurrentRow.Cells("Stok").Value)
        If stok <= 0 Then
            Warn("Stok produk ini habis.") : Return
        End If
        Dim v = Prompt("Tambah ke Keranjang", {"Jumlah (maks " & stok & ")"}, {"1"})
        If v Is Nothing Then Return
        Dim qty As Integer
        If Not Integer.TryParse(v(0), qty) OrElse qty <= 0 OrElse qty > stok Then
            Warn("Jumlah harus antara 1 dan " & stok & ".") : Return
        End If
        Exec("IF EXISTS (SELECT 1 FROM Cart WHERE CustomerId=@u AND CatalogId=@k) " &
             "UPDATE Cart SET Qty = CASE WHEN Qty + @q > @m THEN @m ELSE Qty + @q END WHERE CustomerId=@u AND CatalogId=@k " &
             "ELSE INSERT INTO Cart(CustomerId,CatalogId,Qty) VALUES(@u,@k,@q)",
             P("@u", Session.UserId), P("@k", id), P("@q", qty), P("@m", stok))
        Info("Produk ditambahkan ke keranjang.")
        SafeRefresh()
    End Sub

    ' ================= Keranjang =================
    Private Sub UbahJumlah(s As Object, e As EventArgs)
        Dim id = SelId(gCart, "CartId") : If id = 0 Then Return
        Dim stok = Convert.ToInt32(Scalar("SELECT k.Stock FROM Cart c JOIN Catalog k ON k.CatalogId=c.CatalogId WHERE c.CartId=@c", P("@c", id)))
        Dim v = Prompt("Ubah Jumlah", {"Jumlah (maks " & stok & ")"}, {gCart.CurrentRow.Cells("Jumlah").Value.ToString()})
        If v Is Nothing Then Return
        Dim qty As Integer
        If Not Integer.TryParse(v(0), qty) OrElse qty <= 0 OrElse qty > stok Then
            Warn("Jumlah harus antara 1 dan " & stok & ".") : Return
        End If
        Exec("UPDATE Cart SET Qty=@q WHERE CartId=@c", P("@q", qty), P("@c", id))
        SafeRefresh()
    End Sub

    Private Sub HapusCart(s As Object, e As EventArgs)
        Dim id = SelId(gCart, "CartId") : If id = 0 Then Return
        Exec("DELETE FROM Cart WHERE CartId=@c", P("@c", id))
        SafeRefresh()
    End Sub

    Private Sub Checkout(s As Object, e As EventArgs)
        Dim cart = Query("SELECT c.CatalogId, c.Qty, k.SellPrice, p.Name FROM Cart c " &
                         "JOIN Catalog k ON k.CatalogId=c.CatalogId JOIN Products p ON p.ProductId=k.ProductId " &
                         "WHERE c.CustomerId=@u", P("@u", Session.UserId))
        If cart.Rows.Count = 0 Then
            Warn("Keranjang masih kosong.") : Return
        End If
        Dim total As Decimal = 0
        For Each r As DataRow In cart.Rows
            total += Convert.ToDecimal(r("SellPrice")) * Convert.ToInt32(r("Qty"))
        Next
        If Not Ask("Checkout " & cart.Rows.Count & " produk dengan total " & Rp(total) & "?") Then Return

        Try
            Dim orderId As Integer
            InTx(Sub(cn, tx)
                     orderId = Convert.ToInt32(TxScalar(cn, tx,
                         "INSERT INTO Orders(CustomerId,Total,Status) OUTPUT INSERTED.OrderId VALUES(@u,@t,'Menunggu')",
                         P("@u", Session.UserId), P("@t", total)))
                     For Each r As DataRow In cart.Rows
                         Dim q As Integer = Convert.ToInt32(r("Qty"))
                         Dim n = TxExec(cn, tx, "UPDATE Catalog SET Stock=Stock-@q WHERE CatalogId=@k AND IsListed=1 AND Stock>=@q",
                                        P("@q", q), P("@k", r("CatalogId")))
                         If n = 0 Then
                             Throw New InvalidOperationException("Stok '" & r("Name").ToString() & "' tidak mencukupi atau produk tidak tersedia.")
                         End If
                         TxExec(cn, tx, "UPDATE Products SET Stock=Stock-@q WHERE ProductId=(SELECT ProductId FROM Catalog WHERE CatalogId=@k)",
                                P("@q", q), P("@k", r("CatalogId")))
                         TxExec(cn, tx, "INSERT INTO OrderItems(OrderId,CatalogId,Qty,Price) VALUES(@o,@k,@q,@p)",
                                P("@o", orderId), P("@k", r("CatalogId")), P("@q", q), P("@p", r("SellPrice")))
                     Next
                     TxExec(cn, tx, "DELETE FROM Cart WHERE CustomerId=@u", P("@u", Session.UserId))
                 End Sub)
            Info("Pesanan berhasil dibuat dengan nomor ORD-" & orderId.ToString("00000") & ".")
        Catch ex As InvalidOperationException
            Warn(ex.Message)
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

    Private Sub BatalPesan(s As Object, e As EventArgs)
        Dim id = SelId(gPesan, "OrderId") : If id = 0 Then Return
        If SelStr(gPesan, "Status") <> "Menunggu" Then
            Warn("Hanya pesanan berstatus 'Menunggu' yang dapat dibatalkan.") : Return
        End If
        If Not Ask("Batalkan pesanan ini?") Then Return
        If CancelOrder(id) Then SafeRefresh()
    End Sub
End Class
