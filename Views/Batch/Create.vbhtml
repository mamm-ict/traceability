@Code
    ViewData("Title") = "Create Batch"
End Code

@*<h2>Create Batch</h2>
    <br />*@
@*<div class="pretty-form-container">
        <form method="post" action="/Batch/Create" class="pretty-form">

            <div class="form-title">Create Batch</div>

            <div class="form-grid">

                <div class="form-group">
                    <label>Model</label>
                    <input type="text" name="Model" value="@ViewData("Model")" class="vk-input" />
                </div>

                <div class="form-group">
                    <label>Bara Core Date</label>
                    <input type="date" name="BaraCore" class="vk-input" />
                </div>

                <div class="form-group">
                    <label>Line No</label>
                    <input type="text" name="Line" value="@ViewData("Line")" class="vk-input" />
                </div>

                <div class="form-group">
                    <label>Operator No</label>
                    <input type="text" name="OperatorID" value="@ViewData("OperatorID")" class="vk-input" />
                </div>

                <div class="form-group">
                    <label>Quantity</label>
                    <input type="number" name="Quantity" class="vk-input" />
                </div>

            </div>

            <div id="dupError" class="error-msg">Duplicate material names detected!</div>

            <button type="submit" id="submitBtn" class="pretty-btn" disabled>Submit</button>

        </form>
    </div>*@

<div class="mes-container">
    <form method="post" action="/Batch/Create" class="mes-panel">

        <div class="mes-title">Create Batch</div>

        <div class="mes-grid">

            <div>
                <label class="mes-label">Model</label>
                <input type="text" name="Model" value="@ViewData("Model")" class="mes-input vk-input">
            </div>

            <div>
                <label class="mes-label">Bara Core Date</label>
                <input type="date" name="BaraCoreDate" class="mes-input vk-input">
            </div>

            <div>
                <label class="mes-label">Line No</label>
                <input type="text" name="Line" value="@ViewData("Line")" class="mes-input vk-input">
            </div>

            <div>
                <label class="mes-label">Operator No</label>
                <input type="text" name="OperatorID" value="@ViewData("OperatorID")" class="mes-input vk-input">
            </div>

            <div>
                <label class="mes-label">Quantity</label>
                <input type="number" name="InitQty" class="mes-input vk-input">
            </div>

        </div>

        <div id="dupError" class="mes-error">Duplicate material names detected!</div>

        <button type="submit" id="submitBtn" class="mes-btn" disabled>Submit</button>

    </form>
</div>
<script>
    function checkForm() {
        let hasError = false;

        // Check main fields only
        ["Line", "OperatorID", "Model", "BaraCore", "InitQty"].forEach(function (field) {
            let el = document.querySelector("input[name='" + field + "']");
            if (!el) return;

            if (el.value.trim() === "") {
                hasError = true;
                if (el.dataset.touched) el.style.border = "2px solid red";
            } else {
                el.style.border = "";
            }
        });

        document.getElementById("submitBtn").disabled = hasError;
    }

    // Input triggers
    document.addEventListener("input", function (e) {
        if (["BaraCore", "Line", "InitQty", "OperatorID", "Model"].includes(e.target.name)) {
            checkForm();
        }
    });

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
        checkForm();
    };
</script>

@*<script>
        function addRow() {
            var table = document.getElementById("rawMaterials");
            var row = table.insertRow();
            row.innerHTML = `
                <td><input type="text" name="RawMaterialNames" style="font-size:18px; padding:8px; width:100%;" class="vk-input"  /></td>
                <td><input type="number" name="Quantities" style="font-size:18px; padding:8px; width:100%;" class="vk-input"  /></td>
                <td><button type="button" onclick="this.parentElement.parentElement.remove()" style="font-size:18px; padding:8px;">-</button></td>
            `;
            attachValidationEvents(row);

            row.scrollIntoView({ behavior: 'smooth', block: 'center' });
            // Focus the first input in the new row
            const firstInput = row.querySelector("input.vk-input");
            setTimeout(() => firstInput.focus(), 50); // slight delay avoids click race
        }

    </script>*@

