Imports HairSalonPOS.Wpf.Models

Namespace Helpers
    Public Module ReceiptModelMapper
        Public Function FromSaleRecord(sale As SaleRecord) As ReceiptModel
            If sale Is Nothing Then Return Nothing

            Dim discountLabel = ResolveDiscountLabel(sale.PromoCode)

            Return New ReceiptModel With {
                .SaleId = sale.SaleId,
                .ReceiptNumber = sale.ReceiptNumber,
                .SaleDate = sale.SaleDate,
                .CashierName = sale.CashierName,
                .CustomerName = sale.CustomerName,
                .StylistName = sale.StylistName,
                .PaymentMethod = sale.PaymentMethod,
                .SubTotal = sale.SubTotal,
                .DiscountAmount = sale.DiscountAmount,
                .DiscountLabel = discountLabel,
                .PromoCode = sale.PromoCode,
                .VatableSales = 0D,
                .Tax = sale.Tax,
                .Total = sale.Total,
                .AmountTendered = sale.AmountTendered,
                .ChangeGiven = sale.ChangeGiven,
                .AllLines = If(sale.Lines?.ToList(), New List(Of SaleLineRecord)()),
                .ServiceLines = If(sale.Lines?.Where(Function(l) l.IsService).ToList(), New List(Of SaleLineRecord)()),
                .ProductLines = If(sale.Lines?.Where(Function(l) Not l.IsService).ToList(), New List(Of SaleLineRecord)())
            }
        End Function

        Public Function ToSaleRecord(receipt As ReceiptModel) As SaleRecord
            If receipt Is Nothing Then Return Nothing

            Return New SaleRecord With {
                .SaleId = receipt.SaleId,
                .ReceiptNumber = receipt.ReceiptNumber,
                .SaleDate = receipt.SaleDate,
                .CashierName = receipt.CashierName,
                .CustomerName = receipt.CustomerName,
                .StylistName = receipt.StylistName,
                .PaymentMethod = receipt.PaymentMethod,
                .SubTotal = receipt.SubTotal,
                .DiscountAmount = receipt.DiscountAmount,
                .Tax = receipt.Tax,
                .Total = receipt.Total,
                .PromoCode = receipt.PromoCode,
                .AmountTendered = receipt.AmountTendered,
                .ChangeGiven = receipt.ChangeGiven,
                .Lines = If(receipt.AllLines?.ToList(), New List(Of SaleLineRecord)())
            }
        End Function

        Private Function ResolveDiscountLabel(promoCode As String) As String
            If String.IsNullOrWhiteSpace(promoCode) Then Return String.Empty
            If promoCode.Equals("SENIOR", StringComparison.OrdinalIgnoreCase) Then Return "Senior/PWD"
            If promoCode.Equals("BDAY", StringComparison.OrdinalIgnoreCase) Then Return "Birthday Promo"
            Return "Promo"
        End Function
    End Module
End Namespace
