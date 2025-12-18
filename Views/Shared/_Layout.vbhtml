<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>@ViewBag.Title</title>
    @Styles.Render("~/Content/css")
    @Scripts.Render("~/bundles/modernizr")


</head>

<body>
    <nav class="navbar navbar-expand-sm navbar-toggleable-sm navbar-dark bg-dark">
        <div class="container">
            @Html.ActionLink("Lot Traceability", "Create", "Batch", New With {.area = ""}, New With {.class = "navbar-brand"})
            <button type="button" class="navbar-toggler" data-bs-toggle="collapse" data-bs-target=".navbar-collapse" title="Toggle navigation" aria-controls="navbarSupportedContent"
                    aria-expanded="false" aria-label="Toggle navigation">
                <span class="navbar-toggler-icon"></span>
            </button>
            <div class="collapse navbar-collapse d-sm-inline-flex justify-content-between">
                <ul class="navbar-nav flex-grow-1">
                    <li>@Html.ActionLink("Batch", "Create", "Batch", New With {.area = ""}, New With {.class = "nav-link"})</li>
                    <li>@Html.ActionLink("History", "Index", "History", New With {.area = ""}, New With {.class = "nav-link"})</li>
                    <li>@Html.ActionLink("Process", "StartProcess", "Process", New With {.area = ""}, New With {.class = "nav-link"})</li>
                    <li>@Html.ActionLink("Process Master", "ProcessMaster", "Process", New With {.area = ""}, New With {.class = "nav-link"})</li>
                </ul>

                <!-- ⭐ CLOCK DI HUJUNG KANAN -->
                <span id="navClock" style="
    color:#90CAF9;
    font-weight:600;
    margin-left:20px;
    font-size:16px;
    text-shadow:0 0 6px rgba(144,202,249,0.8);
