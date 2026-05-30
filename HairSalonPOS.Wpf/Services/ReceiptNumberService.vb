Imports System.Configuration
Imports System.IO
Imports System.Text.Json
Imports HairSalonPOS.Wpf.Models
Imports Microsoft.Data.SqlClient

Namespace Services
    Public Class ReceiptNumberService
        Private Shared ReadOnly _instance As New Lazy(Of ReceiptNumberService)(Function() New ReceiptNumberService())
        Private ReadOnly _ledgerPath As String
        Private ReadOnly _lock As New Object()

        Public Shared ReadOnly Property Instance As ReceiptNumberService
            Get
                Return _instance.Value
            End Get
        End Property

        Private Sub New()
            Dim folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CindyHairSalonPOS")
            Directory.CreateDirectory(folder)
            _ledgerPath = Path.Combine(folder, "receipts.json")
        End Sub

        Public Function IssueNextOrNumber(receipt As ReceiptModel) As String
            SyncLock _lock
                Dim sequence = GetNextSequence()
                Dim orNumber = $"OR-{sequence:D5}"
                receipt.ReceiptNumber = orNumber
                PersistReceipt(orNumber, sequence, receipt)
                Return orNumber
            End SyncLock
        End Function

        Private Function GetNextSequence() As Integer
            Dim ledger = LoadLedger()
            Dim fromFile = ledger.LastOrSequence + 1
            Dim fromDb = TryGetNextSequenceFromDatabase()
            If fromDb.HasValue Then Return Math.Max(fromDb.Value, fromFile)
            Return fromFile
        End Function

        Private Sub PersistReceipt(orNumber As String, sequence As Integer, receipt As ReceiptModel)
            If TrySaveToDatabase(orNumber, sequence, receipt) Then Return

            Dim ledger = LoadLedger()
            ledger.LastOrSequence = Math.Max(ledger.LastOrSequence, sequence)
            ledger.Receipts.Add(New IssuedReceiptRecord With {
                .OrNumber = orNumber,
                .OrSequence = sequence,
                .SaleId = receipt.SaleId,
                .IssuedAt = receipt.SaleDate,
                .CashierName = receipt.CashierName,
                .Total = receipt.Total
            })
            SaveLedger(ledger)
        End Sub

        Private Function TryGetNextSequenceFromDatabase() As Integer?
            Dim connectionString = ConfigurationManager.ConnectionStrings("HairSalonDb")?.ConnectionString
            If String.IsNullOrWhiteSpace(connectionString) Then Return Nothing
            Try
                Using conn As New SqlConnection(connectionString)
                    conn.Open()
                    Using cmd As New SqlCommand("SELECT ISNULL(MAX(CAST(SUBSTRING(OrNumber, 4, 20) AS INT)), 0) + 1 FROM Receipts WHERE OrNumber LIKE 'OR-%'", conn)
                        Return CInt(cmd.ExecuteScalar())
                    End Using
                End Using
            Catch
                Return Nothing
            End Try
        End Function

        Private Function TrySaveToDatabase(orNumber As String, sequence As Integer, receipt As ReceiptModel) As Boolean
            Dim connectionString = ConfigurationManager.ConnectionStrings("HairSalonDb")?.ConnectionString
            If String.IsNullOrWhiteSpace(connectionString) Then Return False
            Try
                Using conn As New SqlConnection(connectionString)
                    conn.Open()
                    Using cmd As New SqlCommand(
                        "INSERT INTO Receipts (OrNumber, SaleId, IssuedAt, CashierName, CustomerName, StylistName, SubTotal, Discount, Tax, Total, PaymentMethod, ReceiptJson)
                         VALUES (@OrNumber, @SaleId, @IssuedAt, @CashierName, @CustomerName, @StylistName, @SubTotal, @Discount, @Tax, @Total, @PaymentMethod, @ReceiptJson)", conn)
                        cmd.Parameters.AddWithValue("@OrNumber", orNumber)
                        cmd.Parameters.AddWithValue("@SaleId", receipt.SaleId)
                        cmd.Parameters.AddWithValue("@IssuedAt", receipt.SaleDate)
                        cmd.Parameters.AddWithValue("@CashierName", receipt.CashierName)
                        cmd.Parameters.AddWithValue("@CustomerName", If(String.IsNullOrWhiteSpace(receipt.CustomerName), DBNull.Value, receipt.CustomerName))
                        cmd.Parameters.AddWithValue("@StylistName", If(String.IsNullOrWhiteSpace(receipt.StylistName), DBNull.Value, receipt.StylistName))
                        cmd.Parameters.AddWithValue("@SubTotal", receipt.SubTotal)
                        cmd.Parameters.AddWithValue("@Discount", receipt.DiscountAmount)
                        cmd.Parameters.AddWithValue("@Tax", receipt.Tax)
                        cmd.Parameters.AddWithValue("@Total", receipt.Total)
                        cmd.Parameters.AddWithValue("@PaymentMethod", receipt.PaymentMethod)
                        cmd.Parameters.AddWithValue("@ReceiptJson", JsonSerializer.Serialize(receipt))
                        cmd.ExecuteNonQuery()
                    End Using
                End Using
                Return True
            Catch
                Return False
            End Try
        End Function

        Private Class ReceiptLedger
            Public Property LastOrSequence As Integer
            Public Property Receipts As New List(Of IssuedReceiptRecord)
        End Class

        Private Function LoadLedger() As ReceiptLedger
            If Not File.Exists(_ledgerPath) Then Return New ReceiptLedger()
            Try
                Return If(JsonSerializer.Deserialize(Of ReceiptLedger)(File.ReadAllText(_ledgerPath)), New ReceiptLedger())
            Catch
                Return New ReceiptLedger()
            End Try
        End Function

        Private Sub SaveLedger(ledger As ReceiptLedger)
            File.WriteAllText(_ledgerPath, JsonSerializer.Serialize(ledger, New JsonSerializerOptions With {.WriteIndented = True}))
        End Sub
    End Class
End Namespace
