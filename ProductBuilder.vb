Imports System.Data
Imports Microsoft.Data.SqlClient
Imports System.Drawing
Imports System.Windows.Forms

''' <summary>Dialog untuk membuat produk baru berdasarkan komponen yang tersedia.</summary>
Public Class ProductBuilder
    Inherits Form

    Private txtKode As New TextBox With {.Left = 150, .Top = 15, .Width = 200}
    Private txtNama As New TextBox With {.Left = 150, .Top = 47, .Width = 440}
    Private txtKat As New TextBox With {.Left = 150, .Top = 79, .Width = 200}
    Private txtDesk As New TextBox With {.Left = 150, .Top = 111, .Width = 440}
    Private cmb As New ComboBox With {.Left = 15, .Top = 158, .Width = 300, .DropDownStyle = ComboBoxStyle.DropDownList}
    Private numJml As New NumericUpDown With {.Left = 325, .Top = 158, .Width = 70, .Minimum = 1, .Maximum = 10000, .Value = 1}
    Private numProd As New NumericUpDown With {.Left = 250, .Top = 500, .Width = 90, .Minimum = 0, .Maximum = 100000, .Value = 0}
    Private grid As DataGridView = NewGrid()
    Private bom As New DataTable()

    Public Sub New()
        Text = "Buat Produk Baru"
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath)
        FormBorderStyle = FormBorderStyle.FixedDialog
        StartPosition = FormStartPosition.CenterParent
        MaximizeBox = False : MinimizeBox = False
        ClientSize = New Size(610, 580)

        AddLabel("Kode produk", 18) : AddLabel("Nama produk", 50)
        AddLabel("Kategori", 82) : AddLabel("Deskripsi", 114)
        Controls.AddRange({txtKode, txtNama, txtKat, txtDesk})

        Controls.Add(New Label With {.Text = "Komponen penyusun (per 1 unit produk):", .Left = 15, .Top = 138, .AutoSize = True, .Font = New Font("Segoe UI", 9, FontStyle.Bold)})
        Controls.Add(cmb) : Controls.Add(numJml)
        Dim bAdd = Btn("Tambah", AddressOf AddLine, 90) : bAdd.Left = 405 : bAdd.Top = 155
        Dim bDel = Btn("Hapus Baris", AddressOf DelLine, 90) : bDel.Left = 500 : bDel.Top = 155
        Controls.Add(bAdd) : Controls.Add(bDel)

        bom.Columns.Add("ComponentId", GetType(Integer))
        bom.Columns.Add("Komponen", GetType(String))
        bom.Columns.Add("Jumlah", GetType(Integer))
        grid.DataSource = bom
        HideCols(grid, "ComponentId")
        Dim host As New Panel With {.Left = 15, .Top = 195, .Width = 580, .Height = 290}
        host.Controls.Add(grid)
        Controls.Add(host)

        Controls.Add(New Label With {.Text = "Jumlah unit yang diproduksi sekarang:", .Left = 15, .Top = 503, .AutoSize = True})
        Controls.Add(numProd)
        Controls.Add(New Label With {.Text = "(komponen akan otomatis dikurangi dari stok)", .Left = 350, .Top = 503, .AutoSize = True, .ForeColor = Color.Gray})

        Dim bSave = Btn("Simpan Produk", AddressOf Simpan, 130) : bSave.Left = 350 : bSave.Top = 535
        Dim bCancel As New Button With {.Text = "Batal", .DialogResult = DialogResult.Cancel, .Left = 490, .Top = 535, .Width = 100, .Height = 32}
        Controls.Add(bSave) : Controls.Add(bCancel)
        CancelButton = bCancel

        LoadComponents()
    End Sub

    Private Sub AddLabel(t As String, top As Integer)
        Controls.Add(New Label With {.Text = t, .Left = 15, .Top = top, .Width = 130})
    End Sub

    Private Sub LoadComponents()
        cmb.DataSource = Query("SELECT ComponentId, Name, Name + ' (stok ' + CAST(Stock AS VARCHAR(10)) + ')' AS Label FROM Components WHERE IsActive=1 ORDER BY Name")
        cmb.DisplayMember = "Label"
        cmb.ValueMember = "ComponentId"
    End Sub

    Private Sub AddLine(s As Object, e As EventArgs)
        Dim drv = TryCast(cmb.SelectedItem, DataRowView)
        If drv Is Nothing Then Return
        Dim id As Integer = Convert.ToInt32(drv("ComponentId"))
        Dim jml As Integer = CInt(numJml.Value)
        For Each r As DataRow In bom.Rows
            If Convert.ToInt32(r("ComponentId")) = id Then
                r("Jumlah") = Convert.ToInt32(r("Jumlah")) + jml
                Return
            End If
        Next
        bom.Rows.Add(id, drv("Name").ToString(), jml)
    End Sub

    Private Sub DelLine(s As Object, e As EventArgs)
        If grid.CurrentRow Is Nothing Then Return
        bom.Rows.RemoveAt(grid.CurrentRow.Index)
    End Sub

    Private Sub Simpan(s As Object, e As EventArgs)
        If txtKode.Text.Trim() = "" OrElse txtNama.Text.Trim() = "" Then
            Warn("Kode dan nama produk wajib diisi.") : Return
        End If
        If bom.Rows.Count = 0 Then
            Warn("Tambahkan minimal satu komponen penyusun produk.") : Return
        End If
        Dim qty As Integer = CInt(numProd.Value)
        Try
            InTx(Sub(cn, tx)
                     Dim pid As Integer = Convert.ToInt32(TxScalar(cn, tx,
                         "INSERT INTO Products(Code,Name,Category,Description,Stock,CreatedBy) OUTPUT INSERTED.ProductId VALUES(@c,@n,@k,@d,@s,@u)",
                         P("@c", txtKode.Text.Trim()), P("@n", txtNama.Text.Trim()), P("@k", txtKat.Text.Trim()),
                         P("@d", txtDesk.Text.Trim()), P("@s", qty), P("@u", Session.UserId)))
                     For Each r As DataRow In bom.Rows
                         TxExec(cn, tx, "INSERT INTO ProductComponents(ProductId,ComponentId,Qty) VALUES(@p,@c,@q)",
                                P("@p", pid), P("@c", r("ComponentId")), P("@q", r("Jumlah")))
                         If qty > 0 Then
                             Dim n = TxExec(cn, tx, "UPDATE Components SET Stock=Stock-@x WHERE ComponentId=@c AND Stock>=@x",
                                            P("@x", qty * Convert.ToInt32(r("Jumlah"))), P("@c", r("ComponentId")))
                             If n = 0 Then
                                 Throw New InvalidOperationException("Stok komponen '" & r("Komponen").ToString() &
                                     "' tidak cukup untuk memproduksi " & qty & " unit.")
                             End If
                         End If
                     Next
                 End Sub)
            DialogResult = DialogResult.OK
            Close()
        Catch ex As InvalidOperationException
            Warn(ex.Message)
        Catch ex As SqlException
            Warn("Gagal menyimpan produk (kode mungkin sudah dipakai)." & vbCrLf & ex.Message)
        End Try
    End Sub
End Class
