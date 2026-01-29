@Code
    ViewData("Title") = "NG Disposal"
    Dim batch As Batch = CType(ViewData("Batch"), Batch)
    Dim processes As List(Of ProcessMaster) = CType(ViewData("Processes"), List(Of ProcessMaster))
    Dim logs As List(Of ProcessLog) = CType(ViewData("Logs"), List(Of ProcessLog))

    ' Ambil first pending completion log
    Dim pendingLog = logs.Where(Function(l) l.Status = "Pending Completion") _
                         .OrderBy(Function(l) l.ScanTime) _
                         .FirstOrDefault()

    Dim activeProcessName As String = "N/A"
    Dim currentProcessId As Integer? = Nothing
    If pendingLog IsNot Nothing Then
        currentProcessId = pendingLog.ProcessID
        Dim proc = processes.FirstOrDefault(Function(p) p.ID = pendingLog.ProcessID)
        If proc IsNot Nothing Then activeProcessName = proc.Name
    End If
End Code
@Code
    Dim scannedProcessIdValue As Integer = 0
    Dim scannedOperatorIdValue As String = ""

    If ViewData("ScannedProcessId") IsNot Nothing Then
        scannedProcessIdValue = Convert.ToInt32(ViewData("ScannedProcessId"))
    End If

    If ViewData("ScannedOperatorId") IsNot Nothing Then
        scannedOperatorIdValue = ViewData("ScannedOperatorId").ToString()
    End If
End Code

<div class="mes-container">

    <h2 class="mes-title">NG Disposal</h2>

    @If pendingLog IsNot Nothing AndAlso currentProcessId.HasValue Then
        @<div class="mes-process-card">

            <h4 style="margin-bottom:12px;">
                Auto - redirect in <span id="timer">20</span> seconds...
            </h4>

            <h3 class="mes-card-subtitle">@activeProcessName</h3>

            <!-- Hidden fields -->
            <input type="hidden" id="scannedProcessId" value="@scannedProcessIdValue" />
            <input type="hidden" id="scannedOperatorId" value="@scannedOperatorIdValue" />


            <input type="hidden" id="traceId" value="@batch.TraceID" />
            <input type="hidden" id="logId" value="@pendingLog.ID" />

            <!-- INPUT BUFFER -->
            <label class="mes-label" hidden>Qty Out</label>
            <input type="number" id="qtyOut" class="mes-input" min="0" placeholder="Enter Qty Out" hidden />

            <label class="mes-label">Qty Reject</label>
            <input type="number" id="qtyReject" class="mes-input vk-input" min="0" placeholder="Enter Qty Reject" />

            <button class="mes-btn-primary" id="submitBuffer">Submit</button>

        </div>
    Else
        @<p> No pending buffer input.</p>
    End If

</div>

<style>
    .mes-process-card {
        max-width: 500px;
        margin: 30px auto;
        padding: 25px;
        background: #fff;
        border-radius: 15px;
        box-shadow: 0 10px 25px rgba(0,0,0,0.08);
    }

    .mes-input {
        width: 100%;
        padding: 14px 16px;
        font-size: 18px;
        border-radius: 10px;
        border: 2px solid #ccc;
        outline: none;
        margin-bottom: 16px;
    }

        .mes-input:focus {
            border-color: #007bff;
            box-shadow: 0 0 6px rgba(0,123,255,0.3);
        }

    .mes-card-subtitle {
        font-size: 20px;
        font-weight: 600;
        color: #2b4c7e;
        text-align: center;
        margin-bottom: 16px;
    }

    .mes-btn-primary {
        width: 100%;
        padding: 12px;
        background-color: #2b4c7e;
        color: #fff;
        border: none;
        border-radius: 8px;
        cursor: pointer;
        font-size: 16px;
    }

        .mes-btn-primary:active {
            transform: scale(0.97);
        }

    #timer {
        font-weight: bold;
        color: #007bff;
    }
</style>

<script>
    window.onload = function () {
        alert("ProcessID: " + scannedProcessIdValue + ", OperatorID: " + scannedOperatorIdValue + "huhu" + pendingLog.ID);
    };

    let autoRedirectTimer;

    let countdown = 200;
    const timerDisplay = document.getElementById("timer");

    function startAutoRedirect() {
        countdown = 200;
        timerDisplay.textContent = countdown;
        if (autoRedirectTimer) clearTimeout(autoRedirectTimer);

        autoRedirectTimer = setInterval(() => {
            countdown--;
            timerDisplay.textContent = countdown;
            if (countdown <= 0) {
                clearInterval(autoRedirectTimer);
                window.location.href = "@Url.Action("ProcessBatch", "Process", New With {.TraceID = batch.TraceID})";
            }
        }, 1000);
    }

    startAutoRedirect();

    document.getElementById("submitBuffer")?.addEventListener("click", function() {
        const btn = this;
        const logId = parseInt(document.getElementById("logId").value, 10);
        const traceId = document.getElementById("traceId").value;
        const qtyOut = parseInt(document.getElementById("qtyOut").value, 10) || 0;
        const qtyReject = parseInt(document.getElementById("qtyReject").value, 10) || 0;
        const scannedProcessId = document.getElementById("scannedProcessId").value;
        const scannedOperatorId = document.getElementById("scannedOperatorId").value;

        if (qtyOut < 0 || qtyReject < 0) {
            alert("Qty cannot be negative");
            return;
        }

        btn.disabled = true; // prevent multiple clicks

        const payload = { logId, qtyOut, qtyReject, scannedProcessId, scannedOperatorId };

        fetch("@Url.Action("CompleteBuffer")", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload)
        })
        .then(r => r.json())
        .then(res => {
            if (!res.success) {
                alert(res.message);
                btn.disabled = false;
                return;
            }

            clearInterval(autoRedirectTimer);
            if (res.isFinal) {
                // buka PDF dalam tab baru
                window.open("@Url.Action("DownloadTracePdf", "Process")?traceId=" + res.traceId, "_blank");

                // redirect ke ProcessBatch
                window.location.href = "@Url.Action("ProcessBatch", "Process", New With {.TraceID = batch.TraceID})";
            } else {
                // Non-final: redirect seperti biasa
                window.location.href = res.redirectUrl;
            }
        })
        .catch(err => {
            alert("Error submitting buffer: " + err);
            btn.disabled = false;
        });
    });

</script>
