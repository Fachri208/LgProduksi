Imports System.Data
Imports Microsoft.Data.SqlClient

''' <summary>Akses database SQL Server. Ubah ConnStr sesuai server Anda.</summary>
Public Module Db
    Public ConnStr As String =
        "Server=Fachri\SQLEXPRESS;Database=LgProduksiDB;Trusted_Connection=True;TrustServerCertificate=True;"

    Public Function P(name As String, value As Object) As SqlParameter
        Return New SqlParameter(name, If(value, DBNull.Value))
    End Function

    Public Function Query(sql As String, ParamArray ps() As SqlParameter) As DataTable
        Dim dt As New DataTable()
        Using cn As New SqlConnection(ConnStr)
            Using cmd As New SqlCommand(sql, cn)
                cmd.Parameters.AddRange(ps)
                Using da As New SqlDataAdapter(cmd)
                    da.Fill(dt)
                End Using
            End Using
        End Using
        Return dt
    End Function

    Public Function Exec(sql As String, ParamArray ps() As SqlParameter) As Integer
        Using cn As New SqlConnection(ConnStr)
            Using cmd As New SqlCommand(sql, cn)
                cmd.Parameters.AddRange(ps)
                cn.Open()
                Return cmd.ExecuteNonQuery()
            End Using
        End Using
    End Function

    Public Function Scalar(sql As String, ParamArray ps() As SqlParameter) As Object
        Using cn As New SqlConnection(ConnStr)
            Using cmd As New SqlCommand(sql, cn)
                cmd.Parameters.AddRange(ps)
                cn.Open()
                Return cmd.ExecuteScalar()
            End Using
        End Using
    End Function

    ' ---------- Transaksi ----------
    Public Sub InTx(work As Action(Of SqlConnection, SqlTransaction))
        Using cn As New SqlConnection(ConnStr)
            cn.Open()
            Dim tx As SqlTransaction = cn.BeginTransaction()
            Try
                work(cn, tx)
                tx.Commit()
            Catch
                tx.Rollback()
                Throw
            End Try
        End Using
    End Sub

    Public Function TxExec(cn As SqlConnection, tx As SqlTransaction, sql As String, ParamArray ps() As SqlParameter) As Integer
        Using cmd As New SqlCommand(sql, cn, tx)
            cmd.Parameters.AddRange(ps)
            Return cmd.ExecuteNonQuery()
        End Using
    End Function

    Public Function TxScalar(cn As SqlConnection, tx As SqlTransaction, sql As String, ParamArray ps() As SqlParameter) As Object
        Using cmd As New SqlCommand(sql, cn, tx)
            cmd.Parameters.AddRange(ps)
            Return cmd.ExecuteScalar()
        End Using
    End Function

    Public Function Rp(v As Object) As String
        Return "Rp " & Convert.ToDecimal(v).ToString("N0", Globalization.CultureInfo.GetCultureInfo("id-ID"))
    End Function
End Module
