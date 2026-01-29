Public Class TimeProvider
    Public Shared FakeTime As DateTime? = Nothing
    'Public Shared FakeTime As DateTime? = DateTime.Parse("2026-01-24 00:57:00")

    Public Shared Function Now() As DateTime
        If FakeTime.HasValue Then
            Return FakeTime.Value
        Else
            Return DateTime.Now
        End If
    End Function

    Public Shared Function GetTraceDate() As Date
        Dim now = TimeProvider.Now()
        Dim shiftBoundary As New TimeSpan(7, 45, 0)

        If now.TimeOfDay < shiftBoundary Then
            Return now.Date.AddDays(-1)
        Else
            Return now.Date
        End If
    End Function

End Class
