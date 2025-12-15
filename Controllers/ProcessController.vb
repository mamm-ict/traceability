Imports System.Data.SqlClient
Imports System.IO
Imports System.Web.Mvc
Imports Newtonsoft.Json
Imports QRCoder

Public Class ProcessController
    Inherits Controller

    Public Function ProcessMaster() As ActionResult
        ' Fetch processes from DB
        Dim processList As New List(Of Dictionary(Of String, String))()

        Using conn As New SqlConnection(DbHelper.GetConnectionString())
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
                    process("UpperItem") = reader("upper_item").ToString()
                    process("BufferFlag") = Convert.ToInt64(reader("buffer_flag"))

                    ' Pipe-delimited QR content
                    Dim qrData As String = String.Format("{0}|{1}|{2}|{3}|{4}",
                    reader("id").ToString(),
                    reader("proc_code").ToString(),
                    reader("proc_name").ToString(),
                    reader("proc_level").ToString(),
                    reader("proc_flow_id").ToString())

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

    ' GET: /Process/Start
    Public Function StartProcess() As ActionResult
        ' You can optionally show last scanned batch status here
        ViewData("StatusMessage") = "Batch not scanned yet."
        Return View()
    End Function

    ' POST: /Process/Start
    <HttpPost>
    Public Function StartProcess(scanInput As String) As ActionResult
        If String.IsNullOrWhiteSpace(scanInput) Then
            ViewData("StatusMessage") = "Invalid QR. Try again."
            Return View()
        End If

        Dim traceId As String = scanInput.Trim()

        ' STRICT TraceID validation
        Dim traceIdPattern As String = "^[A-Z]{3}-\d{8}-\d{3}$"

        If Not System.Text.RegularExpressions.Regex.IsMatch(traceId, traceIdPattern) Then
            ViewData("StatusMessage") = "QR format invalid."
            Return View()
        End If

        ' Parse raw materials
        'Dim rawMaterials As New List(Of RawMaterialEntry)
        'For Each item In rawMatPart.Split(","c)
        '    Dim kv = item.Split(":"c)
        '    If kv.Length = 2 Then
        '        rawMaterials.Add(New RawMaterialEntry With {
        '        .Name = kv(0).Trim(),
        '        .Quantity = Convert.ToDecimal(kv(1).Trim())
        '    })
        '    End If
        'Next

        ' Buat batch object sementara
        Dim batch As New Batch With {
            .TraceID = traceId
        } '    .Line = Line,
        '    .OperatorID = OperatorID,
        '    .CreatedDate = Convert.ToDateTime(dateStr),
        '    .Shift = shift,
        '    .Model = model

        ' Load previous logs dari database
        Dim logs As List(Of ProcessLog) = DbHelper.GetProcessLogsByTraceID(batch.TraceID)

        ViewData("Batch") = batch
        ViewData("Processes") = DbHelper.GetAllProcesses()
        ViewData("Logs") = logs.OrderBy(Function(l) l.scan_time).ToList()

        Return View("~/Views/Process/ProcessBatch.vbhtml")
    End Function


    ' GET: /Process/ProcessBatch
    Public Function ProcessBatch(TraceID As String) As ActionResult
        Dim batch As Batch = LoadBatch(TraceID)
        If batch Is Nothing Then
            Return Content("Batch not found.")
        End If

        Dim processes As List(Of ProcessMaster) = DbHelper.GetAllProcesses()

        ' Ambil logs berdasarkan TraceID penuh
        Dim logs As List(Of ProcessLog) = DbHelper.GetProcessLogsByTraceID(TraceID)

        ViewData("Batch") = batch
        ViewData("Processes") = processes
        ViewData("Logs") = logs.OrderBy(Function(l) l.scan_time).ToList()
        Return View()
    End Function

    ' POST: /Process/StartScanBatch
    '<HttpPost>
    'Public Function StartScanBatch(scanInput As String) As ActionResult
    '    ' Parse batch QR
    '    Dim parts = scanInput.Split("|"c)
    '    If parts.Length < 8 Then
    '        ViewData("StatusMessage") = "QR format invalid."
    '        Return View("~/Views/Process/StartProcess.vbhtml")
    '    End If

    '    Dim traceId = parts(0).Trim()
    '    ' Load batch dari DB
    '    Dim batch As Batch = LoadBatch(traceId)
    '    If batch Is Nothing Then
    '        Return Content("Batch not found.")
    '    End If

    '    ViewData("Batch") = batch
    '    ViewData("Processes") = DbHelper.GetAllProcesses()
    '    Return View("~/Views/Process/ProcessBatch.vbhtml")
    'End Function

    ' POST: /Process/ScanProcess
    <HttpPost>
    Public Function ScanProcess(traceId As String, processData As String, OperatorID As String) As ActionResult
        ' -----------------------------
        ' 1️⃣ Load batch
        ' -----------------------------
        Dim batch As Batch = LoadBatch(traceId)
        If batch Is Nothing Then
            Return Content("Batch not found.")
        End If

        Dim processes = DbHelper.GetAllProcesses()
        Dim logs = DbHelper.GetProcessLogsByTraceID(traceId)

        ' -----------------------------
        ' 2️⃣ Parse scanned process QR
        ' -----------------------------
        Dim parts = processData.Split("|"c)
        If parts.Length < 5 Then
            ViewData("ErrorMessage") = "Invalid Process QR"
            GoTo ShowView
        End If

        Dim processId As Integer = Convert.ToInt32(parts(0))
        Dim scannedProcess = processes.FirstOrDefault(Function(p) p.ID = processId)
        If scannedProcess Is Nothing Then
            ViewData("ErrorMessage") = "Process not found in master."
            GoTo ShowView
        End If

        ' -----------------------------
        ' 3️⃣ Determine current batch level
        ' -----------------------------
        Dim alreadyScanned = logs.Any(Function(l)
                                          Dim proc = processes.FirstOrDefault(Function(p) p.ID = l.ProcessID)
                                          Return proc IsNot Nothing AndAlso proc.Level = scannedProcess.Level
                                      End Function)

        If alreadyScanned Then
            ViewData("ErrorMessage") = $"Process {scannedProcess.Name} at Level {scannedProcess.Level} already scanned for this batch."
            GoTo ShowView
        End If
        ' -----------------------------
        ' 4️⃣ Prevent skip levels
        ' -----------------------------
        Dim lastLevel As Integer = If(logs.Count = 0, 0, logs.Max(Function(l) processes.First(Function(p) p.ID = l.ProcessID).Level))
        If scannedProcess.Level > lastLevel + 1 Then
            ViewData("ErrorMessage") = $"Cannot scan {scannedProcess.Name} yet. Complete previous level(s) first."
            GoTo ShowView
        End If

        ' -----------------------------
        ' 5️⃣ Check duplicate: same level + same prefix
        ' -----------------------------
        'Dim duplicateExists = logs.Any(Function(l)
        '                                   Dim proc = processes.FirstOrDefault(Function(p) p.ID = l.ProcessID)
        '                                   Return proc IsNot Nothing AndAlso
        '                                     proc.Level = scannedProcess.Level AndAlso
        '                                     proc.Name.Substring(0, 3) = scannedProcess.Name.Substring(0, 3)
        '                               End Function)
        'If duplicateExists Then
        '    ViewData("StatusMessage") = $"Cannot scan {scannedProcess.Name}. Same level process with same prefix already scanned."
        '    GoTo ShowView
        'End If

        ' -----------------------------
        ' 6️⃣ Auto-complete previous In Progress processes with lower level
        ' -----------------------------
        For Each prevLog In logs.Where(Function(l) l.Status = "In Progress")
            Dim prevProcess = processes.FirstOrDefault(Function(p) p.ID = prevLog.ProcessID)
            If prevProcess IsNot Nothing AndAlso prevProcess.Level < scannedProcess.Level Then
                DbHelper.UpdateProcessLogStatus(prevLog.ID, "Completed")
            End If
        Next

        ' -----------------------------
        ' 7️⃣ Log current process as In Progress
        ' -----------------------------
        DbHelper.LogBatchProcess(traceId, scannedProcess.ID, OperatorID, "In Progress")
        ViewData("StatusMessage") = $"Batch {traceId} scanned for process {scannedProcess.Name}."

