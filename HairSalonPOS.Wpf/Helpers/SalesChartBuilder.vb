Imports System.Collections.ObjectModel
Imports System.Windows.Media
Imports HairSalonPOS.Wpf.Models

Namespace Helpers
    Public Module SalesChartBuilder
        Public Const CompactWidth As Double = 304
        Public Const WideWidth As Double = 720
        Private Const ChartHeight As Double = 120

        Public Function BuildDailyChart(
                sales As IEnumerable(Of SaleRecord),
                anchorDate As Date,
                Optional title As String = "Daily sales",
                Optional subtitle As String = Nothing,
                Optional chartWidth As Double = CompactWidth) As DashboardLineChart
            Dim hourly = Enumerable.Range(8, 11).
                Select(Function(hour)
                           Dim amount = sales.
                               Where(Function(s) s.SaleDate.Date = anchorDate.Date AndAlso s.SaleDate.Hour = hour).
                               Sum(Function(s) s.Total)
                           Return New ChartDatum With {
                               .Label = New Date(anchorDate.Year, anchorDate.Month, anchorDate.Day, hour, 0, 0).ToString("h tt"),
                               .Amount = amount,
                               .IsEmphasis = hour = DateTime.Now.Hour AndAlso anchorDate.Date = Date.Today,
                               .ShowLabel = True
                           }
                       End Function).ToList()

            If subtitle Is Nothing Then
                subtitle = $"Sales by hour · {anchorDate:MMM d, yyyy}"
            End If

            Return BuildAreaChart(title, subtitle, hourly, chartWidth)
        End Function

        Public Function BuildWeeklyChart(
                sales As IEnumerable(Of SaleRecord),
                anchorDate As Date,
                Optional title As String = "Weekly sales",
                Optional subtitle As String = Nothing,
                Optional chartWidth As Double = CompactWidth,
                Optional asArea As Boolean = False) As DashboardLineChart
            Dim weekStart = anchorDate.Date.AddDays(-CInt(anchorDate.DayOfWeek))
            Dim weekly = Enumerable.Range(0, 7).
                Select(Function(offset)
                           Dim day = weekStart.AddDays(offset)
                           Dim amount = sales.
                               Where(Function(s) s.SaleDate.Date = day).
                               Sum(Function(s) s.Total)
                           Return New ChartDatum With {
                               .Label = day.ToString("ddd"),
                               .Amount = amount,
                               .IsEmphasis = day = Date.Today,
                               .ShowLabel = True
                           }
                       End Function).ToList()

            If subtitle Is Nothing Then
                subtitle = $"Week of {weekStart:MMM d, yyyy}"
            End If

            Return If(asArea, BuildAreaChart(title, subtitle, weekly, chartWidth), BuildBarChart(title, subtitle, weekly, chartWidth))
        End Function

        Public Function BuildMonthlyChart(
                sales As IEnumerable(Of SaleRecord),
                anchorDate As Date,
                Optional title As String = "Monthly sales",
                Optional subtitle As String = Nothing,
                Optional chartWidth As Double = CompactWidth,
                Optional asArea As Boolean = False) As DashboardLineChart
            Dim daysInMonth = Date.DaysInMonth(anchorDate.Year, anchorDate.Month)
            Dim monthly = Enumerable.Range(1, daysInMonth).
                Select(Function(day)
                           Dim dateValue = New Date(anchorDate.Year, anchorDate.Month, day)
                           Dim amount = sales.
                               Where(Function(s) s.SaleDate.Date = dateValue).
                               Sum(Function(s) s.Total)
                           Dim showLabel = day = 1 OrElse day = daysInMonth OrElse day Mod 5 = 0
                           Return New ChartDatum With {
                               .Label = day.ToString(),
                               .Amount = amount,
                               .IsEmphasis = dateValue = Date.Today,
                               .ShowLabel = showLabel
                           }
                       End Function).ToList()

            If subtitle Is Nothing Then
                subtitle = $"Sales in {anchorDate:MMMM yyyy}"
            End If

            Return If(asArea, BuildAreaChart(title, subtitle, monthly, chartWidth), BuildBarChart(title, subtitle, monthly, chartWidth))
        End Function

        Public Function BuildYearlyChart(
                sales As IEnumerable(Of SaleRecord),
                anchorDate As Date,
                Optional title As String = "Yearly sales",
                Optional subtitle As String = Nothing,
                Optional chartWidth As Double = CompactWidth,
                Optional asArea As Boolean = False) As DashboardLineChart
            Dim yearly = Enumerable.Range(1, 12).
                Select(Function(month)
                           Dim amount = sales.
                               Where(Function(s) s.SaleDate.Year = anchorDate.Year AndAlso s.SaleDate.Month = month).
                               Sum(Function(s) s.Total)
                           Return New ChartDatum With {
                               .Label = New Date(anchorDate.Year, month, 1).ToString("MMM"),
                               .Amount = amount,
                               .IsEmphasis = month = Date.Today.Month AndAlso anchorDate.Year = Date.Today.Year,
                               .ShowLabel = True
                           }
                       End Function).ToList()

            If subtitle Is Nothing Then
                subtitle = $"Sales in {anchorDate.Year}"
            End If

            Return If(asArea, BuildAreaChart(title, subtitle, yearly, chartWidth), BuildBarChart(title, subtitle, yearly, chartWidth))
        End Function

        Private Function BuildAreaChart(title As String, subtitle As String, data As IList(Of ChartDatum), chartWidth As Double) As DashboardLineChart
            Dim points = BuildPoints(data, chartWidth, isBar:=False)
            Return New DashboardLineChart With {
                .Title = title,
                .Subtitle = subtitle,
                .ChartKind = "Area",
                .ChartWidth = chartWidth,
                .CurveGeometry = ChartPathHelper.BuildSmoothCurve(points),
                .AreaGeometry = ChartPathHelper.BuildAreaFill(points, ChartHeight),
                .MaxAmountLabel = MaxLabel(data),
                .Points = New ObservableCollection(Of DashboardChartPoint)(points)
            }
        End Function

        Private Function BuildBarChart(title As String, subtitle As String, data As IList(Of ChartDatum), chartWidth As Double) As DashboardLineChart
            Dim points = BuildPoints(data, chartWidth, isBar:=True)
            Return New DashboardLineChart With {
                .Title = title,
                .Subtitle = subtitle,
                .ChartKind = "Bar",
                .ChartWidth = chartWidth,
                .CurveGeometry = New PathGeometry(),
                .AreaGeometry = New PathGeometry(),
                .MaxAmountLabel = MaxLabel(data),
                .Points = New ObservableCollection(Of DashboardChartPoint)(points)
            }
        End Function

        Private Function BuildPoints(data As IList(Of ChartDatum), chartWidth As Double, isBar As Boolean) As List(Of DashboardChartPoint)
            Dim points = New List(Of DashboardChartPoint)()
            If data Is Nothing OrElse data.Count = 0 Then Return points

            Dim maxAmount = data.Max(Function(d) d.Amount)
            If maxAmount <= 0D Then maxAmount = 1D
            Dim count = data.Count
            Dim slotWidth = chartWidth / count

            For i = 0 To count - 1
                Dim amountRatio = CDbl(data(i).Amount) / CDbl(maxAmount)
                Dim y = ChartHeight - (amountRatio * (ChartHeight - 16)) - 8
                Dim x = If(count = 1, chartWidth / 2, i * chartWidth / (count - 1))
                Dim barWidth = Math.Max(4, slotWidth * 0.6)
                Dim barHeight = Math.Max(2, amountRatio * (ChartHeight - 16))
                Dim barLeft = i * slotWidth + (slotWidth - barWidth) / 2
                Dim barTop = ChartHeight - barHeight - 8
                points.Add(New DashboardChartPoint With {
                    .Label = data(i).Label,
                    .Amount = data(i).Amount,
                    .X = If(isBar, barLeft + barWidth / 2, x),
                    .Y = y,
                    .MarkerLeft = x - 4,
                    .MarkerTop = y - 4,
                    .BarLeft = barLeft,
                    .BarTop = barTop,
                    .BarWidth = barWidth,
                    .BarHeight = barHeight,
                    .IsEmphasis = data(i).IsEmphasis,
                    .ShowLabel = data(i).ShowLabel,
                    .BarOpacity = If(data(i).IsEmphasis, 1.0, 0.72)
                })
            Next

            Return points
        End Function

        Private Function MaxLabel(data As IList(Of ChartDatum)) As String
            If data Is Nothing OrElse data.Count = 0 Then Return "₱0"
            Dim maxAmount = data.Max(Function(d) d.Amount)
            If maxAmount <= 0D Then Return "₱0"
            Return $"₱{maxAmount:N0}"
        End Function

        Private Class ChartDatum
            Public Property Label As String = String.Empty
            Public Property Amount As Decimal
            Public Property IsEmphasis As Boolean
            Public Property ShowLabel As Boolean = True
        End Class
    End Module
End Namespace
