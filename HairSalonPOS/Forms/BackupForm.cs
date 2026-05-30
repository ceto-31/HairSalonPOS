using HairSalonPOS.Helpers;
using HairSalonPOS.Services;

namespace HairSalonPOS.Forms;

public class BackupForm : Form
{
    private readonly BackupService _service = new();
    private readonly TextBox _txtFolder = new() { Width = 350, ReadOnly = true };
    private readonly ListBox _lstBackups = new() { Width = 450, Height = 200 };

    public BackupForm()
    {
        Text = "Database Backup & Restore";
        try { SessionContext.RequireRole("Admin"); }
        catch (UnauthorizedAccessException ex)
        {
            Load += (_, _) => { MessageBox.Show(ex.Message); Close(); };
            return;
        }

        var folder = _service.GetDefaultBackupFolder();
        _txtFolder.Text = folder;

        var lblInfo = new Label
        {
            Text = "Create backups before major changes. Restore will replace all current data.",
            Location = new Point(20, 15),
            AutoSize = true,
            ForeColor = Color.DarkRed
        };

        var lblFolder = new Label { Text = "Backup folder:", Location = new Point(20, 45), AutoSize = true };
        _txtFolder.Location = new Point(120, 42);
        var btnBrowse = new Button { Text = "Browse...", Location = new Point(480, 40), Width = 80 };
        btnBrowse.Click += (_, _) =>
        {
            using var fbd = new FolderBrowserDialog { SelectedPath = _txtFolder.Text };
            if (fbd.ShowDialog() == DialogResult.OK) _txtFolder.Text = fbd.SelectedPath;
        };

        var btnBackup = new Button { Text = "Create Backup Now", Location = new Point(20, 80), Width = 150, Height = 35, BackColor = Color.LightGreen };
        btnBackup.Click += (_, _) => CreateBackup();

        var btnRefresh = new Button { Text = "Refresh List", Location = new Point(180, 80), Width = 100, Height = 35 };
        btnRefresh.Click += (_, _) => LoadBackupList();

        var lblList = new Label { Text = "Available backups:", Location = new Point(20, 130), AutoSize = true };
        _lstBackups.Location = new Point(20, 155);

        var btnRestore = new Button { Text = "Restore Selected", Location = new Point(20, 365), Width = 130, Height = 35, BackColor = Color.LightCoral };
        btnRestore.Click += (_, _) => RestoreBackup();

        ClientSize = new Size(580, 420);
        Controls.AddRange(new Control[] { lblInfo, lblFolder, _txtFolder, btnBrowse, btnBackup, btnRefresh, lblList, _lstBackups, btnRestore });

        Load += (_, _) => LoadBackupList();
    }

    private void LoadBackupList()
    {
        _lstBackups.Items.Clear();
        if (!Directory.Exists(_txtFolder.Text)) return;
        foreach (var file in Directory.GetFiles(_txtFolder.Text, "*.bak").OrderByDescending(f => f))
            _lstBackups.Items.Add(file);
    }

    private void CreateBackup()
    {
        try
        {
            var path = _service.CreateBackup(SessionContext.CurrentUser!.UserId, _txtFolder.Text);
            MessageBox.Show($"Backup created successfully:\n{path}", "Backup", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadBackupList();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Backup failed: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RestoreBackup()
    {
        if (_lstBackups.SelectedItem is not string path) return;

        var confirm = MessageBox.Show(
            "WARNING: This will replace ALL current database data with the backup.\nThe application will close after restore.\n\nContinue?",
            "Confirm Restore",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirm != DialogResult.Yes) return;

        try
        {
            _service.RestoreBackup(path);
            MessageBox.Show("Database restored successfully. Please restart the application.", "Restore", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Application.Exit();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Restore failed: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
