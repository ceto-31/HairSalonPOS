using HairSalonPOS.Services;

namespace HairSalonPOS.Forms;

public class LoginForm : Form
{
    private readonly TextBox _txtUsername = new() { Width = 220 };
    private readonly TextBox _txtPassword = new() { Width = 220, UseSystemPasswordChar = true };
    private readonly Label _lblError = new() { ForeColor = Color.DarkRed, AutoSize = true };
    private int _failedAttempts;

    public LoginForm()
    {
        Text = "Fix Republic POS - Login";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(380, 260);

        var lblTitle = new Label
        {
            Text = "Fix Republic POS",
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(60, 20)
        };

        var lblUser = new Label { Text = "Username:", Location = new Point(50, 80), AutoSize = true };
        _txtUsername.Location = new Point(130, 77);

        var lblPass = new Label { Text = "Password:", Location = new Point(50, 115), AutoSize = true };
        _txtPassword.Location = new Point(130, 112);

        _lblError.Location = new Point(50, 145);

        var btnLogin = new Button { Text = "Login", Location = new Point(130, 175), Width = 100 };
        btnLogin.Click += BtnLogin_Click;

        var btnExit = new Button { Text = "Exit", Location = new Point(240, 175), Width = 80 };
        btnExit.Click += (_, _) => Application.Exit();

        AcceptButton = btnLogin;
        Controls.AddRange(new Control[] { lblTitle, lblUser, _txtUsername, lblPass, _txtPassword, _lblError, btnLogin, btnExit });
    }

    private void BtnLogin_Click(object? sender, EventArgs e)
    {
        _lblError.Text = "";
        if (string.IsNullOrWhiteSpace(_txtUsername.Text) || string.IsNullOrWhiteSpace(_txtPassword.Text))
        {
            _lblError.Text = "Enter username and password.";
            return;
        }

        try
        {
            var auth = new AuthService();
            var user = auth.Authenticate(_txtUsername.Text.Trim(), _txtPassword.Text);
            if (user == null)
            {
                _failedAttempts++;
                _lblError.Text = _failedAttempts >= 5
                    ? "Too many failed attempts. Contact administrator."
                    : "Invalid username or password.";
                if (_failedAttempts >= 5) _txtPassword.Enabled = false;
                return;
            }

            Helpers.SessionContext.SetUser(user);
            Hide();
            using var main = new MainShellForm();
            main.ShowDialog();
            Close();
        }
        catch (Exception ex)
        {
            _lblError.Text = "Connection error: " + ex.Message;
        }
    }
}
