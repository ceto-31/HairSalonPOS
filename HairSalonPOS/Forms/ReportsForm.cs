using System.Text;
using HairSalonPOS.Helpers;
using HairSalonPOS.Models;
using HairSalonPOS.Services;

namespace HairSalonPOS.Forms;

public class ReportsForm : Form
{
    private readonly ReportService _reportService = new();
    private readonly InventoryService _inventoryService = new();
    private readonly TabControl _tabs = new() { Dock = DockStyle.Fill };
    private readonly DateTimePicker _dtpDate = new() { Format = DateTimePickerFormat.Short, Width = 120 };
    private readonly NumericUpDown _numYear = new() { Minimum = 2020, Maximum = 2100, Value = DateTime.Now.Year, Width = 80 };
    private readonly NumericUpDown _numMonth = new() { Minimum = 1, Maximum = 12, Value = DateTime.Now.Month, Width = 50 };
    private readonly Label _lblSummary = new() { AutoSize = true, Font = new Font("Segoe UI", 11) };
    private readonly DataGridView _gridTop = new() { Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = false };
    private readonly DataGridView _gridInventory = new() { Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = false };
    private readonly DataGridView _gridMovements = new() { Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = false };

    public ReportsForm()
    {
        Text = "Reports";
        if (SessionContext.HasRole("Cashier") && !SessionContext.HasRole("Admin"))
            Text += " (Daily only)";

        BuildSalesTab();
        BuildInventoryTab();

        Controls.Add(_tabs);
    }

    private void BuildSalesTab()
    {
        var tab = new TabPage("Sales Reports");
        var top = new Panel { Dock = DockStyle.Top, Height = 100 };

        _dtpDate.Location = new Point(10, 15);
        _numYear.Location = new Point(10, 50);
        _numMonth.Location = new Point(100, 50);

        var btnDaily = new Button { Text = "Daily", Location = new Point(160, 12), Width = 70 };
        btnDaily.Click += (_, _) => RunSalesReport("daily");
        var btnWeekly = new Button { Text = "Weekly", Location = new Point(235, 12), Width = 70 };
        btnWeekly.Click += (_, _) => RunSalesReport("weekly");
        var btnMonthly = new Button { Text = "Monthly", Location = new Point(310, 12), Width = 70 };
        btnMonthly.Click += (_, _) => RunSalesReport("monthly");
        var btnAnnual = new Button { Text = "Annual", Location = new Point(385, 12), Width = 70 };
        btnAnnual.Click += (_, _) => RunSalesReport("annual");

        if (SessionContext.HasRole("Cashier") && !SessionContext.HasRole("Admin"))
        {
            btnWeekly.Enabled = btnMonthly.Enabled = btnAnnual.Enabled = false;
        }

        var btnExport = new Button { Text = "Export CSV", Location = new Point(470, 12), Width = 90 };
        btnExport.Click += (_, _) => ExportSummary();
        var btnPrint = new Button { Text = "Print", Location = new Point(565, 12), Width = 70 };
        btnPrint.Click += (_, _) => PrintSummary();

        _lblSummary.Location = new Point(160, 50);
        top.Controls.AddRange(new Control[]
        {
            new Label { Text = "Date:", Location = new Point(10, 18), AutoSize = true }, _dtpDate,
            new Label { Text = "Year/Month:", Location = new Point(10, 53), AutoSize = true }, _numYear, _numMonth,
            btnDaily, btnWeekly, btnMonthly, btnAnnual, btnExport, btnPrint, _lblSummary
        });

        _gridTop.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ProductName", HeaderText = "Product/Service", Width = 200 });
        _gridTop.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TotalQty", HeaderText = "Qty Sold", Width = 80 });
        _gridTop.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TotalRevenue", HeaderText = "Revenue", DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });

        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 120 };
        split.Panel1.Controls.Add(top);
        split.Panel2.Controls.Add(_gridTop);
        tab.Controls.Add(split);
        _tabs.TabPages.Add(tab);

