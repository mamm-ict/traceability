@Code
    ' Path to schedule
    Dim schedulePath As String = Server.MapPath("~/Config/schedule.txt")
    Dim scheduleLines() As String = System.IO.File.ReadAllLines(schedulePath)

    ' Shift names
    Dim shiftNames() As String = {"A", "C"}

    ' Map lines to shifts
    Dim scheduleList = scheduleLines.
        Where(Function(line) Not String.IsNullOrWhiteSpace(line)).
        Select(Function(line, idx)
                   Dim startTime As DateTime = DateTime.ParseExact(line.Trim(), "HH:mm", Nothing)
                   Return New With {
                       .Shift = shiftNames(idx),
                       .Start = startTime
                   }
               End Function).ToList()

    ' Tentukan current shift
    Dim nowTime As DateTime = TimeProvider.Now()
    Dim currentShift As String = ""

    For i As Integer = 0 To scheduleList.Count - 1
        Dim s = scheduleList(i)
        Dim nextShift = scheduleList((i + 1) Mod scheduleList.Count)

        ' End time = next shift start
        Dim endTime As DateTime = nextShift.Start
        ' Adjust endTime if crossing midnight
        If endTime <= s.Start Then endTime = endTime.AddDays(1)

        ' Adjust nowTime for comparison
        Dim checkTime As DateTime = nowTime
        If checkTime.TimeOfDay < s.Start.TimeOfDay AndAlso endTime.Day > checkTime.Day Then
            checkTime = checkTime.AddDays(1)
        End If

        If checkTime >= s.Start AndAlso checkTime < endTime Then
            currentShift = s.Shift
            Exit For
        End If
    Next


End Code

<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>@ViewBag.Title</title>
    @Styles.Render("~/Content/css")
    @Scripts.Render("~/bundles/modernizr")
    <link rel="icon" type="image/x-icon" href="~/favicon.ico" />
</head>

<body>
    <nav class="navbar navbar-expand-sm navbar-toggleable-sm navbar-dark bg-dark">
        <div class="container">
            @Html.ActionLink("Lot Traceability", "Create", "Batch", New With {.area = ""}, New With {.class = "navbar-brand"})
            <button type="button" class="navbar-toggler" data-bs-toggle="collapse" data-bs-target=".navbar-collapse" title="Toggle navigation" aria-controls="navbarSupportedContent"
                    aria-expanded="false" aria-label="Toggle navigation">
                <span class="navbar-toggler-icon"></span>
            </button>
            <div class="collapse navbar-collapse">
                <!-- LEFT LINKS -->
                <ul class="navbar-nav me-auto">
                    <li class="nav-item">@Html.ActionLink("Create Route Card", "Create", "Batch", New With {.area = ""}, New With {.class = "nav-link"})</li>
                    <li class="nav-item">@Html.ActionLink("History", "Index", "History", New With {.area = ""}, New With {.class = "nav-link"})</li>
                    <li class="nav-item">@Html.ActionLink("Process", "StartProcess", "Process", New With {.area = ""}, New With {.class = "nav-link"})</li>
                    <li class="nav-item">@Html.ActionLink("Process Master", "ProcessMaster", "Process", New With {.area = ""}, New With {.class = "nav-link"})</li>
                    <li class="nav-item">@Html.ActionLink("Final Process", "FinalProcess", "Process", New With {.area = ""}, New With {.class = "nav-link"})</li>
                    <li class="nav-item">@Html.ActionLink("Finished Process", "FinishedProcess", "History", New With {.area = ""}, New With {.class = "nav-link"})</li>
                    @*<li>@Html.ActionLink("Buffer", "Index", "Buffer", New With {.area = ""}, New With {.class = "nav-link"})</li>*@
                </ul>

                <div class="d-flex align-items-center" style="gap:20px;">
                    <span id="navShift"
                          style="
          background:#444;          /* gelap tapi bukan hitam pekat */
          color:#FFD700;           /* text kuning tapi soft */
          font-weight:bold;
          font-size:16px;
          padding:3px 10px;
          border-radius:5px;
          box-shadow:0 0 5px rgba(255,215,0,0.5);
      ">
                        @currentShift
                    </span>


                    <span id="navClock"
                          style="
             color:#90CAF9;
             font-weight:600;
             font-size:18px;
             letter-spacing: 1px;
             text-shadow:0 0 6px rgba(144,202,249,0.8);
          ">
                        00:00:00
                    </span>
                </div>

            </div>
        </div>
    </nav>
    <div id="fullscreenInfo" style="
    display: none;
    position: fixed;
    top: 10px;
    right: 20px;
    display: flex;
    gap: 20px; /* jarak antara clock & shift */
    align-items: center;
    z-index: 10000;
