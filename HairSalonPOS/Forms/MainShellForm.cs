using HairSalonPOS.Helpers;
using HairSalonPOS.Services;

namespace HairSalonPOS.Forms;

public class MainShellForm : Form
{
    private readonly Label _lblUser = new() { AutoSize = true };
    private readonly Label _lblLowStock = new()
    {
        AutoSize = true,
        ForeColor = Color.DarkRed,
        Font = new Font("Segoe UI", 10, FontStyle.Bold),
        Cursor = Cursors.Hand
    };
    private readonly Panel _contentPanel = new() { Dock = DockStyle.Fill };
    private readonly InventoryService _inventoryService = new();
    private Form? _activeForm;

    public MainShellForm()
    {
        Text = "Hair Salon POS";
        WindowState = FormWindowState.Maximized;
        StartPosition = FormStartPosition.CenterScreen;

        var menu = new MenuStrip();
        var mPos = new ToolStripMenuItem("POS");
        var mProducts = new ToolStripMenuItem("Products");
        var mInventory = new ToolStripMenuItem("Inventory");
        var mReports = new ToolStripMenuItem("Reports");
        var mUsers = new ToolStripMenuItem("Users");
        var mBackup = new ToolStripMenuItem("Backup");
        var mLogout = new ToolStripMenuItem("Logout");

        mPos.Click += (_, _) => ShowChild(new PosForm());
        mProducts.Click += (_, _) => ShowChild(new ProductForm());
        mInventory.Click += (_, _) => ShowChild(new InventoryForm());
        mReports.Click += (_, _) => ShowChild(new ReportsForm());
        mUsers.Click += (_, _) => ShowChild(new UserManagementForm());
        mBackup.Click += (_, _) => ShowChild(new BackupForm());
        mLogout.Click += (_, _) => { SessionContext.Clear(); Close(); };

        menu.Items.AddRange(new ToolStripItem[] { mPos, mProducts, mInventory, mReports, mUsers, mBackup, mLogout });

        if (!SessionContext.HasRole("Admin", "Manager"))
            mProducts.Visible = mInventory.Visible = false;
        if (!SessionContext.HasRole("Admin"))
            mUsers.Visible = mBackup.Visible = false;

        var status = new StatusStrip();
        _lblUser.Text = $"Logged in: {SessionContext.CurrentUser?.FullName} ({SessionContext.CurrentUser?.RoleName})";
        _lblLowStock.Click += (_, _) => ShowChild(new InventoryForm(showLowStockOnly: true));
        status.Items.Add(new ToolStripStatusLabel { Text = _lblUser.Text });
        status.Items.Add(new ToolStripStatusLabel { Spring = true });
        status.Items.Add(new ToolStripStatusLabel { Text = "Low stock: " });
        status.Items.Add(new ToolStripStatusLabel { Text = "", DisplayStyle = ToolStripItemDisplayStyle.Text });
        ((ToolStripStatusLabel)status.Items[^1]).Click += (_, _) => ShowChild(new InventoryForm(showLowStockOnly: true));

        var topPanel = new Panel { Dock = DockStyle.Top, Height = 35, BackColor = Color.MistyRose };
        _lblLowStock.Location = new Point(10, 8);
        topPanel.Controls.Add(_lblLowStock);

        MainMenuStrip = menu;
        Controls.Add(_contentPanel);
        Controls.Add(topPanel);
        Controls.Add(menu);
        Controls.Add(status);

        Load += (_, _) =>
        {
            UpdateLowStockAlert();
            ShowChild(new PosForm());
        };
    }

    private void UpdateLowStockAlert()
    {
        try
        {
            var count = _inventoryService.GetLowStockCount();
            _lblLowStock.Text = count > 0
                ? $"⚠ {count} product(s) low on stock — click to view"
                : "✓ All stock levels OK";
            _lblLowStock.ForeColor = count > 0 ? Color.DarkRed : Color.DarkGreen;
        }
        catch
        {
            _lblLowStock.Text = "Unable to check stock levels";
        }
    }

    public void RefreshAlerts() => UpdateLowStockAlert();

    private void ShowChild(Form child)
    {
        _activeForm?.Close();
        _activeForm = child;
        child.TopLevel = false;
        child.FormBorderStyle = FormBorderStyle.None;
        child.Dock = DockStyle.Fill;
        _contentPanel.Controls.Clear();
        _contentPanel.Controls.Add(child);
        child.Show();
        if (child is PosForm) UpdateLowStockAlert();
    }
}
