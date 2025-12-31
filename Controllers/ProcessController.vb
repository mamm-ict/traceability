Imports System.Data.SqlClient
Imports System.IO
Imports System.Web.Mvc
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

        Dim scannedProcess = processes.FirstOrDefault(Function(p) p.Code = processQr.ToUpper())
        If scannedProcess Is Nothing Then
            ViewData("StatusMessage") = "Invalid process QR."
            Return View("~/Views/Process/StartProcess.vbhtml")
        End If

        Dim pendingBufferLog = logs.FirstOrDefault(Function(l) l.Status = "Pending Completion")
        If pendingBufferLog IsNot Nothing Then
            ' Redirect to ProcessBuffer page for pending process
            Return RedirectToAction("ProcessBuffer", New With {.traceId = traceId, .procId = pendingBufferLog.ProcessID})
        End If

        ' 3️⃣ Check process sequence & buffer/material (sama macam sebelum)
        Dim activeLogs = logs.Where(Function(l) l.Status = "In Progress").ToList()
        Dim progressLogs = logs.Where(Function(l) l.Status = "In Progress" OrElse l.Status = "Completed").ToList()

        If activeLogs.Any(Function(l) l.ProcessID = scannedProcess.ID) Then
            ViewData("StatusMessage") = $"Process {scannedProcess.Name} already in progress."
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
                ViewData("StatusMessage") = $"Material not scanned for {proc.Name}."
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
            Return RedirectToAction("ProcessBuffer", New With {.traceId = traceId, .procId = pendingBuffers.First().ProcessID})
        End If


        ' 5️⃣ Log current process FIRST
        DbHelper.LogBatchProcess(
            traceId:=traceId,
            processId:=scannedProcess.ID,
            operatorId:=operatorId,
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
            Return RedirectToAction("ProcessBuffer", "Process", New With {.traceId = traceId, .procId = scannedProcess.ID})

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

    Public Function ProcessBuffer(traceId As String, procId As Integer) As ActionResult
        If String.IsNullOrEmpty(traceId) Then
            Return RedirectToAction("StartProcess")
        End If

        Dim batch As Batch = LoadBatch(traceId)
        If batch Is Nothing Then
            Return Content("Batch not found")
        End If

        Dim processes As List(Of ProcessMaster) = DbHelper.GetAllProcesses()
        Dim logs As List(Of ProcessLog) = DbHelper.GetProcessLogsByTraceID(traceId)

        Dim pendingLog = logs.Where(Function(l) l.Status = "Pending Completion") _
            .OrderBy(Function(l) l.ScanTime) _
            .FirstOrDefault()

        ViewData("Batch") = batch
        ViewData("Processes") = processes
        ViewData("Logs") = logs

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

        Using conn As New SqlConnection(DbHelper.GetConnectionString("EmpDB"))
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
            AND CAST(created_date AS DATE) = CAST(GETDATE() AS DATE);
        ", conn)
            cmd.Parameters.AddWithValue("@ControlNo", controlNo)
            Dim traceId = cmd.ExecuteScalar()
            If traceId Is Nothing Then
                Return Json(New With {.success = False, .message = "Process not found."})
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

        ' Ambil logs berdasarkan TraceID penuh
        Dim logs As List(Of ProcessLog) = DbHelper.GetProcessLogsByTraceID(TraceID)

        ViewData("Batch") = batch
        ViewData("Processes") = processes
        ViewData("Logs") = logs.OrderBy(Function(l) l.ScanTime).ToList()
        Return View()
    End Function

    <HttpPost>
    Public Function SubmitBuffer(traceId As String, processId As Integer, operatorId As String, qtyOut As Integer, qtyReject As Integer) As ActionResult

        Try
            ' 1️⃣ Get batch and current process
            Dim batch = DbHelper.GetBatchByTraceID(traceId)
            If batch Is Nothing Then
                Return Json(New With {.success = False, .message = "Batch not found"})
            End If

            Dim currentProcess = DbHelper.GetProcessById(processId)
            If currentProcess Is Nothing Then
                Return Json(New With {.success = False, .message = "Process not found"})
            End If

            ' 2️⃣ Complete current process (update qty, status)
            DbHelper.CompleteProcessLog(
            logId:=DbHelper.GetProcessLogsByTraceID(traceId) _
                        .FirstOrDefault(Function(l) l.ProcessID = processId AndAlso l.Status = "In Progress").ID,
            batch:=batch,
            qtyOut:=qtyOut,
            qtyReject:=qtyReject)
            ',operatorId:=operatorId


            ' 3️⃣ Auto-register next process
            DbHelper.RegisterNextProcess(batch, currentProcess, operatorId)

            ' 4️⃣ Return success
            Return Json(New With {.success = True, .message = "Buffer submitted and next process registered"})

        Catch ex As Exception
            Return Json(New With {.success = False, .message = ex.Message})
        End Try

    End Function

    <HttpPost>
    Public Function ScanMaterial(model As MaterialLog) As ActionResult
        Try
            ' 🔐 SAFETY CHECK
            If model Is Nothing Then
                Return Content("Invalid payload")
            End If

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
    Public Function DeleteTraceMaterial(id As Integer) As ActionResult
        Try
            DbHelper.DeleteTraceMaterial(id)
            Return Content("OK")
        Catch ex As Exception
            Return Content(ex.Message)
        End Try
    End Function

    <HttpPost>
    Public Function CompleteBuffer(logId As Integer, qtyReject As Integer, qtyOut As Integer) As ActionResult
        ' ambil process log
        Dim log = DbHelper.GetProcessLogById(logId)
        If log Is Nothing Then
            Return Content("Invalid log.")
        End If

        ' 🔒 hanya pending completion boleh complete
        If log.Status <> "Pending Completion" Then
            Return Content("Process not ready for completion.")
        End If

        ' ambil batch (trace)
        Dim batch = DbHelper.GetBatchByTraceID(log.TraceID)
        If batch Is Nothing Then
            Return Content("Invalid batch.")
        End If

        ' ambil process master (untuk process code)
        Dim process = DbHelper.GetProcessById(log.ProcessID)
        If process Is Nothing Then
            Return Content("Invalid process.")
        End If

        Try
            DbHelper.CompleteProcessLog(
            logId:=logId,
            batch:=batch,
            qtyOut:=qtyOut,
            qtyReject:=qtyReject)
            'processCode:=process.Code

            Dim currentProcess = DbHelper.GetProcessById(log.ProcessID)
            DbHelper.RegisterNextProcess(batch, currentProcess, log.OperatorID)

            Dim logs = DbHelper.GetProcessLogsByTraceID(batch.TraceID)
            Dim pendingBuffer = logs.Any(Function(l) l.Status = "Pending Completion")

            If pendingBuffer Then
                Return RedirectToAction("ProcessBuffer", New With {.traceId = batch.TraceID, .procId = log.ProcessID})
            End If

            Return Content("OK")

        Catch ex As Exception
            Return Content(ex.Message)
        End Try

        Return RedirectToAction("ProcessBatch", New With {.traceId = batch.TraceID})
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

End Class
