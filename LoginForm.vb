Imports Microsoft.Data.SqlClient
Imports System.Drawing
Imports System.Windows.Forms

Public Class LoginForm
    Inherits Form

    Private txtU As New TextBox With {.Left = 120, .Top = 95, .Width = 210}
    Private txtP As New TextBox With {.Left = 120, .Top = 130, .Width = 210, .UseSystemPasswordChar = True}

    Public Sub New()
        Text = "Login - Sistem Produksi & Penjualan"
        Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath)
        FormBorderStyle = FormBorderStyle.FixedDialog
        StartPosition = FormStartPosition.CenterScreen
        MaximizeBox = False
        ClientSize = New Size(360, 260)

        Dim head As New Panel With {.Dock = DockStyle.Top, .Height = 70, .BackColor = LgRed}
        head.Controls.Add(New Label With {
            .Text = "LG Electronics Indonesia", .ForeColor = Color.White, .AutoSize = True,
            .Font = New Font("Segoe UI", 15, FontStyle.Bold), .Left = 16, .Top = 8})
        head.Controls.Add(New Label With {
            .Text = "Sistem Manajemen Produksi & Penjualan", .ForeColor = Color.White, .AutoSize = True, .Left = 18, .Top = 40})
        Controls.Add(head)

        Controls.Add(New Label With {.Text = "Username", .Left = 20, .Top = 98, .Width = 90})
        Controls.Add(New Label With {.Text = "Password", .Left = 20, .Top = 133, .Width = 90})
        Controls.Add(txtU)
        Controls.Add(txtP)

        Dim bLogin = Btn("Login", AddressOf DoLogin, 100)
        bLogin.Left = 120 : bLogin.Top = 170
        Dim bReg = Btn("Daftar Customer", AddressOf DoRegister, 110)
        bReg.Left = 225 : bReg.Top = 170
        Controls.Add(bLogin) : Controls.Add(bReg)
        AcceptButton = bLogin


    End Sub

    Private Sub DoLogin(s As Object, e As EventArgs)
        If txtU.Text.Trim() = "" OrElse txtP.Text = "" Then
            Warn("Username dan password wajib diisi.") : Return
        End If
        Try
            Dim dt = Query("SELECT UserId, Username, FullName, Role FROM Users WHERE Username=@u AND PasswordHash=@p AND IsActive=1",
                           P("@u", txtU.Text.Trim()), P("@p", Hash(txtP.Text)))
            If dt.Rows.Count = 0 Then
                Warn("Username atau password salah.") : Return
            End If
            Session.UserId = Convert.ToInt32(dt.Rows(0)("UserId"))
            Session.Username = dt.Rows(0)("Username").ToString()
            Session.FullName = dt.Rows(0)("FullName").ToString()
            Session.Role = dt.Rows(0)("Role").ToString()
            Session.LoggedOut = False
            DialogResult = DialogResult.OK
        Catch ex As SqlException
            Warn("Tidak dapat terhubung ke database:" & vbCrLf & ex.Message)
        End Try
    End Sub

    Private Sub DoRegister(s As Object, e As EventArgs)
        Dim v = Prompt("Daftar Customer Baru", {"Username", "Password", "Nama Lengkap", "Telepon", "Alamat"})
        If v Is Nothing Then Return
        If v(0) = "" OrElse v(1) = "" OrElse v(2) = "" Then
            Warn("Username, password, dan nama lengkap wajib diisi.") : Return
        End If
        Try
            Exec("INSERT INTO Users(Username,PasswordHash,FullName,Role,Phone,Address) VALUES(@u,@p,@n,'Customer',@t,@a)",
                 P("@u", v(0)), P("@p", Hash(v(1))), P("@n", v(2)), P("@t", v(3)), P("@a", v(4)))
            Info("Pendaftaran berhasil. Silakan login.")
            txtU.Text = v(0) : txtP.Clear()
        Catch ex As SqlException
            Warn("Gagal mendaftar (username mungkin sudah dipakai)." & vbCrLf & ex.Message)
        End Try
    End Sub
End Class
