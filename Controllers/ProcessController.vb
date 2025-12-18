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

        Dim batch As New Batch With {
            .TraceID = traceId
        }

        ' Load previous logs dari database
        Dim logs As List(Of ProcessLog) = DbHelper.GetProcessLogsByTraceID(batch.TraceID)

        ViewData("Batch") = LoadBatch(traceId)
        ViewData("Processes") = DbHelper.GetAllProcesses()
        ViewData("Logs") = logs.OrderBy(Function(l) l.ScanTime).ToList()

        Return View("~/Views/Process/ProcessBatch.vbhtml")
    End Function

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
        Dim batch As Batch = LoadBatch(TraceID)
        If batch Is Nothing Then
            Return Content("Batch not found.")
        End If

        Dim processes As List(Of ProcessMaster) = DbHelper.GetAllProcesses()

        ' Ambil logs berdasarkan TraceID penuh
        Dim logs As List(Of ProcessLog) = DbHelper.GetProcessLogsByTraceID(TraceID)

        ViewData("Batch") = batch
        ViewData("Processes") = processes
        ViewData("Logs") = logs.OrderBy(Function(l) l.ScanTime).ToList()
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
    Public Function ScanProcess(traceId As String,
                            processQr As String,
                            operatorId As String) As ActionResult

        Dim batch = DbHelper.GetBatchByTraceID(traceId)
        Dim processes = DbHelper.GetAllProcesses()
        Dim logs = DbHelper.GetProcessLogsByTraceID(traceId)

        If batch Is Nothing Then
            Return RenderError("Invalid batch.", batch, processes, logs)
        End If
        ' -----------------------------
        ' 1️⃣ Resolve scanned process
        ' -----------------------------
        Dim scannedProcess =
        processes.FirstOrDefault(Function(p) p.Code = processQr)

        If scannedProcess Is Nothing Then
            Return RenderError("Invalid process QR.", batch, processes, logs)
        End If

        ' -----------------------------
        ' 2️⃣ Get active In Progress logs
        ' -----------------------------
        Dim activeLogs =
    logs.Where(Function(l) l.Status = "In Progress").ToList()

        Dim progressLogs =
            logs.Where(Function(l) l.Status = "In Progress" OrElse l.Status = "Completed").ToList()

        If activeLogs.Any(Function(l)
                              Dim p = processes.First(Function(x) x.ID = l.ProcessID)
                              Return p.Level = scannedProcess.Level
                          End Function) Then

            Return RenderError(
            $"Process level {scannedProcess.Level} already active.",
            batch, processes, logs
        )
        End If
        ' -----------------------------
        ' 3️⃣ Enforce NO SKIP LEVEL
        ' -----------------------------
        If progressLogs.Any() Then

            Dim maxLevel =
            progressLogs.Max(Function(l)
                                 Dim p = processes.First(Function(x) x.ID = l.ProcessID)
                                 Return p.Level
                             End Function)

            If scannedProcess.Level < maxLevel Then
                Return RenderError(
                "Cannot scan lower level process.",
                batch, processes, logs
            )
            End If

            If scannedProcess.Level > maxLevel + 1 Then
                Return RenderError(
                $"Cannot skip process level. Expected level {maxLevel + 1}.",
                batch, processes, logs
            )
            End If
        End If
        '    -----------------------------
        ' Material check (CURRENT process)
        ' -----------------------------
        'If scannedProcess.MaterialFlag = 1 Then
        '    If Not DbHelper.HasMaterialForProcess(traceId, scannedProcess.ID) Then
        '        Return RenderError(
        '        $"Material not scanned for {scannedProcess.Name}.",
        '        batch, processes, logs
        '    )
        '    End If
        'End If
        ' -----------------------------
        ' 4️⃣ COMPLETE lower level processes (with checks)
        ' -----------------------------
        For Each log In activeLogs

            Dim proc =
            processes.FirstOrDefault(Function(p) p.ID = log.ProcessID)

            If proc Is Nothing OrElse proc.Level >= scannedProcess.Level Then
                Continue For
            End If

            ' Material mandatory
            If proc.MaterialFlag = 1 AndAlso
           Not DbHelper.HasMaterialForProcess(traceId, proc.ID) Then

                Return RenderError(
                $"Material not scanned for {proc.Name}.",
                batch, processes, logs
            )
            End If

            ' Buffer mandatory
            If proc.BufferFlag = 1 AndAlso
   log.QtyOut = 0 AndAlso log.QtyReject = 0 Then

                ' 🚦 TANDA PERLU INPUT BUFFER
                DbHelper.UpdateProcessLogStatus(log.ID, "Pending Completion")

                ViewData("ErrorMessage") =
        $"Please enter buffer quantity for {proc.Name} before proceeding."

                Return RenderView(batch, processes, traceId)
            End If


            DbHelper.UpdateProcessLogStatus(log.ID, "Completed")
        Next

        ' -----------------------------
        ' 5️⃣ Prevent duplicate In Progress same process
        ' -----------------------------
        ' LOGIC ERROR SINI - PWT ALREADY COMPLETE SO CAN SCAN 2X. NEED TO AUTOMATICALLY REGISTER NEXT PROCESS AFTER ADD BUFFER
        Dim allowSameProcessScanForTesting As Boolean = True

        If Not allowSameProcessScanForTesting Then
            If activeLogs.Any(Function(l) l.ProcessID = scannedProcess.ID) Then
                Return RenderError(
                $"Process {scannedProcess.Name} already in progress.",
                batch, processes, logs
            )
            End If
        End If
        ' -----------------------------
        ' 6️⃣ Log current process
        ' -----------------------------
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

        ViewData("StatusMessage") =
        $"Batch {traceId} scanned for process {scannedProcess.Name}."

        Return RenderView(batch, processes, traceId)
    End Function
    Private Function RenderError(
    message As String,
    batch As Batch,
    processes As List(Of ProcessMaster),
    logs As List(Of ProcessLog)
) As ActionResult

        ViewData("ErrorMessage") = message
        ViewData("Batch") = batch
        ViewData("Processes") = processes
        ViewData("Logs") = logs

        Return View("~/Views/Process/ProcessBatch.vbhtml")
    End Function
    Private Function RenderView(
    batch As Batch,
    processes As List(Of ProcessMaster),
    traceId As String
) As ActionResult

        ViewData("Batch") = batch
        ViewData("Processes") = processes
        ViewData("Logs") = DbHelper.GetProcessLogsByTraceID(traceId)

        Return View("~/Views/Process/ProcessBatch.vbhtml")
    End Function

    '    <HttpPost>
    '    Public Function UpdateRawMaterials(
    '    traceId As String,
    '    RawMaterialNames As List(Of String),
    '    Quantities As List(Of Integer)
    ') As ActionResult

    '        ' 1️⃣ Ambil batch / route (SOURCE OF TRUTH)
    '        Dim batch = DbHelper.GetBatchByTraceID(traceId)
    '        If batch Is Nothing Then
    '            Return Content("Batch not found.")
    '        End If

    '        ' ⚠️ WAJIB ADA PART CODE
    '        Dim partCode As String = batch.PartCode
    '        If String.IsNullOrEmpty(partCode) Then
    '            Return Content("Part code not found for batch.")
    '        End If

    '        ' 2️⃣ Ambil active process (material attach pada process aktif)
    '        Dim logs = DbHelper.GetProcessLogsByTraceID(traceId)
    '        Dim activeLog = logs.
    '        Where(Function(l) l.Status = "In Progress").
    '        OrderByDescending(Function(l) l.ScanTime).
    '        FirstOrDefault()

    '        If activeLog Is Nothing Then
    '            Return Content("No active process.")
    '        End If

    '        Dim procId As Integer = activeLog.ProcessID

    '        ' 3️⃣ Loop setiap material (1 QR = 1 row, 2 QR = 2 row)
    '        For i As Integer = 0 To RawMaterialNames.Count - 1

    '            If String.IsNullOrWhiteSpace(RawMaterialNames(i)) Then Continue For
    '            If Quantities(i) <= 0 Then Continue For

    '            ' 🔹 QR material kau (TAB separated)
    '            ' lower_material<TAB>batch_lot<TAB>uom<TAB>vendor_code<TAB>vendor_lot
    '            Dim parts = RawMaterialNames(i).Split(ControlChars.Tab)

    '            If parts.Length <> 6 Then
    '                Return Content("Invalid material QR format.")
    '            End If

    '            Dim lowerMaterial As String = parts(0).Trim()
    '            Dim batchLot As String = parts(1).Trim()
    '            Dim usageQty As Integer = parts(2).Trim()
    '            Dim uom As String = parts(3).Trim()
    '            Dim vendorCode As String = parts(4).Trim()
    '            Dim vendorLot As String = parts(5).Trim()

    '            DbHelper.InsertTraceMaterial(
    '            traceId:=traceId,
    '            procId:=procId,
    '            partCode:=partCode,
    '            lowerMaterial:=lowerMaterial,
    '            batchLot:=batchLot,
    '            usageQty:=usageQty,
    '            uom:=uom,
    '            vendorCode:=vendorCode,
    '            vendorLot:=vendorLot
    '        )
    '        Next

    '        Return RedirectToAction(
    '        "ProcessBatch",
    '        New With {.traceId = traceId}
    '    )
    '    End Function


    '<HttpPost>
    'Public Function ScanMaterial(model As MaterialLog) As ActionResult

    '    ' 🔐 BASIC VALIDATION
    '    If model Is Nothing Then
    '        Return Content("Invalid payload")
    '    End If

    '    If String.IsNullOrWhiteSpace(model.TraceID) Then
    '        Return Content("TraceID missing")
    '    End If

    '    If String.IsNullOrWhiteSpace(model.LowerMaterial) Then
    '        Return Content("Invalid material")
    '    End If

    '    ' 🔒 SOURCE OF TRUTH (DISYORKAN)
    '    ' Override PartCode dari DB supaya client tak boleh tipu
    '    Dim batch = DbHelper.GetBatchByTraceID(model.TraceID)
    '    If batch Is Nothing Then
    '        Return Content("Batch not found")
    '    End If

    '    model.PartCode = batch.PartCode

    '    If String.IsNullOrEmpty(model.PartCode) Then
    '        Return Content("PartCode not found for batch")
    '    End If

    '    ' 💾 INSERT TO DB
    '    Using conn As New SqlConnection(DbHelper.GetConnectionString())
    '        conn.Open()

    '        Dim cmd As New SqlCommand("
    '        INSERT INTO pp_trace_material
    '        (trace_id, proc_id, part_code, lower_material, batch_lot, usage_qty, uom, vendor_code, vendor_lot, created_date)
    '        VALUES
    '        (@TraceID, @ProcID, @PartCode, @LowerMat, @BatchLot, @Qty, @UOM, @Vendor, @VendorLot, GETDATE())
    '    ", conn)

    '        cmd.Parameters.AddWithValue("@TraceID", model.TraceID)
    '        cmd.Parameters.AddWithValue("@ProcID", model.ProcID)
    '        cmd.Parameters.AddWithValue("@PartCode", model.PartCode)
    '        cmd.Parameters.AddWithValue("@LowerMat", model.LowerMaterial)
    '        cmd.Parameters.AddWithValue("@BatchLot", model.BatchLot)
    '        cmd.Parameters.AddWithValue("@Qty", model.UsageQty)
    '        cmd.Parameters.AddWithValue("@UOM", model.UOM)
    '        cmd.Parameters.AddWithValue("@Vendor", model.VendorCode)
    '        cmd.Parameters.AddWithValue("@VendorLot", model.VendorLot)

    '        cmd.ExecuteNonQuery()
    '    End Using

    '    Return Content("OK")
    'End Function

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
    Public Function CompleteProcess(
    logId As Integer,
    qtyReject As Integer,
    qtyOut As Integer
) As ActionResult

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

        Catch ex As Exception
            Return Content(ex.Message)
        End Try

        Return RedirectToAction(
        "ProcessBatch",
        New With {.traceId = batch.TraceID}
    )

    End Function


    Private Function LoadBatch(traceId As String) As Batch
        Dim batch As Batch = Nothing
        Using conn As New SqlConnection(DbHelper.GetConnectionString())
            conn.Open()
            Dim cmd As New SqlCommand("SELECT * FROM pp_trace_route WHERE trace_id=@TraceID", conn)
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
                        .Model = reader("model_name").ToString(),
                        .PartCode = reader("part_code").ToString()
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
                'Dim qrData As String = String.Format("{0}|{1}|{2}|{3}|{4}",
                '    reader("id").ToString(),
                '    reader("proc_code").ToString(),
                '    reader("proc_name").ToString(),
                '    reader("proc_level").ToString(),
                '    reader("proc_flow_id").ToString())

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
                    'Dim qrData As String = String.Format("{0}|{1}|{2}|{3}|{4}",
                    'reader("id").ToString(),
                    'reader("proc_code").ToString(),
                    'reader("proc_name").ToString(),
                    'reader("proc_level").ToString(),
                    'reader("proc_flow_id").ToString())

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
