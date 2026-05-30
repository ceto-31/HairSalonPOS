using HairSalonPOS.Forms;
using HairSalonPOS.Services;

namespace HairSalonPOS;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        if (args.Contains("--test"))
        {
            Environment.Exit(IntegrationTestRunner.Run() ? 0 : 1);
            return;
        }

        ApplicationConfiguration.Initialize();

        try
        {
            AuthService.EnsureDefaultPasswords();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Could not connect to the database.\n\n" + ex.Message +
                "\n\nEnsure SQL Server Express is running and Encrypt is set to Optional.",
                "Database Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        Application.Run(new LoginForm());
    }
}
