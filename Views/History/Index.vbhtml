@Code
    ViewData("Title") = "Batch History"

    ' Cast ViewData("BatchList") to List(Of Dictionary(Of String, String))
    Dim batchList = TryCast(ViewData("BatchList"), List(Of Dictionary(Of String, String)))

    Dim groupedBatches As IEnumerable(Of Object) = {}
    If batchList IsNot Nothing Then
        ' Add a temporary property for grouping
        groupedBatches = batchList.
            Select(Function(b) New With {Key .CreatedDate = b("CreatedDate"), Key .Batch = b}).
            GroupBy(Function(x) x.CreatedDate).
            OrderByDescending(Function(g) g.Key).
            Select(Function(g) New With {Key .CreatedDate = g.Key, Key .Batches = g.Select(Function(x) x.Batch)})
    End If
End Code


<h2 class="mes-title" style="margin-top:10px;">Batch History</h2>

<div class="mes-container">
    <div class="mes-panel">
        @For Each group In groupedBatches
            @<div class="date-group">
                <h3 class="date-header" onclick="toggleGroup(this)">
                    @group.CreatedDate
                    <span class="arrow">&#9654;</span>
                </h3>
                <table class="mes-table group-table" style="display:none;">
                    <thead>
                        <tr>
                            <th> Trace ID</th>
                            <th>Date</th>
                            <th> Shift</th>
                            <th> Model</th>
                            <th> Line</th>
                            <th> Operator</th>
                            <th>QR Code</th>
                        </tr>
                    </thead>
                    <tbody>
                        @For Each batch As Dictionary(Of String, String) In group.Batches

                            @<tr>
                                <td>
                                    <a href="@Url.Action("ProcessLogs", "History", New With {.traceId = batch("TraceID")})" class="mes-link">
                                        @batch("TraceID")
                                    </a>
                                </td>

                                <td>@batch("CreatedDate")</td>
                                <td>@batch("Shift")</td>
                                <td>@batch("Model")</td>
                                <td>@batch("Line")</td>
                                <td>@batch("OperatorID")</td>
                                <td>
                                    <img src="data:image/png;base64,@batch("QRCodeImage")"
                                         alt="QR Code"
                                         class="qr-img"
                                         onclick="enlargeQRCode(this)" />
                                </td>
                            </tr>
                        Next
                    </tbody>
                </table>
            </div>
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
