Imports System.Security.Cryptography
Imports System.Text

Public Module Session
    Public UserId As Integer
    Public Username As String = ""
    Public FullName As String = ""
    Public Role As String = ""
    Public LoggedOut As Boolean

    Public Sub Clear()
        UserId = 0 : Username = "" : FullName = "" : Role = "" : LoggedOut = False
    End Sub

    Public Function Hash(s As String) As String
        Return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s)))
    End Function
End Module
