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
    Dim activeProc = processes.FirstOrDefault(Function(p) p.ID = activeProcessId.Value)
    Dim currentProcessName As String = "N/A"
    If activeProcessId.HasValue Then


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

        ' Pass required materials (just lower_item strings) to JS
        Dim requiredMaterials As List(Of Dictionary(Of String, String)) =
    DbHelper.GetRequiredMaterials(batch.TraceID, activeProcessId.Value, batch.PartCode)

        ViewData("RequiredMaterials") = requiredMaterials
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
                Raw Materials for @activeProc.Name
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

            <div id="materialStatus"
                 style="margin-top:10px; font-weight:bold;"></div>
            <!-- LIST TABLE -->
            <div style="overflow-x:auto; margin-top:20px;">
                <div id="manualMaterialForm" style="display:none; margin-bottom:20px;">

                    <input class="mes-input vk-input" id="mLowerMaterial" placeholder="Lower Material" />
                    <input class="mes-input vk-input" id="mBatchLot" placeholder="Batch Lot" />

                    <div>
                        <input class="mes-input vk-input" id="mQty" placeholder="Qty" type="number" />
                        <input class="mes-input vk-input" id="mUom" placeholder="UOM" />
                    </div>

                    <input class="mes-input vk-input" id="mVendor" placeholder="Vendor Code" />
                    <input class="mes-input vk-input" id="mVendorLot" placeholder="Vendor Lot" />

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
                                 data-lowerdesc="@m.LowerDesc"
                                 data-batchlot="@m.BatchLot"
                                 data-usageqty="@m.UsageQty"
                                 data-uom="@m.UOM"
                                 data-vendorcode="@m.VendorCode"
                                 data-vendorlot="@m.VendorLot"
                                 data-isduplicate="@m.IsDuplicate">

                                <td>@m.LowerDesc</td>
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

    /* ===== Fullscreen Only ===== */
    body.is-fullscreen {
        /* Card */
        .mes-process-card

    {
        max-width: 900px;
        padding: 30px;
        font-size: 1.05rem;
    }

    /* Inputs */
    .mes-input {
        font-size: 1.1rem;
        padding: 16px 18px;
    }

    /* Qty + UOM row: center aligned */
    /* ===== Fullscreen Only: Manual Qty + UOM Alignment ===== */
    /* ===== Fullscreen Only: Qty + UOM Center Fix ===== */
    body.is-fullscreen #manualMaterialForm > div {
        display: flex !important;
        gap: 12px !important;
        align-items: center !important; /* center vertically */
    }

        body.is-fullscreen #manualMaterialForm > div input {
            flex: 1;
            height: 56px; /* match other manual inputs */
            padding: 16px 18px;
            font-size: 1.1rem;
            box-sizing: border-box;
            vertical-align: middle; /* extra safety */
        }

    /* Checkbox */
    #enableManualMaterial {
        transform: scale(1.6);
        margin-right: 10px;
    }

    label[for="enableManualMaterial"], label strong {
        font-size: 1.1rem;
        line-height: 1.4;
        vertical-align: middle;
    }

    /* Buttons */
    .mes-btn-primary {
        font-size: 1.1rem;
        padding: 14px 18px;
    }

    .mes-btn-danger {
        font-size: 1rem;
        padding: 8px 14px;
    }

    /* Table fonts */
    .mes-table th, .mes-table td {
        font-size: 1rem;
        padding: 12px;
    }

    .mes-card-subtitle {
        font-size: 1.25rem;
    }

    #timer {
        font-size: 1.1rem;
    }

    }
</style>

