using HairSalonPOS.Models;
using HairSalonPOS.Services;

namespace HairSalonPOS.Forms;

public class ProductForm : Form
{
    private readonly ProductService _service = new();
    private readonly DataGridView _grid = new() { Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect };
    private readonly TextBox _txtSearch = new() { Width = 200 };

    public ProductForm()
    {
        Text = "Product & Service Management";
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Name", HeaderText = "Name", Width = 180 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CategoryName", HeaderText = "Category", Width = 100 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Price", HeaderText = "Price", DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });
        _grid.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = "IsService", HeaderText = "Service" });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ReorderLevel", HeaderText = "Reorder" });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "QuantityOnHand", HeaderText = "Stock" });

        var toolbar = new Panel { Dock = DockStyle.Top, Height = 45 };
        _txtSearch.Location = new Point(10, 10);
        _txtSearch.PlaceholderText = "Search...";
        _txtSearch.TextChanged += (_, _) => LoadData();
        var btnAdd = new Button { Text = "Add", Location = new Point(220, 8), Width = 70 };
        btnAdd.Click += (_, _) => EditProduct(null);
        var btnEdit = new Button { Text = "Edit", Location = new Point(295, 8), Width = 70 };
        btnEdit.Click += (_, _) =>
        {
            if (_grid.CurrentRow?.DataBoundItem is Product p) EditProduct(p);
        };
        var btnDelete = new Button { Text = "Delete", Location = new Point(370, 8), Width = 70 };
        btnDelete.Click += (_, _) => DeleteProduct();
        var btnRefresh = new Button { Text = "Refresh", Location = new Point(445, 8), Width = 70 };
        btnRefresh.Click += (_, _) => LoadData();
        toolbar.Controls.AddRange(new Control[] { _txtSearch, btnAdd, btnEdit, btnDelete, btnRefresh });

        Controls.Add(_grid);
        Controls.Add(toolbar);
        Load += (_, _) => LoadData();
    }

    private void LoadData()
    {
        _grid.DataSource = _service.GetProducts(search: _txtSearch.Text.Trim()).ToList();
    }

    private void EditProduct(Product? product)
    {
        using var dlg = new ProductEditDialog(product, _service.GetCategories().ToList());
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            _service.SaveProduct(dlg.Product);
            LoadData();
        }
    }

    private void DeleteProduct()
    {
        if (_grid.CurrentRow?.DataBoundItem is not Product p) return;
        if (MessageBox.Show($"Delete '{p.Name}'?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
        {
            _service.DeleteProduct(p.ProductId);
            LoadData();
        }
    }
}

internal class ProductEditDialog : Form
{
    public Product Product { get; private set; }
    private readonly ComboBox _cboCategory = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
    private readonly TextBox _txtName = new() { Width = 200 };
    private readonly NumericUpDown _numPrice = new() { Width = 120, DecimalPlaces = 2, Maximum = 999999, Minimum = 0 };
    private readonly CheckBox _chkService = new() { Text = "Is Service (no inventory)" };
    private readonly NumericUpDown _numReorder = new() { Width = 80, Maximum = 9999, Minimum = 0, Value = 5 };

    public ProductEditDialog(Product? existing, List<Category> categories)
    {
        Text = existing == null ? "Add Product/Service" : "Edit Product/Service";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(340, 280);

        Product = existing ?? new Product();
        _cboCategory.DataSource = categories;
        _cboCategory.DisplayMember = "CategoryName";
        _cboCategory.ValueMember = "CategoryId";

        if (existing != null)
        {
            _cboCategory.SelectedValue = existing.CategoryId;
            _txtName.Text = existing.Name;
            _numPrice.Value = existing.Price;
            _chkService.Checked = existing.IsService;
            _numReorder.Value = existing.ReorderLevel;
        }

        var y = 20;
        Controls.Add(new Label { Text = "Category:", Location = new Point(20, y), AutoSize = true });
        _cboCategory.Location = new Point(120, y - 3); y += 35;
        Controls.Add(_cboCategory);
        Controls.Add(new Label { Text = "Name:", Location = new Point(20, y), AutoSize = true });
        _txtName.Location = new Point(120, y - 3); y += 35;
        Controls.Add(_txtName);
        Controls.Add(new Label { Text = "Price:", Location = new Point(20, y), AutoSize = true });
        _numPrice.Location = new Point(120, y - 3); y += 35;
        Controls.Add(_numPrice);
        _chkService.Location = new Point(120, y); y += 35;
        Controls.Add(_chkService);
        Controls.Add(new Label { Text = "Reorder Level:", Location = new Point(20, y), AutoSize = true });
        _numReorder.Location = new Point(120, y - 3); y += 45;
        Controls.Add(_numReorder);

        var btnOk = new Button { Text = "Save", Location = new Point(120, y), DialogResult = DialogResult.OK };
        var btnCancel = new Button { Text = "Cancel", Location = new Point(210, y), DialogResult = DialogResult.Cancel };
        btnOk.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(_txtName.Text))
            {
                MessageBox.Show("Name is required.");
                DialogResult = DialogResult.None;
                return;
            }
            Product.CategoryId = (int)_cboCategory.SelectedValue!;
            Product.Name = _txtName.Text.Trim();
            Product.Price = _numPrice.Value;
            Product.IsService = _chkService.Checked;
            Product.ReorderLevel = (int)_numReorder.Value;
        };
        Controls.AddRange(new Control[] { btnOk, btnCancel });
        AcceptButton = btnOk;
        CancelButton = btnCancel;
    }
}
