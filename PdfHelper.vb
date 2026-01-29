Imports System.Data.SqlClient
Imports System.IO
Imports iTextSharp.text
Imports iTextSharp.text.pdf
Imports QRCoder
Imports System.Drawing.Imaging

' Custom cell event to anchor image bottom-right
Public Class BottomRightImageEvent
    Implements IPdfPCellEvent

    Private ReadOnly _image As iTextSharp.text.Image
    Public Sub New(img As iTextSharp.text.Image)
        _image = img
    End Sub

    Public Sub CellLayout(cell As PdfPCell, position As iTextSharp.text.Rectangle, canvases() As PdfContentByte) Implements IPdfPCellEvent.CellLayout
        Dim cb As PdfContentByte = canvases(2) ' text layer
        _image.SetAbsolutePosition(position.Right - _image.ScaledWidth - 5, position.Bottom + 5)
        cb.AddImage(_image)
    End Sub
End Class

Public Class PdfHelper
    Public Shared Function GenerateTracePdf(traceId As String) As Byte()
        Using ms As New MemoryStream()
            Dim doc As New Document(PageSize.A4, 36, 36, 36, 36)
            Dim writer = PdfWriter.GetInstance(doc, ms)
            doc.Open()

            ' Fonts
            Dim boldFont = FontFactory.GetFont("Arial", 12, Font.BOLD)
            Dim normalFont = FontFactory.GetFont("Arial", 11, Font.NORMAL)
            Dim largeFont = FontFactory.GetFont("Arial", 20, Font.BOLD)
            Dim qcFont = FontFactory.GetFont("Arial", 22, Font.BOLD)

            Dim headerStyle As BaseColor = New BaseColor(220, 220, 220) ' light gray header

            Using conn As New SqlConnection(DbHelper.GetConnectionString("BatchDB"))
                conn.Open()

                ' Fetch trace_route
                Dim cmdRoute As New SqlCommand("
                    SELECT tr.part_code, tr.current_qty, tr.created_date, tr.update_date, tr.shift, tr.line, tr.die, mm.part_desc
                    FROM pp_trace_route tr
                    JOIN pp_master_material mm ON tr.part_code = mm.upper_item
                    WHERE trace_id = @TraceID
                ", conn)
                cmdRoute.Parameters.AddWithValue("@TraceID", traceId)

                Dim routeRow As Dictionary(Of String, Object) = Nothing
                Using reader = cmdRoute.ExecuteReader()
                    If reader.Read() Then
                        routeRow = New Dictionary(Of String, Object) From {
                            {"part_code", reader("part_desc")},
                            {"current_qty", reader("current_qty")},
                            {"updated_date", reader("update_date")},
                            {"shift", reader("shift")},
                            {"die", reader("die")},
                            {"line", reader("line")}
                        } '{"created_date", reader("created_date")},
                    End If
                End Using

                If routeRow Is Nothing Then
                    doc.Add(New Paragraph("Trace not found.", normalFont))
                    doc.Close()
                    Return ms.ToArray()
                End If

                ' Employee ID
                Dim empId As String = ""
                Dim cmdBuffer As New SqlCommand("
                    SELECT operator_id
                    FROM pp_trace_processes
                    WHERE trace_id = @TraceID AND (process_id = 11 OR process_id = 12)
                ", conn)
                cmdBuffer.Parameters.AddWithValue("@TraceID", traceId)
                Using reader = cmdBuffer.ExecuteReader()
                    If reader.Read() Then empId = reader("operator_id").ToString()
                End Using

                ' Lot No Date
                Dim dateLot As String = ""
                Dim cmdDate As New SqlCommand("
                    SELECT scan_time
                    FROM pp_trace_processes tp
                    JOIN pp_master_process mp ON tp.process_id = mp.id
                    WHERE trace_id = @TraceID AND proc_code like 'PWT%'
                ", conn)
                cmdDate.Parameters.AddWithValue("@TraceID", traceId)
                Using reader = cmdDate.ExecuteReader()
                    If reader.Read() Then dateLot = reader("scan_time").ToString()
                End Using

                ' Process Code
                Dim procCode As String = ""
                Dim cmdProc As New SqlCommand("
                    SELECT short_code
                    FROM pp_trace_processes  tp
                    join pp_master_process mp on tp.process_id = mp.id
                    WHERE trace_id = @TraceID AND (proc_code like 'PWT%' OR proc_code LIKE 'OVN%')
                ", conn)
                cmdProc.Parameters.AddWithValue("@TraceID", traceId)
                Using reader = cmdProc.ExecuteReader()
                    While reader.Read()
                        procCode &= reader("short_code").ToString().Trim()
                    End While
                    Debug.WriteLine(procCode & "this is proccode")
                End Using

                ' --- Main Table ---
                Dim table As New PdfPTable(3) With {
                    .WidthPercentage = 100,
                    .SpacingBefore = 10,
                    .SpacingAfter = 10
                }
                table.SetWidths(New Single() {1.0F, 1.0F, 1.0F})

                ' 1️⃣ Part number header
                table.AddCell(New PdfPCell(New Phrase("PART NUMBER", boldFont)) With {
                    .Colspan = 3,
                    .HorizontalAlignment = Element.ALIGN_LEFT,
                    .Padding = 5,
                    .BackgroundColor = headerStyle
                })
                table.AddCell(New PdfPCell(New Phrase(routeRow("part_code").ToString(), largeFont)) With {
                    .Colspan = 3,
                    .HorizontalAlignment = Element.ALIGN_CENTER,
                    .PaddingTop = 8,
                    .PaddingBottom = 8
                })

                ' 2️⃣ Description header
                table.AddCell(New PdfPCell(New Phrase("DESCRIPTION", boldFont)) With {
                    .Colspan = 3,
                    .HorizontalAlignment = Element.ALIGN_LEFT,
                    .Padding = 5,
                    .BackgroundColor = headerStyle
                })
                table.AddCell(New PdfPCell(New Phrase("ARMATURE ASSY - M36N", normalFont)) With {
                    .Colspan = 3,
                    .HorizontalAlignment = Element.ALIGN_CENTER,
                    .PaddingTop = 6,
                    .PaddingBottom = 6
                })

                ' 3️⃣ Quantity / Lot / Line
                'Dim lotNo = Convert.ToDateTime(dateLot).ToString("yyMMdd") & " - " & routeRow("shift").ToString()
                Dim lotNo = DbHelper.GenerateBaraCoreLot(dateLot) & procCode

                Dim cmdLotNo As New SqlCommand("
                    UPDATE pp_trace_route
                    SET lot_no = @LotNo
                    WHERE trace_id = @TraceID
                ", conn)

                cmdLotNo.Parameters.AddWithValue("@LotNo", lotNo)
                cmdLotNo.Parameters.AddWithValue("@TraceID", traceId)

                cmdLotNo.ExecuteNonQuery()

                table.AddCell(New PdfPCell(New Phrase("QUANTITY", boldFont)) With {.HorizontalAlignment = Element.ALIGN_CENTER, .BackgroundColor = headerStyle, .Padding = 5})
                table.AddCell(New PdfPCell(New Phrase("LOT NO", boldFont)) With {.HorizontalAlignment = Element.ALIGN_CENTER, .BackgroundColor = headerStyle, .Padding = 5})
                table.AddCell(New PdfPCell(New Phrase("DIE CORE", boldFont)) With {.HorizontalAlignment = Element.ALIGN_CENTER, .BackgroundColor = headerStyle, .Padding = 5})

                table.AddCell(New PdfPCell(New Phrase(routeRow("current_qty").ToString(), normalFont)) With {.HorizontalAlignment = Element.ALIGN_CENTER, .Padding = 5})
                table.AddCell(New PdfPCell(New Phrase(lotNo, normalFont)) With {.HorizontalAlignment = Element.ALIGN_CENTER, .Padding = 5})
                ' gabung value Line & Die dalam satu string
                Dim lineDieText As String = $"{routeRow("die")} {routeRow("line")}"
                ' letak dalam satu cell je
                table.AddCell(New PdfPCell(New Phrase(lineDieText, normalFont)) With {.HorizontalAlignment = Element.ALIGN_CENTER, .Padding = 5})

                ' --- BOTTOM SECTION: 3 ROWS, 2 COLUMNS HALF-HALF ---
                Dim mfgDate = Convert.ToDateTime(routeRow("updated_date")).ToString("yyyy-MM-dd")
                ' --- BOTTOM SECTION: 2 ROWS, 2 COLUMNS (right cell merged across 2 rows) ---
                Dim bottomTable As New PdfPTable(2) With {.WidthPercentage = 100}
                bottomTable.SetWidths(New Single() {1.0F, 1.0F}) ' 50/50 width

                ' Row 1: Header
                bottomTable.AddCell(New PdfPCell(New Phrase("MANUFACTURING DATE", boldFont)) With {
    .HorizontalAlignment = Element.ALIGN_CENTER,
    .VerticalAlignment = Element.ALIGN_MIDDLE,
    .BackgroundColor = headerStyle,
    .Padding = 5
})
                bottomTable.AddCell(New PdfPCell(New Phrase("QC STATUS", boldFont)) With {
    .HorizontalAlignment = Element.ALIGN_CENTER,
    .VerticalAlignment = Element.ALIGN_MIDDLE,
    .BackgroundColor = headerStyle,
    .Padding = 5
})

                ' Row 2: Values (merge right cell with next row)
                ' Row 2: Values (merge right cell with next row)
                bottomTable.AddCell(New PdfPCell(New Phrase(mfgDate, normalFont)) With {
    .HorizontalAlignment = Element.ALIGN_CENTER,
    .VerticalAlignment = Element.ALIGN_MIDDLE,
    .Padding = 5
})

                ' PASS cell, merged with row below
                Dim passCell As New PdfPCell(New Phrase("PASS", qcFont)) With {
    .HorizontalAlignment = Element.ALIGN_CENTER,  ' top-center
    .VerticalAlignment = Element.ALIGN_TOP,
    .PaddingTop = 5,
    .PaddingBottom = 5,
    .Rowspan = 2
}

                ' Letakkan logo di kanan bawah PASS cell
                If File.Exists("C:\Users\mys360114\Projects\traceability\logo.PNG") Then
                    Dim logoImg = Image.GetInstance("C:\Users\mys360114\Projects\traceability\logo.PNG")
                    logoImg.ScaleToFit(50, 50)
                    passCell.CellEvent = New BottomRightImageEvent(logoImg)
                End If

                bottomTable.AddCell(passCell)

                ' Row 3: QR + Trace/Employee in left cell only
                Dim qrTextCell As New PdfPCell() With {.Padding = 5, .VerticalAlignment = Element.ALIGN_MIDDLE}
                Dim qrTextTable As New PdfPTable(2) With {.WidthPercentage = 100}
                qrTextTable.SetWidths(New Single() {1.0F, 2.0F})

                ' QR
                Dim qrGen As New QRCodeGenerator()
                Dim qrData = qrGen.CreateQrCode(traceId, QRCodeGenerator.ECCLevel.Q)
                Dim qrCode = New QRCode(qrData)
                Using qrBitmap = qrCode.GetGraphic(20)
                    Using msQr As New MemoryStream()
                        qrBitmap.Save(msQr, ImageFormat.Png)
                        Dim qrImage = Image.GetInstance(msQr.ToArray())
                        qrImage.ScaleToFit(70, 70)
                        qrTextTable.AddCell(New PdfPCell(qrImage) With {
            .Border = Rectangle.NO_BORDER,
            .HorizontalAlignment = Element.ALIGN_CENTER,
            .VerticalAlignment = Element.ALIGN_MIDDLE
        })
                    End Using
                End Using

                ' Trace + Employee
                Dim textCell As New PdfPCell() With {.Border = Rectangle.NO_BORDER, .VerticalAlignment = Element.ALIGN_MIDDLE}
                textCell.AddElement(New Paragraph("TRACE ID: " & traceId.ToUpper(), normalFont))
                textCell.AddElement(New Paragraph("EMPLOYEE ID: " & empId.ToUpper(), normalFont))
                qrTextTable.AddCell(textCell)

                qrTextCell.AddElement(qrTextTable)

                bottomTable.AddCell(qrTextCell)


                ' Add bottomTable to main table as single row cell
                table.AddCell(New PdfPCell(bottomTable) With {.Colspan = 3, .Border = Rectangle.BOX, .Padding = 0})

                ' Add main table to document
                doc.Add(table)
            End Using

            doc.Close()
            Return ms.ToArray()
        End Using
    End Function
End Class
