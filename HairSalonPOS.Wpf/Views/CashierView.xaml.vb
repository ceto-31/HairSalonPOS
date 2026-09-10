Imports HairSalonPOS.Wpf.ViewModels

Namespace Views
    Partial Public Class CashierView
        Inherits UserControl

        Public Sub New()
            InitializeComponent()
        End Sub

        Private Sub AmountInput_LostFocus(sender As Object, e As RoutedEventArgs)
            Dim vm = TryCast(DataContext, CashierViewModel)
            vm?.FormatAmountTenderedInput()
        End Sub
    End Class
End Namespace
