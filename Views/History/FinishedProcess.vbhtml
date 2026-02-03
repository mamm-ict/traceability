@Code
    ViewData("Title") = "Finished Process"

    Dim tableRows As New List(Of Dictionary(Of String, String))
    For Each row In Model
        Dim traceId = row("TraceID")
        Dim printedDate = If(String.IsNullOrEmpty(row("PrintedDate")), "", row("PrintedDate"))
        Dim alreadyDownloaded = If(String.IsNullOrEmpty(row("PrintedDate")), "false", "true")
        Dim pdfLink = Url.Action("DownloadTracePdf", "Process", New With {Key .traceId = traceId})

        Dim newRow As New Dictionary(Of String, String)

        For Each kvp In row
            newRow(kvp.Key) = If(kvp.Value IsNot Nothing, kvp.Value.ToString(), "")
        Next

        newRow("PdfLink") = pdfLink
        newRow("AlreadyDownloaded") = alreadyDownloaded
        newRow("PrintedDateSafe") = printedDate

        tableRows.Add(newRow)
    Next
    ' Group data by PartCode
    Dim grouped As New Dictionary(Of String, List(Of Dictionary(Of String, String)))
    For Each row In Model
        Dim part = row("PartCode")
        If Not grouped.ContainsKey(part) Then
            grouped(part) = New List(Of Dictionary(Of String, String))
        End If
        grouped(part).Add(row)
    Next
End Code
@*<link rel="stylesheet"
    href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.5.1/css/all.min.css" />*@
<style>
    .accordion-item {
        border: 1px solid #ddd;
        border-radius: 8px;
        margin-bottom: 12px;
        overflow: hidden;
        box-shadow: 0 2px 6px rgba(0,0,0,0.08);
    }

    .accordion-header {
        background: #2b4c7e;
        color:white;
        padding: 12px 16px;
        cursor: pointer;
        font-weight: 600;
        display: flex;
        justify-content: space-between;
        align-items: center;
        transition: background 0.2s;
    }

        .accordion-header:hover {
            background: #e0e0e0;
            color: black;
        }

        .accordion-header::after {
            content: '\25BC'; /* down arrow */
            transition: transform 0.3s;
        }

        .accordion-header.active::after {
            transform: rotate(-180deg);
        }

    .accordion-body {
        display: none;
        padding: 12px 16px;
        background: #fff;
    }

    .mes-table {
        width: 100%;
        border-collapse: collapse;
    }

        .mes-table th, .mes-table td {
            padding: 8px 10px;
            border: 1px solid #ddd;
            text-align: center;
        }

        .mes-table th {
            background: #f0f0f0;
            font-weight: 600;
        }

    .mes-btn-pdf {
        background: #dc3545;
        color: #fff;
        border: none;
        padding: 6px 14px;
        border-radius: 8px;
        font-size: 14px;
        font-weight: 600;
        cursor: pointer;
        transition: background 0.2s;
        display: inline-flex;
        align-items: center;
        gap: 6px;
    }

        .mes-btn-pdf:hover {
            background: #bb2d3b;
        }

        .mes-btn-pdf img {
            width: 20px; /* ikut design button */
            height: 20px; /* ikut design button */
            vertical-align: middle;
            margin-right: 6px; /* jarak icon dengan text */
        }
</style>

