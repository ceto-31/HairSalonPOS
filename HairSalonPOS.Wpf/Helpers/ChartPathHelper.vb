Imports System.Windows
Imports System.Windows.Media
Imports HairSalonPOS.Wpf.Models

Namespace Helpers
    Public Module ChartPathHelper
        Public Function BuildSmoothCurve(points As IList(Of DashboardChartPoint)) As PathGeometry
            Dim geometry = New PathGeometry()

            If points Is Nothing OrElse points.Count = 0 Then
                Return geometry
            End If

            If points.Count = 1 Then
                Dim point = points(0)
                Dim dot = New PathFigure With {
                    .StartPoint = New Point(point.X, point.Y),
                    .IsClosed = False
                }
                dot.Segments.Add(New LineSegment(New Point(point.X + 0.01, point.Y), True))
                geometry.Figures.Add(dot)
                geometry.Freeze()
                Return geometry
            End If

            Dim figure = New PathFigure With {
                .StartPoint = New Point(points(0).X, points(0).Y),
                .IsClosed = False
            }

            If points.Count = 2 Then
                figure.Segments.Add(New LineSegment(New Point(points(1).X, points(1).Y), True))
                geometry.Figures.Add(figure)
                geometry.Freeze()
                Return geometry
            End If

            For i = 0 To points.Count - 2
                Dim previous = points(Math.Max(i - 1, 0))
                Dim current = points(i)
                Dim [next] = points(i + 1)
                Dim following = points(Math.Min(i + 2, points.Count - 1))

                Dim control1 = New Point(
                    current.X + ([next].X - previous.X) / 6,
                    current.Y + ([next].Y - previous.Y) / 6)
                Dim control2 = New Point(
                    [next].X - (following.X - current.X) / 6,
                    [next].Y - (following.Y - current.Y) / 6)

                figure.Segments.Add(New BezierSegment(
                    control1,
                    control2,
                    New Point([next].X, [next].Y),
                    True))
            Next

            geometry.Figures.Add(figure)
            geometry.Freeze()
            Return geometry
        End Function

        Public Function BuildAreaFill(points As IList(Of DashboardChartPoint), chartHeight As Double) As PathGeometry
            Dim curve = BuildSmoothCurve(points)
            If curve.Figures.Count = 0 OrElse points Is Nothing OrElse points.Count = 0 Then
                Return New PathGeometry()
            End If

            Dim figure = curve.Figures(0).Clone()
            figure.IsClosed = True
            Dim last = points(points.Count - 1)
            Dim first = points(0)
            figure.Segments.Add(New LineSegment(New Point(last.X, chartHeight), True))
            figure.Segments.Add(New LineSegment(New Point(first.X, chartHeight), True))

            Dim fill As New PathGeometry()
            fill.Figures.Add(figure)
            fill.Freeze()
            Return fill
        End Function
    End Module
End Namespace
