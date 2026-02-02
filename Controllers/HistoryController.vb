Imports System.Data.SqlClient
Imports Newtonsoft.Json
Imports QRCoder

Public Class HistoryController
    Inherits Controller

    Function Index() As ActionResult
        Dim batches As New List(Of Dictionary(Of String, String))()

        Using conn As New SqlConnection(DbHelper.GetConnectionString("BatchDB"))
            conn.Open()
            Dim cmd As New SqlCommand("SELECT * FROM pp_trace_route ORDER BY created_date DESC, shift desc", conn)
            Using reader = cmd.ExecuteReader()
                While reader.Read()
                    Dim batch As New Dictionary(Of String, String)
                    batch("TraceID") = reader("trace_id").ToString()
                    batch("Model") = reader("model_name").ToString()
                    batch("InitQty") = Convert.ToInt64(reader("initial_qty"))
                    batch("Shift") = reader("shift").ToString()
                    batch("Die") = reader("die").ToString()
                    batch("Line") = reader("line").ToString()
                    batch("OperatorID") = reader("operator_id").ToString()
                    batch("CreatedDate") = GetDateFromTraceID(batch("TraceID")).ToString("yyyy-MM-dd")
                    batch("ControlNo") = reader("control_no").ToString()

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
    Public Function GetDateFromTraceID(traceID As String) As Date
        ' Expect format: PPA-YYYYMMDD-XXX
        Dim parts() As String = traceID.Split("-"c)
        If parts.Length >= 2 Then
            Dim dtStr As String = parts(1)
            Return Date.ParseExact(dtStr, "yyyyMMdd", Nothing)
        End If
        Return Date.MinValue ' fallback
    End Function

    Public Function ProcessLogs(traceId As String) As ActionResult
        If traceId Is Nothing Then
            Return RedirectToAction("Index")
        End If
        Dim batch As Batch = DbHelper.LoadBatch(traceId)

        Dim partDesc As String
        Using conn As New SqlConnection(DbHelper.GetConnectionString("BatchDB"))
            conn.Open()
            Dim cmd As New SqlCommand("SELECT part_desc FROM pp_master_material WHERE part_code = @PartCode", conn)
            cmd.Parameters.AddWithValue("@PartCode", batch.PartCode)


            partDesc = cmd.ExecuteScalar()
        End Using

        ' --- Load process logs ---
        Dim logs As New List(Of Dictionary(Of String, String))()
        Using conn As New SqlConnection(DbHelper.GetConnectionString("BatchDB"))
            conn.Open()
            Dim cmd As New SqlCommand("
            SELECT tp.*, mp.proc_code, mp.proc_name
            FROM pp_trace_processes tp
            JOIN pp_master_process mp ON tp.process_id = mp.id
            WHERE tp.trace_id = @TraceID
            ORDER BY tp.scan_time
        ", conn)
            cmd.Parameters.AddWithValue("@TraceID", traceId)

            Using reader = cmd.ExecuteReader()
                While reader.Read()
                    Dim log As New Dictionary(Of String, String)
                    log("ProcessID") = reader("proc_code").ToString()
                    log("ProcessName") = reader("proc_name").ToString()
                    log("ScanTime") = Convert.ToDateTime(reader("scan_time")).ToString("yyyy-MM-dd HH:mm:ss")
                    log("OperatorID") = reader("operator_id").ToString()
                    log("QtyIn") = reader("qty_in").ToString()
                    log("QtyOut") = reader("qty_out").ToString()
                    log("QtyReject") = reader("qty_reject").ToString()
                    log("Status") = reader("status").ToString()
                    log("ProcID") = reader("process_id").ToString()   ' <-- key untuk material
                    logs.Add(log)
                End While
            End Using
        End Using

        ' --- Load materials grouped by proc_id ---
        Dim materialsDict As New Dictionary(Of String, List(Of MaterialLog))()
        For Each log In logs
            Dim procId As String = log("ProcID")
            If Not materialsDict.ContainsKey(procId) Then
                materialsDict(procId) = DbHelper.GetTraceMaterialsByTrace(traceId) _
                                    .Where(Function(m) m.ProcCode = log("ProcessID")) _
                                    .ToList()
            End If
        Next

        ViewData("Batch") = batch
        ViewData("PartDesc") = partDesc
        ViewData("Logs") = logs
        ViewData("Materials") = materialsDict

        Return View()
    End Function

    Function FinishedProcess() As ActionResult
        Dim buffers As New List(Of Dictionary(Of String, String))()

        Using conn As New SqlConnection(DbHelper.GetConnectionString("BatchDB"))
            conn.Open()

            Dim sql As String = "
                    SELECT 
                        tr.trace_id,
                        tr.model_name,
                         mm.part_desc,
                        tr.part_code,
                        tr.current_qty,
                        tp.id AS process_id,
                        tp.qty_out,
                        tr.update_date,
                        tr.printed_date
                    FROM pp_trace_route tr
                    JOIN pp_trace_processes tp
                        ON tr.trace_id = tp.trace_id AND tr.current_qty = tp.qty_in
                    JOIN pp_master_material mm ON tr.part_code = mm.upper_item 
                    WHERE tr.status = 'COMPLETED'
                      AND tp.status = 'Completed'
                    ORDER BY tr.trace_id desc
                "

            Using cmd As New SqlCommand(sql, conn)
                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        Dim buffer As New Dictionary(Of String, String)
                        buffer("TraceID") = reader("trace_id").ToString()
                        buffer("ModelName") = reader("model_name").ToString()
                        buffer("PartCode") = reader("part_desc").ToString()
                        buffer("CurQty") = reader("current_qty").ToString()
                        buffer("UpdateDate") = If(IsDBNull(reader("update_date")), "", Convert.ToDateTime(reader("update_date")).ToString("yyyy-MM-dd HH:mm:ss"))
                        buffer("PrintedDate") = If(IsDBNull(reader("printed_date")), "", Convert.ToDateTime(reader("printed_date")).ToString("dd/MM HH:mm"))


                        buffers.Add(buffer)
                    End While
                End Using
            End Using
        End Using

        Return View(buffers.OrderBy(Function(x) x("PartCode")).ThenByDescending(Function(x) x("TraceID")).ToList())
    End Function
End Class