<style>

    .mes-table td {
        vertical-align: middle;
    }

    h2 {
        margin-bottom: 20px;
        font-weight: 600;
    }

    /*        table {
            background: #fff;
        }

            table th {
                background: #f8f9fa;
                text-align: center;
                vertical-align: middle;
            }

            table td {
                vertical-align: middle;
                text-align: center;
            }*/

    /* PDF button */
    .pdf-btn {
        background: #dc3545;
        color: #fff;
        border: none;
        padding: 6px 14px;
        border-radius: 6px;
        font-size: 14px;
        cursor: pointer;
        display: inline-flex;
        align-items: center;
        gap: 6px;
        transition: background 0.2s ease;
    }

        .pdf-btn i {
            font-size: 16px;
        }

        .pdf-btn:hover {
            background: #bb2d3b;
        }

    /* Modal overlay */
    #pdfModal {
        display: none;
        position: fixed;
        inset: 0;
        background: rgba(0,0,0,0.55);
        z-index: 9999;
        justify-content: center;
        align-items: center;
    }

        #pdfModal h2 {
            font-size: 18px; /* kecilkan daripada default 32px */
            font-weight: 600;
            margin-bottom: 12px; /* jarak dengan message */
            display: flex;
            align-items: center; /* icon & text align center */
            gap: 6px; /* jarak icon-text */
        }

            #pdfModal h2 img,
            #modalDownloadAgain img {
                width: 20px; /* ikut design */
                height: 20px;
                vertical-align: middle;
                margin-right: 6px; /* space icon-text */
            }

    #modalDownloadAgain,
    #modalClose {
        padding: 6px 12px; /* kurangkan supaya tak oversized */
        font-size: 14px; /* ikut design button */
        display: inline-flex;
        align-items: center;
        gap: 6px; /* space icon-text */
    }

        #modalDownloadAgain img {
            width: 16px;
            height: 16px;
        }

    /* Modal box */
    #pdfModal .modal-box {
        background: #fff;
        padding: 24px;
        border-radius: 12px;
        width: 100%;
        max-width: 420px;
        box-shadow: 0 10px 30px rgba(0,0,0,0.2);
        animation: pop 0.2s ease;
        justify-content: flex-start !important;
        text-align: left !important;
    }

    @@keyframes pop {
        from {
            transform: scale(0.95);
            opacity: 0;
        }

        to {
            transform: scale(1);
            opacity: 1;
        }
    }

    #pdfModal h4 {
        margin-bottom: 10px;
        font-weight: 600;
    }

    #pdfModal a {
        display: inline-block;
        margin-bottom: 12px;
        color: #dc3545;
        font-weight: 500;
        text-decoration: none;
    }

        #pdfModal a:hover {
            text-decoration: underline;
        }

    /* Modal buttons */
    .modal-actions {
        display: flex;
        justify-content: flex-end;
        gap: 10px;
        margin-top: 15px;
    }

    #modalDownloadAgain {
        background: #dc3545;
        border: none;
        color: #fff;
        padding: 6px 14px;
        border-radius: 6px;
        cursor: pointer;
    }

    #modalClose {
        background: #6c757d;
        border: none;
        color: #fff;
        padding: 6px 14px;
        border-radius: 6px;
        cursor: pointer;
    }

    #modalDownloadAgain:hover {
        background: #bb2d3b;
    }

    #modalClose:hover {
        background: #5c636a;
    }

    .mes-table {
        width: 100%;
        table-layout: fixed;
    }

    .col-hidden {
        display: none;
    }

    .mes-table td,
    .mes-table th {
        padding: 10px 12px;
        white-space: normal; /* allow wrapping */
        word-break: break-word; /* force long words to wrap */
    }

    @@media (max-width: 768px) {
        .mes-table {
            display: block;
            overflow-x: auto;
            width: 100%;
        }

            .mes-table th,
            .mes-table td {
                white-space: normal;
                word-break: break-word;
            }
    }
</style>
<h1 class="mes-title">
    Completed Batches
