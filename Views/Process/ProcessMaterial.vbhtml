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

            <div style="margin-bottom:12px;">
                <label style="display:flex; align-items:center; gap:8px;">
                    <input type="checkbox" id="enableManualMaterial" />
                    <strong>Manual Material Entry</strong>
                </label>
            </div>

            <!-- SCAN INPUT -->
            <input type="text"
                   id="materialQr"
                   class="mes-input"
                   autocomplete="off" autofocus
                   placeholder="Scan material QR here" />




            <!-- LIST TABLE -->
            <div style="overflow-x:auto; margin-top:20px;">
                <div id="manualMaterialForm" style="display:none; margin-bottom:20px;">

                    <input class="mes-input" id="mLowerMaterial" placeholder="Lower Material" />
                    <input class="mes-input" id="mBatchLot" placeholder="Batch Lot" />

                    <div style="display:flex; gap:10px;">
                        <input class="mes-input" id="mQty" placeholder="Qty" type="number" />
                        <input class="mes-input" id="mUom" placeholder="UOM" />
                    </div>

                    <input class="mes-input" id="mVendor" placeholder="Vendor Code" />
                    <input class="mes-input" id="mVendorLot" placeholder="Vendor Lot" />

                    <button type="button" class="mes-btn-primary" id="addManualMaterial">
                        ➕ Add Manual Material
                    </button>

                </div>
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
                            @<tr class="@rowClass"
                                 data-traceid="@m.TraceID"
                                 data-procid="@m.ProcID"
                                 data-partcode="@m.PartCode"
                                 data-lowermaterial="@m.LowerMaterial"
                                 data-batchlot="@m.BatchLot"
                                 data-usageqty="@m.UsageQty"
                                 data-uom="@m.UOM"
                                 data-vendorcode="@m.VendorCode"
                                 data-vendorlot="@m.VendorLot"
                                 data-isduplicate="@m.IsDuplicate">

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
            <button class="mes-btn-primary" id="submitMaterial">Submit</button>

        </div>
    End If
</div>

<style>
    .mes-btn-primary {
        width: 100%;
        padding: 12px;
        margin-top: 20px;
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
    const manualToggle = document.getElementById("enableManualMaterial");
    const manualForm = document.getElementById("manualMaterialForm");
    const scanInput = document.getElementById("materialQr");

    manualToggle.addEventListener("change", function () {
        if (this.checked) {
            manualForm.style.display = "block";
            scanInput.value = "";
            scanInput.blur();              // ⬅️ PENTING
            scanInput.disabled = true;
        } else {
            manualForm.style.display = "none";
            scanInput.disabled = false;
            scanInput.focus();             // scanner only when enabled
        }
    });
</script>
<script>
let autoRedirectTimer;
let countdown = 200; // 20 seconds
const timerDisplay = document.getElementById("timer");

// Start or reset auto-redirect timer
function startAutoRedirect() {
    countdown = 200;
    timerDisplay.textContent = countdown;

    if (autoRedirectTimer) clearInterval(autoRedirectTimer);

            // start new timer
    autoRedirectTimer = setInterval(() => {
        countdown--;
        timerDisplay.textContent = countdown;

        if (countdown <= 0) {
            clearInterval(autoRedirectTimer);
            window.location.href = '@Url.Action("ProcessBatch", "Process", New With {.TraceID = batch.TraceID})';
        }
    }, 1000);
}

// Start timer initially
startAutoRedirect();

// Submit button logic
const submitBtn = document.getElementById("submitMaterial");

if (submitBtn) {
    submitBtn.addEventListener("click", function () {


        const rows = document.querySelectorAll("#materialList tr");
        if (rows.length === 0) {
            alert("No material scanned.");
            return;
        }
        else {
            // STOP auto redirect timer
            if (autoRedirectTimer) {
                clearInterval(autoRedirectTimer);
            }

        // Optional safety
        this.disabled = true;

        // DIRECT REDIRECT
        window.location.href =
            '@Url.Action("ProcessBatch","Process", New With {.TraceID = batch.TraceID})';
        }

    });
}


// QR scan handler
    document.getElementById("materialQr")?.addEventListener("change", function () {
        // ⛔ BLOCK SCANNER IF MANUAL MODE
        if (manualToggle.checked) {
            this.value = "";
            return;
        }

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

    fetch('@Url.Action("ScanMaterial")', {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload)
    })
    .then(r => r.text())
    .then(res => {
        if (res === "DUPLICATE") {
            alert("⚠ Material already scanned before!");
        } else if (res !== "OK") {
            alert(res);
            this.value = "";
            this.focus();
            // Optional: reload page if you want duplicate prevention
            window.location.href = window.location.href;
            return;
        }

        // Clear input and focus
        this.value = "";
        this.focus();
        window.location.href = window.location.href;

        // Reset auto-redirect timer
        startAutoRedirect();
    })
    .catch(err => {
        alert("Scan error: " + err);
        this.value = "";
        this.focus();
        startAutoRedirect();
    });
});

document.getElementById("addManualMaterial")
?.addEventListener("click", function () {

    const lower = mLowerMaterial.value.trim();
    const lot   = mBatchLot.value.trim();
    const qty   = parseInt(mQty.value, 10);
    const uom   = mUom.value.trim().toUpperCase();
    const v     = mVendor.value.trim();
    const vlot  = mVendorLot.value.trim();

    if (!lower || !lot || isNaN(qty)) {
        alert("Incomplete manual material");
        return;
    }

    const payload = {
        TraceID: traceId.value,
        ProcID: parseInt(procId.value),
        PartCode: partCode.value,
        LowerMaterial: lower,
        BatchLot: lot,
        UsageQty: qty,
        UOM: uom,
        VendorCode: v,
        VendorLot: vlot,
        IsManual: true
    };

    fetch('@Url.Action("ScanMaterial","Process")', {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload)
    })
    .then(r => r.text())
    .then(res => {
        if (res !== "OK") {
            alert(res);
            return;
        }

        // UI only AFTER DB success
        const tr = document.createElement("tr");
        tr.dataset.manual = "1";
        tr.innerHTML = `
            <td>${lower}</td>
            <td>${lot}</td>
            <td>${qty}</td>
            <td>${uom}</td>
            <td>${v}</td>
            <td>${vlot}</td>
            <td><button class="mes-btn-danger">✖</button></td>
        `;
        tr.querySelector("button").onclick = () => tr.remove();
        materialList.appendChild(tr);

        // clear form
        ["mLowerMaterial","mBatchLot","mQty","mUom","mVendor","mVendorLot"]
            .forEach(id => document.getElementById(id).value = "");

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
