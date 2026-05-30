using HairSalonPOS.Helpers;
using HairSalonPOS.Models;
using HairSalonPOS.Services;

namespace HairSalonPOS.Forms;

public class UserManagementForm : Form
{
    private readonly UserService _service = new();
    private readonly DataGridView _grid = new() { Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect };

    public UserManagementForm()
    {
        Text = "User Management";
        try { SessionContext.RequireRole("Admin"); }
        catch (UnauthorizedAccessException ex)
        {
            Load += (_, _) => { MessageBox.Show(ex.Message); Close(); };
            return;
        }

        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Username", HeaderText = "Username", Width = 100 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "FullName", HeaderText = "Full Name", Width = 150 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "RoleName", HeaderText = "Role", Width = 80 });
        _grid.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = "IsActive", HeaderText = "Active" });

        var toolbar = new Panel { Dock = DockStyle.Top, Height = 45 };
        var btnAdd = new Button { Text = "Add User", Location = new Point(10, 8), Width = 90 };
        btnAdd.Click += (_, _) => EditUser(null);
        var btnEdit = new Button { Text = "Edit", Location = new Point(105, 8), Width = 70 };
        btnEdit.Click += (_, _) => { if (_grid.CurrentRow?.DataBoundItem is UserAccount u) EditUser(u); };
        var btnDeactivate = new Button { Text = "Deactivate", Location = new Point(180, 8), Width = 90 };
        btnDeactivate.Click += (_, _) => DeactivateUser();
        var btnRefresh = new Button { Text = "Refresh", Location = new Point(275, 8), Width = 70 };
        btnRefresh.Click += (_, _) => LoadData();
        toolbar.Controls.AddRange(new Control[] { btnAdd, btnEdit, btnDeactivate, btnRefresh });

        Controls.Add(_grid);
        Controls.Add(toolbar);
        Load += (_, _) => LoadData();
    }

    private void LoadData() => _grid.DataSource = _service.GetAllUsers().ToList();

    private void EditUser(UserAccount? user)
    {
        using var dlg = new UserEditDialog(user, _service.GetRoles().ToList());
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            _service.SaveUser(dlg.User, dlg.NewPassword);
            LoadData();
        }
    }

    private void DeactivateUser()
    {
        if (_grid.CurrentRow?.DataBoundItem is not UserAccount u) return;
        if (u.UserId == SessionContext.CurrentUser?.UserId)
        {
            MessageBox.Show("You cannot deactivate your own account.");
            return;
        }
        if (MessageBox.Show($"Deactivate user '{u.Username}'?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
        {
            _service.DeactivateUser(u.UserId);
            LoadData();
        }
    }
}

internal class UserEditDialog : Form
{
    public UserAccount User { get; private set; }
    public string? NewPassword { get; private set; }

    private readonly TextBox _txtUsername = new() { Width = 180 };
    private readonly TextBox _txtFullName = new() { Width = 180 };
    private readonly ComboBox _cboRole = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180 };
    private readonly CheckBox _chkActive = new() { Text = "Active", Checked = true };
    private readonly TextBox _txtPassword = new() { Width = 180, UseSystemPasswordChar = true };

    public UserEditDialog(UserAccount? existing, List<Role> roles)
    {
        Text = existing == null ? "Add User" : "Edit User";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(320, 280);
        MaximizeBox = false;

        User = existing ?? new UserAccount { IsActive = true };
        _cboRole.DataSource = roles;
        _cboRole.DisplayMember = "RoleName";
        _cboRole.ValueMember = "RoleId";

        if (existing != null)
        {
            _txtUsername.Text = existing.Username;
            _txtFullName.Text = existing.FullName;
            _cboRole.SelectedValue = existing.RoleId;
            _chkActive.Checked = existing.IsActive;
        }

        var y = 20;
        AddRow("Username:", _txtUsername, ref y);
        AddRow("Full Name:", _txtFullName, ref y);
        AddRow("Role:", _cboRole, ref y);
        _chkActive.Location = new Point(110, y); y += 30;
        Controls.Add(_chkActive);
        AddRow(existing == null ? "Password:" : "New Password:", _txtPassword, ref y);

        var btnOk = new Button { Text = "Save", Location = new Point(110, y), DialogResult = DialogResult.OK };
        var btnCancel = new Button { Text = "Cancel", Location = new Point(200, y), DialogResult = DialogResult.Cancel };
        btnOk.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(_txtUsername.Text) || string.IsNullOrWhiteSpace(_txtFullName.Text))
            {
                MessageBox.Show("Username and full name are required.");
                DialogResult = DialogResult.None;
                return;
            }
            if (User.UserId == 0 && string.IsNullOrWhiteSpace(_txtPassword.Text))
            {
                MessageBox.Show("Password is required for new users.");
                DialogResult = DialogResult.None;
                return;
            }
            User.Username = _txtUsername.Text.Trim();
            User.FullName = _txtFullName.Text.Trim();
            User.RoleId = (int)_cboRole.SelectedValue!;
            User.IsActive = _chkActive.Checked;
            NewPassword = string.IsNullOrWhiteSpace(_txtPassword.Text) ? null : _txtPassword.Text;
        };
        Controls.AddRange(new Control[] { btnOk, btnCancel });
        AcceptButton = btnOk;
        CancelButton = btnCancel;
    }

    private void AddRow(string label, Control control, ref int y)
    {
        Controls.Add(new Label { Text = label, Location = new Point(20, y + 3), AutoSize = true });
        control.Location = new Point(110, y);
        Controls.Add(control);
        y += 35;
    }
}
