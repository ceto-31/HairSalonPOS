Imports System.Windows
Imports System.Windows.Media
Imports HairSalonPOS.Wpf.Models

Namespace Helpers
    Public Module DonutChartBuilder
        Public Const Size As Double = 132
        Private Const OuterRadius As Double = 58
        Private Const InnerRadius As Double = 34
        Private ReadOnly Center As New Point(Size / 2, Size / 2)

        Public Function Build(items As IList(Of Tuple(Of String, Decimal)),
                              Optional customBrushes As IList(Of Brush) = Nothing) As List(Of DashboardDonutSlice)
            Dim slices = New List(Of DashboardDonutSlice)()
            If items Is Nothing Then Return slices

            Dim total = items.Sum(Function(i) i.Item2)
            If total <= 0D Then Return slices

            Dim colors = If(customBrushes IsNot Nothing AndAlso customBrushes.Count > 0, customBrushes, Palette())
            Dim angle = -90.0
            Dim colorIndex = 0

            For Each item In items.Where(Function(i) i.Item2 > 0D)
                Dim sweep = CDbl(item.Item2 / total) * 360.0
                If sweep <= 0 Then Continue For
                If sweep >= 359.99 Then sweep = 359.99

                slices.Add(New DashboardDonutSlice With {
                    .Label = item.Item1,
                    .Amount = item.Item2,
                    .AmountLabel = $"₱{item.Item2:N0}",
                    .PercentLabel = $"{(item.Item2 / total):P0}",
                    .SliceBrush = colors(colorIndex Mod colors.Count),
                    .SliceGeometry = BuildSlice(angle, sweep)
                })
                angle += sweep
                colorIndex += 1
            Next

            Return slices
        End Function

        Private Function BuildSlice(startAngle As Double, sweep As Double) As PathGeometry
            Dim outerStart = PointOnCircle(Center, OuterRadius, startAngle)
            Dim outerEnd = PointOnCircle(Center, OuterRadius, startAngle + sweep)
            Dim innerEnd = PointOnCircle(Center, InnerRadius, startAngle + sweep)
            Dim innerStart = PointOnCircle(Center, InnerRadius, startAngle)
            Dim isLarge = sweep > 180

            Dim figure As New PathFigure With {
                .StartPoint = outerStart,
                .IsClosed = True
            }
            figure.Segments.Add(New ArcSegment(outerEnd, New Size(OuterRadius, OuterRadius), 0, isLarge, SweepDirection.Clockwise, True))
            figure.Segments.Add(New LineSegment(innerEnd, True))
            figure.Segments.Add(New ArcSegment(innerStart, New Size(InnerRadius, InnerRadius), 0, isLarge, SweepDirection.Counterclockwise, True))

            Dim geometry As New PathGeometry()
            geometry.Figures.Add(figure)
            geometry.Freeze()
            Return geometry
        End Function

        Private Function PointOnCircle(center As Point, radius As Double, angleDegrees As Double) As Point
            Dim radians = angleDegrees * Math.PI / 180.0
            Return New Point(center.X + radius * Math.Cos(radians), center.Y + radius * Math.Sin(radians))
        End Function

        Private Function Palette() As List(Of Brush)
            Dim keys = {"AccentBrush", "SoftGoldBrush", "MutedTextBrush", "HeaderStatBrush", "CtaDarkBrush", "ChipBorderBrush"}
            Dim brushes = New List(Of Brush)()
            For Each key In keys
                Dim found = TryCast(Application.Current?.Resources(key), Brush)
                If found IsNot Nothing Then
                    brushes.Add(found)
                End If
            Next
            If brushes.Count = 0 Then
                brushes.Add(New SolidColorBrush(CType(ColorConverter.ConvertFromString("#6B4423"), Color)))
                brushes.Add(New SolidColorBrush(CType(ColorConverter.ConvertFromString("#C8A97E"), Color)))
                brushes.Add(New SolidColorBrush(CType(ColorConverter.ConvertFromString("#8B7355"), Color)))
            End If
            Return brushes
        End Function
    End Module
End Namespace
