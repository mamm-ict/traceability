Imports System.Data.SqlClient
Imports System.Configuration
Imports Microsoft.Ajax.Utilities

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

    Public Shared Function GetFinalQtyByPartCode(partCode As String) As Integer
        Using conn As New SqlConnection(GetConnectionString("BatchDB"))
            conn.Open()
            Dim cmd As New SqlCommand("SELECT start_qty FROM pp_master_material WHERE part_code=@PartCode", conn)
            cmd.Parameters.AddWithValue("@PartCode", partCode)
            Dim result = cmd.ExecuteScalar()
            If result IsNot Nothing Then Return Convert.ToInt32(result)
        End Using
        Return 0
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
            SET last_proc_code=@LastProc, update_date=GETDATE(), status = 'ONGOING'
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

        Using conn As New SqlConnection(GetConnectionString("BatchDB"))
            conn.Open()

            ' ✅ Check if this material has been scanned before
            Dim cmdCheck As New SqlCommand("
                SELECT COUNT(*) 
                FROM pp_trace_material
                WHERE trace_id = @TraceID
                AND proc_id = @ProcID
                AND part_code = @PartCode
                AND lower_material = @LowerMaterial
                AND batch_lot = @BatchLot
                AND vendor_code = @VendorCode
                AND vendor_lot = @VendorLot
            ", conn)

            cmdCheck.Parameters.AddWithValue("@TraceID", traceId)
            cmdCheck.Parameters.AddWithValue("@ProcID", procId)
            cmdCheck.Parameters.AddWithValue("@PartCode", partCode)
            cmdCheck.Parameters.AddWithValue("@LowerMaterial", lowerMaterial)
            cmdCheck.Parameters.AddWithValue("@BatchLot", batchLot)
            cmdCheck.Parameters.AddWithValue("@VendorCode", vendorCode)
            cmdCheck.Parameters.AddWithValue("@VendorLot", vendorLot)

            Dim count As Integer = Convert.ToInt32(cmdCheck.ExecuteScalar())
            Dim isDuplicate As Boolean = (count > 0)

            ' ✅ Insert material anyway
            Dim cmdInsert As New SqlCommand("
                INSERT INTO pp_trace_material
                (trace_id, proc_id, part_code, lower_material, batch_lot,
                 usage_qty, uom, vendor_code, vendor_lot, is_duplicate, created_date)
                VALUES
                (@TraceID, @ProcID, @PartCode, @LowerMaterial, @BatchLot,
                 @UsageQty, @UOM, @VendorCode, @VendorLot, @IsDuplicate, GETDATE())
            ", conn)

            cmdInsert.Parameters.AddWithValue("@TraceID", traceId)
            cmdInsert.Parameters.AddWithValue("@ProcID", procId)
            cmdInsert.Parameters.AddWithValue("@PartCode", partCode)
            cmdInsert.Parameters.AddWithValue("@LowerMaterial", lowerMaterial)
            cmdInsert.Parameters.AddWithValue("@BatchLot", batchLot)
            cmdInsert.Parameters.AddWithValue("@UsageQty", usageQty)
            cmdInsert.Parameters.AddWithValue("@UOM", uom)
            cmdInsert.Parameters.AddWithValue("@VendorCode", vendorCode)
            cmdInsert.Parameters.AddWithValue("@VendorLot", vendorLot)
            cmdInsert.Parameters.AddWithValue("@IsDuplicate", If(isDuplicate, 1, 0))

            cmdInsert.ExecuteNonQuery()
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

            Using reader = cmd.ExecuteReader()
                While reader.Read()
                    Dim m As New MaterialLog With {
                        .ID = reader("id"),
                        .TraceID = reader("trace_id").ToString(),
                        .ProcID = Convert.ToInt32(reader("proc_id")),
                        .PartCode = reader("part_code").ToString(),
                        .LowerMaterial = reader("lower_material").ToString(),
                        .BatchLot = reader("batch_lot").ToString(),
                        .UsageQty = Convert.ToInt64(reader("usage_qty")),
                        .UOM = reader("uom").ToString(),
                        .VendorCode = reader("vendor_code").ToString(),
                        .VendorLot = reader("vendor_lot").ToString(),
                        .IsDuplicate = Convert.ToBoolean(reader("is_duplicate"))
                    }
                    list.Add(m)
                End While
            End Using
        End Using

        Return list
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

    Public Shared Sub CompleteFinalRoute(traceId As String)
        Using conn As New SqlConnection(GetConnectionString("BatchDB"))
            conn.Open()
            Dim cmd As New SqlCommand(
                "UPDATE pp_trace_route
                SET status = 'COMPLETED', update_date = GETDATE()
                WHERE trace_id = @TraceID", conn)

            cmd.Parameters.AddWithValue("@TraceID", traceId)
            cmd.ExecuteNonQuery()
        End Using
    End Sub

    Public Shared Sub CompleteBufferRejectOnly(
            logId As Integer,
            batch As Batch,
            qtyReject As Integer
        )

        If qtyReject < 0 Then
            Throw New Exception("Qty reject cannot be negative")
        End If

        Dim log = GetProcessLogById(logId)
        If log Is Nothing Then Throw New Exception("Process log not found")

        Dim process = GetProcessById(log.ProcessID)
        If process Is Nothing Then Throw New Exception("Process not found")

        ' 1️⃣ Update process log (NO qtyOut)
        Using conn As New SqlConnection(GetConnectionString("BatchDB"))
            conn.Open()

            Using cmd As New SqlCommand("
                    UPDATE pp_trace_processes
                    SET qty_in = @QtyIn,
                        qty_out = 0,
                        qty_reject = @QtyReject,
                        status = 'Completed'
                    WHERE id = @ID
                ", conn)

                cmd.Parameters.AddWithValue("@QtyIn", batch.CurQty)
                cmd.Parameters.AddWithValue("@QtyReject", qtyReject)
                cmd.Parameters.AddWithValue("@ID", logId)
                cmd.ExecuteNonQuery()
            End Using

            ' 2️⃣ Tolak reject SAHAJA dari batch
            Using cmd2 As New SqlCommand("
                    UPDATE pp_trace_route
                    SET current_qty = current_qty - @RejectQty,
                        update_date = GETDATE()
                    WHERE trace_id = @TraceID
                ", conn)
                cmd2.Parameters.AddWithValue("@RejectQty", qtyReject)
                cmd2.Parameters.AddWithValue("@TraceID", batch.TraceID)
                cmd2.ExecuteNonQuery()
            End Using
        End Using

        ' 3️⃣ Update memory
        batch.CurQty -= qtyReject
        batch.LastProc = process.Code

    End Sub

    Public Shared Sub CompleteFinalProcess(
             logId As Integer,
             batch As Batch,
             qtyReject As Integer
         )

        If qtyReject < 0 Then
            Throw New Exception("Qty reject cannot be negative")
        End If

        Dim log = GetProcessLogById(logId)
        If log Is Nothing Then Throw New Exception("Process log not found")

        Dim process = GetProcessById(log.ProcessID)
        If process Is Nothing Then Throw New Exception("Process not found")

        ' 1️⃣ Ambil FINAL QTY dari master material
        Dim finalQty As Integer
        Using conn As New SqlConnection(GetConnectionString("BatchDB"))
            conn.Open()
            Using cmd As New SqlCommand("
                    SELECT final_qty
                    FROM pp_master_material
                    WHERE upper_item = @PartCode
                ", conn)
                cmd.Parameters.AddWithValue("@PartCode", batch.PartCode)
                finalQty = Convert.ToInt32(cmd.ExecuteScalar())
            End Using
        End Using

        ' 2️⃣ VALIDATION
        If batch.CurQty < finalQty + qtyReject Then
            Throw New Exception("Qty reject terlalu besar. Melebihi baki batch.")
        End If

        ' 3️⃣ KIRA qtyOut (INI LOGIK BETUL)
        Dim qtyOut As Integer = batch.CurQty - finalQty - qtyReject
        If qtyOut < 0 Then qtyOut = 0

        Dim qtyIn = finalQty

        ' 4️⃣ UPDATE PROCESS LOG
        Using conn As New SqlConnection(GetConnectionString("BatchDB"))
            conn.Open()

            Using cmd As New SqlCommand("
                    UPDATE pp_trace_processes
                    SET qty_in = @QtyIn,
                        qty_out = @QtyOut,
                        qty_reject = @QtyReject,
                        status = 'Completed'
                    WHERE id = @ID
                ", conn)
                cmd.Parameters.AddWithValue("@QtyIn", qtyIn)
                cmd.Parameters.AddWithValue("@QtyOut", qtyOut)
                cmd.Parameters.AddWithValue("@QtyReject", qtyReject)
                cmd.Parameters.AddWithValue("@ID", logId)
                cmd.ExecuteNonQuery()
            End Using

            ' 5️⃣ UPDATE ROUTE (FINAL)
            Using cmd2 As New SqlCommand("
                    UPDATE pp_trace_route
                    SET current_qty = @FinalQty,
                        status = 'COMPLETED',
                        update_date = GETDATE()
                    WHERE trace_id = @TraceID
                ", conn)
                cmd2.Parameters.AddWithValue("@FinalQty", finalQty)
                cmd2.Parameters.AddWithValue("@TraceID", batch.TraceID)
                cmd2.ExecuteNonQuery()
            End Using
        End Using

        ' 6️⃣ UPDATE MEMORY
        batch.CurQty = finalQty
        batch.LastProc = process.Code
    End Sub
End Class