ShowView:
        ' -----------------------------
        ' 8️⃣ Refresh view data
        ' -----------------------------
        ViewData("Batch") = batch
        ViewData("Processes") = processes
        ViewData("Logs") = DbHelper.GetProcessLogsByTraceID(traceId)
        ' Recalculate enableRawMaterial
        Dim triggerLevels As Integer() = {1, 3}
        Dim enableRawMaterial As Boolean = logs.Any(Function(l)
                                                        Dim proc = processes.FirstOrDefault(Function(p) p.ID = l.ProcessID)
                                                        Return proc IsNot Nothing AndAlso triggerLevels.Contains(proc.Level)
                                                    End Function)
        ViewData("EnableRawMaterial") = enableRawMaterial
        Return View("~/Views/Process/ProcessBatch.vbhtml")
    End Function

    Private Function LoadBatch(traceId As String) As Batch
        Dim batch As Batch = Nothing
        Using conn As New SqlConnection(DbHelper.GetConnectionString())
            conn.Open()
            Dim cmd As New SqlCommand("SELECT * FROM Batch WHERE trace_id=@TraceID", conn)
            cmd.Parameters.AddWithValue("@TraceID", traceId)
            Using reader = cmd.ExecuteReader()
                If reader.Read() Then
                    'Dim rawList = JsonConvert.DeserializeObject(Of List(Of RawMaterialEntry))(reader("RawMaterial").ToString())
                    batch = New Batch With {
                        .TraceID = reader("trace_id").ToString(),
                        .Line = reader("line").ToString(),
                        .OperatorID = reader("operator_id").ToString(),
                        .CreatedDate = Convert.ToDateTime(reader("created_date")),
                        .Shift = reader("shift").ToString(),
                        .Model = reader("model_name").ToString()
                    }
                End If
            End Using
        End Using
        Return batch
    End Function

    Function ProcessQR() As ActionResult
        Dim processList As New List(Of Dictionary(Of String, String))()

        Using conn As New SqlConnection(DbHelper.GetConnectionString())
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

                ' Pipe-delimited QR content
                Dim qrData As String = String.Format("{0}|{1}|{2}|{3}|{4}",
                    reader("id").ToString(),
                    reader("proc_code").ToString(),
                    reader("proc_name").ToString(),
                    reader("proc_level").ToString(),
                    reader("proc_flow_id").ToString())

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

        Using conn As New SqlConnection(DbHelper.GetConnectionString())
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

                    ' Pipe-delimited QR content
                    Dim qrData As String = String.Format("{0}|{1}|{2}|{3}|{4}",
                    reader("id").ToString(),
                    reader("proc_code").ToString(),
                    reader("proc_name").ToString(),
                    reader("proc_level").ToString(),
                    reader("proc_flow_id").ToString())

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
