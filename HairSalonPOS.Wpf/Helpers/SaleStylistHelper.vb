Imports HairSalonPOS.Wpf.Models

Namespace Helpers
    Public Module SaleStylistHelper
        Public Function BuildSaleStylistSummary(lines As IEnumerable(Of SaleLineRecord)) As String
            If lines Is Nothing Then Return String.Empty

            Dim names = lines.
                Where(Function(l) l IsNot Nothing AndAlso l.IsService AndAlso Not String.IsNullOrWhiteSpace(l.StylistName)).
                Select(Function(l) l.StylistName.Trim()).
                Distinct(StringComparer.OrdinalIgnoreCase).
                ToList()

            Return String.Join(", ", names)
        End Function

        Public Function ResolveLineStylist(line As SaleLineRecord, sale As SaleRecord) As String
            If line Is Nothing OrElse Not line.IsService Then Return String.Empty
            If Not String.IsNullOrWhiteSpace(line.StylistName) Then Return line.StylistName.Trim()

            If sale Is Nothing OrElse sale.Lines Is Nothing OrElse String.IsNullOrWhiteSpace(sale.StylistName) Then
                Return String.Empty
            End If

            Dim serviceLines = sale.Lines.Where(Function(l) l IsNot Nothing AndAlso l.IsService)
            If serviceLines.Any(Function(l) Not String.IsNullOrWhiteSpace(l.StylistName)) Then
                Return String.Empty
            End If

            Return sale.StylistName.Trim()
        End Function

        Public Function GetCreditedServiceLines(sale As SaleRecord, staffName As String) As IEnumerable(Of SaleLineRecord)
            If sale Is Nothing OrElse sale.Lines Is Nothing OrElse String.IsNullOrWhiteSpace(staffName) Then
                Return Enumerable.Empty(Of SaleLineRecord)()
            End If

            Return sale.Lines.
                Where(Function(l) l IsNot Nothing AndAlso l.IsService).
                Where(Function(l) staffName.Equals(ResolveLineStylist(l, sale), StringComparison.OrdinalIgnoreCase))
        End Function

        Public Function CountServicesForStaff(sales As IEnumerable(Of SaleRecord), staffName As String) As Integer
            If sales Is Nothing OrElse String.IsNullOrWhiteSpace(staffName) Then Return 0

            Return sales.Sum(Function(s) GetCreditedServiceLines(s, staffName).Sum(Function(l) l.Quantity))
        End Function

        Public Function StylistSummaryLabel(stylistSummary As String) As String
            If String.IsNullOrWhiteSpace(stylistSummary) Then Return String.Empty
            If stylistSummary.Contains(","c) Then Return "Stylists"
            Return "Stylist"
        End Function
    End Module
End Namespace
