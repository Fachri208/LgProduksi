Imports System.Data
Imports System.IO
Imports System.Windows.Forms
Imports QuestPDF.Fluent
Imports QuestPDF.Helpers
Imports QuestPDF.Infrastructure

''' <summary>Laporan Penjualan PDF (hanya pesanan berstatus Selesai).</summary>
Public Module SalesReport

    Private Const MerahLg As String = "#A50034"
    Private ReadOnly Cult As Globalization.CultureInfo = Globalization.CultureInfo.GetCultureInfo("id-ID")

    Public Sub CetakLaporan()
        Dim dari As Date, sampai As Date
        If Not PilihPeriode(dari, sampai) Then Return

        Dim pA = P("@a", dari)
        Dim pB = P("@b", sampai.AddDays(1))
        Dim orders As DataTable = Query(
            "SELECT o.OrderId, o.OrderDate, u.FullName, o.Total FROM Orders o JOIN Users u ON u.UserId=o.CustomerId " &
            "WHERE o.Status='Selesai' AND o.OrderDate >= @a AND o.OrderDate < @b ORDER BY o.OrderDate", pA, pB)
        If orders.Rows.Count = 0 Then
            Info("Tidak ada penjualan berstatus Selesai pada periode tersebut.") : Return
        End If

        Dim terlaris As DataTable = Query(
            "SELECT TOP 5 p.Name, SUM(i.Qty) AS Unit, SUM(i.Qty * i.Price) AS Omzet " &
            "FROM OrderItems i JOIN Orders o ON o.OrderId=i.OrderId " &
            "JOIN Catalog k ON k.CatalogId=i.CatalogId JOIN Products p ON p.ProductId=k.ProductId " &
            "WHERE o.Status='Selesai' AND o.OrderDate >= @a AND o.OrderDate < @b " &
            "GROUP BY p.Name ORDER BY SUM(i.Qty) DESC", P("@a", dari), P("@b", sampai.AddDays(1)))

        Dim total As Decimal = 0
        For Each r As DataRow In orders.Rows
            total += Convert.ToDecimal(r("Total"))
        Next
        Dim jumlah As Integer = orders.Rows.Count
        Dim rata As Decimal = total / jumlah

        Using sfd As New SaveFileDialog With {
            .Filter = "File PDF (*.pdf)|*.pdf",
            .FileName = "Laporan_Penjualan_" & Date.Now.ToString("yyyyMMdd_HHmm") & ".pdf"
        }
            If sfd.ShowDialog() <> DialogResult.OK Then Return
            Try
                QuestPDF.Settings.License = LicenseType.Community
                Dim logo As Byte() = GetLogo()
                Dim periode As String = dari.ToString("dd MMMM yyyy", Cult) & " s.d. " & sampai.ToString("dd MMMM yyyy", Cult)
                Dim dicetak As String = "Dicetak " & Date.Now.ToString("dd/MM/yyyy HH:mm") & " oleh " & Session.FullName

                Dim doc = Document.Create(
                    Sub(container)
                        container.Page(
                            Sub(page)
                                page.Size(PageSizes.A4)
                                page.Margin(30)
                                page.DefaultTextStyle(Function(x) x.FontSize(10))

                                ' ---------- Kop ----------
                                page.Header().Column(
                                    Sub(h)
                                        h.Item().Row(
                                            Sub(row)
                                                If logo IsNot Nothing Then row.ConstantItem(44).Image(logo)
                                                row.RelativeItem().PaddingLeft(10).Column(
                                                    Sub(c)
                                                        c.Item().Text("LG Electronics Indonesia").FontSize(16).Bold().FontColor(MerahLg)
                                                        c.Item().Text("Laporan Penjualan").FontSize(12).SemiBold()
                                                        c.Item().Text("Periode: " & periode)
                                                        c.Item().Text(dicetak).FontSize(8).FontColor("#7F7F7F")
                                                    End Sub)
                                            End Sub)
                                        h.Item().PaddingTop(6).LineHorizontal(1).LineColor(MerahLg)
                                    End Sub)

                                ' ---------- Isi ----------
                                page.Content().PaddingVertical(10).Column(
                                    Sub(col)
                                        col.Spacing(12)

                                        ' Ringkasan
                                        col.Item().Text("Ringkasan").FontSize(12).Bold().FontColor(MerahLg)
                                        col.Item().Table(
                                            Sub(t)
                                                t.ColumnsDefinition(
                                                    Sub(cd)
                                                        cd.ConstantColumn(200)
                                                        cd.RelativeColumn()
                                                    End Sub)
                                                BarisRingkas(t, "Jumlah pesanan selesai", jumlah.ToString())
                                                BarisRingkas(t, "Total penjualan", Rp(total))
                                                BarisRingkas(t, "Rata-rata per pesanan", Rp(rata))
                                            End Sub)

                                        ' Tabel pesanan
                                        col.Item().Text("Daftar Pesanan Selesai").FontSize(12).Bold().FontColor(MerahLg)
                                        col.Item().Table(
                                            Sub(t)
                                                t.ColumnsDefinition(
                                                    Sub(cd)
                                                        cd.ConstantColumn(28)
                                                        cd.ConstantColumn(95)
                                                        cd.ConstantColumn(80)
                                                        cd.RelativeColumn()
                                                        cd.ConstantColumn(105)
                                                    End Sub)
                                                t.Header(
                                                    Sub(h)
                                                        HeadCell(h.Cell()).Text("No").Bold().FontColor("#FFFFFF")
                                                        HeadCell(h.Cell()).Text("Tanggal").Bold().FontColor("#FFFFFF")
                                                        HeadCell(h.Cell()).Text("No. Pesanan").Bold().FontColor("#FFFFFF")
                                                        HeadCell(h.Cell()).Text("Customer").Bold().FontColor("#FFFFFF")
                                                        HeadCell(h.Cell()).AlignRight().Text("Total").Bold().FontColor("#FFFFFF")
                                                    End Sub)
                                                Dim no As Integer = 0
                                                For Each r As DataRow In orders.Rows
                                                    no += 1
                                                    BodyCell(t.Cell()).Text(no.ToString())
                                                    BodyCell(t.Cell()).Text(Convert.ToDateTime(r("OrderDate")).ToString("dd/MM/yyyy HH:mm"))
                                                    BodyCell(t.Cell()).Text("ORD-" & Convert.ToInt32(r("OrderId")).ToString("00000"))
                                                    BodyCell(t.Cell()).Text(r("FullName").ToString())
                                                    BodyCell(t.Cell()).AlignRight().Text(Rp(r("Total")))
                                                Next
                                            End Sub)

                                        ' Produk terlaris
                                        If terlaris.Rows.Count > 0 Then
                                            col.Item().Text("Produk Terlaris").FontSize(12).Bold().FontColor(MerahLg)
                                            col.Item().Table(
                                                Sub(t)
                                                    t.ColumnsDefinition(
                                                        Sub(cd)
                                                            cd.RelativeColumn()
                                                            cd.ConstantColumn(80)
                                                            cd.ConstantColumn(120)
                                                        End Sub)
                                                    t.Header(
                                                        Sub(h)
                                                            HeadCell(h.Cell()).Text("Produk").Bold().FontColor("#FFFFFF")
                                                            HeadCell(h.Cell()).AlignRight().Text("Unit Terjual").Bold().FontColor("#FFFFFF")
                                                            HeadCell(h.Cell()).AlignRight().Text("Omzet").Bold().FontColor("#FFFFFF")
                                                        End Sub)
                                                    For Each r As DataRow In terlaris.Rows
                                                        BodyCell(t.Cell()).Text(r("Name").ToString())
                                                        BodyCell(t.Cell()).AlignRight().Text(r("Unit").ToString())
                                                        BodyCell(t.Cell()).AlignRight().Text(Rp(r("Omzet")))
                                                    Next
                                                End Sub)
                                        End If
                                    End Sub)

                                ' ---------- Footer ----------
                                page.Footer().AlignCenter().Text(
                                    Sub(t)
                                        t.Span("Halaman ")
                                        t.CurrentPageNumber()
                                        t.Span(" dari ")
                                        t.TotalPages()
                                    End Sub)
                            End Sub)
                    End Sub)

                doc.GeneratePdf(sfd.FileName)
                Process.Start(New ProcessStartInfo(sfd.FileName) With {.UseShellExecute = True})
            Catch ex As Exception
                Warn("Gagal membuat PDF:" & vbCrLf & ex.Message)
            End Try
        End Using
    End Sub

    ' ---------- Helper tampilan ----------
    Private Function HeadCell(c As IContainer) As IContainer
        Return c.Background(MerahLg).Padding(5)
    End Function

    Private Function BodyCell(c As IContainer) As IContainer
        Return c.BorderBottom(0.5F).BorderColor("#BFBFBF").PaddingVertical(4).PaddingHorizontal(5)
    End Function

    Private Sub BarisRingkas(t As TableDescriptor, label As String, nilai As String)
        t.Cell().PaddingVertical(2).Text(label)
        t.Cell().PaddingVertical(2).Text(nilai).SemiBold()
    End Sub

    ' ---------- Logo dari logo_LG.ico ----------
    Private Function GetLogo() As Byte()
        Try
            Dim p As String = Path.Combine(Application.StartupPath, "logo_LG.ico")
            Dim ico As System.Drawing.Icon
            If File.Exists(p) Then
                ico = New System.Drawing.Icon(p, 128, 128)
            Else
                ico = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath)
            End If
            Using bmp = ico.ToBitmap()
                Using ms As New MemoryStream()
                    bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png)
                    Return ms.ToArray()
                End Using
            End Using
        Catch
            Return Nothing
        End Try
    End Function

    ' ---------- Dialog pilih periode ----------
    Private Function PilihPeriode(ByRef dari As Date, ByRef sampai As Date) As Boolean
        Using f As New Form With {
            .Text = "Periode Laporan", .FormBorderStyle = FormBorderStyle.FixedDialog,
            .StartPosition = FormStartPosition.CenterParent, .MaximizeBox = False, .MinimizeBox = False,
            .ClientSize = New System.Drawing.Size(340, 150)
        }
            Dim d1 As New DateTimePicker With {
                .Left = 130, .Top = 20, .Width = 190, .Format = DateTimePickerFormat.Short,
                .Value = New Date(Date.Today.Year, Date.Today.Month, 1)}
            Dim d2 As New DateTimePicker With {
                .Left = 130, .Top = 60, .Width = 190, .Format = DateTimePickerFormat.Short, .Value = Date.Today}
            f.Controls.Add(New Label With {.Text = "Dari tanggal", .Left = 15, .Top = 24, .Width = 110})
            f.Controls.Add(New Label With {.Text = "Sampai tanggal", .Left = 15, .Top = 64, .Width = 110})
            f.Controls.Add(d1) : f.Controls.Add(d2)
            Dim ok As New Button With {.Text = "Buat PDF", .DialogResult = DialogResult.OK, .Left = 140, .Top = 105, .Width = 90}
            Dim batal As New Button With {.Text = "Batal", .DialogResult = DialogResult.Cancel, .Left = 240, .Top = 105, .Width = 80}
            f.Controls.Add(ok) : f.Controls.Add(batal)
            f.AcceptButton = ok : f.CancelButton = batal

            If f.ShowDialog() <> DialogResult.OK Then Return False
            If d2.Value.Date < d1.Value.Date Then
                Warn("Tanggal akhir tidak boleh lebih awal dari tanggal awal.") : Return False
            End If
            dari = d1.Value.Date
            sampai = d2.Value.Date
            Return True
        End Using
    End Function
End Module
