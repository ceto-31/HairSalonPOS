Imports System.Windows.Threading
Imports HairSalonPOS.Wpf.Models

Namespace Services
    Public Class ExpirationScanService
        Private Shared ReadOnly _instance As New Lazy(Of ExpirationScanService)(Function() New ExpirationScanService())

        Private ReadOnly _store As InMemoryDataStore = InMemoryDataStore.Instance
        Private ReadOnly _scanTimer As DispatcherTimer
        Private Const DefaultWarningDays As Integer = 7
        Private Const ScanIntervalHours As Integer = 4

        Public Shared ReadOnly Property Instance As ExpirationScanService
            Get
                Return _instance.Value
            End Get
        End Property

        Public Event AlertsUpdated As EventHandler

        Private Sub New()
            _scanTimer = New DispatcherTimer With {
                .Interval = TimeSpan.FromHours(ScanIntervalHours)
            }
            AddHandler _scanTimer.Tick, AddressOf OnScanTimerTick
        End Sub

        Public Sub StartPeriodicScan()
            RunScanAndNotify()
            If Not _scanTimer.IsEnabled Then _scanTimer.Start()
        End Sub

        Public Sub StopPeriodicScan()
            _scanTimer.Stop()
        End Sub

        Public Function Scan() As List(Of ExpirationAlertRow)
            Dim today = Date.Today
            Dim rows As New List(Of ExpirationAlertRow)

            For Each batch In _store.StockBatches.Where(
                Function(b) b.QuantityRemaining > 0 AndAlso b.ExpirationDate.HasValue AndAlso
                            (Not b.AcknowledgedUntil.HasValue OrElse b.AcknowledgedUntil.Value < today))

                Dim product = _store.Products.FirstOrDefault(Function(p) p.Sku = batch.Sku)
                If product Is Nothing OrElse Not product.IsActive Then Continue For

                Dim warningDays = If(product.ExpirationWarningDays, DefaultWarningDays)
                Dim daysRemaining = CInt((batch.ExpirationDate.Value.Date - today).TotalDays)
                If daysRemaining > warningDays Then Continue For

                rows.Add(New ExpirationAlertRow With {
                    .BatchId = batch.BatchId,
                    .Sku = batch.Sku,
                    .ProductName = product.Name,
                    .BoxCode = batch.BoxCode,
                    .ExpirationDate = batch.ExpirationDate.Value.Date,
                    .ImagePath = product.ImagePath,
                    .QuantityRemaining = batch.QuantityRemaining,
                    .IsReserve = batch.IsReserve
                })
            Next

            Return rows.OrderBy(Function(r) r.DaysRemaining).ThenBy(Function(r) r.ProductName).ToList()
        End Function

        Private Sub OnScanTimerTick(sender As Object, e As EventArgs)
            RunScanAndNotify()
        End Sub

        Private Sub RunScanAndNotify()
            Scan()
            RaiseEvent AlertsUpdated(Me, EventArgs.Empty)
        End Sub
    End Class
End Namespace
