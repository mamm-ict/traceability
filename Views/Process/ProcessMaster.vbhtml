@Imports System.Linq

@Code
    ViewData("Title") = "Process Master"
    Dim processList As List(Of Dictionary(Of String, String)) = CType(ViewData("ProcessList"), List(Of Dictionary(Of String, String)))

    ' Group by first 3 letters of ProcessCode safely
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

<h2 class="mes-title" style="margin-top:10px;">Process Master</h2>

<div class="mes-container">
    <div class="mes-panel">

        @For Each prefixGroup In groupedProcesses
            @Code
                Dim firstProcess = prefixGroup.First()
                Dim level As Integer = CInt(firstProcess("ProcessLevel"))

                Dim displayName As String = RemoveLast2Digits(firstProcess("ProcessName"))

                Dim headerText As String = $"{level}. {displayName}"
            End Code

            @<h3 class="date-header" onclick="toggleGroup(this)">
                @headerText
                <span class="arrow">&#9654;</span>
            </h3>

            @<table class="mes-table group-table" style="display:none;">
                <thead>
                    <tr>
                        <th> No</th>
                        <th> Machine</th>
                        <th> QR Code</th>
                    </tr>
                </thead>
                <tbody>
                    @Code Dim counter As Integer = 1 End Code
                    @For Each process As Dictionary(Of String, String) In prefixGroup
                        @<tr>
                            <td>@counter</td>
                            <td>
                                <a class="mes-link" href="@Url.Action("Detail", "Process", New With {.processId = process("ProcessID")})">
                                    @process("ProcessCode")
                                </a>
                            </td>
                            <td>
                                <img src="data:image/png;base64,@process("QRCodeImage")"
                                     alt="QR Code" class="qr-img" onclick="enlargeQRCode(this)" />
                            </td>
                        </tr>
                        @Code counter += 1 End Code
                    Next
                </tbody>
            </table>
        Next
    </div>
</div>

<!-- QR Modal -->
<div id="qrModal" class="qr-modal">
    <span class="closeBtn" onclick="closeQRCode()">&times;</span>
    <img id="qrModalImg" class="qr-large" />
</div>

<script>
    function toggleGroup(header) {
        const table = header.nextElementSibling;
        const arrow = header.querySelector(".arrow");
        if (table.style.display === "none") {
            table.style.display = "table";
            arrow.innerHTML = "&#9660;"; // down arrow
        } else {
            table.style.display = "none";
            arrow.innerHTML = "&#9654;"; // right arrow
        }
    }

    function enlargeQRCode(img) {
        document.getElementById("qrModal").style.display = "flex";
        document.getElementById("qrModalImg").src = img.src;
    }

    function closeQRCode() {
        document.getElementById("qrModal").style.display = "none";
    }
</script>

<style>
    .date-header {
        background: #2b4c7e;
        color: white;
        padding: 10px 15px;
        cursor: pointer;
        margin: 0;
        font-size: 18px;
        user-select: none;
        display: flex;
        justify-content: space-between;
        align-items: center;
    }

        .date-header:hover {
            background: #1e355a;
        }

    .arrow {
        font-size: 18px;
    }

    .group-table {
        margin-bottom: 20px;
    }
</style>
