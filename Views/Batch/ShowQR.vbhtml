
@Code
    ViewData("Title") = "Route Card"
End Code
@Code
    If Not String.IsNullOrEmpty(ViewData("ControlNo")) Then
        ViewData("ShowScanModal") = False  ' <-- flag to trigger scan modal
        System.Diagnostics.Debug.WriteLine("Ni debug" & ViewData("ControlNo") & ViewData("TraceID"))
    Else
        System.Diagnostics.Debug.WriteLine("Takde control no")
        ViewData("ShowScanModal") = True  ' <-- flag to trigger scan modal

    End If
End Code
<h1 class="mes-title">Route Card</h1>

<div class="mes-card mes-process-card" style="padding-top:25px;">

    <!-- TRACE ID FULL WIDTH -->
    <h2 class="mes-card-title" style="text-align:center; margin-bottom:20px;">
        @ViewData("TraceID")
    </h2>

    <!-- TWO COLUMN WRAPPER -->
    <div style="display:flex; justify-content:space-between; align-items:flex-start; gap:25px;">

        <!-- LEFT COLUMN -->
        <div style="flex:1;">
            <p><strong>Date:</strong> @ViewData("CreatedDate")</p>
            <p><strong>Shift:</strong> @ViewData("Shift")</p>
            <p><strong>Quantity:</strong> @ViewData("InitQty") pcs</p>
            <p><strong>Model:</strong> @ViewData("Model")</p>
            <p><strong>Line:</strong> @ViewData("Line")</p>
            <p><strong>Operator:</strong> @ViewData("OperatorID")</p>
            <p><strong>Bara Core:</strong> @ViewData("BaraCoreLot")</p>
        </div>

        <!-- RIGHT COLUMN (QR) -->
        <div style="flex:0 0 auto; text-align:center;">
            <img src="data:image/png;base64,@(ViewData("QRCodeImage"))"
                 class="mes-qr-large"
                 style="max-width:220px;" />
        </div>

    </div>

</div>

<!-- ========================================================= -->
<h2 class="mes-card-title" style="text-align:center; margin-bottom:15px;">
    ESL 2.9" Landscape
</h2>

<div style="
    width: 340px;
    height: 150px;
    border:1px solid #000;
    padding:12px 14px;
    margin:auto;
    display:flex;
    justify-content:space-between;
    align-items:center;
    font-family:Arial, sans-serif;
    border-radius:8px;
">

    <!-- LEFT TEXT -->
    <div style="flex:1; line-height:1.25;">
        <p style="font-size:20px; margin:0 0 6px 0; font-weight:700; color:#1d3557;">
            @ViewData("Model")
        </p>

        <p style="font-size:14px; margin:3px 0;">
            <strong>ID:</strong> @ViewData("TraceID")
        </p>

        <p style="font-size:14px; margin:3px 0;">
            <strong>Bara Core:</strong> @ViewData("BaraCoreLot")
        </p>

        <p style="font-size:14px; margin:3px 0;">
            <strong>Qty:</strong> @ViewData("InitQty") pcs
        </p>
    </div>

    <!-- QR -->
    <div style="flex:0 0 auto; text-align:right;">
        <img src="data:image/png;base64,@(ViewData("QRCodeImage"))"
             style="
                width:95px;
                height:95px;
                border:1px solid #444;
                padding:3px;
                background:#fff;
                border-radius:4px;
            " />
    </div>

</div>

<div id="scanModal" style="
    display:none;
    position:fixed;
    top:0; left:0; width:100%; height:100%;
    background:rgba(0,0,0,0.6);
    justify-content:center; align-items:center;
    z-index:9999;
">
    <div style="
        background:#fff;
        padding:25px 30px;
        border-radius:8px;
        max-width:600px;
        width:100%;
        text-align:center;
        box-shadow:0 4px 15px rgba(0,0,0,0.3);
    ">
        <h3 style="margin-bottom:15px;">Scan Route Card</h3>
        <input type="text" id="ControlNoInput" class="mes-input" placeholder="Scan Control Card" autofocus />
        <p id="ControlNoStatus" style="margin-top:10px; font-weight:700;"></p>
        <button id="closeModalBtn" class="mes-btn" style="margin-top:15px; width:auto; padding:8px 16px;">Close</button>
    </div>
</div>
<script>
window.addEventListener("DOMContentLoaded", () => {

    const modal = document.getElementById("scanModal");
    const controlInput = document.getElementById("ControlNoInput");
    const statusMsg = document.getElementById("ControlNoStatus");
    const closeBtn = document.getElementById("closeModalBtn");

    const showModal = @((If(ViewData("ShowScanModal") IsNot Nothing AndAlso ViewData("ShowScanModal") = True, "true", "false")));
    if (showModal === true || showModal === "true") {
        modal.style.display = "flex";
        controlInput.focus();
    }

    // Disable manual close while empty
    closeBtn.addEventListener("click", () => {
        if (!controlInput.value.trim()) {
            alert("You must enter a Control Number before closing!");
            controlInput.focus();
        } else {
            modal.style.display = "none";
        }
    });

    // Force focus on modal
    modal.addEventListener("click", (e) => {
        if (e.target === modal) {
            controlInput.focus();
        }
    });

    controlInput.addEventListener("change", function () {
        const controlNo = this.value.trim();
        if (!controlNo) return;

        fetch('@Url.Action("AddControlNo", "Batch")', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                traceID: '@ViewData("TraceID")',
                controlNo: controlNo
            })
        })
        .then(res => res.json())
        .then(data => {
            if (data.success) {
                statusMsg.style.color = 'green';
                statusMsg.textContent = data.message;

                setTimeout(() => { modal.style.display = "none"; }, 1000);

            } else {
                statusMsg.style.color = 'red';
                statusMsg.innerHTML = data.message.replace(/\n/g, "<br>");

                this.value = "";
                this.focus();
            }
        })
        .catch(err => {
            console.error(err);
            statusMsg.style.color = 'red';
            statusMsg.textContent = "Failed to save control card.";
            this.value = "";
            this.focus();
        });
    });

});

</script>

<p>
    <a class="mes-link" href="@Url.Action("Index", "History")">Back</a>
</p>
<script>
    setTimeout(function () {
        window.location.href = '@Url.Action("Create", "Batch")';
    }, 20000); // 30 seconds
</script>
