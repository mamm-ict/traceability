@Code
    ViewData("Title") = "Create Batch"
    Dim batch As Batch = Nothing
    If ViewData("Batch") IsNot Nothing Then
        batch = CType(ViewData("Batch"), Batch)
    End If
End Code
<script src="https://code.jquery.com/jquery-3.6.0.min.js"></script>

<div class="mes-container">
    <form method="post" action="/Batch/Create" class="mes-panel">
        <div class="mes-title">Create Batch</div>

        <div class="mes-grid">

            <div>
                <label class="mes-label">Model</label>
                <input type="text" name="Model" value="@ViewData("Model")" class="mes-input vk-input" placeholder="Model" required>
            </div>

            <div>
                <label class="mes-label">Part Code</label>
                <select name="PartCode"
                        id="PartCode"
                        class="mes-input vk-input"
                        required>

                    <option disabled selected hidden value="">Select</option>

                    @For Each p In CType(ViewData("PartMasters"), List(Of MaterialMaster))
                        @<option value="@p.PartCode">
                            @p.PartDesc
                        </option>
                    Next
                </select>
            </div>

            <div>
                <label class="mes-label">Bara Core Date</label>
                <input type="date" name="BaraCoreDate" class="mes-input" required>
            </div>

            <div>
                <label class="mes-label">Line No</label>
                <input type="text" name="Line" value="@ViewData("Line")" class="mes-input vk-input" placeholder="Line No" required>
            </div>

            <div>
                <label class="mes-label">Operator No</label>
                <input type="text" name="OperatorID" id="OperatorID" value="@ViewData("OperatorID")" class="mes-input" placeholder="Employee ID" required>
            </div>

            <div>
                <label class="mes-label">Quantity</label>
                <input type="number"
                       name="InitQty"
                       id="InitQty"
                       class="mes-input vk-input"
                       placeholder="0"
                       readonly
                       required />
                <input type="hidden" name="CurQty" id="CurQty" value="" />
            </div>

        </div>

        <button type="submit" id="submitBtn" class="mes-btn" disabled>Submit</button>

    </form>

    @If batch IsNot Nothing Then
        @<div class="overlay-card">
            <div class="overlay-content">
                <h3 class="mes-card-title">Pending Route Card</h3>
                <p> A batch exists without a route card. You must link it before continuing.</p>
                <a href="@Url.Action("ShowQR", "Batch", New With {.TraceID = batch.TraceID})" class="mes-btn">
                    Link Route Card
                </a>
            </div>
        </div>
    End If
</div>

<style>
    .overlay-card {
        position: absolute;
        top: 0;
        left: 0;
        width: 100%;
        height: 100%;
        background: rgba(0,0,0,0.6);
        display: flex;
        justify-content: center;
        align-items: center;
        z-index: 9999;
    }

    .overlay-content {
        background: #f4f4f4;
        padding: 30px 35px;
        border-radius: 8px;
        text-align: center;
        max-width: 400px;
        box-shadow: 0 4px 15px rgba(0,0,0,0.3);
    }

        .overlay-content .mes-card-title {
            font-size: clamp(20px, 3vw, 26px);
            font-weight: 800;
            color: #2b4c7e;
            margin-bottom: 12px;
            text-transform: uppercase;
        }

        .overlay-content p {
            font-size: clamp(14px, 2.5vw, 16px);
            color: #1e1e1e;
            margin-bottom: 18px;
        }

        .overlay-content .mes-btn {
            display: inline-block;
            padding: clamp(12px, 2.5vw, 16px) clamp(20px, 3vw, 28px);
            font-size: clamp(16px, 2.5vw, 20px);
            font-weight: 700;
            background-color: #2563eb;
            color: #fff;
            border-radius: 6px;
            text-decoration: none;
            transition: 0.2s;
            border: 3px solid #1a4eb8;
        }

            .overlay-content .mes-btn:hover {
                background-color: #1a4eb8;
                border-color: #153b90;
                cursor: pointer;
            }

            .overlay-content .mes-btn:active {
                background-color: #153b90;
            }
