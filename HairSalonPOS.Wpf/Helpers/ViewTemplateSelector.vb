Imports System.Windows
Imports System.Windows.Controls
Imports HairSalonPOS.Wpf.ViewModels

Namespace Helpers
    Public Class ViewTemplateSelector
        Inherits DataTemplateSelector

        Public Property LoginTemplate As DataTemplate
        Public Property CashierTemplate As DataTemplate
        Public Property InventoryTemplate As DataTemplate
        Public Property ReportsTemplate As DataTemplate
        Public Property CustomersTemplate As DataTemplate
        Public Property StaffTemplate As DataTemplate
        Public Property DiscountsTemplate As DataTemplate
        Public Property AppointmentsTemplate As DataTemplate
        Public Property SettingsTemplate As DataTemplate

        Public Overrides Function SelectTemplate(item As Object, container As DependencyObject) As DataTemplate
            Select Case True
                Case TypeOf item Is LoginViewModel
                    Return LoginTemplate
                Case TypeOf item Is CashierViewModel
                    Return CashierTemplate
                Case TypeOf item Is InventoryViewModel
                    Return InventoryTemplate
                Case TypeOf item Is ReportsViewModel
                    Return ReportsTemplate
                Case TypeOf item Is CustomersViewModel
                    Return CustomersTemplate
                Case TypeOf item Is StaffViewModel
                    Return StaffTemplate
                Case TypeOf item Is DiscountsViewModel
                    Return DiscountsTemplate
                Case TypeOf item Is AppointmentsViewModel
                    Return AppointmentsTemplate
                Case TypeOf item Is SettingsViewModel
                    Return SettingsTemplate
                Case Else
                    Return MyBase.SelectTemplate(item, container)
            End Select
        End Function
    End Class
End Namespace
