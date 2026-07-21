Imports System.Windows
Imports HairSalonPOS.Wpf.Services

Class Application
    Protected Overrides Sub OnStartup(e As StartupEventArgs)
        MyBase.OnStartup(e)
        ThemeService.Apply(AppSettingsService.Instance.Settings.IsDarkMode)
    End Sub
End Class
