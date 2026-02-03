@Code
    ViewData("Title") = "Process Details"
    Dim process As Dictionary(Of String, String) = ViewData("Process")
End Code

<h1 class="mes-title">Process Detail</h1>

<div class="premium-card" style="display:flex; border:1px solid #ccc; border-radius:12px; overflow:hidden; box-shadow:0 4px 10px rgba(0,0,0,0.1); min-height:320px;">

    <!-- Left: Details + QR -->
    <div class="mes-card" style="flex:2; padding:20px; background:#f9f9f9; display:flex; flex-direction:column; border-right:2px solid #ddd;">
        <div>
            <h2 style="margin-bottom:15px;">@process("ProcessName")</h2>
            <div class="mes-deets" style="margin-bottom:20px;">
                <p><strong>Code:</strong> @process("ProcessCode")</p>
                <p><strong>Level:</strong> @process("ProcessLevel")</p>
                <p><strong>Flow:</strong> @process("ProcFlowId")</p>
                @*<p><strong>Control No:</strong> @process("ControlNo")</p>*@
            </div>
        </div>
        <div style="margin-top:auto; align-self:center;">
            <img src='data:image/png;base64,@process("QRCodeImage")' class="mes-qr-large" style="width:180px; height:180px;" />
        </div>
    </div>

    <!-- Right: Edit Form with overlay -->
    <div class="mes-card" id="editPanel" style="flex:1; padding:20px; background:#fff; position:relative;">

        <!-- Grey overlay -->
        <div id="overlay" style="
            position:absolute;
            top:0; left:0; right:0; bottom:0;
            background:rgba(200,200,200,0.7);
            cursor:pointer;
            display:flex;
            align-items:center;
            justify-content:center;
            z-index:2;
            font-weight:bold;
            color:#555;
            text-align:center;
        ">
            Click to edit (password required)
        </div>

        <!-- Form -->
        <form id="editForm" method="post" action="@Url.Action("UpdateProcess","Process")">
            <input type="hidden" name="processId" value="@process("ProcessID")" />

            <label>Process Name</label>
            <input type="text" name="processName" value="@process("ProcessName")" class="mes-input" disabled />

            <label>Process Code</label>
            <input type="text" name="processCode" value="@process("ProcessCode")" class="mes-input" disabled />

            <label>Flow ID</label>
            <input type="text" name="procFlowId" value="@process("ProcFlowId")" class="mes-input" disabled />

            <label>Level</label>
            <input type="number" name="processLevel" value="@process("ProcessLevel")" class="mes-input" disabled />

            <label>Control No</label>
            <input type="text" name="controlNo" value="@process("ControlNo")" class="mes-input" disabled />

            <button type="submit" class="mes-btn" disabled style="margin-top:15px; width:100%;">Save Changes</button>
        </form>

    </div>
</div>

<style>
    .mes-input {
        display: block;
        width: 100%;
        margin-bottom: 10px;
        padding: 8px 10px;
        border-radius: 5px;
        border: 1px solid #ccc;
        font-size: 14px;
    }

    .mes-btn {
        background-color: #4CAF50;
        color: #fff;
        border: none;
        border-radius: 5px;
        padding: 10px;
        font-size: 15px;
        cursor: pointer;
    }

        .mes-btn:hover {
            background-color: #45a049;
        }

    .mes-title {
        font-size: 22px;
        margin-bottom: 15px;
        font-weight: bold;
    }

    .mes-card p {
        margin: 5px 0;
    }

    .mes-qr-large {
        border: 1px solid #ccc;
        padding: 5px;
        border-radius: 8px;
        background: #fff;
    }
</style>

<script>
    document.getElementById("overlay").addEventListener("click", function () {
        var password = prompt("Enter admin password to edit:");
        if (password === "admin") { // tukar dengan password sebenar atau AJAX verify
            // hilangkan overlay
            this.style.display = "none";

            // enable semua input & button
            var inputs = document.querySelectorAll("#editForm input, #editForm button");
            inputs.forEach(function (i) { i.disabled = false; });
        } else {
            alert("Incorrect password!");
        }
    });
</script>
<script>
    @If TempData("SuccessMessage") IsNot Nothing Then
        @:alert("@TempData("SuccessMessage")");
    End If

    @If TempData("ErrorMessage") IsNot Nothing Then
        @:alert("@TempData("ErrorMessage")");
    End If
</script>
