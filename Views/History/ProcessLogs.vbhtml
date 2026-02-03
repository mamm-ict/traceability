@Code
    ViewData("Title") = "Process Logs"
    Dim batch As Batch = CType(ViewData("Batch"), Batch)

    Dim logs As List(Of Dictionary(Of String, String)) = Nothing
    Dim materialsDict As Dictionary(Of String, List(Of MaterialLog)) = Nothing

    If ViewData("Logs") IsNot Nothing Then
        logs = CType(ViewData("Logs"), List(Of Dictionary(Of String, String)))
    Else
        logs = New List(Of Dictionary(Of String, String))()
    End If

    If ViewData("Materials") IsNot Nothing Then
        materialsDict = CType(ViewData("Materials"), Dictionary(Of String, List(Of MaterialLog)))
    Else
        materialsDict = New Dictionary(Of String, List(Of MaterialLog))()
    End If

End Code
<div class="mes-header">
    <div class="mes-title">
        Process Logs
        <span class="trace-id">Trace ID: @batch.TraceID</span>
    </div>

    <div class="mes-info">
        <div class="info-item">
            <label>Die Core</label>
            <span>@batch.Die@batch.Line</span>
        </div>
        <div class="info-item">
            <label>Bara Core</label>
            <span>@batch.BaraCoreLot</span>
        </div>
        <div class="info-item">
            <label>Part Code</label>
            <span>@ViewData("PartDesc")</span>
        </div>
    </div>
</div>


@If logs.Any() Then
    @<table class="mes-table">
        <thead style="background:#eee;">
            <tr>
                <th>Time</th>
                <th>Machine</th>
                <th>Status</th>
                <th>Qty In</th>
                <th>Qty Reject</th>
                <th>Operator</th>
            </tr>
        </thead>
        <tbody>
            @For Each log In logs
                    Dim statusClass As String = ""
                    If log.ContainsKey("Status") Then
                        statusClass = log("Status").Replace(" ", "-").ToLower()
                    End If

                    Dim matsExist As Boolean = materialsDict.ContainsKey(log("ProcID")) AndAlso materialsDict(log("ProcID")).Any()
                    Dim rowId As String = "row-" & log("ProcID")

                @<tr Class="process-row @statusClass @(If(matsExist,"has-material",""))" data-rowid="@rowId">

    <td>@log("ScanTime")</td>
    <td class="mono">@log("ProcessID")</td>
    <td>
        <span class="status-badge @statusClass">
            @log("Status")
        </span>
    </td>

    <td>@log("QtyIn")</td>
    <td>@log("QtyReject")</td>
    <td class="operator">
        @log("OperatorID")
        @If matsExist Then
            @<span class="arrow">&#9654;</span>
        End If
    </td>

</tr>

                @* Hidden material row *@
                @If matsExist Then
                    Dim mats = materialsDict(log("ProcID"))
                    @<tr class="material-row" id="@rowId">
                        <td colspan="6">
                            <table class="material-table">
                                <thead>
                                    <tr style="background:#eee;">
                                        <th>Material</th>
                                        <th>Qty</th>
                                        <th>Vendor Lot</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    @For Each mat In mats
                                        @<tr>
                                            <td>@mat.LowerMaterial</td>
                                            <td>@mat.UsageQty @mat.UOM</td>
                                            <td>@mat.VendorLot</td>
                                        </tr>
                                    Next
                                </tbody>
                            </table>
                        </td>
                    </tr>
                End If
            Next
        </tbody>
    </table>
Else

    @<p>No process logs yet.</p>
End If
@*<style>
        .accordion {
            background-color: #f1f1f1;
            color: #444;
            cursor: pointer;
            padding: 8px 12px;
            width: 100%;
            border: none;
            text-align: left;
            outline: none;
            font-size: 0.95rem;
            border-radius: 4px;
        }

            .active, .accordion:hover {
                background-color: #ddd;
            }

        .panel {
            padding: 0 12px;
            display: none;
            background-color: white;
            overflow: hidden;
        }
    </style>*@
<style>
    .material-row {
        display: none;
    }

    .arrow {
        margin-left: 8px;
        cursor: pointer;
        transition: transform 0.3s ease;
    }

        .arrow.expanded {
            transform: rotate(90deg);
        }

    .process-row.has-material {
        cursor: pointer;
    }
</style>
<script>
    // Auto redirect after 20 seconds
    setTimeout(function () {
        window.location.href = '@Url.Action("Index", "History")';
    }, 20000);

    const activeRow = document.querySelector('.process-row.in-progress');
    if (activeRow) {
        activeRow.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }

