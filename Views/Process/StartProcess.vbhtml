@Code
	ViewData("Title") = "Start Process"
End Code

<h2>Start Process</h2>

<p>@ViewData("StatusMessage")</p>

@Code
	Dim form = Html.BeginForm()
End Code

<form method="post" action="@Url.Action("StartProcess")">
	<label>Scan Batch QR:</label>
	<input type="text" name="scanInput" autofocus />
	<input type="submit" value="Submit" />
</form>

@Code
	form.Dispose()
End Code
