@Code
    ViewData("Title") = "Batch History"
End Code

<h2 class="mes-title" style="margin-top:10px;">Batch History</h2>


<div class="mes-container">
    <div class="mes-panel" style="padding:0;">


        <table class="mes-table">
            <thead>
                <tr>
                    <th>Trace ID</th>
                    <th>Date</th>
                    <th>Shift</th>
                    <th>Model</th>
                    <th>Line</th>
                    <th>Operator</th>
                    <th>QR Code</th>
                </tr>
            </thead>
            <tbody>
                @For Each batch As Dictionary(Of String, String) In ViewData("BatchList")
                    @<tr>
                        <td>
                            <a href="@Url.Action("ShowQR", "Batch", New With {.TraceID = batch("TraceID")})" class="mes-link">
                                @batch("TraceID")
                            </a>
                        </td>
                        <td>@batch("CreatedDate")</td>
                        <td>@batch("Shift")</td>
                        <td>@batch("Model")</td>
                        <td>@batch("Line")</td>
                        <td>@batch("OperatorID")</td>
                        @*<td>
                            <ol class="mes-ol">
                                @For Each item As String In batch("RawMaterial").Split(","c)
                                    @<li>@item.Replace(":", " : ")</li>
                                Next
                            </ol>
                        </td>*@
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
</div>

<!-- QR Modal -->
<div id="qrModal" class="qr-modal">
    <span class="closeBtn" onclick="closeQRCode()">&times;</span>
    <img id="qrModalImg" class="qr-large" />
</div>

<style>
    .mes-container {
        width: 100%;
        padding: 18px;
    }

    .mes-panel {
        background: #f4f4f4;
        border: 3px solid #2b4c7e;
        border-radius: 6px;
        padding: 20px;
    }

    .mes-title {
        text-align: center;
        font-size: clamp(26px, 4vw, 34px);
        font-weight: 800;
        color: #2b4c7e;
        text-transform: uppercase;
    }

    /* TABLE */
    .mes-table {
        width: 100%;
        border-collapse: collapse;
        background: #ffffff;
        border: 3px solid #2b4c7e;
    }

        .mes-table th {
            background: #2b4c7e;
            color: white;
            padding: 12px;
            font-size: 16px;
            border-bottom: 3px solid #1e355a;
            text-transform: uppercase;
        }

        .mes-table td {
            padding: 10px;
            border-bottom: 2px solid #d0d0d0;
            font-size: 15px;
            font-weight: 600;
            color: #333;
        }

        .mes-table tr:hover {
            background: #e8f1ff;
        }

    .mes-link {
        color: #1a73e8;
        font-weight: 700;
        text-decoration: none;
    }

        .mes-link:hover {
            text-decoration: underline;
        }

    .mes-ol {
        margin: 0;
        padding-left: 18px;
        font-size: 14px;
    }

    /* QR Images */
    .qr-img {
        max-width: 70px;
        cursor: pointer;
        border: 2px solid #2b4c7e;
        border-radius: 4px;
        padding: 4px;
        background: white;
    }

    .qr-modal {
        display: none;
        position: fixed;
        z-index: 1000;
        left: 0;
        top: 0;
        width: 100%;
        height: 100%;
        background: rgba(0,0,0,0.8);
        justify-content: center;
        align-items: center;
    }

    .qr-large {
        max-width: 80%;
        max-height: 80%;
        border: 3px solid white;
        border-radius: 6px;
    }

    .closeBtn {
        position: absolute;
        top: 20px;
        right: 30px;
        font-size: 40px;
        color: white;
        cursor: pointer;
        font-weight: bold;
    }
</style>

<script>
    function enlargeQRCode(img) {
        document.getElementById("qrModal").style.display = "flex";
        document.getElementById("qrModalImg").src = img.src;
    }
    function closeQRCode() {
        document.getElementById("qrModal").style.display = "none";
    }
</script>