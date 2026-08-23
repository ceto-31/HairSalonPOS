Imports HairSalonPOS.Wpf.Models
Imports HairSalonPOS.Wpf.ViewModels

Namespace Views
    Partial Public Class AppointmentsView
        Inherits UserControl

        Public Sub New()
            InitializeComponent()
        End Sub

        Private Sub AppointmentHistoryGrid_MouseDoubleClick(sender As Object, e As MouseButtonEventArgs)
            If AppointmentHistoryGrid.SelectedItem Is Nothing Then Return

            Dim row = TryCast(AppointmentHistoryGrid.SelectedItem, AppointmentHistoryRow)
            If row?.SourceAppointment Is Nothing Then Return

            Dim vm = TryCast(DataContext, AppointmentsViewModel)
            vm?.ViewAppointmentCommand?.Execute(row.SourceAppointment)
        End Sub
    End Class
End Namespace
