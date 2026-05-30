Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Text

Namespace Services
    Public Class RawPrinterHelper
        <StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Auto)>
        Private Structure DOCINFOA
            Public pDocName As String
            Public pOutputFile As String
            Public pDataType As String
        End Structure

        <DllImport("winspool.drv", CharSet:=CharSet.Auto, SetLastError:=True)>
        Private Shared Function OpenPrinter(pPrinterName As String, ByRef phPrinter As IntPtr, pDefault As IntPtr) As Boolean
        End Function

        <DllImport("winspool.drv", SetLastError:=True)>
        Private Shared Function ClosePrinter(hPrinter As IntPtr) As Boolean
        End Function

        <DllImport("winspool.drv", CharSet:=CharSet.Auto, SetLastError:=True)>
        Private Shared Function StartDocPrinter(hPrinter As IntPtr, level As Integer, ByRef di As DOCINFOA) As Boolean
        End Function

        <DllImport("winspool.drv", SetLastError:=True)>
        Private Shared Function EndDocPrinter(hPrinter As IntPtr) As Boolean
        End Function

        <DllImport("winspool.drv", SetLastError:=True)>
        Private Shared Function StartPagePrinter(hPrinter As IntPtr) As Boolean
        End Function

        <DllImport("winspool.drv", SetLastError:=True)>
        Private Shared Function EndPagePrinter(hPrinter As IntPtr) As Boolean
        End Function

        <DllImport("winspool.drv", SetLastError:=True)>
        Private Shared Function WritePrinter(hPrinter As IntPtr, pBytes As IntPtr, dwCount As Integer, ByRef dwWritten As Integer) As Boolean
        End Function

        Public Shared Sub SendStringToPrinter(printerName As String, data As String)
            SendBytesToPrinter(printerName, Encoding.GetEncoding(437).GetBytes(data))
        End Sub

        Public Shared Sub SendBytesToPrinter(printerName As String, bytes As Byte())
            If String.IsNullOrWhiteSpace(printerName) Then Throw New InvalidOperationException("No thermal printer selected.")
            Dim handle As IntPtr = IntPtr.Zero
            Dim di As New DOCINFOA With {.pDocName = "Cindy POS Receipt", .pDataType = "RAW"}
            Dim written As Integer
            Try
                If Not OpenPrinter(printerName, handle, IntPtr.Zero) Then
                    Throw New InvalidOperationException($"Unable to open printer '{printerName}'.")
                End If
                If Not StartDocPrinter(handle, 1, di) Then Throw New InvalidOperationException("StartDocPrinter failed.")
                If Not StartPagePrinter(handle) Then Throw New InvalidOperationException("StartPagePrinter failed.")
                Dim unmanagedBytes = Marshal.AllocCoTaskMem(bytes.Length)
                Try
                    Marshal.Copy(bytes, 0, unmanagedBytes, bytes.Length)
                    If Not WritePrinter(handle, unmanagedBytes, bytes.Length, written) Then
                        Throw New InvalidOperationException("WritePrinter failed.")
                    End If
                Finally
                    Marshal.FreeCoTaskMem(unmanagedBytes)
                End Try
                EndPagePrinter(handle)
                EndDocPrinter(handle)
            Finally
                If handle <> IntPtr.Zero Then ClosePrinter(handle)
            End Try
        End Sub

        Public Shared Function BuildEscPosReceipt(lines As IEnumerable(Of String)) As Byte()
            Using ms As New MemoryStream()
                ms.WriteByte(&H1B) : ms.WriteByte(&H40) ' Initialize
                For Each line In lines
                    Dim trimmed = If(line, String.Empty)
                    Dim isCenter = trimmed.StartsWith("[[C]]")
                    If isCenter Then trimmed = trimmed.Substring(5)
                    If isCenter Then
                        ms.WriteByte(&H1B) : ms.WriteByte(&H61) : ms.WriteByte(1) ' center
                    Else
                        ms.WriteByte(&H1B) : ms.WriteByte(&H61) : ms.WriteByte(0) ' left
                    End If
                    Dim payload = Encoding.GetEncoding(437).GetBytes(trimmed & vbLf)
                    ms.Write(payload, 0, payload.Length)
                Next
                ms.WriteByte(&H1B) : ms.WriteByte(&H61) : ms.WriteByte(1)
                ms.Write(Encoding.GetEncoding(437).GetBytes("CUSTOMER COPY" & vbLf), 0, 15)
                ms.WriteByte(&H1A) ' feed
                ms.WriteByte(&H1D) : ms.WriteByte(&H56) : ms.WriteByte(&H0) ' cut
                Return ms.ToArray()
            End Using
        End Function
    End Class
End Namespace
