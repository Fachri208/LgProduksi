Imports System.Windows.Forms

Public Module Program
    <STAThread>
    Public Sub Main()
        Application.EnableVisualStyles()
        Application.SetHighDpiMode(HighDpiMode.SystemAware)

        Do
            Session.Clear()
            Using lf As New LoginForm()
                If lf.ShowDialog() <> DialogResult.OK Then Exit Do
            End Using

            Dim f As Form
            Select Case Session.Role
                Case "Supplier" : f = New SupplierForm()
                Case "Sales" : f = New SalesForm()
                Case Else : f = New CustomerForm()
            End Select
            Application.Run(f)
        Loop While Session.LoggedOut   ' logout -> kembali ke halaman login
    End Sub
End Module
