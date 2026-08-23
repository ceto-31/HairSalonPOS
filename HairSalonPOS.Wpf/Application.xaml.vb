Imports System.Windows
Imports System.Windows.Threading
Imports HairSalonPOS.Wpf.Services

Class Application
    Protected Overrides Sub OnStartup(e As StartupEventArgs)
        MyBase.OnStartup(e)
        AddHandler DispatcherUnhandledException, AddressOf OnDispatcherUnhandledException
        AddHandler AppDomain.CurrentDomain.UnhandledException, AddressOf OnDomainUnhandledException
        ThemeService.Apply(AppSettingsService.Instance.Settings.IsDarkMode)
    End Sub

    Private Sub OnDispatcherUnhandledException(sender As Object, e As DispatcherUnhandledExceptionEventArgs)
        ErrorLogService.LogException("Unhandled dispatcher exception", e.Exception)
    End Sub

    Private Sub OnDomainUnhandledException(sender As Object, e As UnhandledExceptionEventArgs)
        ErrorLogService.LogException("Unhandled domain exception", TryCast(e.ExceptionObject, Exception))
    End Sub
End Class
