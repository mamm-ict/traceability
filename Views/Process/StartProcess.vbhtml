@Code
    ViewData("Title") = "Start Process"
End Code

<style>
 /* ===== Font Faces ===== */
@@font-face {
    font-family: 'Poppins';
    src: url('/Content/Poppins-Regular.ttf') format('truetype');
    font-weight: 400;
    font-style: normal;
}
@@font-face {
    font-family: 'Poppins';
    src: url('/Content/Poppins-Bold.ttf') format('truetype');
    font-weight: 700;
    font-style: normal;
}
@@font-face {
    font-family: 'Poppins';
    src: url('/Content/Poppins-ExtraBold.ttf') format('truetype');
    font-weight: 800;
    font-style: normal;
}

/* ===== Body & Container ===== */
body {
    font-family: 'Poppins', 'Segoe UI', Arial, sans-serif;
    background-color: #f2f4f7;
    margin: 0;
}

.mes-container {
    display: flex;
    justify-content: center;
    align-items: flex-start;
    padding: 50px 10px;
}

.scan-container {
    background: #fff;
    border-radius: 15px;
    padding: 25px;
    max-width: 500px;
    width: 100%;
    box-shadow: 0 8px 20px rgba(0,0,0,0.1);
}

body.is-fullscreen .scan-container {
    max-width: 800px;
    width: 90%;
    padding: 30px;
}

/* ===== Headings & Status ===== */
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

/* ===== Labels ===== */
label, .mes-label {
    font-size: 18px;
    font-weight: bold;
    display: block;
    margin-bottom: 8px;
}

/* ===== Inputs ===== */
input[type="text"],
input[type="submit"] {
    width: 100%;
    max-width: 420px;
    padding: 16px;
    font-size: 22px;
    border-radius: 10px;
    border: 2px solid #ccc;
    outline: none;
    box-sizing: border-box;
    margin-bottom: 16px;
}

input[type="text"]:focus {
    border-color: #007bff;
}

input[type="submit"] {
    background-color: #007bff;
    color: white;
    cursor: pointer;
    border: none;
    border-radius: 12px;
    font-size: 20px;
}

input[type="submit"]:active {
    background-color: #0056b3;
}

/* ===== Input With Icon ===== */
    .input-with-icon {
        display: flex;
        align-items: stretch; /* supaya children ikut height container */
        width: 100%;
        max-width: 420px;
        margin-bottom: 16px;
    }

        .input-with-icon input {
            flex: 1;
            font-size: 22px;
            border-radius: 10px 0 0 10px;
            border: 2px solid #ccc;
            outline: none;
            box-sizing: border-box;
            padding: 0 16px; /* vertical height akan ikut container height */
            height: 56px; /* fix height */
        }

        .input-with-icon button {
            flex: 0 0 50px;
            border-radius: 0 10px 10px 0;
            border: none;
            background-color: #007bff;
            color: white;
            cursor: pointer;
            height: 56px; /* sama dengan input */
        }

.input-with-icon button:disabled {
    opacity: 0.5;
    cursor: not-allowed;
}

/* ===== Fullscreen Overrides ===== */
body.is-fullscreen input[type="text"],
body.is-fullscreen input[type="submit"],
body.is-fullscreen .input-with-icon {
    width: 100%;
    max-width: none;
}

body.is-fullscreen .input-with-icon input,
body.is-fullscreen .input-with-icon button {
    height: 56px;
    font-size: 22px;
}

body.is-fullscreen .input-with-icon input {
    flex: 1;
    border-radius: 10px 0 0 10px;
}

body.is-fullscreen .input-with-icon button {
    flex: 0 0 50px;
    border-radius: 0 10px 10px 0;
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
        //if (/^\d{16}$/.test(val)) {
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
        //}

        // CASE 3: format salah
        //alert("Invalid Route Card");
        //this.value = "";
        //this.focus();
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

            //if (!/^\d{10}$/.test(val)) {
            //    alert("Invalid Process Card");
            //    this.value = "";
            //    this.focus();
            //    return;
            //}
            const url = '@Url.Action("GetProcessByControlNo", "Process")';
            fetch(url, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ controlNo: val })
            })
                .then(res => res.json())
                .then(data => {
                    if (data.success) {
                        this.value = data.processCode;

                        // AUTO SUBMIT
                        //form.submit();
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
    '@Url.Action("ProcessBatch", "Process")?traceId=' + encodeURIComponent(traceId);

    });

</script>

