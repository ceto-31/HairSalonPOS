Imports System.IO
Imports System.Windows.Media
Imports System.Windows.Media.Imaging
Imports Microsoft.Win32

Namespace Services
    Public Class CatalogImageService
        Public Const ProductsKind As String = "products"
        Public Const StaffKind As String = "staff"

        Private Const MaxBytes As Long = 2L * 1024L * 1024L
        Private Const MaxEdge As Integer = 512
        Private Shared ReadOnly AllowedExtensions As String() = {".jpg", ".jpeg", ".png"}

        Private Shared ReadOnly _instance As New Lazy(Of CatalogImageService)(Function() New CatalogImageService())
        Private ReadOnly _root As String

        Public Shared ReadOnly Property Instance As CatalogImageService
            Get
                Return _instance.Value
            End Get
        End Property

        Private Sub New()
            _root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CindyHairSalonPOS", "images")
            Directory.CreateDirectory(Path.Combine(_root, ProductsKind))
            Directory.CreateDirectory(Path.Combine(_root, StaffKind))
        End Sub

        Public Function PickImageFile() As String
            Dim dialog As New OpenFileDialog With {
                .Title = "Choose photo",
                .Filter = "Image files|*.jpg;*.jpeg;*.png|JPEG|*.jpg;*.jpeg|PNG|*.png",
                .CheckFileExists = True
            }
            If dialog.ShowDialog() <> True Then Return Nothing

            Dim errorMessage = Validate(dialog.FileName)
            If errorMessage IsNot Nothing Then
                AppDialogService.ShowError(errorMessage, "Photo")
                Return Nothing
            End If

            Return dialog.FileName
        End Function

        Public Function SaveImage(sourcePath As String, kind As String, id As String) As String
            If String.IsNullOrWhiteSpace(sourcePath) OrElse Not File.Exists(sourcePath) Then Return Nothing

            Dim folder = If(String.Equals(kind, StaffKind, StringComparison.OrdinalIgnoreCase), StaffKind, ProductsKind)
            Directory.CreateDirectory(Path.Combine(_root, folder))
            Dim fileName = $"{SanitizeFileStem(id)}_{DateTime.UtcNow.Ticks}.jpg"
            Dim destAbs = Path.Combine(_root, folder, fileName)

            Dim source = LoadBitmap(sourcePath)
            If source Is Nothing Then Return Nothing

            Dim longest = Math.Max(source.PixelWidth, source.PixelHeight)
            Dim frame As BitmapSource = source
            If longest > MaxEdge AndAlso longest > 0 Then
                Dim scale = MaxEdge / CDbl(longest)
                frame = New TransformedBitmap(source, New ScaleTransform(scale, scale))
            End If

            Dim encoder As New JpegBitmapEncoder With {.QualityLevel = 85}
            encoder.Frames.Add(BitmapFrame.Create(frame))
            Using fs = File.Create(destAbs)
                encoder.Save(fs)
            End Using

            Return $"{folder}/{fileName}"
        End Function

        Public Sub DeleteImage(relativePath As String)
            Dim abs = ResolveAbsolutePath(relativePath)
            If String.IsNullOrWhiteSpace(abs) Then Return
            Try
                If File.Exists(abs) Then File.Delete(abs)
            Catch
            End Try
        End Sub

        Public Function ResolveAbsolutePath(pathOrRelative As String) As String
            If String.IsNullOrWhiteSpace(pathOrRelative) Then Return Nothing
            If Path.IsPathRooted(pathOrRelative) Then
                Return If(File.Exists(pathOrRelative), pathOrRelative, Nothing)
            End If

            Dim combined = Path.Combine(_root, pathOrRelative.Replace("/"c, Path.DirectorySeparatorChar))
            Return If(File.Exists(combined), combined, Nothing)
        End Function

        Public Function CreateImageSource(pathOrRelative As String) As ImageSource
            Dim abs = ResolveAbsolutePath(pathOrRelative)
            If String.IsNullOrWhiteSpace(abs) Then Return Nothing
            Return LoadBitmap(abs)
        End Function

        Private Function Validate(filePath As String) As String
            If String.IsNullOrWhiteSpace(filePath) OrElse Not File.Exists(filePath) Then
                Return "The selected file could not be found."
            End If

            Dim ext = Path.GetExtension(filePath)
            If Not AllowedExtensions.Any(Function(e) e.Equals(ext, StringComparison.OrdinalIgnoreCase)) Then
                Return "Use a JPG or PNG photo."
            End If

            Dim info As New FileInfo(filePath)
            If info.Length > MaxBytes Then
                Return "Photo must be 2 MB or smaller."
            End If

            If LoadBitmap(filePath) Is Nothing Then
                Return "That file could not be opened as an image."
            End If

            Return Nothing
        End Function

        Private Shared Function LoadBitmap(filePath As String) As BitmapImage
            Try
                Dim bmp As New BitmapImage()
                bmp.BeginInit()
                bmp.CacheOption = BitmapCacheOption.OnLoad
                bmp.UriSource = New Uri(filePath)
                bmp.EndInit()
                bmp.Freeze()
                If bmp.PixelWidth <= 0 OrElse bmp.PixelHeight <= 0 Then Return Nothing
                Return bmp
            Catch
                Return Nothing
            End Try
        End Function

        Private Shared Function SanitizeFileStem(id As String) As String
            Dim stem = If(String.IsNullOrWhiteSpace(id), "item", id.Trim())
            For Each c In Path.GetInvalidFileNameChars()
                stem = stem.Replace(c, "_"c)
            Next
            Return stem
        End Function
    End Class
End Namespace
