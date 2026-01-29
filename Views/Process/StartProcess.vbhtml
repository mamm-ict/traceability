@Code
    ViewData("Title") = "Start Process"
End Code

<style>
    body {
        font-family: 'Segoe UI', Arial, sans-serif;
        background-color: #f2f4f7;
    }

    .scan-container {
        max-width: 500px;
        margin: 50px auto;
        background: #fff;
        padding: 25px;
        border-radius: 15px;
        box-shadow: 0 8px 20px rgba(0,0,0,0.1);
    }

    h2 {
        text-align: center;
        margin-bottom: 15px;
    }

    .status-message {
        text-align: center;
        margin-bottom: 20px;
        color: #007bff;
        font-size: 16px;
    }

    label {
        font-size: 18px;
        font-weight: bold;
        display: block;
        margin-bottom: 8px;
    }

    input[type="text"] {
        width: 100%;
        padding: 16px;
        font-size: 22px;
        border-radius: 10px;
        border: 2px solid #ccc;
        outline: none;
    }

        input[type="text"]:focus {
            border-color: #007bff;
        }

    input[type="submit"] {
        width: 100%;
        margin-top: 20px;
        padding: 16px;
        font-size: 20px;
        border-radius: 12px;
        border: none;
        background-color: #007bff;
        color: white;
        cursor: pointer;
    }

        input[type="submit"]:active {
            background-color: #0056b3;
        }
    input[type="text"],
    input[type="submit"] {
        width: 420px;
        max-width: 100%;
    }
    .input-with-icon {
        position: relative;
        width: 420px;
        max-width: 100%;
    }

        .input-with-icon input {
            width: 100%;
            padding-right: 45px; /* ruang untuk button */
            box-sizing: border-box;
        }

        .input-with-icon button {
            position: absolute;
            right: 0;
            top: 0;
            height: 100%;
            width: 40px;
            border: none;
            background-color: #007bff;
            color: white;
            cursor: pointer;
            border-radius: 0 10px 10px 0;
        }

            .input-with-icon button:disabled {
                opacity: 0.5;
                cursor: not-allowed;
            }

</style>

<div class="mes-container">
    <div class="scan-container">
        <h2 class="mes-title">Start Process</h2>

        <div class="status-message">
            @Html.Raw(ViewData("StatusMessage"))
        </div>

        <form method="post" action="@Url.Action("StartProcess")">
            <label>Scan Route Card:</label>
            @*<input type="text" name="traceId" id="traceID" autofocus autocomplete="off" required />*@
            <div class="input-with-icon">
                <input type="text" name="traceId" id="traceID" autofocus autocomplete="off" required />
                <button type="button" id="btnCheckStatus" disabled>🔍</button>
            </div>


            <label class="mes-label">Operator No</label>
            <input type="text" name="operatorId" id="operatorID" required autocomplete="off" />
            <label class="mes-label">Scan Process QR</label>
            <input type="text" name="processQr" id="processQr" required autocomplete="off" />
            <input type="submit" value="Submit" />
        </form>
    </div>
</div>

<script>
    //Route Card scanning
    document.getElementById("traceID").addEventListener("change", function () {
        let val = this.value.trim();
        if (val.length === 0) return;

        if (/^[A-Za-z]{3}-\d{8}-\d{3}$/.test(val)) {
            // valid manual input, just accept
            return;
        }

        // Scan CONTROL_NO (contoh: 10 digit)
        if (/^\d{10}$/.test(val)) {
            const url = '@Url.Action("GetTraceIDByControlNo", "Process")';
            fetch(url, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ controlNo: val })
            })
            .then(res => res.json())
            .then(data => {
                if (data.success) {
                    this.value = data.traceID;
                } else {
                    alert(data.message);
                    this.value = "";
                    this.focus();
                }
            });
            return;
        }

        // CASE 3: format salah
        alert("Invalid Route Card");
        this.value = "";
        this.focus();
    });

    //Operator ID scanning
    document.getElementById("operatorID").addEventListener("change", function () {
        let val = this.value.trim();
        if (val.length === 0) return;

        // CASE 1: user key-in EMPLOYEE_NO (contoh: 6 digit)
        if (/^\d{6}$/.test(val)) {
            // terus guna, tak buat apa-apa
            return;
        }

        // CASE 2: scan CONTROL_NO (contoh: 10 digit)
        if (/^\d{10}$/.test(val)) {
            const url = '@Url.Action("GetEmployeeByControlNo", "Process")';

            fetch(url, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ controlNo: val })
            })
            .then(res => res.json())
            .then(data => {
                if (data.success) {
                    this.value = data.employeeNo;
                } else {
                    alert(data.message);
                    this.value = "";
                    this.focus();
                }
            });
            return;
        }

        // CASE 3: format salah
        alert("Invalid Operator ID / Card");
        this.value = "";
        this.focus();
    });

    //Process Card scanning
    const form = document.querySelector("form");
    const input = document.getElementById("processQr");

    input.addEventListener("keydown", function (e) {
        if (e.key === "Enter") {
            e.preventDefault();
          
            let val = this.value.trim();

            // CASE 1: user type proc_code (OVN-04)
            if (/^[A-Z]{3}-\d{2}$/.test(val)) {
                form.submit();
                return;
            }

            if (!/^\d{10}$/.test(val)) {
                alert("Invalid Process Card");
                this.value = "";
                this.focus();
                return;
            }

            fetch('/Process/GetProcessByControlNo', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ controlNo: val })
            })
                .then(res => res.json())
                .then(data => {
                    if (data.success) {
                        this.value = data.processCode;

                        // AUTO SUBMIT
                        form.submit();
                    } else {
                        alert(data.message);
                        this.value = "";
                        this.focus();
                    }
                });
        }
    });

    const btnStatus = document.getElementById("btnCheckStatus");
    const traceInput = document.getElementById("traceID");

    traceInput.addEventListener("input", function () {
        btnStatus.disabled = this.value.trim().length === 0;
    });

    btnStatus.addEventListener("click", function () {
        const traceId = traceInput.value.trim();
        if (!traceId) return;

        window.location.href =
            `/Process/ProcessBatch?traceId=${encodeURIComponent(traceId)}`;
    });

</script>
