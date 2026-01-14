@Code
    ViewData("Title") = "ProcessBatch"

    Dim batch As Batch = CType(ViewData("Batch"), Batch)
    Dim processes As List(Of ProcessMaster) = CType(ViewData("Processes"), List(Of ProcessMaster))
    Dim logs As List(Of ProcessLog) = If(ViewData("Logs") IsNot Nothing, CType(ViewData("Logs"), List(Of ProcessLog)), New List(Of ProcessLog)())

    ' --- Determine current process name ---
    Dim lastLog = logs.OrderByDescending(Function(l) l.ScanTime).FirstOrDefault()
    Dim currentProcessName As String = "N/A"
    Dim currentProcessLevel As Integer = 0

    If lastLog IsNot Nothing Then
        Dim currentProc = processes.FirstOrDefault(Function(p) p.ID = lastLog.ProcessID)
        If currentProc IsNot Nothing Then
            currentProcessName = currentProc.Name
        End If
    End If

    ' --- Determine last completed level for progress calculation ---
    Dim lastCompletedLog = logs.Where(Function(l) l.Status = "Completed") _
                               .OrderByDescending(Function(l) l.ScanTime) _
                               .FirstOrDefault()

    If lastCompletedLog IsNot Nothing Then
        Dim lastCompletedProc = processes.FirstOrDefault(Function(p) p.ID = lastCompletedLog.ProcessID)
        If lastCompletedProc IsNot Nothing Then
            currentProcessLevel = lastCompletedProc.Level
        End If
    End If

    ' --- Calculate max level dynamically ---
    Dim maxLevel As Integer = processes.Max(Function(p) p.Level)

    ' --- Progress as % of completed levels ---
    Dim progressPercent As Integer = 0
    If maxLevel > 0 Then
        progressPercent = CInt((currentProcessLevel / maxLevel) * 100)
    End If
End Code

<div class="mes-container">

    <h2 class="mes-title">Process Registered</h2>
        <!-- Parent wrapper untuk center -->
        <div style="display:flex; justify-content:center; align-items:center; height:auto;">
            <div class="mes-card shadow" style="
        max-width:600px;
        border-radius:14px;
        background:#fff;
        padding:24px;
        font-family:'Segoe UI', sans-serif;
        border-left:6px solid #1a73e8;
        box-shadow:0 6px 18px rgba(0,0,0,0.12);
    ">
                <!-- --- Card content --- -->
                <!-- Top Bar: TraceID + Date + Shift -->
                <div style="display:flex; justify-content:space-between; align-items:center; margin-bottom:20px;">
                    <h2 style="margin:0; font-size:1.6rem; color:#1a73e8; font-weight:700;">📦 @batch.TraceID</h2>
                    <div style="text-align:right; font-size:0.95rem; color:#555; margin-left:100px">
                        <div><strong>Date:</strong> @batch.CreatedDate.ToString("dd/MM/yyyy")</div>
                        <div><strong>Shift:</strong> @batch.Shift</div>
                    </div>
                </div>

                <!-- Details Grid -->
                <div style="display:grid; grid-template-columns:1fr 1fr; gap:14px; margin-bottom:22px; color:#555;">
                    <div><strong>Model:</strong> @batch.Model</div>
                    <div><strong>Line:</strong> @batch.Line</div>
                    <div><strong>Operator:</strong> @batch.OperatorID</div>
                    <div><strong>Bara Core Lot:</strong> @batch.BaraCoreLot</div>
                </div>

                <!-- Current Process & Progress -->
                <div>
                    <div style="font-weight:600; color:#004d40; margin-bottom:8px; font-size:1rem;">🔄 Current Process</div>
                    <div style="display:flex; align-items:center; gap:12px;">
                        <!-- Progress Bar -->
                        <div style="flex:1; position:relative; height:24px; background:#e0e0e0; border-radius:12px; overflow:hidden; box-shadow: inset 0 1px 3px rgba(0,0,0,0.1);">
                            <div id="progress-fill" style="
                        width:@progressPercent%;
                        height:100%;
                        border-radius:12px;
                        display:flex;
                        align-items:center;
                        justify-content:center;
                        color:#fff;
                        font-weight:600;
                        font-size:0.85rem;
                        transition: width 0.6s ease, background 0.6s ease;
                    ">
                                @(If(progressPercent > 0, progressPercent & "%", ""))
                            </div>
                        </div>

                        <!-- Current Process Name -->
                        <div style="min-width:140px; font-weight:600; color:#004d40;">@currentProcessName</div>
                    </div>

                    <!-- Timer -->
                    <div style="margin-top:10px; font-size:0.9rem; color:#888;">
                        Auto-redirect in <span id="timer">20</span> seconds
                    </div>
                </div>
        </div>
    </div>
   

</div>
   

    <style>
        .mes-card:hover {
            transform: translateY(-3px);
            box-shadow: 0 8px 24px rgba(0,0,0,0.15);
            transition: all 0.25s;
        }
        .mes-card {
            max-width: 700px;
            margin: 30px auto;
            padding: 25px;
/*            background: #fff;*/
/*            border-radius: 15px;*/
/*            box-shadow: 0 10px 25px rgba(0,0,0,0.08);*/
        }
    </style>

    <script>
    let countdown = 20;
    const timerDisplay = document.getElementById("timer");

    function startTimer() {
        countdown = 20;
        timerDisplay.textContent = countdown;

        const interval = setInterval(() => {
            countdown--;
            timerDisplay.textContent = countdown;



            if (countdown <= 0) {
                clearInterval(interval);
                window.location.href = "@Url.Action("StartProcess", "Process")";
                 if (progressPercent === 100) {
                                //clearInterval(interval);
                                window.location.href = "@Url.Action("FinalProcess", "Process")";

                            }
            }
        }, 1000);
    }

    startTimer();

    // Dynamic gradient color for progress bar
    const progressFill = document.getElementById("progress-fill");
    let progressPercent = @progressPercent; // injected VB value

    // Ensure tiny width for 0% so color shows
    let displayWidth = progressPercent;
    if (progressPercent === 0) {
        displayWidth = 2;
    }

    let bgColor = "";
    if (progressPercent === 0) {
        bgColor = "linear-gradient(90deg, #e53935 100%, transparent 100%)"; // red indicator
    } else if (progressPercent < 50) {
        bgColor = "linear-gradient(90deg, #ffb74d, #ff9800)"; // yellow/orange
    } else if (progressPercent < 100) {
        bgColor = "linear-gradient(90deg, #00bcd4, #00acc1)"; // cyan/blue
    } else {
        bgColor = "linear-gradient(90deg, #66bb6a, #43a047)"; // green
    }

    progressFill.style.background = bgColor;
    progressFill.style.width = displayWidth + "%";

    </script>
