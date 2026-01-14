@Code
    ViewData("Title") = "Route Card"

    If Not String.IsNullOrEmpty(ViewData("ControlNo")) Then
        ViewData("ShowScanModal") = False
    Else
        ViewData("ShowScanModal") = True
    End If
End Code
<style>
/* Override for fullscreen / wide screens */
@@media (min-width: 900px) {
    .mes-card {
        margin-left: 50px; /* or any left padding you want */
        margin-right: auto; /* keep right side flexible */
    }

    /* Keep trace ID left-aligned */
    .mes-card h2 {
        text-align: left !important;
    }

    /* Left column info stays left */
    .mes-card > div {
        justify-content: flex-start !important;
        text-align: left !important;
    }
}
</style>

<h1 class="mes-title" style="margin-bottom:20px;">Route Card</h1>

<div class="mes-card" style="
    max-width:600px;
    margin:auto;
    padding:25px;
    border-radius:12px;
    background:#fff;
    box-shadow:0 4px 20px rgba(0,0,0,0.12);
    font-family:'Segoe UI', sans-serif;
">

    <!-- TRACE ID HEADER -->
    <h2 style="
        text-align:center;
        font-size:1.6rem;
        font-weight:700;
        color:#1a73e8;
        margin-bottom:25px;
    ">
        @ViewData("TraceID")
    </h2>

    <!-- INFO GRID -->
    <div style="
        display:flex;
        justify-content:space-between;
        gap:25px;
        flex-wrap:wrap;
    ">
        <!-- LEFT COLUMN -->
        <div style="flex:1; min-width:250px; color:#444;">
            <p><strong>Date:</strong> @ViewData("CreatedDate")</p>
            <p><strong>Shift:</strong> @ViewData("Shift")</p>
            <p><strong>Quantity:</strong> @ViewData("InitQty") pcs</p>
            <p><strong>Model:</strong> @ViewData("Model")</p>
            <p><strong>Line:</strong> @ViewData("Line")</p>
            <p><strong>Operator:</strong> @ViewData("OperatorID")</p>
            <p><strong>Bara Core Lot:</strong> @ViewData("BaraCoreLot")</p>
        </div>

        <!-- RIGHT COLUMN: QR CODE -->
        <div style="flex:0 0 auto; text-align:center;">
            <img src="data:image/png;base64,@(ViewData("QRCodeImage"))"
                 style="
                    max-width:180px;
                    border:1px solid #ccc;
                    padding:5px;
                    border-radius:6px;
                    background:#fff;
                " />
            <p style="margin-top:6px; font-size:0.85rem; color:#555;">Scan QR for details</p>
        </div>
    </div>

    <!-- FOOTER / AUTO REDIRECT INFO -->
    <div style="margin-top:20px; text-align:center; color:#555; font-size:0.9rem;">
        <span>Auto-redirecting in <span id="timer">20</span> seconds...</span>
    </div>
</div>

<!-- SCAN MODAL -->
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
        padding:30px 35px;
        border-radius:10px;
        max-width:500px;
        width:90%;
        text-align:center;
        box-shadow:0 6px 20px rgba(0,0,0,0.25);
    ">
        <h3 style="margin-bottom:15px;">Scan Route Card</h3>
        <input type="text" id="ControlNoInput" class="mes-input" placeholder="Scan Control Card" autofocus />
        <p id="ControlNoStatus" style="margin-top:10px; font-weight:700;"></p>
        <button id="closeModalBtn" class="mes-btn" style="margin-top:20px; width:auto; padding:10px 20px;">Close</button>
    </div>
</div>

<p style="text-align:center; margin-top:20px;">
    <a class="mes-link" href="@Url.Action("Create", "Batch")">Back</a>
</p>

<!-- TIMER & AUTO REDIRECT -->
<script>
    let countdown = 20;
    const timerDisplay = document.getElementById("timer");

    function startTimer() {
        timerDisplay.textContent = countdown;
        const interval = setInterval(() => {
            countdown--;
            timerDisplay.textContent = countdown;
            if(countdown <= 0){
                clearInterval(interval);
                window.location.href = '@Url.Action("Create", "Batch")';
            }
        }, 1000);
    }
    startTimer();
</script>

<!-- SCAN MODAL LOGIC -->
<script>
window.addEventListener("DOMContentLoaded", () => {
    const modal = document.getElementById("scanModal");
    const input = document.getElementById("ControlNoInput");
    const status = document.getElementById("ControlNoStatus");
    const closeBtn = document.getElementById("closeModalBtn");

    const showModal = @((If(ViewData("ShowScanModal") IsNot Nothing AndAlso ViewData("ShowScanModal") = True, "true", "false")));
    if(showModal === true || showModal === "true"){
        modal.style.display = "flex";
        input.focus();
    }

    closeBtn.addEventListener("click", () => {
        if(!input.value.trim()){
            alert("You must enter a Control Number before closing!");
            input.focus();
        } else {
            modal.style.display = "none";
        }
    });

    modal.addEventListener("click", e => {
        if(e.target === modal) input.focus();
    });

    input.addEventListener("change", function(){
        const controlNo = this.value.trim();
        if(!controlNo) return;

        fetch('@Url.Action("AddControlNo","Batch")',{
            method:'POST',
            headers:{'Content-Type':'application/json'},
            body:JSON.stringify({traceID:'@ViewData("TraceID")', controlNo:controlNo})
        })
        .then(r=>r.json())
        .then(data=>{
            if(data.success){
                status.style.color='green';
                status.textContent = data.message;
                setTimeout(()=>{modal.style.display='none';},1000);
            } else {
                status.style.color='red';
                status.innerHTML = data.message.replace(/\n/g,"<br>");
                this.value = "";
                this.focus();
            }
        })
        .catch(err=>{
            console.error(err);
            status.style.color='red';
            status.textContent = "Failed to save control card.";
            this.value="";
            this.focus();
        });
    });
});
</script>
