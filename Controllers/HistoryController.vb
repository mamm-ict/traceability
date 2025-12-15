Imports System.Data.SqlClient
Imports Newtonsoft.Json
Imports QRCoder

Public Class HistoryController
    Inherits System.Web.Mvc.Controller

    Function Index() As ActionResult
        Dim batches As New List(Of Dictionary(Of String, String))()

        Using conn As New SqlConnection(DbHelper.GetConnectionString())
            conn.Open()
            Dim cmd As New SqlCommand("SELECT * FROM pp_trace_route ORDER BY created_date DESC", conn)
            Using reader = cmd.ExecuteReader()
                While reader.Read()
                    Dim batch As New Dictionary(Of String, String)
                    batch("TraceID") = reader("trace_id").ToString()
                    batch("Model") = reader("model_name").ToString()
                    batch("InitQty") = Convert.ToInt64(reader("initial_qty"))
                    batch("Shift") = reader("shift").ToString()
                    batch("Line") = reader("line").ToString()
                    batch("OperatorID") = reader("operator_id").ToString()
                    batch("CreatedDate") = Convert.ToDateTime(reader("created_date")).ToString("yyyy-MM-dd")

                    ' Deserialize raw materials JSON safely
                    'Dim rawMaterialsJson = reader("RawMaterial").ToString()
                    'Try
                    '    Dim rmList = JsonConvert.DeserializeObject(Of List(Of RawMaterialEntry))(rawMaterialsJson)
                    '    batch("RawMaterial") = String.Join(", ", rmList.Select(Function(r) r.Name & ":" & r.Quantity))
                    'Catch ex As Exception
                    '    ' fallback if old format or plain text
                    '    batch("RawMaterial") = rawMaterialsJson
                    'End Try

                    ' Generate QR code for each batch
                    'Dim qrContent = String.Join("|",
                    '                            batch("TraceID"),
                    '                            batch("Model"),
                    '                            batch("MachineNo"),
                    '                            batch("Line"),
                    '                            batch("RawMaterial"),
                    '                            batch("OperatorID"),
                    '                            batch("CreatedDate"),
                    '                            batch("Shift"))

                    Dim qrContent = batch("TraceID")

                    Dim qrBase64 As String
                    Using qrGenerator As New QRCodeGenerator()
                        Using qrCodeData = qrGenerator.CreateQrCode(qrContent, QRCodeGenerator.ECCLevel.Q)
                            Using qrCode = New PngByteQRCode(qrCodeData)
                                qrBase64 = Convert.ToBase64String(qrCode.GetGraphic(10))
                            End Using
                        End Using
                    End Using

                    batch("QRCodeImage") = qrBase64

                    batches.Add(batch)
                End While
            End Using
        End Using

        ViewData("BatchList") = batches
        Return View()
    End Function
End Class

