@Code
    ViewData("Title") = "Process Logs"
End Code

<h2>Process Logs for Batch: @ViewData("TraceID")</h2>

@if ViewData("Logs") IsNot Nothing AndAlso CType(ViewData("Logs"), List(Of Dictionary(Of String, String))).Any() Then
    Dim logs = CType(ViewData("Logs"), List(Of Dictionary(Of String, String)))
    @<table class="mes-table">
        <thead>
            <tr>
                <th>Time</th>
                <th>Process ID</th>
                <th>Status</th>
                <th>Qty In</th>
                <th>Qty Out</th>
                <th>Qty Reject</th>
                <th>Operator</th>
            </tr>
        </thead>
        <tbody>
            @For Each log In logs
                @<tr>
                    <td>@log("ScanTime")</td>
                    <td>@log("ProcessID")</td>
                    <td>@log("Status")</td>
                    <td>@log("QtyIn")</td>
                    <td>@log("QtyOut")</td>
                    <td>@log("QtyReject")</td>
                    <td>@log("OperatorID")</td>
                </tr>
            Next
        </tbody>
    </table>
Else
    @<p>No process logs yet.</p>
End If
