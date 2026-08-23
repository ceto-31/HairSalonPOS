Imports HairSalonPOS.Wpf.Models

Namespace Services
    Public Class InMemoryDataStore
        Private Shared ReadOnly _instance As New Lazy(Of InMemoryDataStore)(Function() New InMemoryDataStore())

        Public Shared ReadOnly Property Instance As InMemoryDataStore
            Get
                Return _instance.Value
            End Get
        End Property

        Public ReadOnly Property Users As New List(Of UserAccount)
        Public ReadOnly Property Services As New List(Of ServiceItem)
        Public ReadOnly Property Products As New List(Of ProductItem)
        Public ReadOnly Property Categories As New List(Of CatalogCategoryNode)
        Public ReadOnly Property Packages As New List(Of PackageItem)
        Public ReadOnly Property Staff As New List(Of StaffMember)
        Public ReadOnly Property Discounts As New List(Of DiscountItem)
        Public ReadOnly Property Appointments As New List(Of AppointmentItem)
        Public ReadOnly Property StockMovements As New List(Of StockMovement)
        Public ReadOnly Property Sales As New List(Of SaleRecord)

        Public Property NextSaleId As Integer = 8
        Public Property NextMovementId As Integer = 1
        Public Const TaxRate As Decimal = 0.12D

        Public Event SaleCompleted As EventHandler
        Public Event InventoryChanged As EventHandler
        Public Event StaffChanged As EventHandler
        Public Event AppointmentsChanged As EventHandler

        Private Sub New()
            SeedData()
        End Sub

        Private Sub SeedData()
            Dim persistedUsers = UserPersistenceService.Instance.LoadUsers()
            If persistedUsers IsNot Nothing Then
                Users.AddRange(persistedUsers)
                EnsureSecurityAnswers()
                PersistUsers()
            Else
                Users.AddRange({
                    New UserAccount With {.UserId = 1, .Username = "admin", .Password = "Admin@123", .FullName = "Ana Reyes", .Role = "Admin", .FavNumber = "7", .FavColor = "blue", .FavAnimal = "dog"},
                    New UserAccount With {.UserId = 2, .Username = "cashier", .Password = "Cashier@123", .FullName = "Ana Reyes", .Role = "Cashier", .FavNumber = "3", .FavColor = "pink", .FavAnimal = "cat"}
                })
                PersistUsers()
            End If

            Dim catalog = CatalogPersistenceService.Instance.Load()
            If catalog IsNot Nothing Then
                If catalog.Services IsNot Nothing Then Services.AddRange(catalog.Services)
                If catalog.Products IsNot Nothing Then Products.AddRange(catalog.Products)
                If catalog.Categories IsNot Nothing AndAlso catalog.Categories.Count > 0 Then
                    Categories.AddRange(CloneCategories(catalog.Categories))
                End If
            End If

            If Categories.Count = 0 Then
                Categories.AddRange(DefaultCategories())
            End If

            ' Default retail products. Only seed SKUs missing after load.
            Dim retailDefaults = {
                New ProductItem With {.Sku = "P001", .Name = "Shampoo 500ml", .Brand = "Dove", .Price = 250D, .Cost = 120D, .StockOnHand = 50, .ReorderLevel = 10, .Category = "HAIR SERVICES", .SubCategory = "Hair Treatment"},
                New ProductItem With {.Sku = "P002", .Name = "Conditioner 500ml", .Brand = "Dove", .Price = 250D, .Cost = 120D, .StockOnHand = 45, .ReorderLevel = 10, .Category = "HAIR SERVICES", .SubCategory = "Hair Treatment"},
                New ProductItem With {.Sku = "P003", .Name = "Hair Color Black", .Brand = "Revlon", .Price = 350D, .Cost = 180D, .StockOnHand = 7, .ReorderLevel = 8, .Category = "HAIR SERVICES", .SubCategory = "Hair Color"},
                New ProductItem With {.Sku = "P004", .Name = "Hair Color Brown", .Brand = "Revlon", .Price = 350D, .Cost = 180D, .StockOnHand = 25, .ReorderLevel = 8, .Category = "HAIR SERVICES", .SubCategory = "Hair Color"},
                New ProductItem With {.Sku = "P005", .Name = "Hair Serum", .Brand = "Vitress", .Price = 180D, .Cost = 90D, .StockOnHand = 40, .ReorderLevel = 10, .Category = "HAIR SERVICES", .SubCategory = "Hair Treatment"}
            }
            For Each p In retailDefaults
                If Not Products.Any(Function(x) x.Sku.Equals(p.Sku, StringComparison.OrdinalIgnoreCase)) Then
                    Products.Add(p)
                End If
            Next

            BackfillProductCategories()
            PersistCatalog()

            Dim persistedStaff = StaffPersistenceService.Instance.Load()
            If persistedStaff IsNot Nothing Then
                Staff.AddRange(persistedStaff)
            Else
                Staff.AddRange({
                    New StaffMember With {.StaffId = 1, .Name = "Maria Santos", .Role = "Senior Stylist", .ContactNumber = "09171234567", .Email = "maria.santos@example.com"},
                    New StaffMember With {.StaffId = 2, .Name = "Ana Reyes", .Role = "Stylist", .ContactNumber = "09181234567", .Email = "ana.reyes@example.com"},
                    New StaffMember With {.StaffId = 3, .Name = "Luz Cruz", .Role = "Stylist", .ContactNumber = "09191234567", .Email = "luz.cruz@example.com"}
                })
                PersistStaff()
            End If

            Dim persistedDiscounts = DiscountPersistenceService.Instance.Load()
            If persistedDiscounts IsNot Nothing Then
                Discounts.AddRange(persistedDiscounts)
            Else
                Discounts.AddRange({
                    New DiscountItem With {.Code = "SENIOR", .Description = "Senior / PWD — 20% off · Always active · BIR compliant", .DiscountType = "Percent", .Value = 20D, .IsSeniorPwd = True, .IsActive = True},
                    New DiscountItem With {.Code = "BDAY", .Description = "Birthday promo — 15% off · Birthday month only", .DiscountType = "Percent", .Value = 15D, .IsActive = True},
                    New DiscountItem With {.Code = "SUMMER2026", .Description = "₱100 off · Promo code · Ends Jun 30", .DiscountType = "Fixed", .Value = 100D, .IsActive = True, .EndDate = New Date(2026, 6, 30)}
                })
                PersistDiscounts()
            End If

            Dim today = Date.Today
            Dim persistedAppointments = AppointmentPersistenceService.Instance.Load()
            If persistedAppointments IsNot Nothing Then
                Appointments.AddRange(persistedAppointments)
            Else
                Appointments.AddRange({
                    New AppointmentItem With {.AppointmentId = 1, .CustomerName = "Joy D.", .StaffName = "Maria Santos", .ServiceName = "Rebond", .StartTime = today.AddHours(9), .DurationMinutes = 120, .Status = AppointmentStatuses.Scheduled, .ContactNumber = "09171234567"},
                    New AppointmentItem With {.AppointmentId = 2, .CustomerName = "Anna C.", .StaffName = "Maria Santos", .ServiceName = "Hair Spa", .StartTime = today.AddHours(11), .DurationMinutes = 60, .Status = AppointmentStatuses.Scheduled, .ContactNumber = "09181234567"},
                    New AppointmentItem With {.AppointmentId = 3, .CustomerName = "Walk-in", .StaffName = "Ana Reyes", .ServiceName = "Haircut", .StartTime = today.AddHours(14), .DurationMinutes = 30, .Status = AppointmentStatuses.Scheduled, .ContactNumber = "09191234567"}
                })
                PersistAppointments()
            End If

            RefreshAppointmentStatuses()

            SeedSampleSales()
        End Sub

        Public Sub PersistUsers()
            UserPersistenceService.Instance.SaveUsers(Users)
        End Sub

        Public Sub PersistCatalog()
            CatalogPersistenceService.Instance.Save(Services, Products, Categories)
        End Sub

        Public Sub PersistAppointments()
            AppointmentPersistenceService.Instance.Save(Appointments)
        End Sub

        Public Sub PersistStaff()
            StaffPersistenceService.Instance.Save(Staff)
        End Sub

        Public Sub PersistDiscounts()
            DiscountPersistenceService.Instance.Save(Discounts)
        End Sub

        Private Sub BackfillProductCategories()
            Const defaultCategory = "HAIR SERVICES"
            Const defaultSubCategory = "Hair Treatment"
            Const hairColorSubCategory = "Hair Color"

            Dim knownSkus As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase) From {
                {"P001", defaultSubCategory},
                {"P002", defaultSubCategory},
                {"P003", hairColorSubCategory},
                {"P004", hairColorSubCategory},
                {"P005", defaultSubCategory}
            }

            For Each product In Products
                product.EnsureDefaults()
                If Not String.IsNullOrWhiteSpace(product.Category) Then Continue For

                Dim subCategory = defaultSubCategory
                If product.Sku IsNot Nothing AndAlso knownSkus.ContainsKey(product.Sku) Then
                    subCategory = knownSkus(product.Sku)
                End If

                product.Category = defaultCategory
                product.SubCategory = subCategory
            Next
        End Sub

        Public Shared Function DefaultCategories() As List(Of CatalogCategoryNode)
            Return New List(Of CatalogCategoryNode) From {
                New CatalogCategoryNode With {.Name = "HAIR SERVICES", .SubCategories = New List(Of String) From {"Rebond Packages", "Hair Treatment Packages", "Cut and Styles", "Hair Color", "Hair Treatment"}},
                New CatalogCategoryNode With {.Name = "NAIL SERVICES", .SubCategories = New List(Of String) From {"Basic Care", "Gel and Extensions"}},
                New CatalogCategoryNode With {.Name = "BODY SERVICES", .SubCategories = New List(Of String) From {"Spa and Scrub Packages", "Paraffin Therapy and Massage"}},
                New CatalogCategoryNode With {.Name = "EYELASH SERVICES"},
                New CatalogCategoryNode With {.Name = "EYEBROW SERVICES"},
                New CatalogCategoryNode With {.Name = "WAXING SERVICES"}
            }
        End Function

        Private Shared Function CloneCategories(source As IEnumerable(Of CatalogCategoryNode)) As List(Of CatalogCategoryNode)
            Return source.Select(Function(c) New CatalogCategoryNode With {
                .Name = c.Name,
                .SubCategories = New List(Of String)(If(c.SubCategories, New List(Of String)()))
            }).ToList()
        End Function

        Private Sub EnsureSecurityAnswers()
            For Each user In Users
                If String.IsNullOrWhiteSpace(user.FavNumber) OrElse
                   String.IsNullOrWhiteSpace(user.FavColor) OrElse
                   String.IsNullOrWhiteSpace(user.FavAnimal) Then
                    If user.Username.Equals("admin", StringComparison.OrdinalIgnoreCase) Then
                        user.FavNumber = "7"
                        user.FavColor = "blue"
                        user.FavAnimal = "dog"
                    ElseIf user.Username.Equals("cashier", StringComparison.OrdinalIgnoreCase) Then
                        user.FavNumber = "3"
                        user.FavColor = "pink"
                        user.FavAnimal = "cat"
                    Else
                        If String.IsNullOrWhiteSpace(user.FavNumber) Then user.FavNumber = "1"
                        If String.IsNullOrWhiteSpace(user.FavColor) Then user.FavColor = "blue"
                        If String.IsNullOrWhiteSpace(user.FavAnimal) Then user.FavAnimal = "dog"
                    End If
                End If
            Next
        End Sub

        Private Sub SeedSampleSales()
            ' Real receipt history comes from the ledger. Don't inject demo OR-001..OR-008 once any real sale exists.
            If ReceiptNumberService.Instance.HasIssuedReceipts() Then
                Sales.Clear()
                Return
            End If

            ' Keep sample data on yesterday so today's real checkouts stay on top when sorted by SaleDate.
            Dim baseDate = Date.Today.AddDays(-1)
            Dim cashier = "Ana Reyes"
            Sales.Clear()
            Sales.AddRange({
                New SaleRecord With {.SaleId = 1, .ReceiptNumber = "OR-001", .SaleDate = baseDate.AddHours(9.5), .CashierName = cashier, .CustomerName = "Walk-in", .StylistName = "Maria Santos", .PaymentMethod = "Cash", .SubTotal = 950D, .DiscountAmount = 0D, .Tax = 101.79D, .Total = 950D, .Lines = New List(Of SaleLineRecord) From {
                    New SaleLineRecord With {.Name = "Haircut", .Quantity = 1, .UnitPrice = 150D, .LineTotal = 150D, .IsService = True},
                    New SaleLineRecord With {.Name = "Hair Coloring", .Quantity = 1, .UnitPrice = 800D, .LineTotal = 800D, .IsService = True}
                }},
                New SaleRecord With {.SaleId = 2, .ReceiptNumber = "OR-002", .SaleDate = baseDate.AddHours(10.2), .CashierName = cashier, .CustomerName = "Joy Dela Cruz", .StylistName = "Maria Santos", .PaymentMethod = "GCash", .SubTotal = 800D, .Total = 800D, .Tax = 85.71D, .Lines = New List(Of SaleLineRecord) From {
                    New SaleLineRecord With {.Name = "Hair Coloring", .Quantity = 1, .UnitPrice = 800D, .LineTotal = 800D, .IsService = True}
                }},
                New SaleRecord With {.SaleId = 3, .ReceiptNumber = "OR-003", .SaleDate = baseDate.AddHours(11), .CashierName = cashier, .CustomerName = "Walk-in", .StylistName = "Luz Cruz", .PaymentMethod = "Card", .SubTotal = 430D, .Total = 430D, .Tax = 46.07D, .Lines = New List(Of SaleLineRecord) From {
                    New SaleLineRecord With {.Name = "Haircut", .Quantity = 1, .UnitPrice = 150D, .LineTotal = 150D, .IsService = True},
                    New SaleLineRecord With {.Name = "Hair Serum", .Quantity = 1, .UnitPrice = 180D, .LineTotal = 180D, .IsService = False}
                }},
                New SaleRecord With {.SaleId = 4, .ReceiptNumber = "OR-004", .SaleDate = baseDate.AddHours(12.1), .CashierName = cashier, .CustomerName = "Anna Cruz", .StylistName = "Maria Santos", .PaymentMethod = "Cash", .SubTotal = 600D, .Total = 600D, .Tax = 64.29D, .Lines = New List(Of SaleLineRecord) From {
                    New SaleLineRecord With {.Name = "Hair Spa", .Quantity = 1, .UnitPrice = 600D, .LineTotal = 600D, .IsService = True}
                }},
                New SaleRecord With {.SaleId = 5, .ReceiptNumber = "OR-005", .SaleDate = baseDate.AddHours(13.3), .CashierName = cashier, .CustomerName = "Walk-in", .StylistName = "Ana Reyes", .PaymentMethod = "GCash", .SubTotal = 150D, .Total = 150D, .Tax = 16.07D, .Lines = New List(Of SaleLineRecord) From {
                    New SaleLineRecord With {.Name = "Haircut", .Quantity = 1, .UnitPrice = 150D, .LineTotal = 150D, .IsService = True}
                }},
                New SaleRecord With {.SaleId = 6, .ReceiptNumber = "OR-006", .SaleDate = baseDate.AddHours(14), .CashierName = cashier, .CustomerName = "Joy Dela Cruz", .StylistName = "Luz Cruz", .PaymentMethod = "Cash", .SubTotal = 2500D, .Total = 2500D, .Tax = 267.86D, .Lines = New List(Of SaleLineRecord) From {
                    New SaleLineRecord With {.Name = "Hair Rebond", .Quantity = 1, .UnitPrice = 2500D, .LineTotal = 2500D, .IsService = True}
                }},
                New SaleRecord With {.SaleId = 7, .ReceiptNumber = "OR-007", .SaleDate = baseDate.AddHours(14.5), .CashierName = cashier, .CustomerName = "Walk-in", .StylistName = "Maria Santos", .PaymentMethod = "GCash", .SubTotal = 800D, .Total = 800D, .Tax = 85.71D, .Lines = New List(Of SaleLineRecord) From {
                    New SaleLineRecord With {.Name = "Hair Coloring", .Quantity = 1, .UnitPrice = 800D, .LineTotal = 800D, .IsService = True}
                }},
                New SaleRecord With {.SaleId = 8, .ReceiptNumber = "OR-008", .SaleDate = baseDate.AddHours(14.55), .CashierName = cashier, .CustomerName = "Walk-in", .StylistName = "Maria Santos", .PaymentMethod = "Cash", .SubTotal = 1064D, .Total = 1064D, .Tax = 114D, .Lines = New List(Of SaleLineRecord) From {
                    New SaleLineRecord With {.Name = "Haircut", .Quantity = 1, .UnitPrice = 150D, .LineTotal = 150D, .IsService = True},
                    New SaleLineRecord With {.Name = "Hair Treatment", .Quantity = 1, .UnitPrice = 500D, .LineTotal = 500D, .IsService = True},
                    New SaleLineRecord With {.Name = "Hair Serum", .Quantity = 2, .UnitPrice = 180D, .LineTotal = 360D, .IsService = False}
                }}
            })
            NextSaleId = 9
        End Sub

        Public Function GetLowStockCount() As Integer
            Return Products.Where(Function(p) p.StockOnHand <= p.ReorderLevel).Count()
        End Function

        Public Function ApplyDiscount(subTotal As Decimal, promoCode As String) As Decimal
            If String.IsNullOrWhiteSpace(promoCode) Then Return 0D
            Dim discount = Discounts.FirstOrDefault(Function(d) d.Code.Equals(promoCode.Trim(), StringComparison.OrdinalIgnoreCase) AndAlso d.IsActive)
            If discount Is Nothing Then Throw New InvalidOperationException("Invalid promo code.")
            If discount.EndDate.HasValue AndAlso discount.EndDate.Value < Date.Today Then Throw New InvalidOperationException("Promo code has expired.")
            If discount.DiscountType = "Percent" Then Return Math.Round(subTotal * discount.Value / 100D, 2)
            Return Math.Min(subTotal, discount.Value)
        End Function

        Public Sub LogMovement(sku As String, changeQty As Integer, movementType As String, userName As String, notes As String)
            Dim product = Products.FirstOrDefault(Function(p) p.Sku = sku)
            StockMovements.Insert(0, New StockMovement With {
                .MovementId = NextMovementId,
                .Sku = sku,
                .ProductName = If(product?.Name, sku),
                .ChangeQty = changeQty,
                .MovementType = movementType,
                .UserName = userName,
                .CreatedAt = DateTime.Now,
                .Notes = notes
            })
            NextMovementId += 1
            RaiseEvent InventoryChanged(Me, EventArgs.Empty)
        End Sub

        Public Sub RaiseSaleCompleted()
            RaiseEvent SaleCompleted(Me, EventArgs.Empty)
        End Sub

        Public Sub RaiseStaffChanged()
            PersistStaff()
            RaiseEvent StaffChanged(Me, EventArgs.Empty)
        End Sub

        Public Sub RaiseDiscountsChanged()
            PersistDiscounts()
        End Sub

        Public Sub MarkAppointmentDone(appointmentId As Integer)
            Dim appt = Appointments.FirstOrDefault(Function(a) a.AppointmentId = appointmentId)
            If appt Is Nothing Then Return
            If appt.Status <> AppointmentStatuses.Scheduled AndAlso appt.Status <> AppointmentStatuses.Confirmed Then Return
            appt.Status = AppointmentStatuses.Done
            appt.CompletedAt = DateTime.Now
            RaiseAppointmentsChanged()
        End Sub

        Public Function RefreshAppointmentStatuses() As Boolean
            Dim changed = False
            For Each appt In Appointments
                If (appt.Status = AppointmentStatuses.Scheduled OrElse appt.Status = AppointmentStatuses.Confirmed) AndAlso
                   appt.EndTime < DateTime.Now Then
                    appt.Status = AppointmentStatuses.NoShow
                    changed = True
                End If
            Next
            Return changed
        End Function

        Public Sub RaiseAppointmentsChanged()
            RefreshAppointmentStatuses()
            PersistAppointments()
            RaiseEvent AppointmentsChanged(Me, EventArgs.Empty)
        End Sub
    End Class
End Namespace
