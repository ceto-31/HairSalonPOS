Imports System.Collections.ObjectModel
Imports System.Windows
Imports System.Windows.Controls
Imports HairSalonPOS.Wpf.Models

Namespace Views
    Partial Public Class MultiSelectStaffDropdown
        Inherits UserControl

        Public Shared ReadOnly StylistOptionsProperty As DependencyProperty = DependencyProperty.Register(
            NameOf(StylistOptions),
            GetType(ObservableCollection(Of StaffSelectionOption)),
            GetType(MultiSelectStaffDropdown),
            New PropertyMetadata(Nothing))

        Public Shared ReadOnly SelectedStylistsLabelProperty As DependencyProperty = DependencyProperty.Register(
            NameOf(SelectedStylistsLabel),
            GetType(String),
            GetType(MultiSelectStaffDropdown),
            New PropertyMetadata("Select staff…"))

        Public Sub New()
            InitializeComponent()
        End Sub

        Public Property StylistOptions As ObservableCollection(Of StaffSelectionOption)
            Get
                Return CType(GetValue(StylistOptionsProperty), ObservableCollection(Of StaffSelectionOption))
            End Get
            Set(value As ObservableCollection(Of StaffSelectionOption))
                SetValue(StylistOptionsProperty, value)
            End Set
        End Property

        Public Property SelectedStylistsLabel As String
            Get
                Return CStr(GetValue(SelectedStylistsLabelProperty))
            End Get
            Set(value As String)
                SetValue(SelectedStylistsLabelProperty, value)
            End Set
        End Property
    End Class
End Namespace
