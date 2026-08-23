Imports System.IO
Imports System.Text

Namespace Services
    ''' <summary>
    ''' Appends full exception detail (including stack trace and line numbers when PDBs are
    ''' present) to a rolling log so failures swallowed by catch blocks stay diagnosable.
    ''' </summary>
    Public Module ErrorLogService
        Private ReadOnly SyncRoot As New Object()
        Private Const MaxBytes As Long = 512L * 1024L

        Private ReadOnly Property LogPath As String
            Get
                Dim folder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "CindyHairSalonPOS", "logs")
                Directory.CreateDirectory(folder)
                Return Path.Combine(folder, "error.log")
            End Get
        End Property

        Public Sub LogException(context As String, ex As Exception)
            If ex Is Nothing Then Return

            Dim entry As New StringBuilder()
            entry.AppendLine("========================================")
            entry.AppendLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  {context}")
            entry.AppendLine(ex.ToString())

            Dim inner = ex.InnerException
            While inner IsNot Nothing
                entry.AppendLine("--- inner exception ---")
                entry.AppendLine(inner.ToString())
                inner = inner.InnerException
            End While

            Append(entry.ToString())
        End Sub

        ''' <summary>Short, user-facing detail line. Full stack traces stay in the log file.</summary>
        Public Function Describe(ex As Exception) As String
            If ex Is Nothing Then Return String.Empty

            Dim detail = $"{ex.GetType().Name}: {ex.Message}"
            Dim path = TryGetLogPath()
            If String.IsNullOrEmpty(path) Then Return detail
            Return $"{detail}{Environment.NewLine}{Environment.NewLine}Technical details were written to:{Environment.NewLine}{path}"
        End Function

        Public Function TryGetLogPath() As String
            Try
                Return LogPath
            Catch
                Return String.Empty
            End Try
        End Function

        Private Sub Append(text As String)
            Try
                SyncLock SyncRoot
                    Dim target = LogPath
                    Dim info As New FileInfo(target)
                    If info.Exists AndAlso info.Length > MaxBytes Then
                        File.Delete(target)
                    End If
                    File.AppendAllText(target, text)
                End SyncLock
            Catch
                ' Logging must never take the app down.
            End Try
        End Sub
    End Module
End Namespace
