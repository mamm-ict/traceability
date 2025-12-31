'Imports System.Data.SqlClient
Imports System.Data.SqlClient
Imports System.Configuration

Public Class DbHelper
    Public Shared Function GetConnectionString(dbName As String) As String
        Dim conn = ConfigurationManager.ConnectionStrings(dbName)
        If conn Is Nothing Then
            Throw New Exception($"Connection string '{dbName}' not found in Web.config")
        End If
        Return conn.ConnectionString
    End Function


    ' Log batch masuk process
    Public Shared Sub LogBatchProcess(
    traceId As String,
    processId As Integer,
    operatorId As String,
    qtyIn As Integer,
    qtyOut As Integer,
    qtyReject As Integer,
    status As String)

        Using conn As New SqlConnection(GetConnectionString("BatchDB"))
            conn.Open()
            Dim cmd As New SqlCommand(
        "INSERT INTO pp_trace_processes
        (trace_id, process_id, scan_time, operator_id, qty_in, qty_out, qty_reject, status)
        VALUES
        (@TraceID, @ProcessID, @ScanTime, @OperatorID, @QtyIn, @QtyOut, @QtyReject, @Status)",
        conn)

            cmd.Parameters.AddWithValue("@TraceID", traceId)
            cmd.Parameters.AddWithValue("@ProcessID", processId)
            cmd.Parameters.AddWithValue("@ScanTime", DateTime.Now)
            cmd.Parameters.AddWithValue("@OperatorID", operatorId)
            cmd.Parameters.AddWithValue("@QtyIn", qtyIn)
            cmd.Parameters.AddWithValue("@QtyOut", qtyOut)
            cmd.Parameters.AddWithValue("@QtyReject", qtyReject)
            cmd.Parameters.AddWithValue("@Status", status)

            cmd.ExecuteNonQuery()
        End Using
    End Sub


    ' Ambil semua process master
    Public Shared Function GetAllProcesses() As List(Of ProcessMaster)
        Dim list As New List(Of ProcessMaster)()
        Using conn As New SqlConnection(GetConnectionString("BatchDB"))
            conn.Open()
            Dim cmd As New SqlCommand("SELECT * FROM pp_master_process", conn)
            Dim reader = cmd.ExecuteReader()
            While reader.Read()
                Dim p As New ProcessMaster With {
                .ID = Convert.ToInt32(reader("id")),
                .Code = reader("proc_code").ToString(),
                .Name = reader("proc_name").ToString(),
                .Level = Convert.ToInt32(reader("proc_level")),
                .MaterialFlag = Convert.ToInt32(reader("material_flag")),
                .BufferFlag = Convert.ToInt32(reader("buffer_flag"))
            }
                list.Add(p)
            End While
        End Using
        Return list
    End Function

    Public Shared Function GetPartMasters() As List(Of MaterialMaster)
        Dim list As New List(Of MaterialMaster)

        Using conn As New SqlConnection(GetConnectionString("BatchDB"))
            conn.Open()

            Dim cmd As New SqlCommand("
             SELECT  DISTINCT part_desc,
                part_code
            FROM pp_master_material
            WHERE part_code IS NOT NULL
            ORDER BY part_desc
        ", conn)

            Using rdr = cmd.ExecuteReader()
                While rdr.Read()
                    list.Add(New MaterialMaster With {
                    .PartCode = rdr("part_code").ToString(),
                    .PartDesc = rdr("part_desc").ToString()
                })
                End While
            End Using
        End Using

        Return list
    End Function



    ' Ambil process logs untuk batch tertentu
    Public Shared Function GetProcessLogs(batchPrefix As String) As List(Of ProcessLog)
        Dim logs As New List(Of ProcessLog)
        Using conn As New SqlConnection(GetConnectionString("BatchDB"))
            conn.Open()
            Dim cmd As New SqlCommand(
            "SELECT * FROM pp_trace_processes  WHERE substr(TraceID,1,3)=@BatchPrefix ORDER BY scan_time ASC", conn)
            cmd.Parameters.AddWithValue("@BatchPrefix", batchPrefix)

            Using reader = cmd.ExecuteReader()
                While reader.Read()
                    logs.Add(New ProcessLog With {
                    .ID = Convert.ToInt32(reader("ID")),
                    .TraceID = reader("TraceID").ToString(),
                    .ProcessID = Convert.ToInt32(reader("ProcessID")),
                    .OperatorID = reader("OperatorID").ToString(),
                    .Status = reader("Status").ToString(),
                    .ScanTime = Convert.ToDateTime(reader("scan_time"))
                })
                End While
            End Using
        End Using

        Return logs
    End Function

    Public Shared Function GetBatchByTraceID(traceId As String) As Batch
        Using conn As New SqlConnection(GetConnectionString("BatchDB"))
            conn.Open()
            Dim cmd As New SqlCommand(
            "SELECT * FROM pp_trace_route WHERE trace_id=@TraceID", conn)
            cmd.Parameters.AddWithValue("@TraceID", traceId)

            Using reader = cmd.ExecuteReader()
                If reader.Read() Then
                    Return New Batch With {
                    .TraceID = reader("trace_id").ToString(),
                    .Model = reader("model_name").ToString(),
                    .PartCode = reader("part_code").ToString(),
                    .InitQty = Convert.ToInt32(reader("initial_qty")),
                    .CurQty = Convert.ToInt32(reader("current_qty")),
                    .LastProc = reader("last_proc_code").ToString(),
                    .Status = reader("status").ToString(),
                    .Shift = reader("shift").ToString(),
                    .Line = reader("line").ToString(),
                    .OperatorID = reader("operator_id").ToString(),
                    .BaraCoreDate = Convert.ToDateTime(reader("bara_core_date")),
                    .BaraCoreLot = reader("bara_core_lot").ToString(),
                    .CreatedDate = Convert.ToDateTime(reader("created_date")),
                    .UpdateDate = Convert.ToDateTime(reader("update_date"))
                }
                End If
            End Using
        End Using
        Return Nothing
    End Function

    'Public Shared Sub UpdateBatchQty(
    'traceId As String,
    'newQty As Integer,
    'lastProcCode As String)

    '    Using conn As New SqlConnection(GetConnectionString("BatchDB"))
    '        conn.Open()
    '        Dim cmd As New SqlCommand(
    '    "UPDATE pp_trace_route
    '     SET current_qty=@Qty,
    '         last_proc_code=@LastProc,
    '         update_date=GETDATE()
    '     WHERE trace_id=@TraceID", conn)

    '        cmd.Parameters.AddWithValue("@Qty", newQty)
    '        cmd.Parameters.AddWithValue("@LastProc", lastProcCode)
    '        cmd.Parameters.AddWithValue("@TraceID", traceId)

    '        cmd.ExecuteNonQuery()
    '    End Using
    'End Sub
    Public Shared Sub CompleteProcessLog(
    logId As Integer,
    batch As Batch,
    qtyOut As Integer,
    qtyReject As Integer)

        If qtyOut < 0 Or qtyReject < 0 Then
            Throw New Exception("Qty cannot be negative")
        End If

        If qtyOut + qtyReject > batch.CurQty Then
            Throw New Exception("Qty exceed current batch quantity")
        End If

        ' qty masuk ke process = current batch qty
        Dim qtyIn As Integer = batch.CurQty

        ' baki selepas buffer + reject
        Dim newQty As Integer = batch.CurQty - qtyOut - qtyReject

        Using conn As New SqlConnection(GetConnectionString("BatchDB"))
            conn.Open()

            ' 🔹 UPDATE PROCESS LOG
            Using cmd As New SqlCommand(
            "UPDATE pp_trace_processes
             SET qty_in=@QtyIn,
                 qty_out=@QtyOut,
                 qty_reject=@QtyReject,
                 status='Completed'
             WHERE id=@ID", conn)

                cmd.Parameters.AddWithValue("@QtyIn", qtyIn)
                cmd.Parameters.AddWithValue("@QtyOut", qtyOut)
                cmd.Parameters.AddWithValue("@QtyReject", qtyReject)
                cmd.Parameters.AddWithValue("@ID", logId)

                cmd.ExecuteNonQuery()
            End Using

            ' 🔹 UPDATE BATCH QTY
            Using cmd2 As New SqlCommand(
 "UPDATE pp_trace_route
 SET current_qty=@Qty,
     update_date=GETDATE()
 WHERE trace_id=@TraceID", conn)

                cmd2.Parameters.AddWithValue("@Qty", newQty)
                cmd2.Parameters.AddWithValue("@TraceID", batch.TraceID)
                cmd2.ExecuteNonQuery()
            End Using


        End Using
        '' 🔹 Auto-register next process
        'Dim logs = GetProcessLogsByTraceID(batch.TraceID)
        'Dim currentLog = logs.FirstOrDefault(Function(l) l.ID = logId)
        'If currentLog IsNot Nothing Then
        '    Dim currentProcess = GetProcessById(currentLog.ProcessID)
        '    RegisterNextProcess(batch, currentProcess, batch.OperatorID)
        'End If
        ' --- Update in-memory batch object ---
        batch.CurQty = newQty
        Dim currentLog = GetProcessLogById(logId)
        Dim currentProcess = GetProcessById(currentLog.ProcessID)
        batch.LastProc = currentProcess.Code

        ' --- Auto-register next process ---
        RegisterNextProcess(batch, currentProcess, batch.OperatorID)
    End Sub

    ' Get a single process by ID
    Public Shared Function GetProcessById(processId As Integer) As ProcessMaster
        Using conn As New SqlConnection(GetConnectionString("BatchDB"))
            conn.Open()
            Dim cmd As New SqlCommand("SELECT * FROM pp_master_process WHERE ID=@ID", conn)
            cmd.Parameters.AddWithValue("@ID", processId)
            Using reader = cmd.ExecuteReader()
                If reader.Read() Then
                    Return New ProcessMaster With {
                    .ID = Convert.ToInt32(reader("id")),
                    .Code = reader("proc_code").ToString(),
                    .Name = reader("proc_name").ToString(),
                    .Level = Convert.ToInt32(reader("proc_level"))
                }
                End If
            End Using
        End Using
        Return Nothing
    End Function

    ' Get last process log for a batch
    'Public Shared Function GetLastProcessLog(traceId As String) As ProcessLog
    '    Using conn As New SqlConnection(GetConnectionString("BatchDB"))
    '        conn.Open()
    '        Dim cmd As New SqlCommand("SELECT *  FROM pp_trace_processes  WHERE trace_id=@TraceID ORDER BY scan_time DESC LIMIT 1", conn)
    '        cmd.Parameters.AddWithValue("@TraceID", traceId)
    '        Using reader = cmd.ExecuteReader()
    '            If reader.Read() Then
    '                Return New ProcessLog With {
    '                .ID = Convert.ToInt32(reader("ID")),
    '                .TraceID = reader("TraceID").ToString(),
    '                .ProcessID = Convert.ToInt32(reader("ProcessID")),
    '                .scan_time = Convert.ToDateTime(reader("scan_time")),
    '                .OperatorID = reader("OperatorID").ToString(),
    '                .Status = reader("Status").ToString(),
    '                .Notes = If(reader("Notes") IsNot DBNull.Value, reader("Notes").ToString(), "")
    '            }
    '            End If
    '        End Using
    '    End Using
    '    Return Nothing
    'End Function

    ' Get all logs for a batch filtered by level
    'Public Shared Function GetProcessLogsByLevel(traceId As String, level As Integer) As List(Of ProcessLog)
    '    Dim list As New List(Of ProcessLog)()
    '    Using conn As New SqlConnection(GetConnectionString("BatchDB"))
    '        conn.Open()
    '        Dim cmd As New SqlCommand("
    '        SELECT tp.* 
    '        FROM pp_trace_processes tp
    '        INNER JOIN pp_master_process pm ON tp.ProcessID = pm.id
    '        WHERE tp.TraceID=@TraceID AND pm.proc_level=@Level
    '        ORDER BY tp.scan_time ASC", conn)
    '        cmd.Parameters.AddWithValue("@TraceID", traceId)
    '        cmd.Parameters.AddWithValue("@Level", level)
    '        Using reader = cmd.ExecuteReader()
    '            While reader.Read()
    '                list.Add(New ProcessLog With {
    '                .ID = Convert.ToInt32(reader("ID")),
    '                .TraceID = reader("TraceID").ToString(),
    '                .ProcessID = Convert.ToInt32(reader("ProcessID")),
    '                .scan_time = Convert.ToDateTime(reader("scan_time")),
    '                .OperatorID = reader("OperatorID").ToString(),
    '                .Status = reader("Status").ToString(),
    '                .Notes = If(reader("Notes") IsNot DBNull.Value, reader("Notes").ToString(), "")
    '            })
    '            End While
    '        End Using
    '    End Using
    '    Return list
    'End Function
    Public Shared Sub UpdateProcessLogStatus(logId As Integer, status As String)
        Using conn As New SqlConnection(GetConnectionString("BatchDB"))
            conn.Open()
            Dim cmd As New SqlCommand("UPDATE pp_trace_processes SET Status=@Status WHERE ID=@ID", conn)
            cmd.Parameters.AddWithValue("@Status", status)
            cmd.Parameters.AddWithValue("@ID", logId)
            cmd.ExecuteNonQuery()
        End Using
    End Sub
    ' Ambil semua process logs untuk batch tertentu (TraceID penuh)
    Public Shared Function GetProcessLogsByTraceID(traceId As String) As List(Of ProcessLog)
        Dim logs As New List(Of ProcessLog)
        Using conn As New SqlConnection(GetConnectionString("BatchDB"))
            conn.Open()
            Dim cmd As New SqlCommand(
                "SELECT * FROM pp_trace_processes WHERE trace_id=@TraceID ORDER BY scan_time ASC", conn)
            cmd.Parameters.AddWithValue("@TraceID", traceId)

            Using reader = cmd.ExecuteReader()
                While reader.Read()
                    logs.Add(New ProcessLog With {
    .ID = Convert.ToInt32(reader("id")),
    .TraceID = reader("trace_id").ToString(),
    .ProcessID = Convert.ToInt32(reader("process_id")),
    .OperatorID = reader("operator_id").ToString(),
    .Status = reader("status").ToString(),
    .ScanTime = Convert.ToDateTime(reader("scan_time")),
    .QtyIn = Convert.ToInt32(reader("qty_in")),
    .QtyOut = Convert.ToInt32(reader("qty_out")),
    .QtyReject = Convert.ToInt32(reader("qty_reject"))
})

                End While
            End Using
        End Using
        Return logs
    End Function

    Public Shared Function GetProcessLogById(logId As Integer) As ProcessLog
        Using conn As New SqlConnection(GetConnectionString("BatchDB"))
            conn.Open()
            Dim cmd As New SqlCommand(
            "SELECT * FROM pp_trace_processes WHERE id=@ID",
            conn
        )
            cmd.Parameters.AddWithValue("@ID", logId)

            Using reader = cmd.ExecuteReader()
                If reader.Read() Then
                    Return New ProcessLog With {
    .ID = Convert.ToInt32(reader("id")),
    .TraceID = reader("trace_id").ToString(),
    .ProcessID = Convert.ToInt32(reader("process_id")),
    .OperatorID = reader("operator_id").ToString(),
    .Status = reader("status").ToString(),
    .ScanTime = Convert.ToDateTime(reader("scan_time")),
    .QtyIn = Convert.ToInt32(reader("qty_in")),
    .QtyOut = Convert.ToInt32(reader("qty_out")),
    .QtyReject = Convert.ToInt32(reader("qty_reject"))
}

                End If
            End Using
        End Using

        Return Nothing
    End Function
    Public Shared Sub UpdateRouteLastProcess(
    traceId As String,
    lastProcCode As String)

        Using conn As New SqlConnection(GetConnectionString("BatchDB"))
            conn.Open()
            Dim cmd As New SqlCommand(
        "UPDATE pp_trace_route
         SET last_proc_code=@LastProc,
             update_date=GETDATE(), status = 'ONGOING'
         WHERE trace_id=@TraceID", conn)

            cmd.Parameters.AddWithValue("@LastProc", lastProcCode)
            cmd.Parameters.AddWithValue("@TraceID", traceId)

            cmd.ExecuteNonQuery()
        End Using
    End Sub

    Public Shared Sub InsertTraceMaterial(
            traceId As String,
            procId As Integer,
            partCode As String,
            lowerMaterial As String,
            batchLot As String,
            usageQty As Long,
            uom As String,
            vendorCode As String,
            vendorLot As String
        )

        If String.IsNullOrEmpty(partCode) Then
            Throw New Exception("part_code is NULL")
        End If

        If TraceMaterialExists(
            traceId, procId, partCode,
            lowerMaterial, batchLot,
            vendorCode, vendorLot
        ) Then
            Throw New Exception("Material already scanned for this process")

        End If

        Using conn As New SqlConnection(GetConnectionString("BatchDB"))
            conn.Open()

            Dim cmd As New SqlCommand(
        "INSERT INTO pp_trace_material
        (trace_id, proc_id, part_code, lower_material, batch_lot,
         usage_qty, uom, vendor_code, vendor_lot, created_date)
         VALUES
        (@traceId, @procId, @partCode, @lowerMaterial, @batchLot,
         @usageQty, @uom, @vendorCode, @vendorLot, GETDATE())",
        conn)

            cmd.Parameters.AddWithValue("@traceId", traceId)
            cmd.Parameters.AddWithValue("@procId", procId)
            cmd.Parameters.AddWithValue("@partCode", partCode)
            cmd.Parameters.AddWithValue("@lowerMaterial", lowerMaterial)
            cmd.Parameters.AddWithValue("@batchLot", batchLot)
            cmd.Parameters.AddWithValue("@usageQty", usageQty)
            cmd.Parameters.AddWithValue("@uom", uom)
            cmd.Parameters.AddWithValue("@vendorCode", vendorCode)
            cmd.Parameters.AddWithValue("@vendorLot", vendorLot)

            cmd.ExecuteNonQuery()
        End Using
    End Sub
    Public Shared Function GetTraceMaterials(
    traceId As String,
    procId As Integer,
    partCode As String
) As List(Of MaterialLog)

        Dim list As New List(Of MaterialLog)

        Using conn As New SqlConnection(GetConnectionString("BatchDB"))
            conn.Open()

            Dim cmd As New SqlCommand("
            SELECT *
            FROM pp_trace_material
            WHERE trace_id = @TraceID
              AND proc_id = @ProcID
AND part_code = @PartCode
            ORDER BY created_date
        ", conn)

            cmd.Parameters.AddWithValue("@TraceID", traceId)
            cmd.Parameters.AddWithValue("@ProcID", procId)
            cmd.Parameters.AddWithValue("@PartCode", partCode)

            Using r = cmd.ExecuteReader()
                While r.Read()
                    list.Add(New MaterialLog With {
                        .ID = Convert.ToInt32(r("id")),
                    .TraceID = r("trace_id").ToString(),
                    .ProcID = CInt(r("proc_id")),
                    .PartCode = r("part_code").ToString(),
                    .LowerMaterial = r("lower_material").ToString(),
                    .BatchLot = r("batch_lot").ToString(),
                    .UsageQty = CInt(r("usage_qty")),
                    .UOM = r("uom").ToString(),
                    .VendorCode = r("vendor_code").ToString(),
                    .VendorLot = r("vendor_lot").ToString()
                })
                End While
            End Using
        End Using

        Return list
    End Function
    Public Shared Sub DeleteTraceMaterial(id As Integer)

        Using conn As New SqlConnection(GetConnectionString("BatchDB"))
            conn.Open()

            Dim cmd As New SqlCommand(
            "DELETE FROM pp_trace_material WHERE id = @id", conn)

            cmd.Parameters.AddWithValue("@id", id)
            cmd.ExecuteNonQuery()
        End Using

    End Sub
    Public Shared Function TraceMaterialExists(
    traceId As String,
    procId As Integer,
    partCode As String,
    lowerMaterial As String,
    batchLot As String,
    vendorCode As String,
    vendorLot As String
) As Boolean

        Using conn As New SqlConnection(GetConnectionString("BatchDB"))
            conn.Open()

            Dim cmd As New SqlCommand("
            SELECT COUNT(1)
            FROM pp_trace_material
            WHERE trace_id = @traceId
              AND proc_id = @procId
              AND part_code = @partCode
              AND lower_material = @lowerMaterial
              AND batch_lot = @batchLot
              AND vendor_code = @vendorCode
              AND vendor_lot = @vendorLot
        ", conn)

            cmd.Parameters.AddWithValue("@traceId", traceId)
            cmd.Parameters.AddWithValue("@procId", procId)
            cmd.Parameters.AddWithValue("@partCode", partCode)
            cmd.Parameters.AddWithValue("@lowerMaterial", lowerMaterial)
            cmd.Parameters.AddWithValue("@batchLot", batchLot)
            cmd.Parameters.AddWithValue("@vendorCode", vendorCode)
            cmd.Parameters.AddWithValue("@vendorLot", vendorLot)

            Return CInt(cmd.ExecuteScalar()) > 0
        End Using
    End Function
    Public Shared Function HasMaterialForProcess(traceId As String, procId As Integer) As Boolean
        Using conn As New SqlConnection(GetConnectionString("BatchDB"))
            conn.Open()

            ' 1️⃣ Ambik level process dari pp_master_process
            Dim cmdLevel As New SqlCommand("
            SELECT proc_level, material_flag
            FROM pp_master_process
            WHERE id = @procId
        ", conn)
            cmdLevel.Parameters.AddWithValue("@procId", procId)

            Dim reader = cmdLevel.ExecuteReader()
            If Not reader.Read() Then Return False
            Dim level As Integer = Convert.ToInt32(reader("proc_level"))
            Dim materialFlag As Integer = Convert.ToInt32(reader("material_flag"))
            reader.Close()

            ' kalau material_flag = 0, return true terus
            If materialFlag = 0 Then Return True

            ' 2️⃣ Check ada material untuk process ni (level sama atau lebih rendah)
            Dim cmdCheck As New SqlCommand("
            SELECT COUNT(1)
            FROM pp_trace_material m
            INNER JOIN pp_master_process p ON m.proc_id = p.id
            WHERE m.trace_id = @traceId
              AND p.proc_level = @level
        ", conn)
            cmdCheck.Parameters.AddWithValue("@traceId", traceId)
            cmdCheck.Parameters.AddWithValue("@level", level)

            Return Convert.ToInt32(cmdCheck.ExecuteScalar()) > 0
        End Using
    End Function

    'Public Shared Sub CompleteProcess(traceId As String, procId As Integer)

    '    Using conn As New SqlConnection(GetConnectionString("BatchDB"))
    '        conn.Open()

    '        Dim cmd As New SqlCommand("
    '        UPDATE pp_trace_process_log
    '        SET status = 'Completed'
    '        WHERE trace_id = @traceId
    '          AND proc_id = @procId
    '          AND status = 'In Progress'
    '    ", conn)

    '        cmd.Parameters.AddWithValue("@traceId", traceId)
    '        cmd.Parameters.AddWithValue("@procId", procId)

    '        cmd.ExecuteNonQuery()
    '    End Using
    'End Sub
    Public Shared Sub RegisterNextProcess(batch As Batch, currentProcess As ProcessMaster, operatorId As String)
        ' Get all processes
        Dim processes = GetAllProcesses()

        ' Determine next process by level
        Dim nextProcess = processes.FirstOrDefault(Function(p) p.Level = currentProcess.Level + 1)
        If nextProcess IsNot Nothing Then
            ' Check if next process already logged
            Dim logs = GetProcessLogsByTraceID(batch.TraceID)
            Dim exists = logs.Any(Function(l) l.ProcessID = nextProcess.ID)
            If Not exists Then
                ' Log next process as "In Progress"
                LogBatchProcess(
                traceId:=batch.TraceID,
                processId:=nextProcess.ID,
                operatorId:=operatorId,
                qtyIn:=batch.CurQty,
                qtyOut:=0,
                qtyReject:=0,
                status:="In Progress"
            )
                ' Update batch last process
                UpdateRouteLastProcess(batch.TraceID, nextProcess.Code)
            End If
        End If
    End Sub

End Class
