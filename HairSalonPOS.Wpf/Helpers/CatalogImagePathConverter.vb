Imports System.Globalization
Imports System.Windows.Data
Imports HairSalonPOS.Wpf.Services

Namespace Helpers
    Public Class CatalogImagePathConverter
        Implements IValueConverter

        Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
            Return CatalogImageService.Instance.CreateImageSource(TryCast(value, String))
        End Function

        Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
            Throw New NotSupportedException()
        End Function
    End Class
End Namespace
