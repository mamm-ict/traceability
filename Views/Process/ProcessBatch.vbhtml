@Code
    ViewData("Title") = "Registered Process"

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

    <div class="mes-card shadow">

        <!-- Top Bar: TraceID + Date + Shift -->
        <div class="top-info">
            <h2>📦 @batch.TraceID</h2>
            <div class="top-meta">
                <div><strong>Date:</strong> @batch.CreatedDate.ToString("dd/MM/yyyy")</div>
                <div><strong>Shift:</strong> @batch.Shift</div>
            </div>
        </div>

        <!-- Details Grid -->
        <div class="details-grid">
            <div><strong>Model:</strong> @batch.Model</div>
            <div><strong>Die Core:</strong> @ViewData("DieCore")</div>
            <div><strong>Operator:</strong> @batch.OperatorID</div>
            <div><strong>Bara Core Lot:</strong> @batch.BaraCoreLot</div>
        </div>

        <!-- Current Process & Progress -->
        <div class="current-process">
            <div>Current Process</div>
            <div class="progress-container">
                <div class="progress-bar">
                    <div id="progress-fill">
                        @(If(progressPercent > 0, progressPercent & "%", ""))
                    </div>
                </div>
                <div class="current-name">@currentProcessName</div>
            </div>
            <div class="timer">
                Auto-redirect in <span id="timer">20</span> seconds
            </div>
        </div>

        <!-- Materials Used Section -->
        <div class="materials-section">
            <div>🗂️ Materials Used</div>

            @If CType(ViewData("MaterialsUsed"), List(Of MaterialLog)).Any() Then
                @<div class="materials-grid">
                    <!-- Header Row -->
                    <div class="header">Process</div>
                    <div class="header">Material</div>
                    <div class="header">Total Qty</div>
                    <div class="header">Vendor Lot</div>

@For Each mat In CType(ViewData("MaterialsUsed"), List(Of MaterialLog))
@<div>@mat.ProcCode</div>
@<div>@mat.LowerMaterial</div>
@<div>@mat.UsageQty @mat.UOM</div>
@<div>@mat.VendorLot</div>

@<div style="grid-column:1 / -1; height:1px; background:#ddd;"></div>
                    Next
                </div>
            Else
                            @<div class="no-materials">No materials registered yet.</div>
            End If
        </div>

    </div>
</div>


<style>
    /*.mes-card {
        width: 100%;*/ /* supaya responsive */
        /*max-width: 600px;*/ /* limit lebar non-fullscreen */
        /*margin: 30px auto;*/ /* horizontal center */
        /*border-radius: 14px;
        background: #fff;
        padding: 24px;
        font-family: 'Segoe UI', sans-serif;
        border-left: 6px solid #1a73e8;
        box-shadow: 0 6px 18px rgba(0,0,0,0.12);
        display: flex;
        flex-direction: column;
        gap: 16px;
        transition: transform 0.25s, box-shadow 0.25s;
    }

        .mes-card:hover {
            transform: translateY(-3px);
            box-shadow: 0 8px 24px rgba(0,0,0,0.15);
        }*/

    /* ===== Non-Fullscreen (default) ===== */
    /* ===== Non-Fullscreen Default ===== */
    .mes-container {
        width: 100%;
        max-width: 600px; /* normal card width */
        margin: 30px auto;
        padding: 24px;
    }

    .mes-card {
        width: 100%;
        padding: 24px;
        border-radius: 14px;
        background: #fff;
        border-left: 6px solid #1a73e8;
        box-shadow: 0 6px 18px rgba(0,0,0,0.12);
        display: flex;
        flex-direction: column;
        gap: 16px;
        font-size: 1rem; /* comfortable default */
    }

    /* Top info */
    .top-info {
        display: grid;
        grid-template-columns: 1fr auto;
        gap: 16px;
        align-items: start;
    }

        .top-info h2 {
            margin: 0;
            font-size: 1.6rem;
            color: #1a73e8;
            font-weight: 700;
        }

    .top-meta {
        display: flex;
        flex-direction: column;
        gap: 6px;
        font-size: 0.95rem;
        color: #555;
    }

    /* Details grid */
    .details-grid {
        display: grid;
        grid-template-columns: repeat(2, 1fr);
        gap: 14px;
        color: #555;
        font-size: 0.95rem;
    }

    /* Current process */
    .current-process div:first-child {
        font-weight: 600;
        color: #004d40;
        margin-bottom: 6px;
        font-size: 1rem;
    }

    .progress-container {
        display: flex;
        align-items: center;
        gap: 12px;
    }

    .progress-bar {
        flex: 1;
        height: 24px;
        background: #e0e0e0;
        border-radius: 12px;
        overflow: hidden;
    }

    #progress-fill {
        height: 100%;
        border-radius: 12px;
        display: flex;
        align-items: center;
        justify-content: center;
        font-weight: 600;
        color: #fff;
        font-size: 0.85rem;
    }

    .current-name {
        min-width: 140px;
        font-weight: 600;
        color: #004d40;
    }

    .timer {
        margin-top: 6px;
        font-size: 0.9rem;
        color: #888;
    }

    /* Materials grid */
    .materials-section div:first-child {
        font-weight: 600;
        color: #6a1b9a;
        margin-bottom: 10px;
        font-size: 1rem;
    }

    .materials-grid {
        display: grid;
        grid-template-columns: repeat(4, 1fr);
        gap: 10px;
        padding: 14px;
        background: #f5f5f5;
        border-radius: 10px;
        align-items: center;
        font-size: 0.9rem;
        max-height: 220px;
        overflow-y: auto;
    }

        .materials-grid .header {
            font-weight: 700;
            background: #e0e0e0;
            padding: 6px;
        }

    .no-materials {
        padding: 12px;
        color: #888;
    }

