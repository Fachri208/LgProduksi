Imports System.Data
Imports System.Windows.Forms

Public Module OrderHelper

    Public Sub ShowOrderDetail(orderId As Integer)
        Dim h = Query("SELECT o.OrderDate, o.Status, o.Total, u.FullName, u.Address " &
                      "FROM Orders o JOIN Users u ON u.UserId=o.CustomerId WHERE o.OrderId=@o", P("@o", orderId))
        If h.Rows.Count = 0 Then Return
        Dim items = Query("SELECT p.Name, i.Qty, i.Price FROM OrderItems i " &
                          "JOIN Catalog k ON k.CatalogId=i.CatalogId JOIN Products p ON p.ProductId=k.ProductId " &
                          "WHERE i.OrderId=@o", P("@o", orderId))
        Dim r = h.Rows(0)
        Dim sb As New System.Text.StringBuilder()
        sb.AppendLine("No. Pesanan : ORD-" & orderId.ToString("00000"))
        sb.AppendLine("Tanggal     : " & Convert.ToDateTime(r("OrderDate")).ToString("dd/MM/yyyy HH:mm"))
        sb.AppendLine("Customer    : " & r("FullName").ToString())
        sb.AppendLine("Alamat      : " & r("Address").ToString())
        sb.AppendLine("Status      : " & r("Status").ToString())
        sb.AppendLine()
        For Each it As DataRow In items.Rows
            sb.AppendLine("- " & it("Name").ToString() & "  x" & it("Qty").ToString() & "  @ " & Rp(it("Price")))
        Next
        sb.AppendLine()
        sb.AppendLine("TOTAL: " & Rp(r("Total")))
        MessageBox.Show(sb.ToString(), "Detail Pesanan", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    ''' <summary>Batalkan pesanan (status Menunggu/Diproses) dan kembalikan stok.</summary>
    Public Function CancelOrder(orderId As Integer) As Boolean
        Try
            InTx(Sub(cn, tx)
                     Dim st = TxScalar(cn, tx, "SELECT Status FROM Orders WHERE OrderId=@o", P("@o", orderId))
                     If st Is Nothing OrElse (st.ToString() <> "Menunggu" AndAlso st.ToString() <> "Diproses") Then
                         Throw New InvalidOperationException("Pesanan ini tidak dapat dibatalkan.")
                     End If
                     TxExec(cn, tx, "UPDATE k SET k.Stock = k.Stock + i.Qty FROM Catalog k JOIN OrderItems i ON i.CatalogId=k.CatalogId WHERE i.OrderId=@o", P("@o", orderId))
                     TxExec(cn, tx, "UPDATE p SET p.Stock = p.Stock + i.Qty FROM Products p JOIN Catalog k ON k.ProductId=p.ProductId JOIN OrderItems i ON i.CatalogId=k.CatalogId WHERE i.OrderId=@o", P("@o", orderId))
                     TxExec(cn, tx, "UPDATE Orders SET Status='Dibatalkan' WHERE OrderId=@o", P("@o", orderId))
                 End Sub)
            Return True
        Catch ex As Exception
            Warn(ex.Message)
            Return False
        End Try
    End Function
End Module
