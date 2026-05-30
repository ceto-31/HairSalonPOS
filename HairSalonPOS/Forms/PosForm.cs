using HairSalonPOS.Helpers;
using HairSalonPOS.Models;
using HairSalonPOS.Services;

namespace HairSalonPOS.Forms;

public class PosForm : Form
{
    private readonly ProductService _productService = new();
    private readonly SalesService _salesService = new();
    private readonly List<CartItem> _cart = new();

    private readonly TextBox _txtSearch = new() { Width = 250 };
    private readonly DataGridView _gridProducts = new();
    private readonly DataGridView _gridCart = new();
    private readonly Label _lblSubTotal = new() { AutoSize = true, Font = new Font("Segoe UI", 10) };
    private readonly Label _lblTax = new() { AutoSize = true };
    private readonly Label _lblTotal = new() { AutoSize = true, Font = new Font("Segoe UI", 12, FontStyle.Bold) };
    private readonly TabControl _tabs = new() { Dock = DockStyle.Fill };

    private bool _showServices = true;

    public PosForm()
    {
        Text = "Point of Sale";
        BackColor = Color.White;

        var topPanel = new Panel { Dock = DockStyle.Top, Height = 45 };
        _txtSearch.Location = new Point(10, 10);
        _txtSearch.PlaceholderText = "Search products or services...";
        _txtSearch.TextChanged += (_, _) => LoadProducts();
        var btnRefresh = new Button { Text = "Refresh", Location = new Point(270, 8), Width = 80 };
        btnRefresh.Click += (_, _) => LoadProducts();
        topPanel.Controls.AddRange(new Control[] { _txtSearch, btnRefresh });

        _tabs.Dock = DockStyle.Top;
        _tabs.Height = 28;
        _tabs.TabPages.Add(new TabPage("Services"));
        _tabs.TabPages.Add(new TabPage("Products"));
        _tabs.SelectedIndexChanged += (_, _) =>
        {
            _showServices = _tabs.SelectedIndex == 0;
            LoadProducts();
        };

        SetupProductGrid(_gridProducts);
        _gridProducts.Dock = DockStyle.Fill;
        _gridProducts.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0) AddSelectedProduct();
        };

        var leftPanel = new Panel { Dock = DockStyle.Fill };
        leftPanel.Controls.Add(_gridProducts);
        leftPanel.Controls.Add(_tabs);

        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, SplitterDistance = 520 };
        split.Panel1.Controls.Add(leftPanel);

        var cartPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
        var lblCart = new Label { Text = "CART", Font = new Font("Segoe UI", 12, FontStyle.Bold), Dock = DockStyle.Top, Height = 30 };

        SetupCartGrid(_gridCart);
        _gridCart.Dock = DockStyle.Fill;

        var totalsPanel = new Panel { Dock = DockStyle.Bottom, Height = 120 };
        _lblSubTotal.Location = new Point(10, 5);
        _lblTax.Location = new Point(10, 28);
        _lblTotal.Location = new Point(10, 55);
        totalsPanel.Controls.AddRange(new Control[] { _lblSubTotal, _lblTax, _lblTotal });

        var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 45, FlowDirection = FlowDirection.LeftToRight };
        var btnAdd = new Button { Text = "Add Selected", Width = 100, Height = 32 };
        btnAdd.Click += (_, _) => AddSelectedProduct();
        var btnRemove = new Button { Text = "Remove", Width = 80, Height = 32 };
        btnRemove.Click += (_, _) => RemoveCartItem();
        var btnClear = new Button { Text = "Clear (Esc)", Width = 90, Height = 32 };
        btnClear.Click += (_, _) => ClearCart();
        var btnCash = new Button { Text = "Pay Cash (F2)", Width = 110, Height = 32, BackColor = Color.LightGreen };
        btnCash.Click += (_, _) => ProcessPayment("Cash");
        var btnCard = new Button { Text = "Pay Card", Width = 90, Height = 32, BackColor = Color.LightBlue };
        btnCard.Click += (_, _) => ProcessPayment("Card");
        btnPanel.Controls.AddRange(new Control[] { btnAdd, btnRemove, btnClear, btnCash, btnCard });

        cartPanel.Controls.Add(_gridCart);
        cartPanel.Controls.Add(btnPanel);
        cartPanel.Controls.Add(totalsPanel);
        cartPanel.Controls.Add(lblCart);
        split.Panel2.Controls.Add(cartPanel);

        Controls.Add(split);
        Controls.Add(topPanel);

        KeyPreview = true;
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.F2) ProcessPayment("Cash");
            if (e.KeyCode == Keys.Escape) ClearCart();
        };

        Load += (_, _) => LoadProducts();
    }

    private void SetupProductGrid(DataGridView grid)
    {
        grid.ReadOnly = true;
        grid.AutoGenerateColumns = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Name", HeaderText = "Name", Width = 180 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CategoryName", HeaderText = "Category", Width = 100 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Price", HeaderText = "Price", DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" }, Width = 80 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "QuantityOnHand", HeaderText = "Stock", Width = 60 });
    }

    private void SetupCartGrid(DataGridView grid)
    {
        grid.ReadOnly = true;
        grid.AutoGenerateColumns = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Name", HeaderText = "Item", Width = 150 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Quantity", HeaderText = "Qty", Width = 50 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "UnitPrice", HeaderText = "Price", DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" }, Width = 70 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "LineTotal", HeaderText = "Total", DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" }, Width = 80 });
    }

    private void LoadProducts()
    {
        try
        {
            var products = _productService.GetProducts(_showServices, _txtSearch.Text.Trim()).ToList();
            _gridProducts.DataSource = products;
        }
        catch (Exception ex)
        {
            MessageBox.Show("Failed to load products: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void AddSelectedProduct()
    {
        if (_gridProducts.CurrentRow?.DataBoundItem is not Product product) return;

        if (!product.IsService && product.QuantityOnHand <= 0)
        {
            MessageBox.Show($"{product.Name} is out of stock.", "Stock", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var existing = _cart.FirstOrDefault(c => c.ProductId == product.ProductId);
        if (existing != null)
        {
            if (!product.IsService && existing.Quantity >= product.QuantityOnHand)
            {
                MessageBox.Show("Not enough stock available.", "Stock", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            existing.Quantity++;
        }
        else
        {
            _cart.Add(new CartItem
            {
                ProductId = product.ProductId,
                Name = product.Name,
                UnitPrice = product.Price,
                Quantity = 1,
                IsService = product.IsService
            });
        }
        RefreshCart();
    }

    private void RemoveCartItem()
    {
        if (_gridCart.CurrentRow?.DataBoundItem is CartItem item)
        {
            _cart.Remove(item);
            RefreshCart();
        }
    }

    private void ClearCart()
    {
        _cart.Clear();
        RefreshCart();
    }

    private void RefreshCart()
    {
        _gridCart.DataSource = null;
        _gridCart.DataSource = _cart.ToList();
        var sub = _cart.Sum(c => c.LineTotal);
        var tax = Math.Round(sub * _salesService.TaxRate, 2);
        _lblSubTotal.Text = $"Subtotal: ₱{sub:N2}";
        _lblTax.Text = $"Tax ({_salesService.TaxRate:P0}): ₱{tax:N2}";
        _lblTotal.Text = $"TOTAL: ₱{(sub + tax):N2}";
    }

    private void ProcessPayment(string method)
    {
        if (_cart.Count == 0)
        {
            MessageBox.Show("Cart is empty.", "POS", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            var userId = SessionContext.CurrentUser!.UserId;
            var saleId = _salesService.ProcessSale(userId, _cart.ToList(), method);
            var receipt = _salesService.GetReceipt(saleId);

            var result = MessageBox.Show(
                $"Sale #{saleId} completed!\nTotal: ₱{receipt.Header.Total:N2}\n\nPrint receipt?",
                "Payment Successful",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Information);

            if (result == DialogResult.Yes)
            {
                var printer = new ReceiptPrinter(receipt);
                printer.Print();
            }
            else if (result == DialogResult.Cancel)
            {
                using var sfd = new SaveFileDialog { Filter = "Text Receipt|*.txt", FileName = $"Receipt_{saleId}.txt" };
                if (sfd.ShowDialog() == DialogResult.OK)
                    new ReceiptPrinter(receipt).SaveToFile(sfd.FileName);
            }

            ClearCart();
            LoadProducts();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Sale Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