<script>
    const manualToggle = document.getElementById("enableManualMaterial");
    const manualForm = document.getElementById("manualMaterialForm");
    const scanInput = document.getElementById("materialQr");
    const statusDiv = document.getElementById("materialStatus");
    let permanentMessage = "";
    let permanentType = "";

    // Helper to show messages without Alert Box
    function showStatus(msg, type = "info") {

        statusDiv.textContent = msg;

        switch (type) {

            case "success-perm":
                statusDiv.style.color = "green";
                permanentMessage = msg;
                permanentType = "green";
                return;

            case "error-perm":
                statusDiv.style.color = "red";
                permanentMessage = msg;
                permanentType = "red";
                return;

            case "success":
                statusDiv.style.color = "green";
                break;

            case "error":
                statusDiv.style.color = "red";
                break;

            default:
                statusDiv.style.color = "black";
        }

        // Auto clear temporary message
        setTimeout(() => {
            if (permanentMessage) {
                statusDiv.textContent = permanentMessage;
                statusDiv.style.color = permanentType;
            } else {
                statusDiv.textContent = "";
            }
        }, 5000);
    }

    manualToggle.addEventListener("change", function () {
        if (this.checked) {
            manualForm.style.display = "block";
            scanInput.value = "";
            scanInput.blur();
            document.getElementById("mLowerMaterial").focus();
            scanInput.disabled = true;
            scanInput.style.display = "none";
        } else {
            manualForm.style.display = "none";
            scanInput.disabled = false;
            scanInput.style.display = "inline-block";
            scanInput.focus();
            
        }
    });
</script>

