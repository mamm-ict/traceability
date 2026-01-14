Public Class TimeProvider
    Public Shared FakeTime As DateTime? = Nothing
    'Public Shared FakeTime As DateTime? = DateTime.Parse("2026-01-12 07:00:00")

    Public Shared Function Now() As DateTime
        If FakeTime.HasValue Then
            Return FakeTime.Value
        Else
            Return DateTime.Now
        End If
    End Function
End Class
