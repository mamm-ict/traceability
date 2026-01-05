@Code
    ViewData("Title") = "Process Material"
    Dim batch As Batch = CType(ViewData("Batch"), Batch)
    Dim processes As List(Of ProcessMaster) = CType(ViewData("Processes"), List(Of ProcessMaster))
    Dim logs As List(Of ProcessLog)

    If ViewData("Logs") IsNot Nothing Then
        logs = CType(ViewData("Logs"), List(Of ProcessLog))
    Else
        logs = New List(Of ProcessLog)()
    End If

    Dim enableRawMaterial As Boolean = False

    Dim lastLog = logs.OrderByDescending(Function(l) l.ScanTime).FirstOrDefault()
    If lastLog IsNot Nothing Then
        Dim lastProc = processes.FirstOrDefault(Function(p) p.ID = lastLog.ProcessID)
    End If

    Dim activeProcessId As Integer? = Nothing
    If lastLog IsNot Nothing AndAlso lastLog.Status = "In Progress" Then
        activeProcessId = lastLog.ProcessID
    End If

    Dim currentProcessName As String = "N/A"
    If activeProcessId.HasValue Then
        Dim activeProc = processes.FirstOrDefault(Function(p) p.ID = activeProcessId.Value)

        If activeProc IsNot Nothing Then
            currentProcessName = activeProc.Name
            If activeProc.MaterialFlag = 1 Then
                enableRawMaterial = True
            End If

        End If
    End If

    If lastLog IsNot Nothing Then
        ' Kalau takde In Progress, tunjuk last process
        Dim lastProc = processes.FirstOrDefault(Function(p) p.ID = lastLog.ProcessID)
        If lastProc IsNot Nothing Then currentProcessName = lastProc.Name
    End If

    Dim materials As New List(Of MaterialLog)

    If activeProcessId.HasValue Then
        materials = DbHelper.GetTraceMaterials(
            batch.TraceID,
            activeProcessId.Value,
            batch.PartCode
        )
    End If

    ViewData("Materials") = materials
    '' ======= FORCE ENABLE FOR TESTING =======
    'enableRawMaterial = True

    '' Pilih process pertama (atau ID mana-mana) untuk testing
    'If processes IsNot Nothing AndAlso processes.Count > 0 Then
    '    activeProcessId = processes.First().ID
    'End If
    '' ======================================
End Code

<div class="mes-container">

    <h2 class="mes-title">Process Material</h2>

    @If enableRawMaterial AndAlso activeProcessId.HasValue Then
        @<div class="mes-process-card">

            <h4 style="margin-bottom:12px;">
                Auto - redirect in <span id="timer">20</span> seconds...
            </h4>

            <h3 class="mes-card-subtitle" style="margin-bottom:16px;">
                Raw Materials
            </h3>

            <!-- hidden context -->
            <input type="hidden" id="traceId" value="@batch.TraceID" />
            <input type="hidden" id="procId" value="@activeProcessId.Value" />
            <input type="hidden" id="partCode" value="@batch.PartCode" />

            <!-- SCAN INPUT -->
            <input type="text"
                   id="materialQr"
                   class="mes-input"
                   autocomplete="off" autofocus
                   placeholder="Scan material QR here" />

            <!-- LIST TABLE -->
            <div style="overflow-x:auto; margin-top:20px;">
                <table class="mes-table">
                    <thead>
                        <tr>
                            <th hidden>Trace ID</th>
                            <th hidden>Proc ID</th>
                            <th hidden>Part Code</th>
                            <th>Lower Material</th>
                            <th>Batch Lot</th>
                            <th>Qty</th>
                            <th>UOM</th>
                            <th>Vendor</th>
                            <th>Vendor Lot</th>
                            <th>Action</th>
                        </tr>
                    </thead>

                    <tbody id="materialList">
                        @For Each m In CType(ViewData("Materials"), List(Of MaterialLog))
                            Dim rowClass = If(m.IsDuplicate, "duplicate-material", "")
                            @<tr class="@rowClass">
                                <td hidden>@m.TraceID</td>
                                <td hidden>@m.ProcID</td>
                                <td hidden>@m.PartCode</td>
                                <td>@m.LowerMaterial</td>
                                <td>@m.BatchLot</td>
                                <td>@m.UsageQty</td>
                                <td>@m.UOM</td>
                                <td>@m.VendorCode</td>
                                <td>@m.VendorLot</td>
                                <td>
                                    <button class="mes-btn-danger" onclick="removeMaterial(@m.ID, @m.IsDuplicate)">
                                        ✖
                                    </button>
                                </td>

                            </tr>
                        Next
                    </tbody>
                </table>
            </div>
        </div>
    End If
