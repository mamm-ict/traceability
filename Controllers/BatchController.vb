Imports System.Data.SqlClient
Imports System.Configuration

Imports Newtonsoft.Json
Imports QRCoder
Imports System.Web.Mvc

Public Class BatchController
    Inherits Controller

    ' GET: Batch/Create
    Public Overloads Function Create() As ActionResult
        ViewData("PartMasters") = DbHelper.GetPartMasters()
        Return View()
    End Function



    ' GET: Batch/ShowQR
    Public Function ShowQR(TraceID As String) As ActionResult
        Dim batch As Batch

        Using conn As New SqlConnection(DbHelper.GetConnectionString())
            conn.Open()
            Dim cmd As New SqlCommand("SELECT * FROM pp_trace_route WHERE trace_id=@TraceID", conn)
            cmd.Parameters.AddWithValue("@TraceID", TraceID)
            Using reader = cmd.ExecuteReader()
                If reader.Read() Then
                    'Dim rawList As List(Of RawMaterialEntry) = JsonConvert.DeserializeObject(Of List(Of RawMaterialEntry))(reader("RawMaterial").ToString())
                    batch = New Batch With {
                        .TraceID = reader("trace_id").ToString(),
                        .Model = reader("model_name").ToString(),
                        .InitQty = Convert.ToInt64(reader("initial_qty")),
                        .Shift = reader("shift").ToString(),
                        .Line = reader("line").ToString(),
                        .OperatorID = reader("operator_id").ToString(),
                        .BaraCoreLot = reader("bara_core_lot").ToString(),
                        .CreatedDate = Convert.ToDateTime(reader("created_date"))
                    }
                End If
            End Using
        End Using
        'End If

        ' Generate QR code
        'Dim qrContent = String.Join("|", batch.TraceID, batch.Model, batch.MachineNo, batch.Line, rmContent, batch.OperatorID, batch.CreatedDate.ToString("yyyy-MM-dd"), batch.Shift)
        Dim qrContent = batch.TraceID
        Dim qrBase64 As String
        Using qrGenerator As New QRCoder.QRCodeGenerator()
            Using qrCodeData = qrGenerator.CreateQrCode(qrContent, QRCoder.QRCodeGenerator.ECCLevel.Q)
                Using qrCode = New QRCoder.PngByteQRCode(qrCodeData)
                    qrBase64 = Convert.ToBase64String(qrCode.GetGraphic(10))
                End Using
            End Using
        End Using

        ' Assign all ViewData
        ViewData("TraceID") = batch.TraceID
        ViewData("Model") = batch.Model
        ViewData("InitQty") = batch.InitQty
        ViewData("CurQty") = batch.CurQty
        ViewData("LastProc") = batch.LastProc
        ViewData("Status") = batch.Status
        ViewData("Shift") = batch.Shift
        ViewData("Line") = batch.Line
        ViewData("OperatorID") = batch.OperatorID
        ViewData("BaraCoreDate") = batch.BaraCoreDate.ToString("yyyy-MM-dd HH:mm:ss")
        ViewData("BaraCoreLot") = batch.BaraCoreLot
        ViewData("CreatedDate") = batch.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss")
        ViewData("UpdateDate") = batch.UpdateDate.ToString("yyyy-MM-dd HH:mm:ss")

        ViewData("QRCodeImage") = qrBase64

        Return View()
    End Function

    ' POST: Batch/Create
    <HttpPost>
    Public Overloads Function Create(batchData As FormCollection) As ActionResult
        ' Read multiple raw materials + quantities
        'Dim names As String() = batchData.GetValues("RawMaterialNames")
        'Dim quantities As String() = batchData.GetValues("Quantities")

        'Dim rawList As New List(Of RawMaterialEntry)()
        'For i As Integer = 0 To names.Length - 1
        '    If Not String.IsNullOrWhiteSpace(names(i)) AndAlso Not String.IsNullOrWhiteSpace(quantities(i)) Then
        '        rawList.Add(New RawMaterialEntry With {.Name = names(i), .Quantity = Convert.ToInt32(quantities(i))})
        '    End If
        'Next

        Dim BaraCoreDate As DateTime = Convert.ToDateTime(batchData("BaraCoreDate"))

        ' Create batch object
        Dim batch As New Batch With {
            .TraceID = GenerateTraceID(),
            .Model = batchData("Model"),
            .PartCode = batchData("PartCode"),
            .InitQty = batchData("InitQty"),
            .CurQty = batchData("InitQty"),
            .LastProc = GetLastProcess(),
            .Status = GetStatus(),
            .Shift = GetCurrentShift(),
            .Line = batchData("Line"),
            .OperatorID = batchData("OperatorID"),
            .BaraCoreDate = BaraCoreDate,
            .BaraCoreLot = GenerateBaraCoreLot(BaraCoreDate),
            .CreatedDate = DateTime.Now,
            .UpdateDate = DateTime.Now
        }

        ' Save to SQLite
        Using conn As New SqlConnection(DbHelper.GetConnectionString())
            conn.Open()
            Dim cmd As New SqlCommand(
    "INSERT INTO pp_trace_route (trace_id, model_name, part_code, initial_qty, current_qty, last_proc_CODE, status, shift, line, 
    operator_id, bara_core_date, bara_core_lot, created_date, update_date) 
     VALUES (@TraceID, @Model, @PartCode, @InitQty, @CurQty, @LastProc, @Status, @Shift, @Line, 
    @OperatorID, @BaraCoreDate, @BaraCoreLot, @CreatedDate, @UpdateDate)", conn)

            cmd.Parameters.AddWithValue("@TraceID", batch.TraceID)
            cmd.Parameters.AddWithValue("@Model", batch.Model)
            cmd.Parameters.AddWithValue("@PartCode", batch.PartCode)
            cmd.Parameters.AddWithValue("@InitQty", batch.InitQty)
            cmd.Parameters.AddWithValue("@CurQty", batch.CurQty)
            cmd.Parameters.AddWithValue("@LastProc", batch.LastProc)
            cmd.Parameters.AddWithValue("@Status", batch.Status)
            cmd.Parameters.AddWithValue("@Shift", batch.Shift)
            cmd.Parameters.AddWithValue("@Line", batch.Line)
            cmd.Parameters.AddWithValue("@OperatorID", batch.OperatorID)
            cmd.Parameters.AddWithValue("@BaraCoreDate", batch.BaraCoreDate.ToString("yyyy-MM-dd HH:mm:ss"))
            cmd.Parameters.AddWithValue("@BaraCoreLot", batch.BaraCoreLot)
            cmd.Parameters.AddWithValue("@CreatedDate", batch.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss"))
            cmd.Parameters.AddWithValue("@UpdateDate", batch.UpdateDate.ToString("yyyy-MM-dd HH:mm:ss"))

            cmd.ExecuteNonQuery()
        End Using

        ' Generate QR code
        'Dim rmContent = String.Join(",", batch.RawMaterials.Select(Function(r) r.Name & ":" & r.Quantity))
        'Dim qrContent = String.Join("|", batch.TraceID, batch.Model, batch.MachineNo, batch.Line, rmContent, batch.OperatorID, batch.CreatedDate.ToString("yyyy-MM-dd"), batch.Shift)
        Dim qrContent = batch.TraceID
        Dim qrBase64 As String
        Using qrGenerator As New QRCodeGenerator()
            Using qrCodeData = qrGenerator.CreateQrCode(qrContent, QRCodeGenerator.ECCLevel.Q)
                Using qrCode = New PngByteQRCode(qrCodeData)
                    qrBase64 = Convert.ToBase64String(qrCode.GetGraphic(10))
                End Using
            End Using
        End Using

        ' Pass data to ShowQR view
        ViewData("TraceID") = batch.TraceID
        ViewData("Model") = batch.Model
        ViewData("InitQty") = batch.InitQty
        ViewData("CurQty") = batch.CurQty
        ViewData("LastProc") = batch.LastProc
        ViewData("Status") = batch.Status
        ViewData("Shift") = batch.Shift
        ViewData("Line") = batch.Line
        ViewData("OperatorID") = batch.OperatorID
        ViewData("BaraCoreDate") = batch.BaraCoreDate.ToString("yyyy-MM-dd HH:mm:ss")
        ViewData("BaraCoreLot") = batch.BaraCoreLot
        ViewData("CreatedDate") = batch.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss")
        ViewData("UpdateDate") = batch.UpdateDate.ToString("yyyy-MM-dd HH:mm:ss")
        ViewData("QRCodeImage") = qrBase64

        Return View("ShowQR")
    End Function

    <HttpPost>
    Public Function DeleteTraceMaterial(id As Integer) As ContentResult
        Try
            DbHelper.DeleteTraceMaterial(id)
            Return Content("OK")
        Catch ex As Exception
            Return Content(ex.Message)
        End Try
    End Function

    ' -------------------------
    ' Helper methods
    ' -------------------------
    Private Function GenerateTraceID() As String
        Dim today As String = DateTime.Now.ToString("yyyyMMdd")
        Dim seq As Integer = 1

        Using conn As New SqlConnection(DbHelper.GetConnectionString())
            conn.Open()

            Dim cmd As New SqlCommand("
            SELECT TOP 1 trace_id FROM pp_trace_route
            WHERE trace_id LIKE 'PPA-" & today & "-%' 
            ORDER BY trace_id DESC 
        ", conn)

            Dim lastIDObj = cmd.ExecuteScalar()

            If lastIDObj IsNot Nothing Then
                Dim lastID As String = lastIDObj.ToString()
                Dim parts = lastID.Split("-"c)
                seq = Convert.ToInt32(parts(2)) + 1
            End If
        End Using

        Return "PPA-" & today & "-" & seq.ToString("000")
    End Function

    Private Function GetCurrentShift() As String
        Dim schedule As String = Server.MapPath("~/Config/schedule.txt")

        Dim lines() As String = System.IO.File.ReadAllLines(schedule)
        Dim shiftA As TimeSpan = TimeSpan.Parse(lines(0))
        Dim shiftC As TimeSpan = TimeSpan.Parse(lines(1))
        Dim nowTime As TimeSpan = DateTime.Now.TimeOfDay

        Return If(nowTime >= shiftA AndAlso nowTime < shiftC, "A", "C")
    End Function

    Private Function GetLastProcess() As String
        Return "-"
    End Function

    Private Function GetStatus() As String
        Return "NEW"
    End Function

    Private Function GenerateBaraCoreLot(BaraDate As DateTime) As String
        Dim year As String = BaraDate.Year
        Dim month As String = BaraDate.Month.ToString()
        Dim day As Integer = BaraDate.Day

        Dim baseYear As Integer = 2011
        Dim alphabets As String = "ABCDEFGHJKLMNPQRSTUVWXYZ"

        Dim calcYear As Integer = year - baseYear

        If calcYear < 0 OrElse calcYear >= alphabets.Length Then
            Throw New ArgumentOutOfRangeException("Year out of supported range.")
        End If

        Dim yearCode As Char = alphabets(calcYear)

        If month.Equals("10") Then
            month = "A"
        ElseIf month.Equals("11") Then
            month = "B"
        ElseIf month.Equals("12") Then
            month = "C"
        Else
            month = month
        End If

        Return yearCode & month & day.ToString("00")
    End Function
End Class
