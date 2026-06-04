<%@ Language="VBScript" CodePage="65001" %>
<% Option Explicit
Response.ContentType = "application/json"
Response.Charset = "utf-8"
Response.AddHeader "Cache-Control", "no-store"
Response.Write "{""status"":""ok"",""service"":""maryar-legacy"",""time"":""" & _
    Year(Now) & "-" & Right("0" & Month(Now),2) & "-" & Right("0" & Day(Now),2) & "T" & _
    Right("0" & Hour(Now),2) & ":" & Right("0" & Minute(Now),2) & ":" & Right("0" & Second(Now),2) & "Z""}"
%>
