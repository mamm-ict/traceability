Imports System.Data.SqlClient
Imports Newtonsoft.Json
Imports QRCoder

Public Class HistoryController
    Inherits Controller

    Function Index() As ActionResult
        Dim batches As New List(Of Dictionary(Of String, String))()

        Using conn As New SqlConnection(DbHelper.GetConnectionString("BatchDB"))
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
                    batch("ControlNo") = reader("control_no").ToString()

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
    Public Function ProcessLogs(traceId As String) As ActionResult
        If traceId Is Nothing Then
            Return RedirectToAction("Index")
        End If
        Dim logs As New List(Of Dictionary(Of String, String))()

        Using conn As New SqlConnection(DbHelper.GetConnectionString("BatchDB"))
            conn.Open()
            Dim cmd As New SqlCommand("
            SELECT * 
            FROM pp_trace_processes
            WHERE trace_id = @TraceID
            ORDER BY scan_time
        ", conn)
            cmd.Parameters.AddWithValue("@TraceID", traceId)

            Using reader = cmd.ExecuteReader()
                While reader.Read()
                    Dim log As New Dictionary(Of String, String)
                    log("ProcessID") = reader("process_id").ToString()
                    log("ScanTime") = Convert.ToDateTime(reader("scan_time")).ToString("yyyy-MM-dd HH:mm:ss")
                    log("OperatorID") = reader("operator_id").ToString()
                    log("QtyIn") = reader("qty_in").ToString()
                    log("QtyOut") = reader("qty_out").ToString()
                    log("QtyReject") = reader("qty_reject").ToString()
                    log("Status") = reader("status").ToString()
                    logs.Add(log)
                End While
            End Using
        End Using

        ViewData("TraceID") = traceId
        ViewData("Logs") = logs
        Return View()
    End Function


End Class