">
        <div id="fullscreenClock" style="
        color: #1f2937;
        font-weight: 700;
        font-size: 25px;
        letter-spacing: 1.5px;
        background: rgba(255,255,255,0.95);
        padding: 4px 10px;
        border-radius: 6px;
        box-shadow: 0 2px 6px rgba(0,0,0,0.15);
        pointer-events: none;
    "></div>

        <div id="fullscreenShift" style="
        background:#444;
        color:#FFD700;
        font-weight:bold;
        font-size:20px;
        padding:5px 12px;
        border-radius:6px;
        box-shadow:0 0 6px rgba(255,215,0,0.6);
    ">
            @currentShift
        </div>
    </div>

    <div class="container body-content" id="contentWrapper" style="padding-bottom:0;">
        @RenderBody()
        <hr />
        <footer>
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
/* ===== Body & Global ===== */
body {
    font-family: 'Segoe UI', Arial, sans-serif;
    background-color: #f4f7fc;
    color: #0f2443;
}

/* ============================= */
/* Virtual Keyboard */
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

.vk-btn {
    font-size: 16px;
    padding: 8px;
    min-width: 40px;
    border-radius: 6px;
    border: 1px solid #666;
    background: white;
    cursor: pointer;
}

/* Keyboard scale by screen width */
@@media (max-width: 1200px) { #virtualKeyboard { transform: scale(0.95); } }
@@media (max-width: 1000px) { #virtualKeyboard { transform: scale(0.85); } }
@@media (max-width: 850px)  { #virtualKeyboard { transform: scale(0.75); } }
@@media (max-width: 700px)  { #virtualKeyboard { transform: scale(0.65); } }
@@media (max-width: 600px)  { #virtualKeyboard { transform: scale(0.55); } }

/* Extra compact mode for really small height screens */
@@media (max-height: 450px) {
    .vk-btn { font-size: 14px; padding: 6px; min-width: 32px; }
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

/* ============================= */
/* MES Page Base Styles */
.mes-title {
    text-align: center;
    font-size: clamp(26px, 4vw, 50px);
    font-weight: 800;
    color: #2b4c7e;
    text-transform: uppercase;
}

.mes-container { width: 100%; padding: 18px; }

        .mes-panel, .mes-process-card, .mes-route-card {
            background: #f4f4f4;
            border: 3px solid #2b4c7e;
            border-radius: 12px;
            padding: 25px;
            box-shadow: 0 4px 12px rgba(0,0,0,0.08);
            margin: 20px auto;
        }

/* Table & QR */
.mes-table, .mes-route-table {
    width: 100%;
    border-collapse: collapse;
    background: #fff;
    border: 3px solid #2b4c7e;
    font-size: 1em;
}

        .mes-table th {
            background: #2b4c7e;
            color: white;
            padding: 12px;
            font-size: 16px;
            border-bottom: 3px solid #1e355a;
            text-transform: uppercase;
            text-align: center;
            vertical-align: middle;
        }

        .mes-table td {
            padding: 10px;
            border-bottom: 2px solid #d0d0d0;
            font-size: 15px;
            font-weight: 600;
            color: #333;
            text-align: center;
            vertical-align: middle;
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
            word-spacing: 5px;
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

/* ============================= */
/* FULLSCREEN NATURAL SCALE */
body.is-fullscreen {
    height: 100vh;
    overflow-y: auto;
    font-size: clamp(22px, 2.5vw, 36px);
    line-height: 1.6;
    padding: 0; margin: 0;
}

body.is-fullscreen #contentWrapper {
    width: 100%;
    max-width: 1400px;
    margin: 0 auto;
    padding: 40px;
    display: flex;
    flex-direction: column;
    align-items: center;
    text-align: center;
}

body.is-fullscreen .mes-title {
    font-size: clamp(40px, 5vw, 65px);
}

body.is-fullscreen .mes-panel,
body.is-fullscreen .mes-process-card,
body.is-fullscreen .mes-route-card {
    width: 100%;
    max-width: 1400px;
    padding: clamp(25px, 3vw, 40px);
    font-size: clamp(1em, 1.2vw, 1.4em);
}

body.is-fullscreen input.vk-input { font-size: 20px; padding: 10px; }
body.is-fullscreen .vk-btn { font-size: 18px; padding: 10px 14px; }
body.is-fullscreen .mes-tick-btn { width: 45px; height: 45px; font-size: 22px; }

body.is-fullscreen .qr-img { width: 90px; height: 90px; }
body.is-fullscreen .mes-qr-large { max-width: 350px; }
body.is-fullscreen .mes-route-qr-img { width: 180px; height: 180px; }

body.is-fullscreen .mes-container { padding: 60px 80px; }

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
    </style>
    @*<style>

           
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

                .mes-route-table.key {
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

            .mes-table.status-badge {
                padding: 4px 10px;
                border-radius: 12px;
                font-weight: 600;
                font-size: 13px;
            }

            .mes-table.status-progress {
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

            .mes-table.row-editable {
                background-color: #fffef5;
            }

            .mes-table.row-locked {
                opacity: 0.75;
            }

            .mes-table.lock-icon {
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

            nav.navbar.fullscreen-hidden {
                display: none !important;
            }

            body.is-fullscreen {
                height: 100vh;
                overflow-y: auto;
                font-size: clamp(28px, 3vw, 48px); /* teks besar untuk older eyes */
                line-height: 1.6;
                padding: 0;
                margin: 0;
            }

                body.is-fullscreen #contentWrapper {
                    max-width: none;
                    width: 100%;
                    margin: 0 auto;
                    padding: 20px;
                    display: flex;
                    flex-direction: column;
                    align-items: center; /* center smaller content */
                    text-align: center; /* semua teks center supaya mudah baca */
                }

            @@media (min-height: 800px) {
                body.is-fullscreen #contentWrapper {
                    display: flex;
                    flex-direction: column;
                    justify-content: center; /* vertical center bila tinggi screen > content */
                }
            }
            body.is-fullscreen .mes-panel,
            body.is-fullscreen .mes-process-card,
            body.is-fullscreen .mes-route-card {
                width: 100%;
                max-width: 1400px;
                margin: 20px auto;
                padding: 30px;
                border-radius: 12px;
                font-size: 1.4em; /* lebih besar dari normal */
            }

            body.is-fullscreen .mes-title {
                font-size: clamp(48px, 6vw, 72px);
            }
            /* MES pages wrapper */
            body.is-fullscreen .mes-container {
                width: 100%;
                max-width: 1400px;
                margin: auto;
            }

            /* Optional: force text-align center for smaller content inside wrapper */
            body.is-fullscreen #contentWrapper > * {
                text-align: center;
            }

            /* ============================= */
            /* FULLSCREEN MES PAGE FIX       */
            /* ============================= */
            body.is-fullscreen {
                height: 100vh;
                overflow-y: auto;
                display: block;
                padding: 0;
                margin: 0;
            }

                /* content wrapper full width */
                body.is-fullscreen #contentWrapper {
                    max-width: none !important;
                    width: 100% !important;
                    margin: 0 !important;
                    padding: 20px; /* optional spacing around edges */
                    display: flex;
                    flex-direction: column;
                    align-items: center; /* center smaller content */
                }

                /* mes container stretch full width */
                body.is-fullscreen .mes-container {
                    width: 100% !important;
                    max-width: none !important;
                    margin: 0 auto;
                    padding: 60px 100px; /* adjust spacing as needed */
                }

                /* panels, cards stretch full width */
                body.is-fullscreen .mes-panel,
                body.is-fullscreen .mes-process-card,
                body.is-fullscreen .mes-route-card {
                    width: 100% !important;
                    max-width: none !important;
                    margin: 20px auto; /* maintain spacing between cards */
                }

                /* optional: center smaller content inside wrapper */
                body.is-fullscreen #contentWrapper > * {
                    text-align: center;
                }

                /* QR, tables, and route cards */
                body.is-fullscreen .mes-table,
                body.is-fullscreen .mes-route-table,
                body.is-fullscreen .mes-route-qr {
                    width: 100% !important;
                    max-width: none !important;
                }
        </style>*@

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
            window.dispatchEvent(new Event('resize'));

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

    <script>
        function updateClock() {
            const now = new Date();

            // ===== SHIFT A START TIME (dari schedule.txt) =====
            const shiftAStartHour = 7;
            const shiftAStartMinute = 45;

            // ===== PRODUCTION DATE LOGIC =====
            const productionDate = new Date(now);

            if (
                now.getHours() < shiftAStartHour ||
                (now.getHours() === shiftAStartHour && now.getMinutes() < shiftAStartMinute)
            ) {
                productionDate.setDate(productionDate.getDate() - 1);
            }

            // ===== FORMAT DATE =====
            const day = String(productionDate.getDate()).padStart(2, "0");
            const month = String(productionDate.getMonth() + 1).padStart(2, "0");
            const year = productionDate.getFullYear();

            // ===== FORMAT TIME (REAL TIME) =====
            const h = String(now.getHours()).padStart(2, "0");
            const m = String(now.getMinutes()).padStart(2, "0");
            const s = String(now.getSeconds()).padStart(2, "0");

            const formatted = `${year}-${month}-${day}        ${h}:${m}:${s}`;

            const navClock = document.getElementById("navClock");
            if (navClock) navClock.innerText = formatted;

            const fsClock = document.getElementById("fullscreenClock");
            if (fsClock) fsClock.innerText = formatted;
        }

        setInterval(updateClock, 1000);
        updateClock();

        function enterFullscreen() {
            const el = document.documentElement; // or contentWrapper
            if (el.requestFullscreen) el.requestFullscreen();
            else if (el.webkitRequestFullscreen) el.webkitRequestFullscreen();
            else if (el.mozRequestFullScreen) el.mozRequestFullScreen();
            else if (el.msRequestFullscreen) el.msRequestFullscreen();
        }

        let lastWidth = window.innerWidth;
        let lastHeight = window.innerHeight;

        window.addEventListener('resize', () => {
            const navbar = document.querySelector('nav.navbar');
            const fsClock = document.getElementById('fullscreenClock'); // ✅ FIX
            if (!navbar) return;

            const isNowFullscreen = window.innerWidth === screen.width && window.innerHeight === screen.height;

            if (isNowFullscreen) {
                navbar.style.display = 'none';
                fsClock.style.display = 'block';
                document.body.classList.add('is-fullscreen'); // 🔥 CENTER ON
            } else {
                navbar.style.display = 'flex';
                fsClock.style.display = 'none';
                document.body.classList.remove('is-fullscreen'); // 🔥 BACK TO NORMAL
            }

        });

        window.addEventListener('resize', () => {
            const navbar = document.querySelector('nav.navbar');
            const fsClock = document.getElementById('fullscreenClock'); // ✅ FIX
            if (!navbar) return;

            const isNowFullscreen = window.innerWidth === screen.width && window.innerHeight === screen.height;

            if (isNowFullscreen) {
                navbar.style.display = 'none';
                fsClock.style.display = 'block';
                document.body.classList.add('is-fullscreen'); // 🔥 CENTER ON
            } else {
                navbar.style.display = 'flex';
                fsClock.style.display = 'none';
                document.body.classList.remove('is-fullscreen'); // 🔥 BACK TO NORMAL
            }

        });

        function checkFullscreen() {
            const navbar = document.querySelector('nav.navbar');
            const fsClock = document.getElementById('fullscreenClock');
            const fsShift = document.getElementById('fullscreenShift'); // ✅
            const fsInfo = document.getElementById('fullscreenInfo');
            if (!navbar) return;

            const isNowFullscreen = window.innerWidth === screen.width && window.innerHeight === screen.height;

            if (isNowFullscreen) {
                navbar.style.display = 'none';
                fsClock.style.display = 'block';
                fsShift.style.display = 'block'; // ✅ tunjuk shift
                fsInfo.style.display = 'flex'; // ✅ wrapper
                document.body.classList.add('is-fullscreen');
            } else {
                navbar.style.display = 'flex';
                fsInfo.style.display = 'none';
                fsClock.style.display = 'none';
                fsShift.style.display = 'none'; // ✅ sembunyi shift
                document.body.classList.remove('is-fullscreen');
            }
        }


        // Jalankan bila DOM siap
        document.addEventListener('DOMContentLoaded', checkFullscreen);

        // Masih run bila resize
        window.addEventListener('resize', checkFullscreen);

    </script>

</body>
</html>