</div>

<style>
    .duplicate-material {
        background-color: #fff3cd; /* kuning light */
        color: #856404; /* teks warna kontras */
    }

    .mes-process-card {
        max-width: 700px;
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
        margin-bottom: 20px;
    }

        .mes-input:focus {
            border-color: #007bff;
            box-shadow: 0 0 6px rgba(0,123,255,0.3);
        }

    .mes-table th, .mes-table td {
        text-align: center;
        padding: 10px;
    }

    .mes-table th {
        background-color: #2b4c7e;
        color: #fff;
        font-weight: 700;
    }

    .mes-table tr:hover {
        background: #e8f1ff;
    }

    .mes-btn-danger {
        background-color: #ff4d4f;
        color: #fff;
        border: none;
        padding: 6px 12px;
        border-radius: 6px;
        cursor: pointer;
    }

        .mes-btn-danger:active {
            transform: scale(0.95);
        }

    .mes-card-subtitle {
        font-size: 20px;
        font-weight: 600;
        color: #2b4c7e;
        text-align: center;
    }

    #timer {
        font-weight: bold;
        color: #007bff;
    }
</style>

<script>
    let autoRedirectTimer;
    let countdown = 20; // 20 seconds
    const timerDisplay = document.getElementById("timer");

    function startAutoRedirect() {
        countdown = 20;
        timerDisplay.textContent = countdown;
    if (autoRedirectTimer) clearTimeout(autoRedirectTimer);

        // start new timer
       autoRedirectTimer = setInterval(() => {
           countdown--;
           timerDisplay.textContent = countdown;

           if (countdown <= 0) {
               clearInterval(autoRedirectTimer);
               window.location.href = "@Url.Action("ProcessBatch", "Process", New With {.TraceID = batch.TraceID})";
           }
       }, 1000);
    }

    // Start timer initially
    startAutoRedirect();

    // Reset timer every time user scans material
    document.getElementById("materialQr")?.addEventListener("change", function () {
        const qr = this.value.trim();
        if (!qr) return;

        const parts = qr.split("\t");
        if (parts.length !== 6) {
            alert("Invalid Material QR");
            this.value = "";
            startAutoRedirect();
            return;
        }

        const rawQty = parts[2].replace(/,/g, "").trim();
        const usageQty = parseInt(rawQty, 10);

        if (isNaN(usageQty)) {
            alert("Invalid quantity in QR");
            this.value = "";
            return;
        }

        const payload = {
            TraceID: document.getElementById("traceId").value,
            ProcID: parseInt(document.getElementById("procId").value, 10),
            PartCode: document.getElementById("partCode").value,
            LowerMaterial: parts[0],
            BatchLot: parts[1],
            UsageQty: usageQty,
            UOM: parts[3].toUpperCase(),
            VendorCode: parts[4],
            VendorLot: parts[5]
        };

        fetch("@Url.Action("ScanMaterial")", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload)
        })
        .then(r => r.text())
        .then(res => {
            if (res !== "OK") {
                alert(res);
                this.value = "";
                this.focus();
                window.location.href = window.location.href;

                return;
            }
            if (res === "DUPLICATE") {
                alert("⚠ Material already scanned before!");
            }

            window.location.href = window.location.href;

            this.value = "";
            this.focus();

            // Reset auto-redirect timer after scan
            startAutoRedirect();
        });
    });
</script>
<script>
   function removeMaterial(id, isDuplicate) {
        if (!isDuplicate) {
            alert("Cannot delete original material!");
            return;
        }

        if (!confirm("Remove this duplicate material?")) return;

        fetch("@Url.Action("DeleteTraceMaterial")", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ id: id })
        })
        .then(r => r.text())
        .then(res => {
            if (res === "OK") {
                location.reload();
            } else {
                alert(res);
            }
        });
    }
</script>