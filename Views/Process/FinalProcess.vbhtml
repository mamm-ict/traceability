@Code
    ViewData("Title") = "Final Process"
    Dim finalList = CType(ViewData("FinalList"), List(Of Dictionary(Of String, Object)))
End Code

<h2>Final Process Completion</h2>

<table class="table table-bordered">
    <thead>
        <tr>
            <th>Trace ID</th>
            <th>Model</th>
            <th>Part Code</th>
            <th>Current Qty</th>
            <th>Last Process</th>
            <th>Status</th>
            <th>Complete</th>
        </tr>
    </thead>
    <tbody>
        @For Each row In finalList
            @<tr>
                <td>@row("TraceID")</td>
                <td>@row("ModelName")</td>
                <td>@row("PartCode")</td>
                <td>@row("CurQty")</td>
                <td>@row("LastProcCode")</td>
                <td>@row("Status")</td>
                <td>
                    <form method="post" action="@Url.Action("CompleteFinal", "Process")">
                        <input type="hidden" name="traceId" value="@row("TraceID")" />
                        <input type="hidden" name="procId" value="@row("ProcessID")" />
                        <button type="submit" class="btn btn-primary">Complete</button>
                    </form>
                </td>
            </tr>
        Next
    </tbody>
</table>
