Imports HairSalonPOS.Wpf.Models

Namespace Services
    Public Class CheckoutRequest
        Public Property Cart As IList(Of CartLine)
        Public Property PaymentMethod As String
        Public Property CashierName As String
        Public Property CustomerName As String
        Public Property StylistName As String
        Public Property PromoCode As String
        Public Property AmountTendered As Decimal
    End Class

    Public Class CheckoutService
        Private ReadOnly _store As InMemoryDataStore = InMemoryDataStore.Instance
        Private ReadOnly _receiptNumbers As ReceiptNumberService = ReceiptNumberService.Instance

        Public Function FinalizeSale(request As CheckoutRequest) As ReceiptModel
            Dim cart = request.Cart
            If cart Is Nothing OrElse cart.Count = 0 Then Throw New InvalidOperationException("Cart is empty.")

            For Each line In cart.Where(Function(c) Not c.IsService)
                Dim product = _store.Products.FirstOrDefault(Function(p) p.Sku = line.Sku)
                If product Is Nothing Then Throw New InvalidOperationException($"Product {line.Sku} not found.")
                If product.StockOnHand < line.Quantity Then
                    Throw New InvalidOperationException($"Insufficient stock for {product.Name}. Available: {product.StockOnHand}")
                End If
            Next

            Dim subTotal = cart.Sum(Function(c) c.LineTotal)
            Dim discount = 0D
            Dim discountLabel = String.Empty
            If Not String.IsNullOrWhiteSpace(request.PromoCode) Then
                discount = _store.ApplyDiscount(subTotal, request.PromoCode)
                discountLabel = ResolveDiscountLabel(request.PromoCode)
            End If

            Dim taxable = Math.Max(0D, subTotal - discount)
            Dim tax = Math.Round(taxable - (taxable / (1D + InMemoryDataStore.TaxRate)), 2)
            Dim vatable = taxable - tax
            Dim total = taxable

            If request.PaymentMethod = "Cash" Then
                If request.AmountTendered <= 0D Then
                    Throw New InvalidOperationException("Enter amount tendered before checkout.")
                End If
                If request.AmountTendered < total Then
                    Throw New InvalidOperationException("Amount tendered is less than total.")
                End If
            End If

            For Each line In cart.Where(Function(c) Not c.IsService)
                Dim product = _store.Products.First(Function(p) p.Sku = line.Sku)
                product.StockOnHand -= line.Quantity
                _store.LogMovement(line.Sku, -line.Quantity, "Sale", request.CashierName, "Sale checkout")
            Next
            _store.PersistCatalog()

            Dim saleId = _store.NextSaleId
            _store.NextSaleId += 1
            Dim change = If(request.PaymentMethod = "Cash", Math.Max(0D, request.AmountTendered - total), 0D)
            Dim lines = cart.Select(Function(c) New SaleLineRecord With {
                .Name = c.Name,
                .Quantity = c.Quantity,
                .UnitPrice = c.UnitPrice,
                .LineTotal = c.LineTotal,
                .IsService = c.IsService
            }).ToList()

            Dim receipt As New ReceiptModel With {
                .SaleId = saleId,
                .SaleDate = DateTime.Now,
                .CashierName = request.CashierName,
                .CustomerName = request.CustomerName,
                .StylistName = request.StylistName,
                .PaymentMethod = request.PaymentMethod,
                .SubTotal = subTotal,
                .DiscountAmount = discount,
                .DiscountLabel = discountLabel,
                .PromoCode = request.PromoCode,
                .VatableSales = vatable,
                .Tax = tax,
                .Total = total,
                .AmountTendered = request.AmountTendered,
                .ChangeGiven = change,
                .AllLines = lines,
                .ServiceLines = lines.Where(Function(l) l.IsService).ToList(),
                .ProductLines = lines.Where(Function(l) Not l.IsService).ToList()
            }

            _receiptNumbers.IssueNextOrNumber(receipt)

            Dim sale As New SaleRecord With {
                .SaleId = saleId,
                .ReceiptNumber = receipt.ReceiptNumber,
                .SaleDate = receipt.SaleDate,
                .CashierName = request.CashierName,
                .CustomerName = request.CustomerName,
                .StylistName = request.StylistName,
                .PaymentMethod = request.PaymentMethod,
                .SubTotal = subTotal,
                .DiscountAmount = discount,
                .Tax = tax,
                .Total = total,
                .PromoCode = request.PromoCode,
                .AmountTendered = request.AmountTendered,
                .ChangeGiven = change,
                .Lines = lines
            }

            _store.Sales.Add(sale)

            Dim customer = _store.Customers.FirstOrDefault(Function(c) c.Name.Equals(request.CustomerName, StringComparison.OrdinalIgnoreCase))
            If customer IsNot Nothing AndAlso Not customer.Name.Equals("Walk-in", StringComparison.OrdinalIgnoreCase) Then
                customer.VisitCount += 1
                customer.LoyaltyPoints += CInt(Math.Floor(total / 10D))
            End If

            _store.RaiseSaleCompleted()
            Return receipt
        End Function

        Private Function ResolveDiscountLabel(promoCode As String) As String
            Dim discount = _store.Discounts.FirstOrDefault(Function(d) d.Code.Equals(promoCode.Trim(), StringComparison.OrdinalIgnoreCase))
            If discount Is Nothing Then Return "Promo"
            If discount.IsSeniorPwd Then Return "Senior/PWD"
            If discount.Code.Equals("BDAY", StringComparison.OrdinalIgnoreCase) Then Return "Birthday Promo"
            Return "Promo"
        End Function
    End Class
End Namespace
