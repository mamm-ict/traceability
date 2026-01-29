@Code
    ViewData("Title") = "Process Details"
    Dim process As Dictionary(Of String, String) = ViewData("Process")
End Code

<h1 class="mes-title">Process Detail</h1>

@If process IsNot Nothing AndAlso process.ContainsKey("QRCodeImage") Then
    @<div class="mes-card mes-process-card">
        <h2 class="mes-card-title">@process("ProcessName")</h2>

        <p><strong>Machine:</strong> @process("ProcessCode")</p>
        <p><strong>Category:</strong> @process("ProcFlowId")</p>
        <p><strong>Level:</strong> @process("ProcessLevel")</p>

        <img src='data:image/png;base64,@process("QRCodeImage")'
             class="mes-qr-large" />
    </div>
End If

<p>
    <a class="mes-link" href="@Url.Action("ProcessMaster", "Process")">
        Back to Process List
    </a>
</p>
<script>
    setTimeout(function () {
        window.location.href = '@Url.Action("ProcessMaster", "Process")';
    }, 20000);
</script>
