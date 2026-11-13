Imports System.Drawing
Imports System.Windows.Forms

''' <summary>Kerangka form utama: header, tombol logout, dan TabControl.</summary>
Public MustInherit Class RoleForm
    Inherits Form

    Protected ReadOnly Tabs As New TabControl With {.Dock = DockStyle.Fill}

    Protected Sub New(title As String)
        Text = title & " - PT LG Electronics Indonesia"
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath)
        Size = New Size(1150, 700)
        StartPosition = FormStartPosition.CenterScreen

        Dim head As New Panel With {.Dock = DockStyle.Top, .Height = 54, .BackColor = LgRed}
        head.Controls.Add(New Label With {
            .Text = "LG Electronics Indonesia  |  " & title, .ForeColor = Color.White, .AutoSize = True,
            .Font = New Font("Segoe UI", 13, FontStyle.Bold), .Left = 14, .Top = 14})

        Dim rp As New FlowLayoutPanel With {
            .Dock = DockStyle.Right, .Width = 380, .FlowDirection = FlowDirection.RightToLeft,
            .Padding = New Padding(0, 10, 10, 0)}
        Dim lo As New Button With {.Text = "Logout", .Width = 80, .Height = 30, .BackColor = Color.White, .FlatStyle = FlatStyle.Flat}
        AddHandler lo.Click, Sub(s, e)
                                 Session.LoggedOut = True
                                 Close()
                             End Sub
        rp.Controls.Add(lo)
        rp.Controls.Add(New Label With {
            .Text = Session.FullName & " (" & Session.Role & ")", .ForeColor = Color.White,
            .AutoSize = True, .Margin = New Padding(0, 7, 12, 0)})
        head.Controls.Add(rp)

        Controls.Add(Tabs)
        Controls.Add(head)
        AddHandler Tabs.SelectedIndexChanged, Sub(s, e) RefreshAll()
    End Sub

    Protected MustOverride Sub RefreshAll()

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        SafeRefresh()
    End Sub

    Protected Sub SafeRefresh()
        Try
            RefreshAll()
        Catch ex As Exception
            Warn("Gagal memuat data: " & ex.Message)
        End Try
    End Sub

    Protected Function AddTab(name As String) As TabPage
        Dim t As New TabPage(name) With {.BackColor = Color.White}
        Tabs.TabPages.Add(t)
        Return t
    End Function

    ''' <summary>Buat tab dashboard berisi panel kartu.</summary>
    Protected Function AddDashboard() As FlowLayoutPanel
        Dim t = AddTab("Dashboard")
        Dim fp As New FlowLayoutPanel With {.Dock = DockStyle.Fill, .Padding = New Padding(20), .BackColor = Color.WhiteSmoke}
        t.Controls.Add(fp)
        Return fp
    End Function

    Protected Function AddCard(host As FlowLayoutPanel, title As String) As Label
        Dim p As New Panel With {.Width = 240, .Height = 110, .BackColor = Color.White, .BorderStyle = BorderStyle.FixedSingle, .Margin = New Padding(10)}
        p.Controls.Add(New Label With {.Text = title, .Left = 12, .Top = 12, .AutoSize = True, .ForeColor = Color.Gray})
        Dim v As New Label With {.Text = "0", .Left = 12, .Top = 45, .AutoSize = True, .Font = New Font("Segoe UI", 18, FontStyle.Bold), .ForeColor = LgRed}
        p.Controls.Add(v)
        host.Controls.Add(p)
        Return v
    End Function
End Class
