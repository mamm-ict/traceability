@Code
    ViewData("Title") = "Final Process"
    Dim finalList = CType(ViewData("FinalList"), List(Of Dictionary(Of String, Object)))
End Code
<style>
    .mes-btn-primary {
        background-color: #1a73e8;
        color: #fff;
        padding: 6px 14px;
        border: none;
        border-radius: 6px;
        font-weight: 600;
        cursor: pointer;
        transition: background 0.2s, transform 0.1s;
    }

        .mes-btn-primary:hover {
            background-color: #1669c1;
            transform: translateY(-1px);
        }

        .mes-btn-primary:active {
            transform: translateY(0);
        }

    .mes-table td form {
        display: flex;
        justify-content: center;
    }
</style>

<h1 class="mes-title">Final Process Completion</h1>

<div class="mes-card mes-process-card">

    <h2 class="mes-card-title">
        Final Station Queue
    </h2>

    @If finalList IsNot Nothing AndAlso finalList.Count > 0 Then

        @<table class="mes-table">
            <thead>
                <tr>
                    <th> Trace ID</th>
                    <th> Model</th>
                    <th> Part Code</th>
                    <th style="text-align:right;"> Qty</th>
                    <th> Last Process</th>
                    <th> Status</th>
                    <th style="width:120px; text-align:center;"> Action</th>
                </tr>
            </thead>

            <tbody>
                @For Each row In finalList
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

                        <td style="text-align:right;">
                            @row("CurQty")
                        </td>

                        <td>
                            @row("LastProcCode")
                        </td>

                        <td>
                            <span class="mes-status mes-status-ready">
                                @row("Status")
                            </span>
                        </td>

                        <td style="text-align:center;">
                            <form method="post" action="@Url.Action("CompleteFinal", "Process")">
                                <input type="hidden" name="traceId" value="@row("TraceID")" />
                                <input type="hidden" name="procId" value="@row("ProcessID")" />

                                <button type="submit" class="mes-btn-primary">
                                    Complete
                                </button>
                            </form>
                        </td>
                    </tr>
                Next
            </tbody>
        </table>

    Else
        @<p class="mes-empty">
            No batches waiting for final completion.
        </p>
    End If

</div>
