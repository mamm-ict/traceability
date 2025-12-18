@Code
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

    If activeProcessId.HasValue Then
        Dim activeProc = processes.FirstOrDefault(Function(p) p.ID = activeProcessId.Value)

        If activeProc IsNot Nothing AndAlso activeProc.MaterialFlag = 1 Then
            enableRawMaterial = True
        End If
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


End Code


<div class="mes-card mes-process-card">
    <h2 class="mes-card-title">
        Batch: @batch.TraceID
    </h2>

    <div class="mes-kv">
        <p><strong>Date:</strong> @batch.CreatedDate.ToString("yyyy-MM-dd")</p>
        <p><strong>Shift:</strong> @batch.Shift</p>
        <p><strong>Model:</strong> @batch.Model</p>
        <p><strong>Line:</strong> @batch.Line</p>
        <p><strong>Operator:</strong> @batch.OperatorID</p>
    </div>
</div>


@if enableRawMaterial AndAlso activeProcessId.HasValue Then
    @<div class="mes-card">
        <h3 class="mes-card-subtitle">
            Raw Materials (Scan QR)
        </h3>

        <!-- hidden context -->
        <input type="hidden" id="traceId" value="@batch.TraceID" />
        <input type="hidden" id="procId" value="@activeProcessId.Value" />
        <input type="hidden" id="partCode" value="@batch.PartCode" />

        <!-- SCAN INPUT -->
        <label class="mes-label">Scan Material QR</label>
        <input type="text"
               id="materialQr"
               @*class="vk-input"*@
               autocomplete="off"
               placeholder="Scan material QR here" />

        <!-- LIST TABLE -->
        <table class="mes-table" style="margin-top:12px;">
            <thead>
                <tr>
                    <th>Trace ID</th>
                    <th>Proc ID</th>
                    <th>Part Code</th>
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
                @If ViewData("Materials") IsNot Nothing Then
                    For Each m In CType(ViewData("Materials"), List(Of MaterialLog))
                        @<tr>
                            <td>@m.TraceID</td>
                            <td>@m.ProcID</td>
                            <td>@m.PartCode</td>
                            <td>@m.LowerMaterial</td>
                            <td>@m.BatchLot</td>
                            <td>@m.UsageQty</td>
                            <td>@m.UOM</td>
                            <td>@m.VendorCode</td>
                            <td>@m.VendorLot</td>
                            <td>
                                <button class="mes-btn-danger"
                                        onclick="removeMaterial(@m.ID)">
                                    ✖
                                </button>
                            </td>

                        </tr>
                    Next
                End If
            </tbody>

        </table>
        @*<button id="saveMaterials" class="mes-btn-primary">
                Save Materials
            </button>*@

    </div>
End If


<div class="mes-card">
    <h3 class="mes-card-subtitle">Scan Process</h3>

    <form method="post" action="@Url.Action("ScanProcess")">
        <input type="hidden" name="traceId" value="@batch.TraceID" />

        <label class="mes-label">Operator No</label>
        <input type="text" name="OperatorId" required class="vk-input" />

        <label class="mes-label">Scan Process QR</label>
        <input type="text" name="processQr" required />

        <button type="submit" class="mes-btn-primary">
            Submit
        </button>
    </form>
</div>


<h3 class="mes-subtitle">Process Logs</h3>

