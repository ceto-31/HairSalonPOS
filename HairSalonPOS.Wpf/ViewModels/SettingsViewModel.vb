Imports CommunityToolkit.Mvvm.Input
Imports HairSalonPOS.Wpf.Services

Namespace ViewModels
    Public Class SettingsViewModel
        Inherits ViewModelBase

        Private ReadOnly _settingsService As AppSettingsService = AppSettingsService.Instance
        Private _printerType As String
        Private _thermalPrinterName As String
        Private _salonName As String
        Private _salonAddress As String
        Private _salonTelephone As String
        Private _salonTin As String
        Private _statusMessage As String = String.Empty

        Public Sub New()
            PrinterTypes = New List(Of String) From {"Standard", "Thermal"}
            LoadFromSettings()
            SaveCommand = New RelayCommand(AddressOf SaveSettings)
        End Sub

        Public Property PrinterTypes As List(Of String)

        Public Property PrinterType As String
            Get
                Return _printerType
            End Get
            Set(value As String)
                SetProperty(_printerType, value)
                OnPropertyChanged(NameOf(IsThermalSelected))
            End Set
        End Property

        Public ReadOnly Property IsThermalSelected As Boolean
            Get
                Return String.Equals(PrinterType, "Thermal", StringComparison.OrdinalIgnoreCase)
            End Get
        End Property

        Public Property ThermalPrinterName As String
            Get
                Return _thermalPrinterName
            End Get
            Set(value As String)
                SetProperty(_thermalPrinterName, value)
            End Set
        End Property

        Public Property SalonName As String
            Get
                Return _salonName
            End Get
            Set(value As String)
                SetProperty(_salonName, value)
            End Set
        End Property

        Public Property SalonAddress As String
            Get
                Return _salonAddress
            End Get
            Set(value As String)
                SetProperty(_salonAddress, value)
            End Set
        End Property

        Public Property SalonTelephone As String
            Get
                Return _salonTelephone
            End Get
            Set(value As String)
                SetProperty(_salonTelephone, value)
            End Set
        End Property

        Public Property SalonTin As String
            Get
                Return _salonTin
            End Get
            Set(value As String)
                SetProperty(_salonTin, value)
            End Set
        End Property

        Public Property StatusMessage As String
            Get
                Return _statusMessage
            End Get
            Set(value As String)
                SetProperty(_statusMessage, value)
            End Set
        End Property

        Public Property SaveCommand As RelayCommand

        Public Sub LoadFromSettings()
            Dim s = _settingsService.Settings
            PrinterType = s.PrinterType
            ThermalPrinterName = s.ThermalPrinterName
            SalonName = s.SalonName
            SalonAddress = s.SalonAddress
            SalonTelephone = s.SalonTelephone
            SalonTin = s.SalonTin
        End Sub

        Private Sub SaveSettings()
            _settingsService.Save(New Models.AppSettings With {
                .PrinterType = PrinterType,
                .ThermalPrinterName = ThermalPrinterName,
                .SalonName = SalonName,
                .SalonAddress = SalonAddress,
                .SalonTelephone = SalonTelephone,
                .SalonTin = SalonTin
            })
            StatusMessage = "Settings saved. Standard auto-detects 58mm receipt rolls. Thermal uses ESC/POS raw printing."
        End Sub
    End Class
End Namespace