@*<script>
        function checkForm() {
            let hasError = false;
            let nameInputs = document.querySelectorAll("input[name='RawMaterialNames']");
            let qtyInputs = document.querySelectorAll("input[name='Quantities']");
            let names = [];

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

            // Main fields
            ["Line", "OperatorID", "Model", "BaraCore", "BatchQuantity"].forEach(function (field) {
                let el = document.querySelector("input[name='" + field + "']");
                if (el.value.trim() === "") {
                    hasError = true;
                    if (el.dataset.touched) el.style.border = "2px solid red";
                } else {
                    el.style.border = "";
                }
            });

            let duplicateExists = checkDuplicates();

            // Enable submit only if no error & no duplicate
            document.getElementById("submitBtn").disabled = hasError || duplicateExists;
        }


        //function checkDuplicates() {
        //    let inputs = document.querySelectorAll("input[name='RawMaterialNames']");
        //    let names = [];
        //    let duplicated = false;

        //    inputs.forEach((input) => {
        //        let val = input.value.trim().toLowerCase();
        //        if (val !== "") {
        //            if (names.includes(val)) {
        //                duplicated = true;
        //                input.style.border = "2px solid red";  // highlight red
        //            } else {
        //                names.push(val);
        //                // only reset border if not empty
        //                if (!duplicated) input.style.border = "";
        //            }
        //        }
        //    });

        //    document.getElementById("dupError").style.display = duplicated ? "block" : "none";
        //    return duplicated; // return status duplicate untuk checkForm
        //}

        // Trigger validation whenever user types
        document.addEventListener("input", function (e) {
            if (e.target.name === "BaraCore" ||
                e.target.name === "Line" || e.target.name === "BatchQuantity" ||
                e.target.name === "OperatorID" || e.target.name === "Model") {
                checkForm();
            }
        });

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


        // Attach events untuk semua prefilled fields
        window.onload = function () {
            let allInputs = document.querySelectorAll("input");
            allInputs.forEach(input => {
                if (input) attachValidationEvents(input);
            });
            checkForm();
        };


    </script>*@


<style>
    /*.pretty-form-container {
        width: 100%;
        padding: 12px;
    }

    .pretty-form {
        width: 100%;
        padding: 20px;
        border-radius: 18px;
        background: #ffffff;
        box-shadow: 0 4px 18px rgba(0,0,0,0.06);
    }

    .form-title {
        font-size: clamp(22px, 4vw, 30px);
        font-weight: 700;
        margin-bottom: 25px;
        text-align: center;
        color: #333;
    }*/

    /* GRID FOR 2 COLUMNS */
    /*.form-grid {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 18px 22px;
    }*/

    /* Collapse to 1 column on tablet kecil / phone */
    /*@@media (max-width: 768px) {
        .form-grid {
            grid-template-columns: 1fr;
        }
    }

    .form-group {
        display: flex;
        flex-direction: column;
    }

        .form-group label {
            font-size: clamp(15px, 3vw, 18px);
            font-weight: 600;
            margin-bottom: 6px;
        }

        .form-group input {
            width: 100%;
            padding: clamp(10px, 2.5vw, 16px);
            font-size: clamp(16px, 3.5vw, 20px);
            border: 1.8px solid #d9d9d9;
            border-radius: 14px;
            background: #fafafa;
            transition: 0.25s;
        }

            .form-group input:focus {
                background: #fff;
                border-color: #6ea8fe;
                box-shadow: 0 0 0 4px rgba(110, 168, 254, 0.25);
                outline: none;
            }

    .pretty-btn {
        width: 100%;
        margin-top: 20px;
        padding: clamp(12px, 3vw, 16px);
        font-size: clamp(18px, 3.5vw, 22px);
        font-weight: bold;
        border: none;
        border-radius: 14px;
        background: linear-gradient(135deg, #4f8bff, #2563eb);
        color: white;
        cursor: pointer;
        transition: 0.25s;
    }

        .pretty-btn:hover {
            opacity: 0.9;
        }

    .error-msg {
        display: none;
        color: #d9534f;
        font-weight: bold;
        margin-bottom: 10px;
        font-size: clamp(14px, 3vw, 16px);
    }*/

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