        RunSalesReport("daily");
    }

    private void BuildInventoryTab()
    {
        if (SessionContext.HasRole("Cashier") && !SessionContext.HasRole("Admin"))
            return;

        var tab = new TabPage("Inventory Reports");
        var innerTabs = new TabControl { Dock = DockStyle.Fill };
        var stockTab = new TabPage("Stock Summary");
        var lowTab = new TabPage("Low Stock");
        var moveTab = new TabPage("Stock Movements");

        _gridInventory.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ProductName", HeaderText = "Product", Width = 200 });
        _gridInventory.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "QuantityOnHand", HeaderText = "Qty", Width = 60 });
        _gridInventory.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ReorderLevel", HeaderText = "Reorder", Width = 70 });
        _gridInventory.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Status", HeaderText = "Status", Width = 60 });

        _gridMovements.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CreatedAt", HeaderText = "Date", DefaultCellStyle = new DataGridViewCellStyle { Format = "g" }, Width = 130 });
        _gridMovements.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ProductName", HeaderText = "Product", Width = 150 });
        _gridMovements.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ChangeQty", HeaderText = "Change", Width = 60 });
        _gridMovements.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TransactionType", HeaderText = "Type", Width = 80 });
        _gridMovements.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "UserName", HeaderText = "User", Width = 100 });

        stockTab.Controls.Add(_gridInventory);
        lowTab.Controls.Add(new DataGridView
        {
            Dock = DockStyle.Fill,
            DataSource = _inventoryService.GetInventory("low").ToList(),
            ReadOnly = true,
            AutoGenerateColumns = true
        });
        _gridMovements.DataSource = _inventoryService.GetTransactionLog(DateTime.Today.AddDays(-30)).ToList();
        moveTab.Controls.Add(_gridMovements);

        innerTabs.TabPages.AddRange(new[] { stockTab, lowTab, moveTab });
        tab.Controls.Add(innerTabs);
        _tabs.TabPages.Add(tab);

        _gridInventory.DataSource = _inventoryService.GetInventory().ToList();
    }

    private SalesReportRow? _lastReport;
    private DateTime? _from;
    private DateTime? _to;

    private void RunSalesReport(string period)
    {
        try
        {
            _lastReport = period switch
            {
                "daily" => _reportService.GetDailyReport(_dtpDate.Value.Date),
                "weekly" => _reportService.GetWeeklyReport(_dtpDate.Value.Date),
                "monthly" => _reportService.GetMonthlyReport((int)_numYear.Value, (int)_numMonth.Value),
                "annual" => _reportService.GetAnnualReport((int)_numYear.Value),
                _ => null
            };

            switch (period)
            {
                case "daily":
                    _from = _dtpDate.Value.Date;
                    _to = _dtpDate.Value.Date;
                    break;
                case "weekly":
                    _from = StartOfWeek(_dtpDate.Value);
                    _to = StartOfWeek(_dtpDate.Value).AddDays(6);
                    break;
                case "monthly":
                    _from = new DateTime((int)_numYear.Value, (int)_numMonth.Value, 1);
                    _to = new DateTime((int)_numYear.Value, (int)_numMonth.Value,
                        DateTime.DaysInMonth((int)_numYear.Value, (int)_numMonth.Value));
                    break;
                case "annual":
                    _from = new DateTime((int)_numYear.Value, 1, 1);
                    _to = new DateTime((int)_numYear.Value, 12, 31);
                    break;
                default:
                    _from = null;
                    _to = null;
                    break;
            }

            if (_lastReport != null)
            {
                _lblSummary.Text = $"{_lastReport.PeriodLabel}  |  Transactions: {_lastReport.TransactionCount}  |  Total: ₱{_lastReport.TotalSales:N2}  |  Avg: ₱{_lastReport.AverageSale:N2}";
                _gridTop.DataSource = _reportService.GetTopProducts(_from, _to).ToList();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Report Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static DateTime StartOfWeek(DateTime dt)
    {
        var diff = (7 + (dt.DayOfWeek - DayOfWeek.Monday)) % 7;
        return dt.AddDays(-diff).Date;
    }

    private void ExportSummary()
    {
        if (_lastReport == null) return;
        using var sfd = new SaveFileDialog { Filter = "CSV|*.csv", FileName = "SalesReport.csv" };
        if (sfd.ShowDialog() != DialogResult.OK) return;

        var sb = new StringBuilder();
        sb.AppendLine("Period,Transactions,TotalSales,AverageSale");
        sb.AppendLine($"{_lastReport.PeriodLabel},{_lastReport.TransactionCount},{_lastReport.TotalSales},{_lastReport.AverageSale}");
        sb.AppendLine();
        sb.AppendLine("Product,QtySold,Revenue");
        foreach (TopProductRow row in _gridTop.DataSource as System.Collections.IList ?? Array.Empty<TopProductRow>())
            sb.AppendLine($"{row.ProductName},{row.TotalQty},{row.TotalRevenue}");
        File.WriteAllText(sfd.FileName, sb.ToString());
        MessageBox.Show("Report exported.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void PrintSummary()
    {
        if (_lastReport == null) return;
        var doc = new System.Drawing.Printing.PrintDocument();
        doc.PrintPage += (_, e) =>
        {
            var g = e.Graphics!;
            float y = 50;
            g.DrawString("Fix Republic POS Sales Report", new Font("Segoe UI", 14, FontStyle.Bold), Brushes.Black, 50, y); y += 30;
            g.DrawString(_lblSummary.Text, new Font("Segoe UI", 10), Brushes.Black, 50, y);
        };
        using var preview = new PrintPreviewDialog { Document = doc, Width = 700, Height = 500 };
        preview.ShowDialog();
    }
}
