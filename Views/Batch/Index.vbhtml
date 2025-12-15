<h1>Batch QR Code Generator</h1>

<form method="post">
	<div id="customRawMaterials">
		<input type="text" name="rawMaterial" placeholder="Scan or type RM" />
	</div>
	<button type="button" onclick="addCustomRM()">Add another</button>

	<div>
		<label>Quantity:</label>
		<input type="number" name="quantity" required />
	</div>

	<div>
		<label>Machine No:</label>
		<input type="text" name="machineNo" required />
	</div>

	<div>
		<label>Line No:</label>
		<input type="text" name="lineNo" required />
	</div>

	<div>
		<label>Operator No:</label>
		<input type="text" name="operatorNo" required />
	</div>

	<button type="submit">Generate QR</button>
</form>

<script>
function addCustomRM() {
    var container = document.getElementById("customRawMaterials");
    var input = document.createElement("input");
    input.type = "text";
    input.name = "rawMaterial"; // same name → MVC binds as array
    input.placeholder = "Scan or type RM";
    container.appendChild(document.createElement("br"));
    container.appendChild(input);
}
</script>


@If ViewData("QRCodeImage") IsNot Nothing Then
	@<div style="margin-top:20px; padding:20px; border:2px solid #333; display:inline-block; text-align:center; background:#f9f9f9; border-radius:10px;">
		<h3>Batch QR Code</h3>
		<p><strong>Batch ID:</strong> @ViewData("BatchID")</p>
		<p><strong>Date:</strong> @ViewData("DateCreated")</p>
		<p><strong>Shift:</strong> @ViewData("Shift")</p>
		<img src="data:image/png;base64,@ViewData("QRCodeImage")" alt="QR Code" style="max-width:250px; margin:10px 0;" />
		<p>@ViewData("RawMaterial") - @ViewData("Quantity") pcs</p>
		<p>Machine: @ViewData("MachineNo") | Line: @ViewData("LineNo") | Operator: @ViewData("OperatorNo")</p>
	</div>
End If
