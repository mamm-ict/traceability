Imports System.Data.SqlClient
Imports System.Web.Mvc

Namespace Controllers
    Public Class BufferController
        Inherits Controller

        ' GET: Buffer
        Function Index() As ActionResult
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
                        bm.printed_date
                    FROM pp_trace_route tr
                    JOIN pp_trace_processes tp
                        ON tr.trace_id = tp.trace_id AND tr.current_qty = tp.qty_in
                    JOIN pp_master_material mm ON tr.part_code = mm.upper_item 
                    JOIN pp_trace_buffer_map bm ON tr.trace_id = bm.ori_trace_id
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
                            buffer("ProcID") = reader("process_id").ToString()
                            buffer("QtyOut") = reader("qty_out").ToString()   ' <-- ni yang jadi buffer_qty
                            buffer("PrintedDate") = reader("printed_date").ToString()

                            buffers.Add(buffer)
                        End While
                    End Using
                End Using
            End Using

            Return View(buffers)
        End Function
    End Class
End Namespace