@if logs.Any() Then
    @<table class="mes-table">
        <thead>
            <tr>
                <th> Time</th>
                <th> Process</th>
                <th> Status</th>
                <th> Qty In</th>
                <th> Qty Buffer</th>
                <th> Qty Reject</th>
                <th></th>
            </tr>
        </thead>
        <tbody>

            @For Each log In logs.OrderBy(Function(l) l.ScanTime)

                Dim proc = processes.FirstOrDefault(Function(p) p.ID = log.ProcessID)
                Dim procName As String = If(proc IsNot Nothing, proc.Name, "Unknown")
                'Dim showBufferForm As Boolean =
                '    log.Status = "In Progress" AndAlso
                '    proc IsNot Nothing AndAlso
                '    proc.BufferFlag = 1

                Dim isBufferProcess As Boolean =
     proc IsNot Nothing AndAlso proc.BufferFlag = 1

                Dim isActiveProcess As Boolean =
                    activeProcessId.HasValue AndAlso log.ProcessID = activeProcessId.Value

                Dim showQtyInput As Boolean =
     proc IsNot Nothing AndAlso
     proc.BufferFlag = 1 AndAlso
     log.Status = "Pending Completion"

                Dim statusClass As String =
    If(log.Status = "In Progress", "status-progress",
    If(log.Status = "Pending Completion", "status-pending",
    "status-done"))

    @<tr class="@(If(showQtyInput, "row-editable", "row-locked"))">

        <td>@log.ScanTime.ToString("HH:mm:ss")</td>
        <td class="mes-left">@procName</td>

        <td>
            <span class="status-badge @statusClass">@log.Status</span>
        </td>

        @* QTY IN *@
        <td>
            <span class="qty-in">@log.QtyIn</span>
        </td>

        @If showQtyInput Then
            @<td colspan="3">
                <form method="post"
                      action="@Url.Action("CompleteProcess")"
                      class="buffer-form">

                    <input type="hidden" name="logId" value="@log.ID" />

                    <input type="number"
                           name="qtyOut"
                           value="@log.QtyOut"
                           class="vk-input qty-out"
                           placeholder="Buffer" />

                    <input type="number"
                           name="qtyReject"
                           value="@log.QtyReject"
                           class="vk-input qty-reject"
                           placeholder="Reject" />

                    <button type="submit" class="mes-tick-btn">✔</button>
                </form>
            </td>
        Else
            @<td>
                <span class="qty-out">@log.QtyOut</span>
            </td>
            @<td>
                <span class="qty-reject">@log.QtyReject</span>
            </td>
            @<td>
                <span class="lock-icon">🔒</span>
            </td>
        End If


    </tr>


Next
        </tbody>
    </table>
Else
    @<p> No process logs yet.</p>
End If



<p style="color: green;">@ViewData("StatusMessage")</p>
<p style="color: red;">@ViewData("ErrorMessage")</p>
<script defer>
    function addRow() {
        var table = document.getElementById("rawMaterials");
        var row = table.insertRow();
        row.innerHTML = `
        <td><input type="text" name="RawMaterialNames" style="font-size:18px; padding:8px; width:100%;" class="vk-input"/></td>
        <td><input type="number" name="Quantities" style="font-size:18px; padding:8px; width:100%;" class="vk-input"/></td>
        <td><button type="button" onclick="this.parentElement.parentElement.remove()" style="font-size:18px; padding:8px;">-</button></td>
    `;
        attachValidationEvents(row);
    }

    function checkForm() {
        let hasError = false;
        let nameInputs = document.querySelectorAll("input[name='RawMaterialNames']");
        let qtyInputs = document.querySelectorAll("input[name='Quantities']");
        let names = [];

        if (!nameInputs || nameInputs.length === 0) return;
        if (!qtyInputs || qtyInputs.length === 0) return;

        for (let i = 0; i < nameInputs.length; i++) {
            let nameInput = nameInputs[i];
            let qtyInput = qtyInputs[i];

            let nameVal = nameInput.value.trim();
            let qtyVal = qtyInput.value.trim();

            // Empty check
            if (nameVal === "" || qtyVal === "") {
                hasError = true;
                // only highlight if touched
                if (nameInput.dataset.touched && nameVal === "") nameInput.style.border = "2px solid red";
                else nameInput.style.border = "";
                if (qtyInput.dataset.touched && qtyVal === "") qtyInput.style.border = "2px solid red";
                else qtyInput.style.border = "";
            } else {
                nameInput.style.border = "";
                qtyInput.style.border = "";
            }

            // Duplicate check
            let lowerName = nameVal.toLowerCase();
            if (lowerName !== "") {
                if (names.includes(lowerName)) {
                    hasError = true;
                    nameInput.style.border = "2px solid red";
                } else {
                    names.push(lowerName);
                }
            }
        }

        let duplicateExists = checkDuplicates();

        // Enable submit only if no error & no duplicate
        document.getElementById("submitBtn").disabled = hasError || duplicateExists;
    }

    function checkDuplicates() {
        let inputs = document.querySelectorAll("input[name='RawMaterialNames']");
        if (!inputs || inputs.length === 0) return false; // <-- prevent errors

        let names = [];
        let duplicated = false;

        inputs.forEach((input) => {
            if (!input) return; // <-- skip nulls
            let val = input.value.trim().toLowerCase();
            if (val !== "") {
                if (names.includes(val)) {
                    duplicated = true;
                    input.style.border = "2px solid red";
                } else {
                    names.push(val);
                    if (!duplicated) input.style.border = "";
                }
            }
        });

        let dupError = document.getElementById("dupError");
        if (dupError) dupError.style.display = duplicated ? "block" : "none";

        return duplicated;
    }

    function attachValidationEvents(rowOrInput) {
        if (!rowOrInput) return;

        let inputs;
        if (rowOrInput.tagName && rowOrInput.tagName.toUpperCase() === "TR") {
            inputs = rowOrInput.querySelectorAll("input");
        } else if (rowOrInput.tagName && rowOrInput.tagName.toUpperCase() === "INPUT") {
            inputs = [rowOrInput];
        } else return;

        inputs.forEach(input => {
            input.addEventListener("focus", function () {
                activeInput = input; // <-- update activeInput pertama
                input.dataset.touched = "true"; // untuk validation
                if (input.classList.contains("vk-input")) showKeyboard(input); // attach keyboard
            });
            input.addEventListener("input", checkForm);
            input.addEventListener("blur", checkForm);
        });

    }

    window.onload = function () {
        document.querySelectorAll("input").forEach(input => attachValidationEvents(input));
        checkForm();
    };
