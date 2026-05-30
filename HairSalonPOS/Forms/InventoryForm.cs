using HairSalonPOS.Helpers;
using HairSalonPOS.Models;
using HairSalonPOS.Services;

namespace HairSalonPOS.Forms;

public class InventoryForm : Form
{
    private readonly InventoryService _service = new();
    private readonly DataGridView _grid = new() { Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect };
    private readonly bool _showLowStockOnly;

    public InventoryForm(bool showLowStockOnly = false)
    {
        _showLowStockOnly = showLowStockOnly;
        Text = showLowStockOnly ? "Low Stock Alert" : "Inventory Management";

        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ProductName", HeaderText = "Product", Width = 200 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "QuantityOnHand", HeaderText = "Qty On Hand", Width = 90 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ReorderLevel", HeaderText = "Reorder Level", Width = 100 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Status", HeaderText = "Status", Width = 70 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "LastUpdated", HeaderText = "Last Updated", DefaultCellStyle = new DataGridViewCellStyle { Format = "g" }, Width = 130 });
        _grid.CellFormatting += (_, e) =>
        {
            if (e.ColumnIndex < 0 || e.RowIndex < 0) return;
            if (_grid.Columns[e.ColumnIndex].DataPropertyName == "Status" && e.Value?.ToString() == "LOW")
            {
                e.CellStyle.ForeColor = Color.White;
                e.CellStyle.BackColor = Color.IndianRed;
            }
        };

        var toolbar = new Panel { Dock = DockStyle.Top, Height = 45 };
        var btnRestock = new Button { Text = "Restock", Location = new Point(10, 8), Width = 80 };
        btnRestock.Click += (_, _) => Restock();
        var btnAdjust = new Button { Text = "Adjust", Location = new Point(95, 8), Width = 80 };
        btnAdjust.Click += (_, _) => Adjust();
        var btnRefresh = new Button { Text = "Refresh", Location = new Point(180, 8), Width = 80 };
        btnRefresh.Click += (_, _) => LoadData();
        var btnShowAll = new Button { Text = "Show All", Location = new Point(265, 8), Width = 80, Visible = showLowStockOnly };
        btnShowAll.Click += (_, _) =>
        {
            var f = new InventoryForm(false);
            f.ShowDialog();
            LoadData();
        };
        toolbar.Controls.AddRange(new Control[] { btnRestock, btnAdjust, btnRefresh, btnShowAll });

        Controls.Add(_grid);
        Controls.Add(toolbar);
        Load += (_, _) => LoadData();
    }

    private void LoadData()
    {
        _grid.DataSource = _service.GetInventory(_showLowStockOnly ? "low" : null).ToList();
    }

    private InventoryRow? Selected => _grid.CurrentRow?.DataBoundItem as InventoryRow;

    private void Restock()
    {
        if (Selected == null) return;
        var qty = PromptInt("Restock Quantity", "Enter quantity to add:", 1);
        if (qty == null) return;
        try
        {
            _service.Restock(Selected.ProductId, qty.Value, SessionContext.CurrentUser!.UserId, "Manual restock");
            LoadData();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void Adjust()
    {
        if (Selected == null) return;
        var qty = PromptInt("Adjust Quantity", $"Set new quantity for {Selected.ProductName}:", Selected.QuantityOnHand);
        if (qty == null) return;
        try
        {
            _service.Adjust(Selected.ProductId, qty.Value, SessionContext.CurrentUser!.UserId, "Manual adjustment");
            LoadData();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private static int? PromptInt(string title, string prompt, int defaultValue)
    {
        using var form = new Form { Text = title, Width = 320, Height = 150, FormBorderStyle = FormBorderStyle.FixedDialog, StartPosition = FormStartPosition.CenterParent };
        var num = new NumericUpDown { Value = defaultValue, Minimum = 0, Maximum = 99999, Location = new Point(20, 40), Width = 260 };
        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(120, 75) };
        form.Controls.AddRange(new Control[] { new Label { Text = prompt, Location = new Point(20, 15), AutoSize = true }, num, ok });
        form.AcceptButton = ok;
        return form.ShowDialog() == DialogResult.OK ? (int)num.Value : null;
    }
}
