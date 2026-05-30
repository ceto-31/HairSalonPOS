Class MainWindow
    Public Sub New()
        InitializeComponent()
        DataContext = New ViewModels.MainViewModel()
    End Sub
End Class
