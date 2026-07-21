Imports System.Windows
Imports System.Windows.Media

Namespace Services
    Public Class ThemeService
        Public Shared Sub Apply(isDarkMode As Boolean)
            If isDarkMode Then
                ApplyDark()
            Else
                ApplyLight()
            End If
        End Sub

        Private Shared Sub ApplyLight()
            SetBrush("AccentBrush", "#1D9E75")
            SetBrush("AccentDarkBrush", "#178564")
            SetBrush("HeaderBrush", "#1C1917")
            SetBrush("PageBackgroundBrush", "#F5F5F4")
            SetBrush("CardBackgroundBrush", "#FFFFFF")
            SetBrush("CardMutedBrush", "#F9F9F5")
            SetBrush("TextBrush", "#292524")
            SetBrush("MutedTextBrush", "#78716C")
            SetBrush("AlertBrush", "#FEF3C7")
            SetBrush("LowStockBrush", "#FEE2E2")
            SetBrush("ShellHeaderBrush", "#FFFFFF")
            SetBrush("ShellBorderBrush", "#E7E5E4")
            SetBrush("DrawerBackgroundBrush", "#FFFFFF")
            SetBrush("DrawerBorderBrush", "#D6D3D1")
            SetBrush("ShellIconBrush", "#1C1917")
            SetBrush("ShellHoverBrush", "#F5F5F4")
            SetBrush("HeaderStatBrush", "#44403C")
            SetBrush("DataGridAltRowBrush", "#FAFAF9")
            SetBrush("SecondaryButtonBrush", "#E7E5E4")
            SetBrush("SecondaryButtonHoverBrush", "#D6D3D1")
            SetBrush("SecondaryButtonForegroundBrush", "#44403C")
            SetBrush("DrawerNavForegroundBrush", "#1C1917")
            SetBrush("InputBackgroundBrush", "#FFFFFF")
            SetBrush("InputBorderBrush", "#D6D3D1")
            SetBrush("ChipBackgroundBrush", "#FFFFFF")
            SetBrush("ChipForegroundBrush", "#57534E")
            SetBrush("ChipBorderBrush", "#D6D3D1")
            SetBrush("ChipHoverBrush", "#F5F5F4")
            SetBrush("CatalogTileHoverBrush", "#ECFDF5")
        End Sub

        Private Shared Sub ApplyDark()
            ' Soft charcoal palette — avoid stark white-on-black contrast
            SetBrush("AccentBrush", "#2A9B7A")
            SetBrush("AccentDarkBrush", "#238B6C")
            SetBrush("HeaderBrush", "#E7E5E4")
            SetBrush("PageBackgroundBrush", "#2A2826")
            SetBrush("CardBackgroundBrush", "#353230")
            SetBrush("CardMutedBrush", "#3F3C39")
            SetBrush("TextBrush", "#E7E5E4")
            SetBrush("MutedTextBrush", "#A8A29E")
            SetBrush("AlertBrush", "#5C4A2A")
            SetBrush("LowStockBrush", "#5C3030")
            SetBrush("ShellHeaderBrush", "#322F2C")
            SetBrush("ShellBorderBrush", "#4A4642")
            SetBrush("DrawerBackgroundBrush", "#322F2C")
            SetBrush("DrawerBorderBrush", "#4A4642")
            SetBrush("ShellIconBrush", "#D6D3D1")
            SetBrush("ShellHoverBrush", "#3F3C39")
            SetBrush("HeaderStatBrush", "#A8A29E")
            SetBrush("DataGridAltRowBrush", "#2F2C2A")
            SetBrush("SecondaryButtonBrush", "#4A4642")
            SetBrush("SecondaryButtonHoverBrush", "#57534E")
            SetBrush("SecondaryButtonForegroundBrush", "#E7E5E4")
            SetBrush("DrawerNavForegroundBrush", "#E7E5E4")
            SetBrush("InputBackgroundBrush", "#3F3C39")
            SetBrush("InputBorderBrush", "#57534E")
            SetBrush("ChipBackgroundBrush", "#3F3C39")
            SetBrush("ChipForegroundBrush", "#D6D3D1")
            SetBrush("ChipBorderBrush", "#57534E")
            SetBrush("ChipHoverBrush", "#4A4642")
            SetBrush("CatalogTileHoverBrush", "#2F4A40")
        End Sub

        Private Shared Sub SetBrush(key As String, hex As String)
            Dim color = CType(ColorConverter.ConvertFromString(hex), Color)
            Dim existing = TryCast(Application.Current.Resources(key), SolidColorBrush)
            If existing IsNot Nothing AndAlso Not existing.IsFrozen Then
                existing.Color = color
                Return
            End If
            Application.Current.Resources(key) = New SolidColorBrush(color)
        End Sub
    End Class
End Namespace
