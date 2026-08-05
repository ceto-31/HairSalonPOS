Imports System.Configuration
Imports System.IO
Imports System.Text.Json
Imports HairSalonPOS.Wpf.Helpers
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

        Public Function GetPersistedSales(fromDate As Date, toDateExclusive As Date) As List(Of SaleRecord)
            SyncLock _lock
                Dim fromDb = TryLoadSalesFromDatabase(fromDate, toDateExclusive)
                If fromDb IsNot Nothing AndAlso fromDb.Count > 0 Then Return fromDb

                Return LoadSalesFromLedger(fromDate, toDateExclusive)
            End SyncLock
        End Function

        Public Function GetReceiptByOrNumber(orNumber As String) As ReceiptModel
            If String.IsNullOrWhiteSpace(orNumber) Then Return Nothing

            SyncLock _lock
                Dim fromDb = TryLoadReceiptFromDatabase(orNumber)
                If fromDb IsNot Nothing Then Return fromDb

                Return LoadReceiptFromLedger(orNumber)
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
                .CustomerName = receipt.CustomerName,
                .StylistName = receipt.StylistName,
                .PaymentMethod = receipt.PaymentMethod,
                .SubTotal = receipt.SubTotal,
                .Discount = receipt.DiscountAmount,
                .Tax = receipt.Tax,
                .Total = receipt.Total,
                .ReceiptJson = JsonSerializer.Serialize(receipt)
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

        Private Function TryLoadSalesFromDatabase(fromDate As Date, toDateExclusive As Date) As List(Of SaleRecord)
            Dim connectionString = ConfigurationManager.ConnectionStrings("HairSalonDb")?.ConnectionString
            If String.IsNullOrWhiteSpace(connectionString) Then Return Nothing
            Try
                Dim results As New List(Of SaleRecord)
                Using conn As New SqlConnection(connectionString)
                    conn.Open()
                    Using cmd As New SqlCommand(
                        "SELECT OrNumber, SaleId, IssuedAt, CashierName, CustomerName, StylistName, SubTotal, Discount, Tax, Total, PaymentMethod, ReceiptJson
                         FROM Receipts
                         WHERE IssuedAt >= @FromDate AND IssuedAt < @ToDate
                         ORDER BY IssuedAt DESC", conn)
                        cmd.Parameters.AddWithValue("@FromDate", fromDate)
                        cmd.Parameters.AddWithValue("@ToDate", toDateExclusive)
                        Using reader = cmd.ExecuteReader()
                            While reader.Read()
                                results.Add(MapReaderToSaleRecord(reader))
                            End While
                        End Using
                    End Using
                End Using
                Return results
            Catch
                Return Nothing
            End Try
        End Function

        Private Function TryLoadReceiptFromDatabase(orNumber As String) As ReceiptModel
            Dim connectionString = ConfigurationManager.ConnectionStrings("HairSalonDb")?.ConnectionString
            If String.IsNullOrWhiteSpace(connectionString) Then Return Nothing
            Try
                Using conn As New SqlConnection(connectionString)
                    conn.Open()
                    Using cmd As New SqlCommand(
                        "SELECT ReceiptJson FROM Receipts WHERE OrNumber = @OrNumber", conn)
                        cmd.Parameters.AddWithValue("@OrNumber", orNumber)
                        Dim json = TryCast(cmd.ExecuteScalar(), String)
                        Return DeserializeReceiptJson(json)
                    End Using
                End Using
            Catch
                Return Nothing
            End Try
        End Function

        Private Function LoadSalesFromLedger(fromDate As Date, toDateExclusive As Date) As List(Of SaleRecord)
            Dim ledger = LoadLedger()
            Return ledger.Receipts.
                Where(Function(r) r.IssuedAt >= fromDate AndAlso r.IssuedAt < toDateExclusive).
                Select(AddressOf MapLedgerRecordToSaleRecord).
                OrderByDescending(Function(s) s.SaleDate).
                ToList()
        End Function

        Private Function LoadReceiptFromLedger(orNumber As String) As ReceiptModel
            Dim ledger = LoadLedger()
            Dim record = ledger.Receipts.FirstOrDefault(Function(r) r.OrNumber.Equals(orNumber, StringComparison.OrdinalIgnoreCase))
            If record Is Nothing Then Return Nothing
            Return DeserializeReceiptJson(record.ReceiptJson)
        End Function

        Private Function MapReaderToSaleRecord(reader As SqlDataReader) As SaleRecord
            Dim json = If(reader.IsDBNull(11), Nothing, reader.GetString(11))
            Dim receipt = DeserializeReceiptJson(json)
            If receipt IsNot Nothing Then Return ReceiptModelMapper.ToSaleRecord(receipt)

            Return New SaleRecord With {
                .SaleId = reader.GetInt32(1),
                .ReceiptNumber = reader.GetString(0),
                .SaleDate = reader.GetDateTime(2),
                .CashierName = If(reader.IsDBNull(3), String.Empty, reader.GetString(3)),
                .CustomerName = If(reader.IsDBNull(4), String.Empty, reader.GetString(4)),
                .StylistName = If(reader.IsDBNull(5), String.Empty, reader.GetString(5)),
                .SubTotal = If(reader.IsDBNull(6), 0D, reader.GetDecimal(6)),
                .DiscountAmount = If(reader.IsDBNull(7), 0D, reader.GetDecimal(7)),
                .Tax = If(reader.IsDBNull(8), 0D, reader.GetDecimal(8)),
                .Total = If(reader.IsDBNull(9), 0D, reader.GetDecimal(9)),
                .PaymentMethod = If(reader.IsDBNull(10), String.Empty, reader.GetString(10)),
                .Lines = New List(Of SaleLineRecord)()
            }
        End Function

        Private Function MapLedgerRecordToSaleRecord(record As IssuedReceiptRecord) As SaleRecord
            Dim receipt = DeserializeReceiptJson(record.ReceiptJson)
            If receipt IsNot Nothing Then Return ReceiptModelMapper.ToSaleRecord(receipt)

            Return New SaleRecord With {
                .SaleId = record.SaleId,
                .ReceiptNumber = record.OrNumber,
                .SaleDate = record.IssuedAt,
                .CashierName = record.CashierName,
                .CustomerName = record.CustomerName,
                .StylistName = record.StylistName,
                .PaymentMethod = record.PaymentMethod,
                .SubTotal = record.SubTotal,
                .DiscountAmount = record.Discount,
                .Tax = record.Tax,
                .Total = record.Total,
                .Lines = New List(Of SaleLineRecord)()
            }
        End Function

        Private Shared Function DeserializeReceiptJson(json As String) As ReceiptModel
            If String.IsNullOrWhiteSpace(json) Then Return Nothing
            Try
                Return JsonSerializer.Deserialize(Of ReceiptModel)(json)
            Catch
                Return Nothing
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
