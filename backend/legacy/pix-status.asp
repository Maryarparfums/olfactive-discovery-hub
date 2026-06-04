<%@ Language="VBScript" CodePage="65001" %>
<% Option Explicit
' Exemplo legado: consulta status de um pedido lendo do MySQL via ADODB + driver MySQL ODBC.
' Uso real: prefira a API .NET (api/payments/status/{orderId}). Mantido para tarefas internas.

Response.ContentType = "application/json"
Response.Charset = "utf-8"
Response.AddHeader "Cache-Control", "no-store"

Dim orderId : orderId = Request.QueryString("orderId")
If Len(orderId) = 0 Then
    Response.Status = "400 Bad Request"
    Response.Write "{""error"":""orderId obrigatório""}"
    Response.End
End If

Dim cn, rs, connStr
' Configure um DSN ODBC chamado MaryarMySQL ou troque pelo DSN-less abaixo.
connStr = "Driver={MySQL ODBC 8.0 Unicode Driver};Server=localhost;Database=maryar;" & _
          "User=maryar_app;Password=TROQUE_AQUI;Option=3;charset=utf8mb4;"

Set cn = Server.CreateObject("ADODB.Connection")
cn.Open connStr

Set rs = Server.CreateObject("ADODB.Recordset")
Dim cmd
Set cmd = Server.CreateObject("ADODB.Command")
cmd.ActiveConnection = cn
cmd.CommandText = "SELECT order_number, payment_status, order_status FROM orders WHERE id = ? LIMIT 1"
cmd.Parameters.Append cmd.CreateParameter("p1", 200, 1, 36, orderId) ' adVarChar
Set rs = cmd.Execute

If rs.EOF Then
    Response.Status = "404 Not Found"
    Response.Write "{""error"":""pedido não encontrado""}"
Else
    Response.Write "{""orderNumber"":""" & rs("order_number") & _
                   """,""paymentStatus"":""" & rs("payment_status") & _
                   """,""orderStatus"":""" & rs("order_status") & """}"
End If

rs.Close : cn.Close
Set rs = Nothing : Set cn = Nothing : Set cmd = Nothing
%>
