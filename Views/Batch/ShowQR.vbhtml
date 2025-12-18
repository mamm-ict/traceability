
@Code
    ViewData("Title") = "Route Card"
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



<p>
    <a class="mes-link" href="@Url.Action("Index", "History")">Back</a>
</p>