</h1>
<div class="mes-container">
    <div class="mes-panel">
        <div class="accordion">
            @If grouped Is Nothing OrElse Not grouped.Any() Then
                @<p style="padding: 20px; text-align: center; color: gray;">No history of completed batches.</p>
            End If
            @For Each partKvp In grouped
                @<div class="accordion-item mes-table group-table">
                    <div class="accordion-header" onclick="toggleAccordion(this)">
                        @partKvp.Key
                    </div>
                    <div class="accordion-body">
                        <table class="mes-table">
                            <thead>
                                <tr>
                                    <th> Trace ID</th>
                                    <th> Model</th>
                                    <th> Quantity</th>
                                    <th> Time </th>
                                    <th> Last Print </th>
                                    <th> Action</th>
                                </tr>
                            </thead>
                            <tbody>
                                @For Each row In partKvp.Value
                                    @<tr>
                                        <td>@row("TraceID")</td>
                                        <td>@row("ModelName")</td>
                                        <td>@row("CurQty")</td>
                                        <td>@row("UpdateDate")</td>
                                        <td>@row("PrintedDate")</td>
                                        <td>
                                            <button class="mes-btn-pdf" data-traceid="@row("TraceID")"> <img src="~/file-pdf-solid-full (1).svg" /> PDF</button>
                                        </td>
                                    </tr>
                                Next
                            </tbody>
                        </table>
                    </div>
                </div>
            Next
        </div>

    </div>
</div>


<!-- Modal -->
<div id="pdfModal">
    <div class="modal-box">
        <h2><img src="~/file-pdf-solid-full.svg" />PDF Already Downloaded</h2>
        @*<i class="fa-solid fa-file-pdf"></i>*@
        <p id="modalMessage"></p>

        <a id="modalLink" href="#" target="_blank">
            <i class="fa-solid fa-arrow-up-right-from-square"></i>
            Open existing PDF
        </a>

        <div class="modal-actions">
            <button id="modalDownloadAgain">
                @*<i class="fa-solid fa-download"></i>*@
                <img src="~/download-solid-full.svg" />Download Again
            </button>
            <button id="modalClose">Cancel</button>
        </div>
    </div>
</div>
<script>
    function toggleAccordion(header) {
        header.classList.toggle('active');
        const body = header.nextElementSibling;
        if (body.style.display === 'block') {
            body.style.display = 'none';
        } else {
            body.style.display = 'block';
        }
    }
</script>

<script>
    const pdfModal = document.getElementById("pdfModal");
    const modalMessage = document.getElementById("modalMessage");
    const modalLink = document.getElementById("modalLink");
    const btnDownloadAgain = document.getElementById("modalDownloadAgain");
    const btnClose = document.getElementById("modalClose");

    let currentTraceId = null;

    // Event delegation untuk semua PDF button dalam accordion
    document.querySelector('.accordion').addEventListener('click', function (e) {
        const btn = e.target.closest('.mes-btn-pdf');
        if (!btn) return; // bukan button

        const traceId = btn.dataset.traceid;
        currentTraceId = traceId;

        fetch(`/Process/CheckPdfStatus?traceId=${traceId}`)
            .then(r => r.json())
            .then(data => {
                if (data.alreadyPrinted) {
                    modalMessage.textContent = `PDF for Trace ID ${traceId} already exists.`;
                    modalLink.href = `/Process/OpenExistingPdf?traceId=${traceId}`;
                    modalLink.textContent = "Open existing PDF";
                    pdfModal.style.display = "flex";
                } else {
                    const newWin = window.open(`/Process/DownloadTracePdf?traceId=${traceId}`, "_blank");

                    // listen tab close, baru reload page
                    const interval = setInterval(() => {
                        if (newWin.closed) {
                            clearInterval(interval);
                            location.reload(); // refresh page bila tab PDF ditutup
                        }
                    }, 500);
                }
            });
    });

    // Download again generates new PDF
    btnDownloadAgain.onclick = () => {
        if (!currentTraceId) return;
        const newWin = window.open(`/Process/DownloadTracePdf?traceId=${currentTraceId}&forceNew=true`, "_blank");
        pdfModal.style.display = "none";

        // listen tab close, baru reload page
        const interval = setInterval(() => {
            if (newWin.closed) {
                clearInterval(interval);
                location.reload(); // refresh page bila tab PDF ditutup
            }
        }, 500);
    };

    // Close modal
    btnClose.onclick = () => {
        pdfModal.style.display = "none";
    };
</script>