</style>
<script>
    function checkForm() {
        let hasError = false;

        ["Model", "PartCode", "BaraCoreDate", "Line", "OperatorID", "InitQty"]
            .forEach(name => {

                const el =
                    document.querySelector(`input[name='${name}']`) ||
                    document.querySelector(`select[name='${name}']`);

                if (!el) return;

                if (!el.value || el.value.trim() === "") {
                    hasError = true;
                    if (el.dataset.touched) el.style.border = "3px solid red";
                } else {
                    el.style.border = "";
                }

                if (el.tagName === "SELECT") {
                    if (el.value === "") {
                        hasError = true;
                        if (el.dataset.touched) el.style.border = "3px solid red";
                        return;
                    }
                }

            });

        document.getElementById("submitBtn").disabled = hasError;
    }

    function bindValidation() {
        // TEXT / NUMBER / DATE inputs
        document.querySelectorAll(
            "input[name='Model'], " +
            "input[name='Line'], " +
            "input[name='OperatorID'], " +
            "input[name='InitQty'], " +
            "input[name='BaraCoreDate']"
        ).forEach(el => {
            el.addEventListener("input", checkForm);
            el.addEventListener("blur", checkForm);
            el.addEventListener("focus", () => el.dataset.touched = "true");
        });

        // SELECT (PartCode)
        const partSelect = document.querySelector("select[name='PartCode']");
        if (partSelect) {
            partSelect.addEventListener("change", checkForm);
            partSelect.addEventListener("focus", () => partSelect.dataset.touched = "true");
            partSelect.addEventListener("blur", checkForm);
        }
    }

    function attachValidationEvents(input) {
        if (!input) return;

        input.addEventListener("focus", function () {
            input.dataset.touched = "true";
            if (input.classList.contains("vk-input")) showKeyboard(input);
        });

        input.addEventListener("input", checkForm);
        input.addEventListener("blur", checkForm);
    }

    window.onload = function () {
        let allInputs = document.querySelectorAll("input");
        allInputs.forEach(input => attachValidationEvents(input));
        bindValidation();
        checkForm();
    };
</script>
<script>
    document.getElementById("OperatorID").addEventListener("change", function () {
        let val = this.value.trim();
        if (val.length === 0) return;

        // CASE 1: user key-in EMPLOYEE_NO (contoh: 6 digit)
        if (/^\d{6}$/.test(val)) {
            // terus guna, tak buat apa-apa
            return;
        }

        // CASE 2: scan CONTROL_NO (contoh: 10 digit)
        if (/^\d{10}$/.test(val)) {
            const url = '@Url.Action("GetEmployeeByControlNo", "Batch")';
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

</script>
<script>
    function submitControlNo(traceID, controlNo) {
        fetch('/Batch/AddControlNo', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                traceID: traceID,
                controlNo: controlNo
            })
        }).then(r => r.json())
            .then(data => {
                if (!data.success) {
                    alert(data.message);
                } else {
                    alert("Success");
                }
            });
    }

    $('#PartCode').change(function () {
        var partCode = $(this).val();
        if (!partCode) return;

        $.ajax({
            url: '@Url.Action("GetFinalQty", "Batch")',
            type: 'POST',
            data: { partCode: partCode }, 
            success: function (res) {
                if (res.success) {
                    $('#InitQty').val(res.finalQty);
                    $('#CurQty').val(res.finalQty);
                    checkForm();
                } else {
                    alert(res.message);
                    $('#InitQty').val('');
                    $('#CurQty').val('');
                }
            },
            error: function () {
                alert('Error mengambil quantity dari server!');
                $('#InitQty').val('');
                $('#CurQty').val('');
            }
        });
    });

</script>

<style>

    /* ====== MES INDUSTRIAL UI STYLE ====== */

    .mes-container {
        width: 100%;
        padding: 18px;
    }

    .mes-panel {
        background: #f4f4f4;
        border: 3px solid #2b4c7e;
        border-radius: 6px;
        padding: 20px;
    }

    .mes-title {
        text-align: center;
        font-size: clamp(26px, 4vw, 34px);
        font-weight: 800;
        color: #2b4c7e;
        margin-bottom: 25px;
        text-transform: uppercase;
    }

    /* 2-Column Grid */
    .mes-grid {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 20px 24px;
    }

    @@media (max-width: 768px) {
        .mes-grid {
            grid-template-columns: 1fr;
        }
    }

    /* Labels */
    .mes-label {
        font-size: clamp(16px, 3vw, 20px);
        font-weight: 700;
        color: #1e1e1e;
        margin-bottom: 6px;
        display: block;
    }

    /* Inputs */
    .mes-input {
        width: 100%;
        padding: clamp(14px, 3vw, 18px);
        font-size: clamp(18px, 4vw, 24px);
        border: 3px solid #b6b6b6;
        border-radius: 4px;
        background: #ffffff;
        font-weight: 600;
        letter-spacing: 0.5px;
        transition: 0.2s;
    }

        .mes-input:focus {
            border-color: #2563eb;
            background: #eaf2ff;
            outline: none;
        }

    /* Submit Button */
    .mes-btn {
        margin-top: 25px;
        width: 100%;
        padding: clamp(16px, 3vw, 20px);
        font-size: clamp(20px, 4vw, 26px);
        font-weight: 900;
        border: none;
        background: #1a73e8;
        color: white;
        text-transform: uppercase;
        border-radius: 4px;
        letter-spacing: 1px;
        transition: 0.15s;
    }

        .mes-btn:disabled {
            background: #9bb6dd;
        }

        .mes-btn:not(:disabled):hover {
            background: #0f58b4;
        }

    /* Error */
    .mes-error {
        color: #d60000;
        font-size: clamp(16px, 3vw, 18px);
        font-weight: 800;
        margin-top: 10px;
        display: none;
    }
</style>