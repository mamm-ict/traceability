@Code
    ViewData("Title") = "Index"

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

End Code
<link rel="stylesheet"
      href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.5.1/css/all.min.css" />
<style>
    .mes-btn-pdf {
        background: #dc3545;
        color: #fff;
        border: none;
        padding: 6px 14px;
        border-radius: 8px;
        font-size: 14px;
        font-weight: 600;
        cursor: pointer;
        display: inline-flex;
        align-items: center;
        gap: 6px;
    }

        .mes-btn-pdf:hover {
            background: #bb2d3b;
        }

    .mes-table td {
        vertical-align: middle;
    }

    h2 {
        margin-bottom: 20px;
        font-weight: 600;
    }

    table {
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
        }

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
        white-space: nowrap;
    }

</style>
<h1 class="mes-title">
    Completed Batches
</h1>
<div class="mes-card mes-process-card">



    <table class="mes-table">
        <thead>
            <tr>
                <th>Trace ID</th>
                <th>Model</th>
                <th>Part Code</th>
                <th>Qty</th>
                <th class="col-hidden">Process ID</th>
                <th>Buffer</th>
                <th style="width:120px; text-align:center;">Action</th>
            </tr>
        </thead>

        <tbody>
            @For Each row In tableRows
                @<tr>
                    <td style="font-weight:600;">
                        @row("TraceID")
                    </td>

                    <td>
                        @row("ModelName")
                    </td>

                    <td>
                        @row("PartCode")
                    </td>

                    <td >
                        @row("CurQty")
                    </td>

                    <td class="col-hidden">
                        @row("ProcID")
                    </td>

                    <td >
                        @row("QtyOut")
                    </td>

                    <td style="text-align:center;">
                        <button class="mes-btn-pdf pdf-btn"
                                data-traceid="@row("TraceID")">
                            <i class="fa-solid fa-file-pdf"></i>
                            PDF
                        </button>
                    </td>
                </tr>
            Next
        </tbody>
    </table>

</div>

<!-- Modal -->
<div id="pdfModal">
    <div class="modal-box">
        <h4><i class="fa-solid fa-file-pdf"></i> PDF Already Downloaded</h4>
        <p id="modalMessage"></p>

        <a id="modalLink" href="#" target="_blank">
            <i class="fa-solid fa-arrow-up-right-from-square"></i>
            Open existing PDF
        </a>

        <div class="modal-actions">
            <button id="modalDownloadAgain">
                <i class="fa-solid fa-download"></i> Download Again
            </button>
            <button id="modalClose">Cancel</button>
        </div>
    </div>
</div>


<script>
    const pdfModal = document.getElementById("pdfModal");
    const modalMessage = document.getElementById("modalMessage");
    const modalLink = document.getElementById("modalLink");
    const btnDownloadAgain = document.getElementById("modalDownloadAgain");
    const btnClose = document.getElementById("modalClose");

    let currentTraceId = null; // Track which PDF is active in modal

    document.querySelectorAll(".pdf-btn").forEach(btn => {
        btn.addEventListener("click", function () {
            const traceId = this.dataset.traceid;
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
                        window.open(`/Process/DownloadTracePdf?traceId=${traceId}`, "_blank");
                    }
                });
        });
    });


    // Download again generates new PDF
    btnDownloadAgain.onclick = () => {
        if (!currentTraceId) return;
        window.open(`/Process/DownloadTracePdf?traceId=${currentTraceId}&forceNew=true`, "_blank");
        pdfModal.style.display = "none";
    };

    // Close modal
    btnClose.onclick = () => {
        pdfModal.style.display = "none";
    };
</script>

