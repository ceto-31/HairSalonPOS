Imports System.Configuration
Imports System.IO
Imports System.Text.Json
Imports HairSalonPOS.Wpf.Models

Namespace Services
    Public Class AppSettingsService
        Private Shared ReadOnly _instance As New Lazy(Of AppSettingsService)(Function() New AppSettingsService())
        Private ReadOnly _settingsPath As String
        Private _settings As AppSettings

        Public Shared ReadOnly Property Instance As AppSettingsService
            Get
                Return _instance.Value
            End Get
        End Property

        Public ReadOnly Property Settings As AppSettings
            Get
                Return _settings
            End Get
        End Property

        Private Sub New()
            Dim folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CindyHairSalonPOS")
            Directory.CreateDirectory(folder)
            _settingsPath = Path.Combine(folder, "settings.json")
            _settings = LoadSettings()
        End Sub

        Private Function LoadSettings() As AppSettings
            Dim defaults As New AppSettings With {
                .SalonName = ConfigurationManager.AppSettings("SalonName"),
                .SalonAddress = ConfigurationManager.AppSettings("SalonAddress"),
                .SalonTelephone = ConfigurationManager.AppSettings("SalonTelephone"),
                .SalonTin = ConfigurationManager.AppSettings("SalonTin")
            }
            If Not File.Exists(_settingsPath) Then Return defaults
            Try
                Dim loaded = JsonSerializer.Deserialize(Of AppSettings)(File.ReadAllText(_settingsPath))
                If loaded Is Nothing Then Return defaults
                If String.IsNullOrWhiteSpace(loaded.SalonName) Then loaded.SalonName = defaults.SalonName
                If String.IsNullOrWhiteSpace(loaded.SalonAddress) Then loaded.SalonAddress = defaults.SalonAddress
                If String.IsNullOrWhiteSpace(loaded.SalonTelephone) Then loaded.SalonTelephone = defaults.SalonTelephone
                If String.IsNullOrWhiteSpace(loaded.SalonTin) Then loaded.SalonTin = defaults.SalonTin
                Return loaded
            Catch
                Return defaults
            End Try
        End Function

        Public Sub Save(settings As AppSettings)
            _settings = settings
            File.WriteAllText(_settingsPath, JsonSerializer.Serialize(settings, New JsonSerializerOptions With {.WriteIndented = True}))
        End Sub
    End Class
End Namespace
