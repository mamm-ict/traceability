Imports System.Data.SqlClient
Imports System.IO
Imports System.Web.Mvc
Imports Microsoft.VisualBasic.Logging
Imports Newtonsoft.Json
Imports QRCoder

Public Class ProcessController
    Inherits Controller

    Public Function StartProcess() As ActionResult
        ViewData("StatusMessage") = "Batch not scanned yet."
        Return View()
    End Function

    <HttpPost>
    Public Function StartProcess(traceId As String, operatorId As String, processQr As String) As ActionResult
        ' 1️⃣ Basic validation
        If String.IsNullOrWhiteSpace(traceId) Then
            ViewData("StatusMessage") = "Invalid input. Try again."
            Return View("~/Views/Process/StartProcess.vbhtml")
        End If

        Dim traceIdPattern As String = "^[A-Z]{3}-\d{8}-\d{3}$"
        If Not System.Text.RegularExpressions.Regex.IsMatch(traceId, traceIdPattern) Then
            ViewData("StatusMessage") = "QR format invalid."
            Return View("~/Views/Process/StartProcess.vbhtml")
        End If

        ' 2️⃣ Load batch, processes, logs
        Dim batch = DbHelper.GetBatchByTraceID(traceId)
        Dim processes = DbHelper.GetAllProcesses()
        Dim logs = DbHelper.GetProcessLogsByTraceID(traceId)

        If batch Is Nothing Then
            ViewData("StatusMessage") = "Invalid batch."
            Return View("~/Views/Process/StartProcess.vbhtml")
        End If

        Dim scannedProcess = processes.FirstOrDefault(Function(p) p.Code = processQr.Trim().ToUpper())
        ' StartProcess
        ' Tambah sebelum redirect atau render view
        ViewData("ScannedProcessId") = If(ViewData("ScannedProcessId"), scannedProcess.ID)
        ViewData("ScannedOperatorId") = If(ViewData("ScannedOperatorId"), operatorId)

        If scannedProcess Is Nothing Then
            ViewData("StatusMessage") = "Invalid process QR."
            Return View("~/Views/Process/StartProcess.vbhtml")
        End If

        Dim pendingBufferLog = logs.FirstOrDefault(Function(l) l.Status = "Pending Completion")
        If pendingBufferLog IsNot Nothing Then
            ' Redirect to ProcessBuffer page for pending process
            ViewData("ScannedProcessId") = scannedProcess.ID
            ViewData("ScannedOperatorId") = operatorId

            Return RedirectToAction("ProcessBuffer", "Process", New With {
        .traceId = traceId,
        .procId = scannedProcess.ID,
        .scannedProcessId = scannedProcess.ID,
        .scannedOperatorId = operatorId
    })
        End If

        ' 3️⃣ Check process sequence & buffer/material (sama macam sebelum)
        Dim activeLogs = logs.Where(Function(l) l.Status = "In Progress").ToList()
        Dim progressLogs = logs.Where(Function(l) l.Status = "In Progress" OrElse l.Status = "Completed").ToList()

        If activeLogs.Any(Function(l) l.ProcessID = scannedProcess.ID) Then
            ViewData("StatusMessage") = $"Process {scannedProcess.Name} already in progress."

            Dim proc = processes.First(Function(p) p.ID = scannedProcess.ID)
            If proc.MaterialFlag = 1 AndAlso Not DbHelper.HasMaterialForProcess(traceId, proc.ID) Then
                ViewData("StatusMessage") = $"Material not scanned for {proc.Name}."
                Return RedirectToAction(
                "ProcessMaterial",
                "Process",
                New With {.traceId = traceId, .procId = scannedProcess.ID}
            )
            End If

            Return View("~/Views/Process/StartProcess.vbhtml")
        End If

        Dim maxLevel As Integer = 0
        If progressLogs.Any() Then
            maxLevel = progressLogs.Max(Function(l) processes.First(Function(x) x.ID = l.ProcessID).Level)
            If scannedProcess.Level <= maxLevel Then
                ViewData("StatusMessage") = "Cannot scan lower level or process with same level."
                Return View("~/Views/Process/StartProcess.vbhtml")
            End If
            If scannedProcess.Level > maxLevel + 1 Then
                ViewData("StatusMessage") = $"Cannot skip process level. Expected level {maxLevel + 1}."
                Return View("~/Views/Process/StartProcess.vbhtml")
            End If
        Else
            If scannedProcess.Level <> 1 Then
                ViewData("StatusMessage") = $"Cannot start process. Expected level 1. Scanned level {scannedProcess.Level}."
                Return View("~/Views/Process/StartProcess.vbhtml")
            End If
        End If

        ' Ambil previous process log (lower level dari current)
        Dim prevLogs = activeLogs.Where(Function(l)
                                            Dim p = processes.First(Function(x) x.ID = l.ProcessID)
                                            Return p.Level < scannedProcess.Level
                                        End Function).ToList()

        ' 1️⃣ Material check (masih return jika missing)
        For Each log In prevLogs
            Dim proc = processes.First(Function(p) p.ID = log.ProcessID)
            If proc.MaterialFlag = 1 AndAlso Not DbHelper.HasMaterialForProcess(traceId, proc.ID) Then
                ViewData("StatusMessage") = $"Material not scanned for {proc.Name}. <br> Please re-scan again to enter material."
                '    Return RedirectToAction(
                '    "ProcessMaterial",
                '    "Process",
                '    New With {.traceId = traceId, .procId = scannedProcess.ID}
                ')
                Return View("~/Views/Process/StartProcess.vbhtml")
            End If
        Next

        ' 2️⃣ Buffer check: set semua previous buffer ke Pending Completion
        Dim pendingBuffers = New List(Of ProcessLog)()
        For Each log In prevLogs
            Dim proc = processes.First(Function(p) p.ID = log.ProcessID)
            If proc.BufferFlag = 1 AndAlso log.QtyOut = 0 AndAlso log.QtyReject = 0 Then
                DbHelper.UpdateProcessLogStatus(log.ID, "Pending Completion")
                pendingBuffers.Add(log)
            Else
                ' Complete previous process kalau bukan buffer
                DbHelper.UpdateProcessLogStatus(log.ID, "Completed")
            End If
        Next

        ' 3️⃣ Kalau ada pending buffer, redirect ke ProcessBuffer
        If pendingBuffers.Any() Then
            ViewData("ScannedProcessId") = scannedProcess.ID
            ViewData("ScannedOperatorId") = operatorId

            Return RedirectToAction("ProcessBuffer", New With {
        .traceId = traceId,
        .procId = pendingBuffers.First().ProcessID,
        .scannedProcessId = scannedProcess.ID,
        .scannedOperatorId = operatorId
    })
        End If


        Dim trueTraceId = traceId
        Dim trueProcessId = scannedProcess.ID
        Dim trueOperatorId = operatorId

        ' 5️⃣ Log current process FIRST
        DbHelper.LogBatchProcess(
            traceId:=trueTraceId,
            processId:=trueProcessId,
            operatorId:=trueOperatorId,
            qtyIn:=batch.CurQty,
            qtyOut:=0,
            qtyReject:=0,
            status:="In Progress"
        )

        DbHelper.UpdateRouteLastProcess(traceId, scannedProcess.Code)

        ' 6️⃣ Redirect ikut material flag
        If scannedProcess.MaterialFlag = 1 Then
            Return RedirectToAction(
                "ProcessMaterial",
                "Process",
                New With {.traceId = traceId, .procId = scannedProcess.ID}
            )
        ElseIf scannedProcess.BufferFlag = 1 Then
            ViewData("ScannedProcessId") = scannedProcess.ID
            ViewData("ScannedOperatorId") = operatorId

                Return RedirectToAction("ProcessBuffer", "Process", New With {
        .traceId = traceId,
        .procId = scannedProcess.ID,
        .scannedProcessId = scannedProcess.ID,
        .scannedOperatorId = operatorId
    })

        Else
            Return RedirectToAction("ProcessBatch", New With {.traceId = traceId})
        End If

        ' ✅ Success message
        ViewData("StatusMessage") = $"Batch {traceId} scanned for process {scannedProcess.Name}."

    End Function

    Public Function ProcessMaterial(traceId As String, procId As Integer) As ActionResult
        Dim batch = DbHelper.GetBatchByTraceID(traceId)
        Dim processes = DbHelper.GetAllProcesses()
        Dim logs = DbHelper.GetProcessLogsByTraceID(traceId)

        If batch Is Nothing Then
            Return Content("Batch not found")
        End If

        ViewData("Batch") = batch
        ViewData("Processes") = processes
        ViewData("Logs") = logs

        Return View()
    End Function

    <HttpPost>
    Public Function DeleteTraceMaterial(id As Integer) As ActionResult
        Try
            DbHelper.DeleteTraceMaterial(id)
            Return Content("OK")
        Catch ex As Exception
            Return Content(ex.Message)
        End Try
    End Function

    Public Function ProcessBuffer(traceId As String, procId As Integer, Optional scannedProcessId As Integer = 0,
    Optional scannedOperatorId As String = "") As ActionResult
        ViewData("ScannedProcessId") = scannedProcessId
        ViewData("ScannedOperatorId") = scannedOperatorId
        If String.IsNullOrEmpty(traceId) Then
            Return RedirectToAction("StartProcess")
        End If

        Dim batch As Batch = LoadBatch(traceId)
        If batch Is Nothing Then
            Return Content("Batch not found")
        End If

        Dim processes = DbHelper.GetAllProcesses()
        Dim logs = DbHelper.GetProcessLogsByTraceID(traceId)

        Dim pendingLog = logs.Where(Function(l) l.Status = "Pending Completion") _
            .OrderBy(Function(l) l.ScanTime) _
            .FirstOrDefault()

        ViewData("Batch") = batch
        ViewData("Processes") = processes
        ViewData("Logs") = logs

        ' Flag if this is final process (no next process)
        Dim currentProcess = processes.FirstOrDefault(Function(p) p.ID = procId)
        ViewData("IsFinalProcess") = (currentProcess IsNot Nothing AndAlso currentProcess.Level = processes.Max(Function(x) x.Level))

        If pendingLog Is Nothing Then
            Return RedirectToAction("ProcessBatch", New With {.TraceID = traceId})
        End If

        Return View()
    End Function

    <HttpPost>
    Public Function GetEmployeeByControlNo() As JsonResult
        Dim jsonString As String
        Using reader = New System.IO.StreamReader(Request.InputStream)
            jsonString = reader.ReadToEnd()
        End Using

        Dim data = Newtonsoft.Json.JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(jsonString)
        Dim controlNo As String = data("controlNo")

        Using conn As New SqlConnection(DbHelper.GetConnectionString("EmpDB2"))
            conn.Open()
            Dim cmd As New SqlCommand("
                SELECT EMPLOYEE_NO
                FROM ZPA_EMPLOYEE
                WHERE CONTROL_NO = @ControlNo
                  AND EMP_STATUS = 'A'
            ", conn)
            cmd.Parameters.AddWithValue("@ControlNo", controlNo)
            Dim empNo = cmd.ExecuteScalar()
            If empNo Is Nothing Then
                Return Json(New With {.success = False, .message = "Employee not found or inactive"})
            End If
            Return Json(New With {.success = True, .employeeNo = empNo.ToString()})
        End Using
    End Function

    <HttpPost>
    Public Function GetProcessByControlNo() As JsonResult
        Dim jsonString As String
        Using reader = New System.IO.StreamReader(Request.InputStream)
            jsonString = reader.ReadToEnd()
        End Using

        Dim data = Newtonsoft.Json.JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(jsonString)
        Dim controlNo As String = data("controlNo")

        Using conn As New SqlConnection(DbHelper.GetConnectionString("BatchDB"))
            conn.Open()
            Dim cmd As New SqlCommand("
                SELECT proc_code
                FROM pp_master_process
                WHERE CONTROL_NO = @ControlNo
            ", conn)
            cmd.Parameters.AddWithValue("@ControlNo", controlNo)
            Dim procCode = cmd.ExecuteScalar()
            If procCode Is Nothing Then
                Return Json(New With {.success = False, .message = "Process not found."})
            End If
            Return Json(New With {.success = True, .processCode = procCode.ToString()})
        End Using
    End Function

    <HttpPost>
    Public Function GetTraceIDByControlNo() As JsonResult
        Dim jsonString As String
        Using reader = New System.IO.StreamReader(Request.InputStream)
            jsonString = reader.ReadToEnd()
        End Using

        Dim data = Newtonsoft.Json.JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(jsonString)
        Dim controlNo As String = data("controlNo")

        Using conn As New SqlConnection(DbHelper.GetConnectionString("BatchDB"))
            conn.Open()
            Dim cmd As New SqlCommand("
                SELECT trace_id
                FROM pp_trace_route
                WHERE CONTROL_NO = @ControlNo
                AND (status LIKE 'NEW' OR status LIKE 'ONGOING')
                ORDER BY trace_id DESC;
            ", conn)
            cmd.Parameters.AddWithValue("@ControlNo", controlNo)
            Dim traceId = cmd.ExecuteScalar()
            If traceId Is Nothing Then
                Return Json(New With {.success = False, .message = "Trace ID not found."})
            End If
            Return Json(New With {.success = True, .traceID = traceId.ToString()})
        End Using
    End Function

    Public Function ProcessMaster() As ActionResult
        ' Fetch processes from DB
        Dim processList As New List(Of Dictionary(Of String, String))()

        Using conn As New SqlConnection(DbHelper.GetConnectionString("BatchDB"))
            conn.Open()
            Dim cmd As New SqlCommand("SELECT * FROM pp_master_process", conn)
            Using reader = cmd.ExecuteReader()
                While reader.Read()
                    Dim process As New Dictionary(Of String, String)()
                    process("ProcessID") = reader("id").ToString()
                    process("ProcessCode") = reader("proc_code").ToString()
                    process("ProcessName") = reader("proc_name").ToString()
                    process("ProcessLevel") = reader("proc_level").ToString()
                    process("FlowId") = reader("proc_flow_id").ToString()
                    process("MaterialFlag") = reader("material_flag").ToString()
                    process("BufferFlag") = Convert.ToInt64(reader("buffer_flag"))

                    Dim qrData As String = reader("proc_code").ToString()

                    ' Generate QR Code as Base64
                    Using qrGenerator As New QRCodeGenerator()
                        Using qrDataObj = qrGenerator.CreateQrCode(qrData, QRCodeGenerator.ECCLevel.Q)
                            Using qrCode = New QRCode(qrDataObj)
                                Using bitmap = qrCode.GetGraphic(20)
                                    Using ms As New MemoryStream()
                                        bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png)
                                        process("QRCodeImage") = Convert.ToBase64String(ms.ToArray())
                                    End Using
                                End Using
                            End Using
                        End Using
                    End Using

                    processList.Add(process)
                End While
            End Using
        End Using

        ViewData("ProcessList") = processList
        Return View()
    End Function

    ' GET: /Process/ProcessBatch
    Public Function ProcessBatch(TraceID As String) As ActionResult
        If TraceID Is Nothing Then
            Return RedirectToAction("StartProcess")
        End If

        Dim batch As Batch = LoadBatch(TraceID)
        Dim processes As List(Of ProcessMaster) = DbHelper.GetAllProcesses()
        Dim logs As List(Of ProcessLog) = DbHelper.GetProcessLogsByTraceID(TraceID).OrderBy(Function(l) l.ScanTime).ToList()

        ' --- Determine current process (last log) ---
        Dim lastLog = logs.OrderByDescending(Function(l) l.ScanTime).FirstOrDefault()
        Dim currentProcID As Integer = If(lastLog IsNot Nothing, lastLog.ProcessID, 0)
        Dim currentPartCode As String = If(batch IsNot Nothing, batch.PartCode, "")

        ' --- Load materials used for this TraceID + Process + Part ---
        Dim materialsUsed As List(Of MaterialLog) = DbHelper.GetTraceMaterialsByTrace(TraceID)

        Dim diecore As String = (If(batch.Die, "").Trim() & " " & If(batch.Line, "").Trim()).Trim()


        ViewData("Batch") = batch
        ViewData("Processes") = processes
        ViewData("Logs") = logs
        ViewData("MaterialsUsed") = materialsUsed
        ViewData("DieCore") = diecore

        Return View()
    End Function


    <HttpPost>
    Public Function ScanMaterial(model As MaterialLog) As ActionResult
        Try
            ' 🔐 SAFETY CHECK
            If model Is Nothing Then
                Return Content("Invalid payload")
            End If

            Using conn As New SqlConnection(DbHelper.GetConnectionString("BatchDB"))
                conn.Open()

                Dim cmd2 As New SqlCommand("SELECT mp.proc_level, mp.proc_flow_id FROM pp_master_process mp 
                JOIN pp_master_material mm ON mp.proc_level = mm.proc_level
                WHERE mp.id = @Id", conn)
                cmd2.Parameters.AddWithValue("@Id", model.ProcID)

                Dim level As Object = Nothing
                Dim flowId As Object = Nothing

                Using reader As SqlDataReader = cmd2.ExecuteReader()
                    If reader.Read() Then
                        level = reader("proc_level")
                        flowId = reader("proc_flow_id")
                    End If
                End Using

                Dim cmd As New SqlCommand("SELECT 1
                    FROM pp_master_material mm
                    WHERE mm.part_code = @PartCode
                    AND mm.lower_item = @LowerMaterial
                    AND mm.proc_level = @Level
                    AND mm.proc_flow_id = @FlowId
                    ", conn)

                cmd.Parameters.AddWithValue("@PartCode", model.PartCode)
                cmd.Parameters.AddWithValue("@LowerMaterial", model.LowerMaterial)
                cmd.Parameters.AddWithValue("@Level", level)
                cmd.Parameters.AddWithValue("@FlowId", flowId)

                Dim exists As Object = cmd.ExecuteScalar()

                Debug.WriteLine(level & flowId & model.PartCode & model.LowerMaterial)

                ' ❌ MATERIAL TAK VALID → STOP & SURUH SCAN LAIN
                If exists Is Nothing Then
                    Return Content("Material not valid for this part. Please scan another material.")
                End If
            End Using

            ' 🔥 SATU-SATUNYA TEMPAT INSERT
            DbHelper.InsertTraceMaterial(
                model.TraceID,
                model.ProcID,
                model.PartCode,
                model.LowerMaterial,
                model.BatchLot,
                model.UsageQty,
                model.UOM,
                model.VendorCode,
                model.VendorLot
            )

            ' 2️⃣ Check buffer flag
            Dim process = DbHelper.GetProcessById(model.ProcID)
            If process.BufferFlag = 1 Then
                ' Set process log status to Pending Completion
                Dim log = DbHelper.GetProcessLogsByTraceID(model.TraceID) _
                        .FirstOrDefault(Function(l) l.ProcessID = model.ProcID AndAlso l.Status = "In Progress")
                If log IsNot Nothing Then
                    DbHelper.UpdateProcessLogStatus(log.ID, "Pending Completion")
                End If
            End If

            Return Content("OK")

        Catch ex As Exception
            ' 👈 exception dari DbHelper (duplicate / partCode null)
            Return Content(ex.Message)
        End Try
    End Function

    <HttpPost>
    Public Function CompleteBuffer(logId As Integer, qtyReject As Integer, qtyOut As Integer, scannedProcessId As Integer,
    scannedOperatorId As String) As ActionResult

        Dim log = DbHelper.GetProcessLogById(logId)
        If log Is Nothing Then
            Return Json(New With {.success = False, .message = "Invalid log."})
        End If

        Dim batch = DbHelper.GetBatchByTraceID(log.TraceID)
        Dim process = DbHelper.GetProcessById(log.ProcessID)

        Dim processes = DbHelper.GetAllProcesses()
        Dim maxLevel = processes.Max(Function(p) p.Level)
        Dim isFinal As Boolean = (process.Level = maxLevel)

        Try
            If isFinal Then
                ' 🔥 FINAL PROCESS
                Dim finalQtyOut As Integer = batch.CurQty - qtyReject
                If finalQtyOut < 0 Then finalQtyOut = 0

                DbHelper.CompleteFinalProcess(
                    logId:=log.ID,
                    batch:=batch,
                     qtyReject:=qtyReject
                )

                Dim bufferQty As Integer
                'If bufferQty < 0 Then bufferQty = 0

                Using conn As New SqlConnection(DbHelper.GetConnectionString("BatchDB"))
                    conn.Open()
                    Dim cmd As New SqlCommand("
                        SELECT qty_out FROM pp_trace_processes WHERE id = @LogID
                    ", conn)

                    cmd.Parameters.AddWithValue("@LogID", log.ID)
                    bufferQty = Convert.ToInt32(cmd.ExecuteScalar())
                End Using

                'If bufferQty > 0 Then
                '    Debug.WriteLine(bufferQty & "up")

                '    Dim activeBufferTrace = DbHelper.GetActiveBufferTrace(DateTime.Today)
                '    If activeBufferTrace Is Nothing OrElse activeBufferTrace.TotalQty + bufferQty > 1392 Then
                '        activeBufferTrace = DbHelper.CreateNewBufferTrace(DateTime.Today, bufferQty, log.OperatorID, log.TraceID)
                '    End If

                '    DbHelper.InsertBufferMap(activeBufferTrace.TraceID, batch.TraceID, bufferQty)
                '    DbHelper.UpdateBufferTraceQty(activeBufferTrace.TraceID, log.OperatorID)
                '    Debug.WriteLine(bufferQty & "down")
                'End If

                'Dim pdfBytes = PdfHelper.GenerateTracePdf(batch.TraceID)
                'Response.Clear()
                'Response.ContentType = "application/pdf"
                'Response.AddHeader("content-disposition", $"attachment;filename={batch.TraceID}.pdf")
                'Response.OutputStream.Write(pdfBytes, 0, pdfBytes.Length)
                'Response.Flush()

                ''qtyOut:=finalQtyOut,
                'Return Json(New With {
                '    .success = True,
                '    .redirectUrl = Url.Action("FinalProcess", "Process")
                '})
                ' ✅ Generate PDF
                'Dim pdfBytes = PdfHelper.GenerateTracePdf(batch.TraceID)

                '' Return PDF as file download
                'Return File(pdfBytes, "application/pdf", $"{batch.TraceID}.pdf")

                Return Json(New With {
                .success = True,
                .isFinal = True,
                .traceId = batch.TraceID
            })
            Else
                ' 🟢 NON-FINAL BUFFER
                DbHelper.CompleteBufferRejectOnly(
                    logId:=log.ID,
                    batch:=batch,
                    qtyReject:=qtyReject
                )

                ''DbHelper.RegisterNextProcess(batch, process, log.OperatorID)
                'Dim scannedProcessId = Convert.ToInt32(Request("scannedProcessId"))
                'Dim scannedOperatorId = Request("scannedOperatorId").ToString()

                'If scannedProcessId > 0 Then
                    Dim scannedProcess = DbHelper.GetProcessById(scannedProcessId)
                '    If scannedProcess IsNot Nothing Then
                DbHelper.RegisterNextProcess(batch, scannedProcess, scannedOperatorId)
                'End If
                'End If


                Return Json(New With {
                    .success = True,
                    .isFinal = False,
                    .redirectUrl = Url.Action("ProcessBatch", "Process", New With {.traceId = batch.TraceID})
                })
            End If

        Catch ex As Exception
            Return Json(New With {.success = False, .message = ex.Message})
        End Try
    End Function

    '=======================
    ' PDF Download Action
    '=======================
    Public Function CheckPdfStatus(traceId As String) As JsonResult
        Dim printedDate As Object = Nothing

        Using conn As New SqlConnection(DbHelper.GetConnectionString("BatchDB"))
            conn.Open()
            Dim cmd As New SqlCommand("
            SELECT printed_date
            FROM pp_trace_route
            WHERE trace_id = @TraceID
        ", conn)
            cmd.Parameters.AddWithValue("@TraceID", traceId)
            printedDate = cmd.ExecuteScalar()
        End Using

        Dim alreadyPrinted As Boolean =
        printedDate IsNot Nothing AndAlso printedDate IsNot DBNull.Value

        Return Json(New With {
        .alreadyPrinted = alreadyPrinted
    }, JsonRequestBehavior.AllowGet)
    End Function

    Public Function DownloadTracePdf(traceId As String, Optional forceNew As Boolean = False) As ActionResult
        Dim folderPath As String = "C:\Temp\TracePdfs\"

        ' ✅ Create folder if not exist
        If Not Directory.Exists(folderPath) Then
            Directory.CreateDirectory(folderPath)
        End If

        Dim pdfPath As String = Path.Combine(folderPath, traceId & ".pdf")
        Dim pdfBytes() As Byte

        ' Check if PDF exists
        If Not forceNew AndAlso System.IO.File.Exists(pdfPath) Then
            ' Serve existing PDF
            pdfBytes = System.IO.File.ReadAllBytes(pdfPath)
        Else
            ' Generate new PDF
            pdfBytes = PdfHelper.GenerateTracePdf(traceId)
            System.IO.File.WriteAllBytes(pdfPath, pdfBytes)

            Dim today As Date = TimeProvider.Now

            ' Update printed_date ONLY when new PDF is created
            Using conn As New SqlConnection(DbHelper.GetConnectionString("BatchDB"))
                conn.Open()
                Dim cmd As New SqlCommand("
                UPDATE pp_trace_route
                SET printed_date = @Today
                WHERE trace_id = @TraceID
            ", conn)
                cmd.Parameters.AddWithValue("@Today", today)
                cmd.Parameters.AddWithValue("@TraceID", traceId)
                cmd.ExecuteNonQuery()
            End Using
        End If
        Return File(pdfBytes, "application/pdf", traceId & ".pdf")
    End Function

    Public Function OpenExistingPdf(traceId As String) As ActionResult
        Dim folderPath As String = "C:\Temp\TracePdfs\"
        Dim pdfPath As String = Path.Combine(folderPath, traceId & ".pdf")
        If Not System.IO.File.Exists(pdfPath) Then
            Return HttpNotFound("PDF not found")
        End If
        Dim bytes = System.IO.File.ReadAllBytes(pdfPath)
        Return File(bytes, "application/pdf")
    End Function

    Private Function LoadBatch(traceId As String) As Batch
        Dim batch As Batch = Nothing

        Using conn As New SqlConnection(DbHelper.GetConnectionString("BatchDB"))
            conn.Open()
            Dim cmd As New SqlCommand("SELECT * FROM pp_trace_route WHERE trace_id=@TraceID", conn)
            cmd.Parameters.AddWithValue("@TraceID", traceId)
            Using reader = cmd.ExecuteReader()
                If reader.Read() Then
                    batch = New Batch With {
                        .TraceID = reader("trace_id").ToString(),
                        .Die = reader("die").ToString(),
                        .Line = reader("line").ToString(),
                        .OperatorID = reader("operator_id").ToString(),
                        .CreatedDate = Convert.ToDateTime(reader("created_date")),
                        .Shift = reader("shift").ToString(),
                        .Model = reader("model_name").ToString(),
                        .PartCode = reader("part_code").ToString(),
                        .BaraCoreLot = reader("bara_core_lot").ToString(),
                        .BaraCoreDate = Convert.ToDateTime(reader("bara_core_date"))
                    }
                End If
            End Using
        End Using
        Return batch
    End Function

    Function ProcessQR() As ActionResult
        Dim processList As New List(Of Dictionary(Of String, String))()

        Using conn As New SqlConnection(DbHelper.GetConnectionString("BatchDB"))
            conn.Open()
            Dim cmd As New SqlCommand("SELECT * FROM pp_master_process", conn)
            Dim reader = cmd.ExecuteReader()

            While reader.Read()
                Dim process As New Dictionary(Of String, String)()
                process("ProcessID") = reader("id").ToString()
                process("ProcessCode") = reader("proc_code").ToString()
                process("ProcessName") = reader("proc_name").ToString()
                process("ProcessLevel") = reader("proc_level").ToString()
                process("ProcFlowId") = reader("proc_flow_id").ToString()

                Dim qrData As String = reader("proc_code").ToString()

                ' Generate QR Code as Base64
                Using qrGenerator As New QRCodeGenerator()
                    Using qrDataObj = qrGenerator.CreateQrCode(qrData, QRCodeGenerator.ECCLevel.Q)
                        Using qrCode = New QRCode(qrDataObj)
                            Using bitmap = qrCode.GetGraphic(20)
                                Using ms As New MemoryStream()
                                    bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png)
                                    process("QRCodeImage") = Convert.ToBase64String(ms.ToArray())
                                End Using
                            End Using
                        End Using
                    End Using
                End Using

                processList.Add(process)
            End While
        End Using

        ViewData("ProcessList") = processList
        Return View()
    End Function

    ' GET: /Process/Detail/1
    Public Function Detail(processId As String) As ActionResult
        Dim process As Dictionary(Of String, String) = Nothing

        Using conn As New SqlConnection(DbHelper.GetConnectionString("BatchDB"))
            conn.Open()
            Dim cmd As New SqlCommand("SELECT * FROM pp_master_process WHERE ID=@ID", conn)
            cmd.Parameters.AddWithValue("@ID", processId)
            Using reader = cmd.ExecuteReader()
                If reader.Read() Then
                    process = New Dictionary(Of String, String)()
                    process("ProcessID") = reader("id").ToString()
                    process("ProcessCode") = reader("proc_code").ToString()
                    process("ProcessName") = reader("proc_name").ToString()
                    process("ProcessLevel") = reader("proc_level").ToString()
                    process("ProcFlowId") = reader("proc_flow_id").ToString()

                    Dim qrData As String = reader("proc_code").ToString()

                    Using qrGenerator As New QRCoder.QRCodeGenerator()
                        Using qrDataObj = qrGenerator.CreateQrCode(qrData, QRCoder.QRCodeGenerator.ECCLevel.Q)
                            Using qrCode = New QRCoder.QRCode(qrDataObj)
                                Using bitmap = qrCode.GetGraphic(20)
                                    Using ms As New IO.MemoryStream()
                                        bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png)
                                        process("QRCodeImage") = Convert.ToBase64String(ms.ToArray())
                                    End Using
                                End Using
                            End Using
                        End Using
                    End Using
                End If
            End Using
        End Using

        If process Is Nothing Then
            Return Content("Process not found.")
        End If

        ViewData("Process") = process
        Return View()
    End Function

    Function FinalProcess() As ActionResult
        Dim FinalList As New List(Of Dictionary(Of String, Object))()

        Using conn As New SqlConnection(DbHelper.GetConnectionString("BatchDB"))
            conn.Open()
            Dim cmd As New SqlCommand("SELECT tr.trace_id, tr.model_name, mm.part_desc, tr.initial_qty,
                tr.current_qty, tr.last_proc_code, tr.status, tp.process_id
                FROM pp_trace_route tr 
                JOIN pp_master_material mm ON tr.part_code = mm.upper_item 
                JOIN pp_trace_processes tp ON tr.trace_id = tp.trace_id
                WHERE tr.last_proc_code LIKE 'INS%' AND tr.status = 'ONGOING' AND tp.status = 'IN PROGRESS'", conn)

            Using reader = cmd.ExecuteReader()
                While reader.Read()
                    Dim finalDict As New Dictionary(Of String, Object)
                    finalDict("TraceID") = reader("trace_id").ToString()
                    finalDict("ModelName") = reader("model_name").ToString()
                    finalDict("PartCode") = reader("part_desc").ToString()
                    finalDict("InitQty") = Convert.ToInt64(reader("initial_qty"))
                    finalDict("CurQty") = Convert.ToInt64(reader("current_qty"))
                    finalDict("LastProcCode") = reader("last_proc_code").ToString()
                    finalDict("Status") = reader("status").ToString()
                    finalDict("ProcessID") = Convert.ToInt64(reader("process_id"))

                    FinalList.Add(finalDict)
                End While
            End Using
        End Using
        ViewData("FinalList") = FinalList
        Return View()
    End Function

    ' POST: Complete process from button click
    <HttpPost>
    Public Function CompleteFinal(traceId As String, procId As Integer) As ActionResult

        ' 1️⃣ Get batch & log
        Dim batch = DbHelper.GetBatchByTraceID(traceId)
        If batch Is Nothing Then
            TempData("Error") = "Batch not found."
            Return RedirectToAction("FinalProcess")
        End If

        Dim log = DbHelper.GetProcessLogsByTraceID(traceId) _
            .FirstOrDefault(Function(l) l.ProcessID = procId)

        If log Is Nothing Then
            TempData("Error") = "Process log not found."
            Return RedirectToAction("FinalProcess")
        End If

        ' 2️⃣ Force status to Pending Completion (buffer required)
        If log.Status = "In Progress" Then
            DbHelper.UpdateProcessLogStatus(log.ID, "Pending Completion")
        End If

        ' 3️⃣ Redirect to buffer page
        Return RedirectToAction(
            "ProcessBuffer",
            "Process",
            New With {.traceId = traceId, .procId = procId}
        )

    End Function

End Class