<script>
    let autoRedirectTimer;
    let countdown = 200; // 20 seconds
    const timerDisplay = document.getElementById("timer");

    function startAutoRedirect() {
        countdown = 200;
        timerDisplay.textContent = countdown;
        if (autoRedirectTimer) clearInterval(autoRedirectTimer);

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

    const submitBtn = document.getElementById("submitMaterial");
    if (submitBtn) {
        submitBtn.addEventListener("click", function () {
            const check = checkRequiredMaterials();
            if (!check.ok) {
                // Using standard confirm instead of alert so user has choice,
                // or just showStatus if strict no-popup needed.
                // Here we use showStatus to respect "no message box"
                //showStatus("❌ Missing: " + check.missing.join(", "), "true");
                document.getElementById("materialQr").focus();
                updateMaterialStatus();
              
                return;
            }

            const rows = document.querySelectorAll("#materialList tr");
            if (rows.length === 0) {
                showStatus("No material scanned.", "true");
                document.getElementById("materialQr").focus();
                updateMaterialStatus();
                return;
            } else {
                if (autoRedirectTimer) clearInterval(autoRedirectTimer);
                this.disabled = true;
                window.location.href = '@Url.Action("ProcessBatch", "Process", New With {.TraceID = batch.TraceID})';
            }
        });
    }

    // ============================================================
    // 🛑 KEYDOWN HANDLER (Blocks Tab & Enter)
    // ============================================================
    const qrInputEl = document.getElementById("materialQr");

    if(qrInputEl) {
        qrInputEl.addEventListener("keydown", function(e) {

            // 1. BLOCK TAB (Key 9)
            if (e.key === "Tab" || e.keyCode === 9) {
                e.preventDefault(); // Stop focus movement

                // Manually insert a tab character so delimiter logic works
                const start = this.selectionStart;
                const end = this.selectionEnd;
                this.value = this.value.substring(0, start) + "\t" + this.value.substring(end);
                this.selectionStart = this.selectionEnd = start + 1;
            }

            // 2. BLOCK ENTER (Key 13) -> TRIGGERS PROCESSING
            else if (e.key === "Enter" || e.keyCode === 13) {
                e.preventDefault(); // Stop form submit/page reload
                processScannedData(this.value); // Trigger logic manually
            }
        });
    }

    // ============================================================
    // ⚙️ LOGIC PROCESSOR (Moved out of 'change' event)
    // ============================================================
    function processScannedData(qrValue) {
        // ⛔ BLOCK IF MANUAL MODE
        if (manualToggle.checked) {
            qrInputEl.value = "";
            return;
        }

        const qr = qrValue.trim();
        if (!qr) return;

        // Intelligent Delimiter Detection
        const allowedDelimiters = ["\t", "|", ";", ","];
        let parts = [];

        for (const d of allowedDelimiters) {
            const temp = qr.split(d);
            if (temp.length === 6) {
                parts = temp;
                break;
            }
        }

        if (parts.length !== 6) {
            console.log("Invalid format: " + qr);
            showStatus("❌ Invalid QR Format. Need 6 columns.", "error");
            qrInputEl.value = "";
            qrInputEl.focus();
            startAutoRedirect();
            return;
        }

        const rawQty = parts[2].replace(/,/g, "").trim();
        const usageQty = parseInt(rawQty, 10);

        if (isNaN(usageQty)) {
            showStatus("❌ Invalid quantity", true);
            qrInputEl.value = "";
            return;
        }

        const payload = {
            TraceID: document.getElementById("traceId").value,
            ProcID: parseInt(document.getElementById("procId").value, 10),
            PartCode: document.getElementById("partCode").value,
            LowerMaterial: parts[0].trim(),
            BatchLot: parts[1].trim(),
            UsageQty: usageQty,
            UOM: parts[3].trim().toUpperCase(),
            VendorCode: parts[4].trim(),
            VendorLot: parts[5].trim()
        };

        // UI Feedback
        showStatus("⏳ Saving...", "info");

        fetch('@Url.Action("ScanMaterial")', {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload)
        })
        .then(r => r.text())
        .then(res => {
            if (res === "DUPLICATE") {
                showStatus("⚠ Material already scanned!", "error");
            } else if (res !== "OK") {
                showStatus("❌ " + res, "error");
                qrInputEl.value = "";
                qrInputEl.focus();
                // window.location.href = window.location.href; // Optional reload
                return;
            }

            // Success
            showStatus("✅ Material Added", "success");
            qrInputEl.value = "";
            qrInputEl.focus();

            // Reload to update table
            window.location.href = window.location.href;
            startAutoRedirect();
        })
        .catch(err => {
            showStatus("❌ Scan error: " + err, "error");
            qrInputEl.value = "";
            qrInputEl.focus();
            startAutoRedirect();
        });
    }

    // Manual Entry Logic
    document.getElementById("addManualMaterial")?.addEventListener("click", function () {
        const lower = mLowerMaterial.value.trim();
        const lot   = mBatchLot.value.trim();
        const qty   = parseInt(mQty.value, 10);
        const uom   = mUom.value.trim().toUpperCase();
        const v     = mVendor.value.trim();
        const vlot  = mVendorLot.value.trim();

        if (!lower || !lot || isNaN(qty)) {
            showStatus("❌ Incomplete manual material", "error");
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

        fetch('@Url.Action("ScanMaterial", "Process")', {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload)
        })
        .then(r => r.text())
        .then(res => {
            if (res !== "OK") {
                showStatus("❌ " + res, "error");
                return;
            }
            showStatus("✅ Manual Add Success", "success");
            window.location.href = window.location.href;
            startAutoRedirect();
        });
    });

    function removeMaterial(id, isDuplicate) {
        if (!confirm("Remove this material?")) return;

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
                showStatus("❌ " + res, "error");
            }
        });
    }

    const REQUIRED_MATERIALS = @Html.Raw(Newtonsoft.Json.JsonConvert.SerializeObject(ViewData("RequiredMaterials")));

    function getScannedMaterials() {
        return Array.from(document.querySelectorAll("#materialList tr"))
            .map(r => r.dataset.lowermaterial);
    }

    function checkRequiredMaterials() {
        const scanned = getScannedMaterials();
        const missing = REQUIRED_MATERIALS.filter(rm => !scanned.includes(rm.lowerItem));
        return {
            ok: missing.length === 0,
            missing: missing.map(m => `${m.lowerDesc} (${m.lowerItem})`)
        };
    }

    function updateMaterialStatus() {
        const check = checkRequiredMaterials();

        if (check.ok) {
            showStatus("✅ All required materials present", "success-perm");
        } else {
            showStatus("⚠ Missing: " + check.missing.join(", "), "error-perm");
        }
    }

    updateMaterialStatus();
</script>