Imports System.Drawing
Imports System.Windows.Forms

Public Module UI
    Public ReadOnly LgRed As Color = Color.FromArgb(165, 0, 52)

    Public Function NewGrid() As DataGridView
        Return New DataGridView With {
            .Dock = DockStyle.Fill,
            .ReadOnly = True,
            .AllowUserToAddRows = False,
            .AllowUserToDeleteRows = False,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .MultiSelect = False,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            .RowHeadersVisible = False,
            .BackgroundColor = Color.White,
            .BorderStyle = BorderStyle.None
        }
    End Function

    Public Function Btn(text As String, handler As EventHandler, Optional w As Integer = 140) As Button
        Dim b As New Button With {
            .Text = text, .Width = w, .Height = 32,
            .BackColor = LgRed, .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat
        }
        AddHandler b.Click, handler
        Return b
    End Function

    Public Function ButtonBar(ParamArray items() As Control) As FlowLayoutPanel
        Dim p As New FlowLayoutPanel With {.Dock = DockStyle.Top, .Height = 46, .Padding = New Padding(6)}
        p.Controls.AddRange(items)
        Return p
    End Function

    ''' <summary>Panel berjudul yang berisi grid.</summary>
    Public Function Titled(title As String, g As DataGridView) As Panel
        Dim p As New Panel With {.Dock = DockStyle.Fill}
        p.Controls.Add(g)
        p.Controls.Add(New Label With {
            .Text = title, .Dock = DockStyle.Top, .Height = 26,
            .Font = New Font("Segoe UI", 10, FontStyle.Bold), .ForeColor = LgRed
        })
        Return p
    End Function

    Public Sub HideCols(g As DataGridView, ParamArray names() As String)
        For Each n In names
            If g.Columns.Contains(n) Then g.Columns(n).Visible = False
        Next
    End Sub

    Public Sub ColFmt(g As DataGridView, col As String, fmt As String)
        If g.Columns.Contains(col) Then g.Columns(col).DefaultCellStyle.Format = fmt
    End Sub

    Public Sub Info(msg As String)
        MessageBox.Show(msg, "Informasi", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Public Sub Warn(msg As String)
        MessageBox.Show(msg, "Perhatian", MessageBoxButtons.OK, MessageBoxIcon.Warning)
    End Sub

    Public Function Ask(msg As String) As Boolean
        Return MessageBox.Show(msg, "Konfirmasi", MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes
    End Function

    ''' <summary>Ambil nilai kolom (angka) dari baris terpilih. Mengembalikan 0 bila tidak ada baris terpilih.</summary>
    Public Function SelId(g As DataGridView, col As String) As Integer
        If g.CurrentRow Is Nothing Then
            Warn("Pilih satu baris data terlebih dahulu.")
            Return 0
        End If
        Return Convert.ToInt32(g.CurrentRow.Cells(col).Value)
    End Function

    Public Function SelStr(g As DataGridView, col As String) As String
        If g.CurrentRow Is Nothing Then Return ""
        Return Convert.ToString(g.CurrentRow.Cells(col).Value)
    End Function

    ''' <summary>Dialog input sederhana. Mengembalikan Nothing bila dibatalkan.</summary>
    Public Function Prompt(title As String, labels() As String, Optional defaults() As String = Nothing) As String()
        Dim h As Integer = 20 + labels.Length * 36 + 60
        Using f As New Form With {
            .Text = title, .FormBorderStyle = FormBorderStyle.FixedDialog,
            .StartPosition = FormStartPosition.CenterParent, .MaximizeBox = False, .MinimizeBox = False,
            .ClientSize = New Size(390, h)
        }
            Dim boxes(labels.Length - 1) As TextBox
            For i As Integer = 0 To labels.Length - 1
                f.Controls.Add(New Label With {.Text = labels(i), .Left = 12, .Top = 24 + i * 36, .Width = 140})
                boxes(i) = New TextBox With {
                    .Left = 155, .Top = 20 + i * 36, .Width = 220,
                    .Text = If(defaults IsNot Nothing, defaults(i), ""),
                    .UseSystemPasswordChar = labels(i).ToLower().Contains("password")
                }
                f.Controls.Add(boxes(i))
            Next
            Dim ok As New Button With {.Text = "OK", .DialogResult = DialogResult.OK, .Left = 200, .Top = h - 45, .Width = 80}
            Dim cancel As New Button With {.Text = "Batal", .DialogResult = DialogResult.Cancel, .Left = 290, .Top = h - 45, .Width = 80}
            f.Controls.Add(ok) : f.Controls.Add(cancel)
            f.AcceptButton = ok : f.CancelButton = cancel
            If f.ShowDialog() <> DialogResult.OK Then Return Nothing
            Return boxes.Select(Function(b) b.Text.Trim()).ToArray()
        End Using
    End Function
End Module
