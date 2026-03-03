using System.Reflection;
using System.Windows.Forms;

namespace BloodshedModToolkitInstaller;

public partial class MainForm : Form
{
    private readonly InstallWorker _worker = new();

    public MainForm()
    {
        InitializeComponent();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        // 타이틀과 체크박스 텍스트에 어셈블리 버전 반영
        var ver = Assembly.GetExecutingAssembly().GetName().Version;
        string verStr = ver is null ? "" : $"{ver.Major}.{ver.Minor}.{ver.Build}";
        Text           = $"Bloodshed Mod Toolkit Installer v{verStr}";
        chkModDll.Text = $"BloodshedModToolkit v{verStr}";

        string? detected = InstallWorker.FindSteamGamePath();
        if (detected != null)
        {
            txtGamePath.Text = detected;
            SetStatus($"Game path detected: {detected}", isInfo: true);
        }
    }

    private void btnBrowse_Click(object? sender, EventArgs e)
    {
        using var dlg = new FolderBrowserDialog
        {
            Description = "Select the Bloodshed game folder (where Bloodshed.exe is located)",
            UseDescriptionForTitle = true,
            SelectedPath = txtGamePath.Text.Trim()
        };
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            string path = dlg.SelectedPath;
            if (!InstallWorker.ValidateGamePath(path))
            {
                MessageBox.Show(
                    "Bloodshed.exe was not found in the selected folder.\nPlease select the correct game directory.",
                    "Invalid Path", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            txtGamePath.Text = path;
        }
    }

    private async void btnInstall_Click(object? sender, EventArgs e)
    {
        string gamePath = txtGamePath.Text.Trim();
        if (!InstallWorker.ValidateGamePath(gamePath))
        {
            MessageBox.Show(
                "Please select a valid Bloodshed game directory first.",
                "No Game Path", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (!chkBepInEx.Checked && !chkModDll.Checked)
        {
            MessageBox.Show(
                "Select at least one item to install.",
                "Nothing Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        await RunInstallAsync(gamePath);
    }

    private async Task RunInstallAsync(string gamePath)
    {
        SetUIEnabled(false);
        progressBar.Value = 0;

        var options = new InstallOptions
        {
            InstallBepInEx = chkBepInEx.Checked,
            InstallModDll  = chkModDll.Checked,
        };

        var progress = new Progress<(int pct, string msg)>(report =>
        {
            progressBar.Value = Math.Clamp(report.pct, 0, 100);
            SetStatus(report.msg);
        });

        try
        {
            await _worker.InstallAsync(gamePath, options, progress);

            progressBar.Value = 100;
            SetStatus("Installation complete!", isInfo: true);
            MessageBox.Show(
                "Installation complete!\n\n" +
                "Next steps:\n" +
                "1. Launch Bloodshed once — BepInEx will generate interop assemblies (may take 1-2 minutes on first run).\n" +
                "2. Launch the game again to activate the mod.\n" +
                "3. Press INSERT in-game to open the cheat menu.",
                "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            SetStatus($"Error: {ex.Message}", isError: true);
            MessageBox.Show(
                $"Installation failed:\n\n{ex.Message}",
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetUIEnabled(true);
        }
    }

    private void SetUIEnabled(bool enabled)
    {
        txtGamePath.Enabled  = enabled;
        btnBrowse.Enabled    = enabled;
        chkBepInEx.Enabled   = enabled;
        chkModDll.Enabled    = enabled;
        btnInstall.Enabled   = enabled;
    }

    private void SetStatus(string message, bool isError = false, bool isInfo = false)
    {
        lblStatus.Text      = message;
        lblStatus.ForeColor = isError ? System.Drawing.Color.Red
                            : isInfo  ? System.Drawing.Color.DarkGreen
                                      : System.Drawing.SystemColors.GrayText;
    }
}