"></span>

            </div>
        </div>
    </nav>
    <div class="container body-content" id="contentWrapper" style="padding-bottom:0;">
        @RenderBody()
        <hr />
        <footer>
            @*<p> @DateTime.Now </p>*@
            <p id="liveClock"></p>

        </footer>
    </div>

    @Scripts.Render("~/bundles/jquery")
    @Scripts.Render("~/bundles/bootstrap")
    @RenderSection("scripts", required:=False)
    @RenderSection("Keyboard", required:=False)

    <!-- ******************************************************** -->
    <!-- ****************** VIRTUAL KEYBOARD START *************** -->
    <!-- ******************************************************** -->
    <!-- WRAPPER -->
    <div id="virtualKeyboard"
         style="display:none; position:fixed; bottom:0; left:0; width:100%;
            background:#222; padding:15px; z-index:9999; color:white;">

        <!-- ABC Keyboard -->
        <div id="keyboard-abc" style="display:flex; flex-direction:column; gap:8px; align-items:center;">

            <!-- Number row -->
            <div style="display:flex; gap:6px;">
                <button class="vk-btn" data-key="1">1</button>
                <button class="vk-btn" data-key="2">2</button>
                <button class="vk-btn" data-key="3">3</button>
                <button class="vk-btn" data-key="4">4</button>
                <button class="vk-btn" data-key="5">5</button>
                <button class="vk-btn" data-key="6">6</button>
                <button class="vk-btn" data-key="7">7</button>
                <button class="vk-btn" data-key="8">8</button>
                <button class="vk-btn" data-key="9">9</button>
                <button class="vk-btn" data-key="0">0</button>
            </div>

            <!-- QWERTY row 1 -->
            <div style="display:flex; gap:6px;">
                <button class="vk-btn" data-key="Q">Q</button>
                <button class="vk-btn" data-key="W">W</button>
                <button class="vk-btn" data-key="E">E</button>
                <button class="vk-btn" data-key="R">R</button>
                <button class="vk-btn" data-key="T">T</button>
                <button class="vk-btn" data-key="Y">Y</button>
                <button class="vk-btn" data-key="U">U</button>
                <button class="vk-btn" data-key="I">I</button>
                <button class="vk-btn" data-key="O">O</button>
                <button class="vk-btn" data-key="P">P</button>
            </div>

            <!-- QWERTY row 2 -->
            <div style="display:flex; gap:6px;">
                <button class="vk-btn" data-key="A">A</button>
                <button class="vk-btn" data-key="S">S</button>
                <button class="vk-btn" data-key="D">D</button>
                <button class="vk-btn" data-key="F">F</button>
                <button class="vk-btn" data-key="G">G</button>
                <button class="vk-btn" data-key="H">H</button>
                <button class="vk-btn" data-key="J">J</button>
                <button class="vk-btn" data-key="K">K</button>
                <button class="vk-btn" data-key="L">L</button>
            </div>

            <!-- QWERTY row 3 -->
            <div style="display:flex; gap:6px;">
                <button class="vk-btn" data-key="Z">Z</button>
                <button class="vk-btn" data-key="X">X</button>
                <button class="vk-btn" data-key="C">C</button>
                <button class="vk-btn" data-key="V">V</button>
                <button class="vk-btn" data-key="B">B</button>
                <button class="vk-btn" data-key="N">N</button>
                <button class="vk-btn" data-key="M">M</button>
                <button class="vk-btn" data-key="backspace" style="background:#ff9999;">⌫</button>
            </div>

            <!-- Bottom -->
            <div style="display:flex; gap:6px;">
                <button class="vk-btn-toggle" data-target="symbols" style="background:#888;">Sym</button>
                <button class="vk-btn" data-key="space" style="min-width:200px;">Space</button>
                <button class="vk-btn" data-key="enter" style="background:#99ddff;">Enter</button>
                <button class="vk-btn" data-key="close" style="background:#ff5555;">Close</button>
            </div>
        </div>

        <!-- SYMBOL Keyboard -->
        <div id="keyboard-symbols" style="display:none; flex-direction:column; gap:8px; align-items:center;">

            <!-- Symbol Row 1 -->
            <div style="display:flex; gap:6px;">
                <button class="vk-btn" data-key="!">!</button>
                <button class="vk-btn" data-key="@@">@@</button>
                <button class="vk-btn" data-key="#">#</button>
                <button class="vk-btn" data-key="$">$</button>
                <button class="vk-btn" data-key="%">%</button>
                <button class="vk-btn" data-key="^">^</button>
                <button class="vk-btn" data-key="&">&</button>
                <button class="vk-btn" data-key="*">*</button>
                <button class="vk-btn" data-key="("> ( </button>
                <button class="vk-btn" data-key=")"> ) </button>
            </div>

            <!-- Symbol Row 2 -->
            <div style="display:flex; gap:6px;">
                <button class="vk-btn" data-key="-">-</button>
                <button class="vk-btn" data-key="_">_</button>
                <button class="vk-btn" data-key="=">=</button>
                <button class="vk-btn" data-key="+">+</button>
                <button class="vk-btn" data-key="[">[</button>
                <button class="vk-btn" data-key="]">]</button>
                <button class="vk-btn" data-key="{">{</button>
                <button class="vk-btn" data-key="}">}</button>
            </div>

            <!-- Symbol Row 3 -->
            <div style="display:flex; gap:6px;">
                <button class="vk-btn" data-key=";">;</button>
                <button class="vk-btn" data-key=":">:</button>
                <button class="vk-btn" data-key="'">'</button>
                <button class="vk-btn" data-key='"'>" </button>
                <button class="vk-btn" data-key=",">,</button>
                <button class="vk-btn" data-key=".">.</button>
                <button class="vk-btn" data-key="/">/</button>
                <button class="vk-btn" data-key="?">?</button>
                <button class="vk-btn" data-key="backspace" style="background:#ff9999;">⌫</button>
            </div>

            <!-- Bottom -->
            <div style="display:flex; gap:6px;">
                <button class="vk-btn-toggle" data-target="abc" style="background:#888;">ABC</button>
                <button class="vk-btn" data-key="space" style="min-width:200px;">Space</button>
                <button class="vk-btn" data-key="enter" style="background:#99ddff;">Enter</button>
                <button class="vk-btn" data-key="close" style="background:#ff5555;">Close</button>
            </div>

        </div>
    </div>


    <style>
        /* Make keyboard responsive (scale down on small screens) */
        #virtualKeyboard {
            display: none;
            position: fixed;
            bottom: 0;
            left: 0;
            width: 100%;
            background: #222;
            padding: 10px;
            z-index: 9999;
            color: white;
            transform-origin: bottom center;
        }

        /* Default button size */
        .vk-btn {
            font-size: 16px;
            padding: 8px;
            min-width: 40px;
            border-radius: 6px;
            border: 1px solid #666;
            background: white;
            cursor: pointer;
        }


        /* Scale keyboard for small notebook screens */
        @@media (max-width: 1200px) {
            #virtualKeyboard {
                transform: scale(0.95);
            }
        }

        @@media (max-width: 1000px) {
            #virtualKeyboard {
                transform: scale(0.85);
            }
        }

        @@media (max-width: 850px) {
            #virtualKeyboard {
                transform: scale(0.75);
            }
        }

        @@media (max-width: 700px) {
            #virtualKeyboard {
                transform: scale(0.65);
            }
        }

        @@media (max-width: 600px) {
            #virtualKeyboard {
                transform: scale(0.55);
            }
        }

        /* Extra compact mode for really small height screens */
        @@media (max-height: 450px) {
            .vk-btn {
                font-size: 14px;
                padding: 6px;
                min-width: 32px;
            }
        }

        @@media (max-width: 1500px) {
            #virtualKeyboard {
                transform: scale(0.95);
            }
        }

        @@media (max-width: 1300px) {
            #virtualKeyboard {
                transform: scale(0.85);
            }
        }

        @@media (max-width: 1100px) {
            #virtualKeyboard {
                transform: scale(0.75);
            }
        }

        @@media (max-width: 900px) {
            #virtualKeyboard {
                transform: scale(0.65);
            }
        }

        @@media (max-width: 750px) {
            #virtualKeyboard {
                transform: scale(0.55);
            }
        }

        body {
            font-family: 'Segoe UI', sans-serif;
            background: #f4f7fc;
            color: #0f2443;
        }

        .mes-title {
            text-align: center;
            font-size: clamp(26px, 4vw, 34px);
            font-weight: 800;
            color: #2b4c7e;
            text-transform: uppercase;
        }

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

        .mes-table {
            width: 100%;
            border-collapse: collapse;
            background: #ffffff;
            border: 3px solid #2b4c7e;
        }

            .mes-table th {
                background: #2b4c7e;
                color: white;
                padding: 12px;
                font-size: 16px;
                border-bottom: 3px solid #1e355a;
                text-transform: uppercase;
            }

            .mes-table td {
                padding: 10px;
                border-bottom: 2px solid #d0d0d0;
                font-size: 15px;
                font-weight: 600;
                color: #333;
            }

            .mes-table tr:hover {
                background: #e8f1ff;
            }

        .mes-link {
            color: #1a73e8;
            font-weight: 700;
            text-decoration: none;
        }

            .mes-link:hover {
                text-decoration: underline;
            }

        .qr-img {
            width: 70px !important;
            height: 70px !important;
            object-fit: contain;
            border: 2px solid #2b4c7e;
            padding: 3px;
            background: #fff;
            cursor: pointer;
        }


        .qr-modal {
            display: none;
            position: fixed;
            z-index: 1000;
            left: 0;
            top: 0;
            width: 100%;
            height: 100%;
            background: rgba(0,0,0,0.8);
            justify-content: center;
            align-items: center;
        }

        .qr-large {
            max-width: 80%;
            max-height: 80%;
            border: 3px solid white;
            border-radius: 6px;
        }

        .closeBtn {
            position: absolute;
            top: 20px;
            right: 30px;
            font-size: 40px;
            color: white;
            cursor: pointer;
            font-weight: bold;
        }

        #navClock {
            letter-spacing: 1.5px;
            word-spacing:5px;
        }
        /* Card wrapper */
        .mes-process-card {
            margin: 20px auto;
            padding: 25px;
            border: 2px solid #333;
            border-radius: 12px;
            width: fit-content;
            text-align: center;
            background: white;
            box-shadow: 0 4px 12px rgba(0,0,0,0.08);
        }

        /* Title dalam card */
        .mes-card-title {
            font-size: 26px;
            font-weight: 700;
            margin-bottom: 15px;
            letter-spacing: 0.5px;
        }

        /* QR besar */
        .mes-qr-large {
            max-width: 260px;
            margin-top: 10px;
            margin-bottom: 10px;
            display: block;
            margin-left: auto;
            margin-right: auto;
        }
        /* Center wrapper */
        .mes-route-wrapper {
            width: 100%;
            display: flex;
            justify-content: center;
            margin-top: 40px;
        }

        /* Route card box */
        .mes-route-card {
            width: 350px;
            padding: 14px 16px;
            border: 1.5px solid #000;
            border-radius: 10px;
            background: white;
            font-family: Arial, sans-serif;
            font-size: 12px;
        }

        /* Title */
        .mes-route-title {
            text-align: center;
            margin: 0 0 12px 0;
            font-size: 22px;
            font-weight: bold;
            letter-spacing: 1px;
        }

        /* Table styling */
        .mes-route-table {
            width: 100%;
            font-size: 12px;
            border-collapse: collapse;
            margin-bottom: 12px;
        }

            .mes-route-table .key {
                font-weight: bold;
                width: 35%;
                padding-right: 6px;
            }

        /* QR */
        .mes-route-qr {
            text-align: center;
        }

        .mes-route-qr-img {
            width: 150px;
            height: 150px;
            display: block;
            margin: 0 auto;
        }
        .mes-table .status-badge {
            padding: 4px 10px;
            border-radius: 12px;
            font-weight: 600;
            font-size: 13px;
        }

        .mes-table .status-progress {
            background: #fff3cd;
            color: #856404;
        }

        .mes-table .status-done {
            background: #e6f4ea;
            color: #1e7e34;
        }
        .status-pending {
            background: #fff3cd;
            color: #856404;
        }


        .mes-table .row-editable {
            background-color: #fffef5;
        }

        .mes-table .row-locked {
            opacity: 0.75;
        }

        .mes-table .lock-icon {
            font-size: 18px;
        }
        .mes-table input.vk-input {
            font-size: 18px;
            padding: 6px;
        }

        .mes-table th,
        .mes-table td {
            text-align: center;
            vertical-align: middle;
        }
        .mes-left {
            text-align: left !important;
            padding-left: 16px;
        }
        .mes-tick-btn {
            background: #2ecc71;
            color: #fff;
            border: none;
            border-radius: 20%;
            width: 34px;
            height: 34px;
            font-size: 18px;
            font-weight: bold;
            cursor: pointer;
        }

            .mes-tick-btn:active {
                transform: scale(0.95);
            }

        .done-icon {
            color: #2ecc71;
            font-size: 18px;
        }
        .mes-tick-btn:disabled {
            background: #ccc;
            cursor: not-allowed;
        }


    </style>




    <script>
        let activeInput = null;
        const kb = document.getElementById("virtualKeyboard");
        const contentWrapper = document.getElementById("contentWrapper");

        // Show keyboard
        function showKeyboard(input) {
            if (!input || !kb) return;
            activeInput = input;
            kb.style.display = "block";
            input.scrollIntoView({ block: "nearest", behavior: "instant" });
            setTimeout(() => {
                contentWrapper.style.paddingBottom = kb.offsetHeight + 20 + "px";

                requestAnimationFrame(() => {
                    const inputRect = input.getBoundingClientRect();
                    const wrapperRect = contentWrapper.getBoundingClientRect();
                    const offset = inputRect.bottom - wrapperRect.bottom;

                    //if (offset > 0)
                    //    contentWrapper.scrollBy({ top: offset + 10, behavior: "smooth" });
                });

            }, 350); // WAS 150, MAKE IT LONGER


        }

        // Hide keyboard
        function hideKeyboard() {
            kb.style.display = "none";
            contentWrapper.style.paddingBottom = "0px";
            //activeInput = null; // reset
        }


        // Keyboard button clicks
        document.addEventListener("DOMContentLoaded", function () {
            document.querySelectorAll(".vk-btn").forEach(btn => {
                btn.addEventListener("click", function () {
                    if (!activeInput) return;
                    const key = this.dataset.key;
                    if (!key) return;

                    if (key === "backspace") activeInput.value = activeInput.value.slice(0, -1);
                    else if (key === "space") activeInput.value += " ";
                    else if (key === "enter") {
                        let inputs = Array.from(document.querySelectorAll("input"));
                        let idx = inputs.indexOf(activeInput);
                        if (idx >= 0 && idx < inputs.length - 1) {
                            inputs[idx + 1].focus();
                            activeInput = inputs[idx + 1];
                        }
                        return;
                    }
                    else if (key === "close") hideKeyboard();
                    else activeInput.value += key;

                    if (activeInput) {
                        activeInput.dispatchEvent(new Event("input", { bubbles: true }));
                    }

                });
            });
        });

        document.addEventListener("mousedown", function (e) {

            // click inside keyboard (including toggle buttons)
            if (kb.contains(e.target)) return;

            // click on active input
            if (activeInput && e.target === activeInput) return;

            hideKeyboard();
        });


        document.querySelectorAll(".vk-btn-toggle").forEach(btn => {
            btn.addEventListener("click", function () {
                const target = this.dataset.target;
                if (target === "symbols") {
                    document.getElementById("keyboard-abc").style.display = "none";
                    document.getElementById("keyboard-symbols").style.display = "flex";
                } else if (target === "abc") {
                    document.getElementById("keyboard-abc").style.display = "flex";
                    document.getElementById("keyboard-symbols").style.display = "none";
                }
            });
        });


        // Attach keyboard to all inputs with class 'vk-input'
        function attachKeyboardInputs() {
            document.querySelectorAll("input.vk-input").forEach(input => {
                input.addEventListener("focus", () => showKeyboard(input));
            });
        }

        // Run on load
        document.addEventListener("DOMContentLoaded", attachKeyboardInputs);
    </script>
    @*<script>
            function updateClock() {
                const now = new Date();

                const formatted =
                    now.getFullYear() + "-" +
                    String(now.getMonth() + 1).padStart(2, "0") + "-" +
                    String(now.getDate()).padStart(2, "0") + " " +
                    String(now.getHours()).padStart(2, "0") + ":" +
                    String(now.getMinutes()).padStart(2, "0") + ":" +
                    String(now.getSeconds()).padStart(2, "0");

                document.getElementById("liveClock").innerText = formatted;
            }

            setInterval(updateClock, 1000);
            updateClock(); // initial call
        </script>*@

    <script>
        function updateClock() {
            const now = new Date();

            const months = [
                "Jan", "Feb", "Mar", "Apr", "May", "Jun",
                "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"
            ];

            const day = String(now.getDate()).padStart(2, "0");
            //const month = months[now.getMonth()];
            const month = String(now.getMonth() + 1).padStart(2, "0"); // FIX: tambah 1 + padStart
            const year = now.getFullYear();

            const h = String(now.getHours()).padStart(2, "0");
            const m = String(now.getMinutes()).padStart(2, "0");
            const s = String(now.getSeconds()).padStart(2, "0");

            //const formatted = `${day} ${month} ${year} ${h}:${m}:${s}`;
            const formatted = `${year}-${month}-${day}        ${h}:${m}:${s}`;

            const clock = document.getElementById("navClock");
            if (clock) clock.innerText = formatted;
        }

        setInterval(updateClock, 1000);
        updateClock();
    </script>


</body>
</html>