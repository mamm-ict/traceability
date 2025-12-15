@Imports System.Linq

@Code
    ViewData("Title") = "Process QR Codes"
    Dim processList As List(Of Dictionary(Of String, String)) = CType(ViewData("ProcessList"), List(Of Dictionary(Of String, String)))

    ' Group by first 3 letters of ProcessName safely
    Dim groupedProcesses = processList _
        .GroupBy(Function(p)
                     Dim name As String = p("ProcessCode")
                     If name.Length >= 3 Then
                         Return name.Substring(0, 3)
                     Else
                         Return name
                     End If
                 End Function) _
        .ToList()

    @Functions
        Function RemoveLast2Digits(desc As String) As String
            If String.IsNullOrWhiteSpace(desc) Then Return desc

            Dim parts = desc.Split(" "c)
            If parts.Length >= 2 AndAlso IsNumeric(parts.Last()) Then
                Return String.Join(" ", parts.Take(parts.Length - 1))
            End If

            Return desc
        End Function
    End Functions
End Code

<h2 class="mes-title" style="margin-top:10px;">Process QR Codes</h2>

<div class="mes-container">
    <div class="mes-panel">

        @For Each prefixGroup In groupedProcesses

            @<h3 class="mes-title" style="font-size:22px; margin-top:25px;">
                @RemoveLast2Digits(prefixGroup.First()("ProcessName"))
            </h3>

            @<table class="mes-table">
                <thead>
                    <tr>
                        <th> No</th>
                        <th> Process Code</th>
                        <th> QR Code</th>
                    </tr>
                </thead>

                <tbody>
                    @Code Dim counter As Integer = 1 End Code

                    @For Each process As Dictionary(Of String, String) In prefixGroup
                        @<tr>
                            <td>@counter</td>

                            <td>
                                <a class="mes-link"
                                   href="@Url.Action("Detail", "Process", New With {.processId = process("ProcessID")})">
                                    @process("ProcessCode")
                                </a>
                            </td>

                            <td>
                                <img src="data:image/png;base64,@process("QRCodeImage")"
                                     class="qr-img"
                                     onclick="enlargeQRCode(this)" />
                            </td>
                        </tr>

                        @Code counter += 1 End Code
                    Next
                </tbody>
            </table>
        Next

    </div>
</div>


<!-- QR Modal: follow History page style exactly -->
<div id="qrModal" class="qr-modal">
    <span class="closeBtn" onclick="closeQRCode()">&times;</span>
    <img id="qrModalImg" class="qr-large" />
</div>


<script>
    function enlargeQRCode(img) {
        document.getElementById("qrModal").style.display = "flex";
        document.getElementById("qrModalImg").src = img.src;
    }

    function closeQRCode() {
        document.getElementById("qrModal").style.display = "none";
    }
</script>
