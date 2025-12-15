'Imports System.Data.SqlClient
Imports System.Data.SqlClient
Imports System.Configuration

Public Class DbHelper
    Public Shared Function GetConnectionString() As String
        Dim conn = ConfigurationManager.ConnectionStrings("BatchDB")
        If conn Is Nothing Then Throw New Exception("Connection string 'BatchDB' not found in Web.config")
        Return conn.ConnectionString
    End Function

    ' Log batch masuk process
    Public Shared Sub LogBatchProcess(traceId As String, processId As Integer, OperatorID As String, status As String)
        Using conn As New SqlConnection(GetConnectionString())
            conn.Open()
            Dim cmd As New SqlCommand(
            "INSERT INTO pp_trace_processes  (TraceID, ProcessID, OperatorID, Status, scan_time) VALUES (@TraceID, @ProcessID, @OperatorID, @Status, @scan_time)",
            conn)
            cmd.Parameters.AddWithValue("@TraceID", traceId)
            cmd.Parameters.AddWithValue("@ProcessID", processId)
            cmd.Parameters.AddWithValue("@OperatorID", OperatorID)
            cmd.Parameters.AddWithValue("@Status", status)
            cmd.Parameters.AddWithValue("@scan_time", DateTime.Now)
            cmd.ExecuteNonQuery()
        End Using
    End Sub


    ' Ambil semua process master
    Public Shared Function GetAllProcesses() As List(Of ProcessMaster)
        Dim list As New List(Of ProcessMaster)()
        Using conn As New SqlConnection(GetConnectionString())
            conn.Open()
            Dim cmd As New SqlCommand("SELECT * FROM pp_master_process", conn)
            Dim reader = cmd.ExecuteReader()
            While reader.Read()
                Dim p As New ProcessMaster With {
                .ID = Convert.ToInt32(reader("id")),
                .Code = reader("proc_code").ToString(),
                .Name = reader("proc_name").ToString(),
                .Level = Convert.ToInt32(reader("proc_level"))
            }
                list.Add(p)
            End While
        End Using
        Return list
    End Function

    ' Ambil process logs untuk batch tertentu
    Public Shared Function GetProcessLogs(batchPrefix As String) As List(Of ProcessLog)
        Dim logs As New List(Of ProcessLog)
        Using conn As New SqlConnection(GetConnectionString())
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
                    .scan_time = Convert.ToDateTime(reader("scan_time"))
                })
                End While
            End Using
        End Using

        Return logs
    End Function



    ' Get a single process by ID
    Public Shared Function GetProcessById(processId As Integer) As ProcessMaster
        Using conn As New SqlConnection(GetConnectionString())
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
    Public Shared Function GetLastProcessLog(traceId As String) As ProcessLog
        Using conn As New SqlConnection(GetConnectionString())
            conn.Open()
            Dim cmd As New SqlCommand("SELECT * FROM pp_trace_processes  WHERE TraceID=@TraceID ORDER BY scan_time DESC LIMIT 1", conn)
            cmd.Parameters.AddWithValue("@TraceID", traceId)
            Using reader = cmd.ExecuteReader()
                If reader.Read() Then
                    Return New ProcessLog With {
                    .ID = Convert.ToInt32(reader("ID")),
                    .TraceID = reader("TraceID").ToString(),
                    .ProcessID = Convert.ToInt32(reader("ProcessID")),
                    .scan_time = Convert.ToDateTime(reader("scan_time")),
                    .OperatorID = reader("OperatorID").ToString(),
                    .Status = reader("Status").ToString(),
                    .Notes = If(reader("Notes") IsNot DBNull.Value, reader("Notes").ToString(), "")
                }
                End If
            End Using
        End Using
        Return Nothing
    End Function

    ' Get all logs for a batch filtered by level
    Public Shared Function GetProcessLogsByLevel(traceId As String, level As Integer) As List(Of ProcessLog)
        Dim list As New List(Of ProcessLog)()
        Using conn As New SqlConnection(GetConnectionString())
            conn.Open()
            Dim cmd As New SqlCommand("
            SELECT tp.* 
            FROM pp_trace_processes tp
            INNER JOIN pp_master_process pm ON tp.ProcessID = pm.id
            WHERE tp.TraceID=@TraceID AND pm.proc_level=@Level
            ORDER BY tp.scan_time ASC", conn)
            cmd.Parameters.AddWithValue("@TraceID", traceId)
            cmd.Parameters.AddWithValue("@Level", level)
            Using reader = cmd.ExecuteReader()
                While reader.Read()
                    list.Add(New ProcessLog With {
                    .ID = Convert.ToInt32(reader("ID")),
                    .TraceID = reader("TraceID").ToString(),
                    .ProcessID = Convert.ToInt32(reader("ProcessID")),
                    .scan_time = Convert.ToDateTime(reader("scan_time")),
                    .OperatorID = reader("OperatorID").ToString(),
                    .Status = reader("Status").ToString(),
                    .Notes = If(reader("Notes") IsNot DBNull.Value, reader("Notes").ToString(), "")
                })
                End While
            End Using
        End Using
        Return list
    End Function
    Public Shared Sub UpdateProcessLogStatus(logId As Integer, status As String)
        Using conn As New SqlConnection(GetConnectionString())
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
        Using conn As New SqlConnection(GetConnectionString())
            conn.Open()
            Dim cmd As New SqlCommand(
                "SELECT * FROM pp_trace_processes WHERE trace_id=@TraceID ORDER BY scan_time ASC", conn)
            cmd.Parameters.AddWithValue("@TraceID", traceId)

            Using reader = cmd.ExecuteReader()
                While reader.Read()
                    logs.Add(New ProcessLog With {
                        .ID = Convert.ToInt32(reader("ID")),
                        .TraceID = reader("TraceID").ToString(),
                        .ProcessID = Convert.ToInt32(reader("ProcessID")),
                        .OperatorID = reader("OperatorID").ToString(),
                        .Status = reader("Status").ToString(),
                        .scan_time = Convert.ToDateTime(reader("scan_time"))
                    })
                End While
            End Using
        End Using
        Return logs
    End Function


End Class
