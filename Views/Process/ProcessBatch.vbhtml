@Code
	Dim batch As Batch = CType(ViewData("Batch"), Batch)
	Dim processes As List(Of ProcessMaster) = CType(ViewData("Processes"), List(Of ProcessMaster))
	Dim logs As List(Of ProcessLog)

	If ViewData("Logs") IsNot Nothing Then
		logs = CType(ViewData("Logs"), List(Of ProcessLog))
	Else
		logs = New List(Of ProcessLog)()
	End If

	Dim triggerLevels As Integer() = {1, 3}

	Dim lastLog = logs.OrderByDescending(Function(l) l.scan_time).FirstOrDefault()
	Dim enableRawMaterial As Boolean = False
	If lastLog IsNot Nothing Then
		Dim lastProc = processes.FirstOrDefault(Function(p) p.ID = lastLog.ProcessID)
		If lastProc IsNot Nothing Then
			enableRawMaterial = triggerLevels.Contains(lastProc.Level)
		End If
	End If
End Code


<h2>Processing Batch: @batch.TraceID</h2>
<p>Date: @batch.CreatedDate.ToString("yyyy-MM-dd") | Shift: @batch.Shift</p>
<p>Model: @batch.Model</p>
<p>Line: @batch.Line | Operator: @batch.OperatorID</p>

@if enableRawMaterial Then
@<h3>Raw Materials</h3>

@<form id="rawMaterialForm" method="post" action="@Url.Action("UpdateRawMaterials")">
	<input type="hidden" name="traceId" value="@batch.TraceID" />
	<table id="rawMaterials" style="width:100%; border-collapse: collapse;">
		<tr>
			<th>Raw Material</th>
			<th>Quantity</th>
			<th>Action</th>
		</tr>
		<tr>
			<td><input type="text" name="RawMaterialNames" class="vk-input" /></td>
			<td><input type="number" name="Quantities" class="vk-input" /></td>
			<td><button type="button" onclick="addRow()">+</button></td>
		</tr>
	</table>
	<button type="submit" id="submitBtn">Update Raw Materials</button>
</form>
End If


<h3>Scan Process</h3>
<form method="post" action="@Url.Action("ScanProcess")">
	<input type="hidden" name="traceId" value="@batch.TraceID" />
	<label>Operator No:</label>
	<input type="text" name="OperatorID" required class="vk-input" />
	<label>Scan Process QR:</label>
	<input type="text" name="processData" required class="vk-input" />
	<button type="submit">Submit</button>
</form>

<h3>Process Logs</h3>
@if logs.Count > 0 Then
	@<ul>
		@For Each log In logs.OrderBy(Function(l) l.scan_time)
			Dim proc = processes.FirstOrDefault(Function(p) p.ID = log.ProcessID)
			Dim procName As String = If(proc IsNot Nothing, proc.Name, "Unknown Process")
			Dim statusColor As String =
				If(log.Status = "In Progress", "blue",
				If(log.Status = "Completed", "green", "red"))
			@<li style="color:@statusColor">
				@log.scan_time.ToString("HH:mm:ss") - @procName by @log.OperatorID (Status: @log.Status)
			</li>
		Next
	</ul>
Else
	@<p>No process logs yet.</p>
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