</script>
<script>
    document.querySelectorAll('.process-row.has-material').forEach(function (row) {
        row.addEventListener('click', function () {
            var rowId = row.getAttribute('data-rowid');
            var matRow = document.getElementById(rowId);

            if (matRow.style.display === 'table-row') {
                matRow.style.display = 'none';
                row.querySelector('.arrow').classList.remove('expanded');
            } else {
                matRow.style.display = 'table-row';
                row.querySelector('.arrow').classList.add('expanded');
            }
        });
    });
</script>
<style>
    /* ===== HEADER ===== */
    .mes-header {
        background: #f6f7f9;
        border: 1px solid #ddd;
        padding: 12px 16px;
        border-radius: 6px;
        margin-bottom: 16px;
        margin-top:20px;
    }

    .mes-title {
        font-size: 1.2rem;
        font-weight: 600;
    }

    .trace-id {
        font-size: 1.0rem;
        color: #666;
        margin-left: 8px;
    }

    .mes-info {
        display: flex;
        gap: 24px;
        margin-top: 8px;
        font-size: 0.85rem;
    }

        .mes-info label {
            color: #777;
            margin-right: 6px;
        }

    /* ===== MAIN TABLE ===== */
    .mes-table {
        width: 100%;
        border-collapse: collapse;
        font-size: 0.85rem;
    }

        .mes-table th {
            background: #eef0f3;
            border-bottom: 2px solid #ccc;
            padding: 8px;
            text-align: left;
        }

        .mes-table td {
            border-bottom: 1px solid #e0e0e0;
            padding: 8px;
            vertical-align: middle;
        }

        .mes-table tbody tr:hover {
            background: #f9fbff;
        }

    .process-row.has-material {
        cursor: pointer;
    }

    /* ===== STATUS ===== */
    .status-badge {
        display: inline-flex;
        align-items: center;
        line-height: 1;
        padding: 4px 10px;
    }

        .status-badge.completed {
            background: #d4f4dd;
            color: #207544;
        }

        .status-badge.in\ progress {
            background: #fff3cd;
            color: #856404;
        }

    /* ===== OPERATOR ===== */
    .operator {
        white-space: nowrap;
    }

    .mono {
        font-family: Consolas, monospace;
    }

    /* ===== MATERIAL ===== */
    .material-row {
        display: none;
        background: #fafafa;
    }

    .material-table {
        width: 100%;
        border-collapse: collapse;
        font-size: 0.8rem;
    }

        .material-table th {
            background: #f0f0f0;
            padding: 6px;
        }

        .material-table td {
            padding: 6px;
            border-bottom: 1px solid #ddd;
        }

    /* ===== ARROW ===== */
    .arrow {
        margin-left: 6px;
        font-size: 0.8rem;
        transition: transform 0.25s ease;
    }

        .arrow.expanded {
            transform: rotate(90deg);
        }

    .mes-header {
        background: #f7f9fc;
        border: 1px solid #dcdfe6;
        border-radius: 8px;
        padding: 14px 18px;
        margin-bottom: 18px;
    }

    .mes-title {
        display: flex;
        justify-content: space-between;
        align-items: center;
        font-size: 1.25rem;
        font-weight: 600;
        color: #2f3542;
    }

    .trace-id {
        background: #e8eef7;
        padding: 4px 10px;
        border-radius: 14px;
        font-weight: 500;
        color: #3a4b6d;
    }

    .mes-info {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
        gap: 12px;
        margin-top: 14px;
    }

    .info-item {
        background: #ffffff;
        border: 1px solid #e1e4ea;
        border-radius: 6px;
        padding: 8px 10px;
    }

        .info-item label {
            display: block;
            font-size: 0.7rem;
            color: #7a7f87;
            margin-bottom: 4px;
            text-transform: uppercase;
            letter-spacing: 0.03em;
        }

        .info-item span {
            font-size: 0.9rem;
            font-weight: 600;
            color: #2f3542;
        }

    .status-badge {
        display: inline-flex;
        align-items: center;
        line-height: 1;
        padding: 4px 10px;
    }
        /* blinking badge */
        .status-badge.in-progress {
            background: #fff3cd;
            color: #856404;
            animation: mes-pulse 1.8s infinite;
        }

    /* highlight whole row */
    .process-row.in-progress {
        background: #fffbe6;
        border-left: 4px solid #ffc107;
    }

    .status-badge.completed {
        background: #d4f4dd;
        color: #207544;
    }

    @@keyframes mes-pulse {
        0% {
            box-shadow: 0 0 0 0 rgba(255, 193, 7, 0.6);
        }

        70% {
            box-shadow: 0 0 0 6px rgba(255, 193, 7, 0);
        }

        100% {
            box-shadow: 0 0 0 0 rgba(255, 193, 7, 0);
        }
    }
</style>
