Imports HairSalonPOS.Wpf.Helpers
Imports HairSalonPOS.Wpf.Models

Namespace Services
    Public Class CheckoutRequest
        Public Property Cart As IList(Of CartLine)
        Public Property PaymentMethod As String
        Public Property CashierName As String
        Public Property CustomerName As String
        Public Property PromoCode As String
        Public Property AmountTendered As Decimal
        Public Property AllowReserveUse As Boolean
    End Class

    Public Class ConsumableStockShortfall
        Public Property Product As ProductItem
        Public Property UnitsNeeded As Integer
        Public Property FromOnHand As Integer
        Public Property FromReserve As Integer
    End Class

    Public Class ConsumableStockAnalysis
        Public Property ReserveShortfalls As New List(Of ConsumableStockShortfall)
        Public Property InsufficientMessage As String = String.Empty
    End Class

    Public Class CheckoutService
        Private ReadOnly _store As InMemoryDataStore = InMemoryDataStore.Instance
        Private ReadOnly _receiptNumbers As ReceiptNumberService = ReceiptNumberService.Instance

        Private Class ConsumableNeed
            Public Property Product As ProductItem
            Public Property UnitsNeeded As Integer
            Public Property ServiceNotes As New List(Of String)
        End Class

        Public Function AnalyzeConsumableStock(cart As IEnumerable(Of CartLine)) As ConsumableStockAnalysis
            Dim analysis As New ConsumableStockAnalysis()
            Dim consumableNeeds = BuildConsumableNeeds(cart)

            For Each need In consumableNeeds.Values
                need.Product.EnsureDefaults()
                Dim fromOnHand = Math.Min(need.Product.StockOnHand, need.UnitsNeeded)
                Dim shortfall = need.UnitsNeeded - fromOnHand

                If shortfall <= 0 Then Continue For

                If need.Product.ReservedQty < shortfall Then
                    analysis.InsufficientMessage = String.Format(
                        "Insufficient {0}. Need {1}, have {2} on hand and {3} reserve stock.",
                        need.Product.Name, need.UnitsNeeded, need.Product.StockOnHand, need.Product.ReservedQty)
                    Return analysis
                End If

                analysis.ReserveShortfalls.Add(New ConsumableStockShortfall With {
                    .Product = need.Product,
                    .UnitsNeeded = need.UnitsNeeded,
                    .FromOnHand = fromOnHand,
                    .FromReserve = shortfall
                })
            Next

            Return analysis
        End Function

        Public Function FinalizeSale(request As CheckoutRequest) As ReceiptModel
            Dim cart = request.Cart
            If cart Is Nothing OrElse cart.Count = 0 Then Throw New InvalidOperationException("Cart is empty.")

            Dim consumableNeeds = BuildConsumableNeeds(cart)
            Dim stockAnalysis = AnalyzeConsumableStock(cart)
            If Not String.IsNullOrWhiteSpace(stockAnalysis.InsufficientMessage) Then
                Throw New InvalidOperationException(stockAnalysis.InsufficientMessage)
            End If
            If stockAnalysis.ReserveShortfalls.Count > 0 AndAlso Not request.AllowReserveUse Then
                Throw New InvalidOperationException("Reserve stock confirmation is required before checkout.")
            End If

            Dim subTotal = cart.Sum(Function(c) c.LineTotal)
            Dim discount = 0D
            Dim discountLabel = String.Empty
            If Not String.IsNullOrWhiteSpace(request.PromoCode) Then
                discount = _store.ApplyDiscount(subTotal, request.PromoCode)
                discountLabel = ResolveDiscountLabel(request.PromoCode)
            End If

            Dim taxable = Math.Max(0D, subTotal - discount)
            Dim tax = 0D
            Dim vatable = 0D
            Dim total = taxable

            If request.PaymentMethod = "Cash" Then
                If request.AmountTendered <= 0D Then
                    Throw New InvalidOperationException("Enter amount tendered before checkout.")
                End If
                If request.AmountTendered < total Then
                    Throw New InvalidOperationException("Amount tendered is less than total.")
                End If
            End If

            For Each need In consumableNeeds.Values
                need.Product.EnsureDefaults()
                Dim fromOnHand = Math.Min(need.Product.StockOnHand, need.UnitsNeeded)
                Dim fromReserve = need.UnitsNeeded - fromOnHand
                Dim notes = String.Join("; ", need.ServiceNotes.Distinct())

                If fromOnHand > 0 Then
                    need.Product.StockOnHand -= fromOnHand
                    _store.LogMovement(need.Product.Sku, -fromOnHand, "Service Use", request.CashierName, notes)
                End If

                If fromReserve > 0 Then
                    If need.Product.ReservedQty < fromReserve Then
                        Throw New InvalidOperationException(String.Format(
                            "Insufficient reserve stock for {0}. Need {1}, have {2}.",
                            need.Product.Name, fromReserve, need.Product.ReservedQty))
                    End If
                    need.Product.ReservedQty -= fromReserve
                    _store.LogMovement(need.Product.Sku, -fromReserve, "Use Reserve Stock (Checkout)", request.CashierName, notes)
                End If
            Next
            _store.PersistCatalog()

            Dim saleId = _store.NextSaleId
            _store.NextSaleId += 1
            Dim change = If(request.PaymentMethod = "Cash", Math.Max(0D, request.AmountTendered - total), 0D)

            For Each line In cart.Where(Function(c) c.IsService)
                If String.IsNullOrWhiteSpace(line.StylistName) Then
                    Throw New InvalidOperationException("Assign a stylist to each service before checkout.")
                End If
            Next

            Dim lines = cart.Select(Function(c) New SaleLineRecord With {
                .Name = c.Name,
                .Quantity = c.Quantity,
                .UnitPrice = c.UnitPrice,
                .LineTotal = c.LineTotal,
                .IsService = c.IsService,
                .StylistName = If(c.IsService, c.StylistName.Trim(), String.Empty)
            }).ToList()

            Dim stylistSummary = SaleStylistHelper.BuildSaleStylistSummary(lines)
            Dim customerName = If(String.IsNullOrWhiteSpace(request.CustomerName), "Walk-in", request.CustomerName.Trim())

            Dim receipt As New ReceiptModel With {
                .SaleId = saleId,
                .SaleDate = DateTime.Now,
                .CashierName = request.CashierName,
                .CustomerName = customerName,
                .StylistName = stylistSummary,
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
                .CustomerName = customerName,
                .StylistName = stylistSummary,
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
            _store.RaiseSaleCompleted()
            Return receipt
        End Function

        Private Function BuildConsumableNeeds(cart As IEnumerable(Of CartLine)) As Dictionary(Of String, ConsumableNeed)
            Dim needs As New Dictionary(Of String, ConsumableNeed)(StringComparer.OrdinalIgnoreCase)

            For Each line In cart.Where(Function(c) c.IsService)
                Dim service = _store.Services.FirstOrDefault(Function(s) s.Sku = line.Sku)
                If service Is Nothing OrElse service.Consumables Is Nothing Then Continue For

                Dim pickOneSlots = service.Consumables.Where(Function(c) c.Kind = ServiceConsumableKind.PickOne).Count()
                If pickOneSlots > 0 Then
                    If line.ConsumableSelections Is Nothing OrElse line.ConsumableSelections.Count < pickOneSlots Then
                        Throw New InvalidOperationException(String.Format("Select products used for {0} before checkout.", service.Name))
                    End If
                End If

                For Each consumable In service.Consumables.Where(Function(c) c.Kind = ServiceConsumableKind.Fixed)
                    If String.IsNullOrWhiteSpace(consumable.ProductSku) OrElse consumable.Quantity <= 0D Then Continue For
                    AddConsumableNeed(needs, consumable.ProductSku, line.Quantity, consumable.Quantity,
                                      String.Format("{0} x {1}", service.Name, line.Quantity))
                Next

                If line.ConsumableSelections IsNot Nothing Then
                    For Each selected In line.ConsumableSelections
                        If String.IsNullOrWhiteSpace(selected.ProductSku) OrElse selected.Quantity <= 0D Then Continue For
                        Dim product = _store.Products.FirstOrDefault(Function(p) p.Sku = selected.ProductSku)
                        Dim productName = If(product?.Name, selected.ProductSku)
                        AddConsumableNeed(needs, selected.ProductSku, line.Quantity, selected.Quantity,
                                          String.Format("{0} - {1}", service.Name, productName))
                    Next
                End If
            Next

            Return needs
        End Function

        Private Sub AddConsumableNeed(needs As Dictionary(Of String, ConsumableNeed),
                                      productSku As String,
                                      serviceLineQty As Integer,
                                      perServiceQty As Decimal,
                                      note As String)
            Dim product = _store.Products.FirstOrDefault(Function(p) p.Sku = productSku)
            If product Is Nothing Then
                Throw New InvalidOperationException(String.Format("Product {0} was not found.", productSku))
            End If
            If Not product.IsActive Then
                Throw New InvalidOperationException(String.Format("{0} is archived.", product.Name))
            End If

            Dim units = ConsumableUnitsNeeded(serviceLineQty, perServiceQty)

            If needs.ContainsKey(productSku) Then
                needs(productSku).UnitsNeeded += units
                needs(productSku).ServiceNotes.Add(note)
            Else
                needs(productSku) = New ConsumableNeed With {
                    .Product = product,
                    .UnitsNeeded = units,
                    .ServiceNotes = New List(Of String) From {note}
                }
            End If
        End Sub

        Private Shared Function ConsumableUnitsNeeded(serviceQty As Integer, perServiceQty As Decimal) As Integer
            Return CInt(Math.Ceiling(serviceQty * perServiceQty))
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