/* ===== Card Base ===== */
/* ===== Fullscreen tweaks: wider + bigger fonts ===== */
body.is-fullscreen {
    font-family: 'Segoe UI', Arial, sans-serif;
    background: #f4f7fc;
    margin: 0;
    padding: 0;
    font-size: 17px;        /* slightly bigger than last tweak */
    line-height: 1.45;
}

/* Full-width container */
body.is-fullscreen .mes-container {
    width: 96%;
    max-width: 1600px;      /* wider fullscreen card */
    margin: 0 auto;
    padding: 30px 2%;
    display: flex;
    flex-direction: column;
    align-items: center;
}

/* Card tweaks */
body.is-fullscreen .mes-card {
    width: 100%;
    max-width: 100%;
    padding: 24px 28px;
    border-radius: 14px;
    background: #fff;
    border-left: 6px solid #1a73e8;
    box-shadow: 0 6px 20px rgba(0,0,0,0.12);
    display: flex;
    flex-direction: column;
    gap: 16px;
    font-size: 1rem;         /* comfortable font size */
}

/* Top info */
body.is-fullscreen .top-info {
    display: grid;
    grid-template-columns: 1fr auto;
    gap: 16px;
    align-items: start;
}

body.is-fullscreen .top-info h2 {
    margin: 0;
    font-size: 1.8rem;       /* bigger than before */
    color: #1a73e8;
    font-weight: 700;
}

body.is-fullscreen .top-meta {
    display: flex;
    flex-direction: column;
    gap: 6px;
    font-size: 1rem;         /* readable meta */
    color: #555;
}

/* Details grid */
body.is-fullscreen .details-grid {
    display: grid;
    grid-template-columns: repeat(2, 1fr);
    gap: 16px;
    color: #555;
    font-size: 1rem;         /* slightly bigger */
}

/* Current process */
body.is-fullscreen .current-process div:first-child {
    font-weight: 600;
    color: #004d40;
    margin-bottom: 6px;
    font-size: 1.1rem;
}

body.is-fullscreen .progress-container {
    display: flex;
    align-items: center;
    gap: 14px;
}

body.is-fullscreen .progress-bar {
    flex: 1;
    height: 26px;
    background: #e0e0e0;
    border-radius: 12px;
    overflow: hidden;
}

body.is-fullscreen #progress-fill {
    height: 100%;
    border-radius: 12px;
    display: flex;
    align-items: center;
    justify-content: center;
    font-weight: 600;
    color: #fff;
    font-size: 0.9rem;
}

/* Materials grid */
body.is-fullscreen .materials-section div:first-child {
    font-weight: 600;
    color: #6a1b9a;
    margin-bottom: 10px;
    font-size: 1.1rem;
}

body.is-fullscreen .materials-grid {
    display: grid;
    grid-template-columns: repeat(4, 1fr);
    gap: 10px;
    padding: 14px;
    background: #f5f5f5;
    border-radius: 10px;
    align-items: center;
    font-size: 0.95rem;
    max-height: 260px;
    overflow-y: auto;
}

body.is-fullscreen .materials-grid .header {
    font-weight: 700;
    background: #e0e0e0;
    padding: 6px;
}

body.is-fullscreen .no-materials {
    padding: 12px;
    color: #888;
}

/* Responsive tweaks */
@@media(max-width:1400px) {
    body.is-fullscreen .details-grid {
        grid-template-columns: 1fr;
    }

    body.is-fullscreen .materials-grid {
        grid-template-columns: repeat(2, 1fr);
    }

    body.is-fullscreen .top-meta {
        flex-direction: column;
        gap: 8px;
    }
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
