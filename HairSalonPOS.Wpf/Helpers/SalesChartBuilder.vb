Imports System.Collections.ObjectModel
Imports HairSalonPOS.Wpf.Models

Namespace Helpers
    Public Module SalesChartBuilder
        Private Const ChartWidth As Double = 304
        Private Const ChartHeight As Double = 120

        Public Function BuildDailyChart(
                sales As IEnumerable(Of SaleRecord),
                anchorDate As Date,
                Optional title As String = "Daily sales",
                Optional subtitle As String = Nothing) As DashboardLineChart
            Dim hourly = Enumerable.Range(8, 11).
                Select(Function(hour)
                           Dim amount = sales.
                               Where(Function(s) s.SaleDate.Date = anchorDate.Date AndAlso s.SaleDate.Hour = hour).
                               Sum(Function(s) s.Total)
                           Return New ChartDatum With {
                               .Label = New Date(anchorDate.Year, anchorDate.Month, anchorDate.Day, hour, 0, 0).ToString("h tt"),
                               .Amount = amount
                           }
                       End Function).ToList()

            If subtitle Is Nothing Then
                subtitle = $"Sales by hour · {anchorDate:MMM d, yyyy}"
            End If

            Return BuildLineChart(title, subtitle, hourly)
        End Function

        Public Function BuildWeeklyChart(
                sales As IEnumerable(Of SaleRecord),
                anchorDate As Date,
                Optional title As String = "Weekly sales",
                Optional subtitle As String = Nothing) As DashboardLineChart
            Dim weekStart = anchorDate.Date.AddDays(-CInt(anchorDate.DayOfWeek))
            Dim weekly = Enumerable.Range(0, 7).
                Select(Function(offset)
                           Dim day = weekStart.AddDays(offset)
                           Dim amount = sales.
                               Where(Function(s) s.SaleDate.Date = day).
                               Sum(Function(s) s.Total)
                           Return New ChartDatum With {
                               .Label = day.ToString("ddd"),
                               .Amount = amount
                           }
                       End Function).ToList()

            If subtitle Is Nothing Then
                subtitle = $"Week of {weekStart:MMM d, yyyy}"
            End If

            Return BuildLineChart(title, subtitle, weekly)
        End Function

        Public Function BuildYearlyChart(
                sales As IEnumerable(Of SaleRecord),
                anchorDate As Date,
                Optional title As String = "Yearly sales",
                Optional subtitle As String = Nothing) As DashboardLineChart
            Dim yearly = Enumerable.Range(1, 12).
                Select(Function(month)
                           Dim amount = sales.
                               Where(Function(s) s.SaleDate.Year = anchorDate.Year AndAlso s.SaleDate.Month = month).
                               Sum(Function(s) s.Total)
                           Return New ChartDatum With {
                               .Label = New Date(anchorDate.Year, month, 1).ToString("MMM"),
                               .Amount = amount
                           }
                       End Function).ToList()

            If subtitle Is Nothing Then
                subtitle = $"Sales in {anchorDate.Year}"
            End If

            Return BuildLineChart(title, subtitle, yearly)
        End Function

        Private Function BuildLineChart(title As String, subtitle As String, data As IList(Of ChartDatum)) As DashboardLineChart
            If data Is Nothing OrElse data.Count = 0 Then
                Return New DashboardLineChart With {
                    .Title = title,
                    .Subtitle = subtitle,
                    .CurveGeometry = ChartPathHelper.BuildSmoothCurve(Nothing),
                    .MaxAmountLabel = "₱0",
                    .Points = New ObservableCollection(Of DashboardChartPoint)()
                }
            End If

            Dim maxAmount = data.Max(Function(d) d.Amount)
            If maxAmount <= 0D Then maxAmount = 1D

            Dim points = New List(Of DashboardChartPoint)()

            For i = 0 To data.Count - 1
                Dim x = If(data.Count = 1, ChartWidth / 2, i * ChartWidth / (data.Count - 1))
                Dim y = ChartHeight - (CDbl(data(i).Amount) / CDbl(maxAmount) * (ChartHeight - 16)) - 8
                points.Add(New DashboardChartPoint With {
                    .Label = data(i).Label,
                    .Amount = data(i).Amount,
                    .X = x,
                    .Y = y,
                    .MarkerLeft = x - 4,
                    .MarkerTop = y - 4
                })
            Next

            Return New DashboardLineChart With {
                .Title = title,
                .Subtitle = subtitle,
                .CurveGeometry = ChartPathHelper.BuildSmoothCurve(points),
                .MaxAmountLabel = $"₱{maxAmount:N0}",
                .Points = New ObservableCollection(Of DashboardChartPoint)(points)
            }
        End Function

        Private Class ChartDatum
            Public Property Label As String = String.Empty
            Public Property Amount As Decimal
        End Class
    End Module
End Namespace