</script>

<script>
    // Enable or disable form based on scanned process level
    const enableForm = @enableRawMaterial.ToString().ToLower(); // true/false
	const form = document.getElementById('rawMaterialForm');
	if (form) {  // Only run if the form exists
		form.querySelectorAll('input, button').forEach(el => {
			el.disabled = !enableForm;
		});
	}

</script>
<script>
    document.addEventListener("input", function (e) {
        if (!e.target.classList.contains("qty-out") &&
            !e.target.classList.contains("qty-reject")) return;

        const row = e.target.closest("tr");
        const qtyOut = row.querySelector(".qty-out");
        const qtyReject = row.querySelector(".qty-reject");
        const qtyInInput = row.querySelector(".qty-in");
        const submitBtn = row.querySelector("button[type='submit']");

        const total = parseInt(qtyInInput.textContent);
        const outVal = parseInt(qtyOut.value);
        const rejectVal = parseInt(qtyReject.value);

        if (outVal + rejectVal > total) {
            submitBtn.disabled = true;
            qtyOut.style.border = "2px solid red";
            qtyReject.style.border = "2px solid red";
        } else {
            submitBtn.disabled = false;
            qtyOut.style.border = "";
            qtyReject.style.border = "";
        }
    });

    //if (!qtyOut || !qtyReject || !submitBtn) return;

</script>
<script>
document.getElementById("materialQr")?.addEventListener("change", function () {

    const qr = this.value.trim();
    if (!qr) return;

    const parts = qr.split("\t");
    if (parts.length !== 6) {
        alert("Invalid Material QR");
        this.value = "";
        return;
    }

    // normalize qty (remove thousand separator)
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
            const input = document.getElementById("materialQr");
            input.value = "";
            input.focus();
            return;
        }

        const row = `
        <tr>
            <td>${payload.TraceID}</td>
            <td>${payload.ProcID}</td>
            <td>${payload.PartCode}</td>
            <td>${payload.LowerMaterial}</td>
            <td>${payload.BatchLot}</td>
            <td>${payload.UsageQty}</td>
            <td>${payload.UOM}</td>
            <td>${payload.VendorCode}</td>
            <td>${payload.VendorLot}</td>
        </tr>`;

        document.getElementById("materialList")
            .insertAdjacentHTML("beforeend", row);

        this.value = "";
    });
});
</script>
<script>
function removeMaterial(id) {

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
            alert(res);
            
        }
    });
}
</script>
