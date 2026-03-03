namespace BloodshedModToolkitInstaller;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;

    private System.Windows.Forms.Label lblGamePath;
    private System.Windows.Forms.TextBox txtGamePath;
    private System.Windows.Forms.Button btnBrowse;
    private System.Windows.Forms.Label lblInstallItems;
    private System.Windows.Forms.CheckBox chkBepInEx;
    private System.Windows.Forms.CheckBox chkModDll;
    private System.Windows.Forms.ProgressBar progressBar;
    private System.Windows.Forms.Label lblStatus;
    private System.Windows.Forms.Button btnInstall;
    private System.Windows.Forms.Panel pnlSeparator;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
            components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        lblGamePath     = new System.Windows.Forms.Label();
        txtGamePath     = new System.Windows.Forms.TextBox();
        btnBrowse       = new System.Windows.Forms.Button();
        lblInstallItems = new System.Windows.Forms.Label();
        chkBepInEx      = new System.Windows.Forms.CheckBox();
        chkModDll       = new System.Windows.Forms.CheckBox();
        pnlSeparator    = new System.Windows.Forms.Panel();
        progressBar     = new System.Windows.Forms.ProgressBar();
        lblStatus       = new System.Windows.Forms.Label();
        btnInstall      = new System.Windows.Forms.Button();
        SuspendLayout();

        // Form — 제목은 MainForm.cs OnLoad 에서 버전 포함해 동적 설정
        Text            = "Bloodshed Mod Toolkit Installer";
        ClientSize      = new System.Drawing.Size(520, 260);
        FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
        MaximizeBox     = false;
        StartPosition   = System.Windows.Forms.FormStartPosition.CenterScreen;
        Font            = new System.Drawing.Font("Segoe UI", 9F);

        // lblGamePath
        lblGamePath.Text     = "Game path:";
        lblGamePath.Location = new System.Drawing.Point(14, 16);
        lblGamePath.Size     = new System.Drawing.Size(80, 20);
        lblGamePath.AutoSize = true;

        // txtGamePath
        txtGamePath.Location = new System.Drawing.Point(14, 36);
        txtGamePath.Size     = new System.Drawing.Size(408, 23);
        txtGamePath.Anchor   = System.Windows.Forms.AnchorStyles.Left |
                               System.Windows.Forms.AnchorStyles.Right;

        // btnBrowse
        btnBrowse.Text     = "Browse...";
        btnBrowse.Location = new System.Drawing.Point(430, 35);
        btnBrowse.Size     = new System.Drawing.Size(76, 25);
        btnBrowse.Click   += btnBrowse_Click;

        // lblInstallItems
        lblInstallItems.Text     = "Install items:";
        lblInstallItems.Location = new System.Drawing.Point(14, 74);
        lblInstallItems.AutoSize = true;

        // chkBepInEx
        chkBepInEx.Text     = "BepInEx 6.0.0-be.754 (IL2CPP win-x64)";
        chkBepInEx.Checked  = true;
        chkBepInEx.Location = new System.Drawing.Point(14, 94);
        chkBepInEx.Size     = new System.Drawing.Size(320, 22);

        // chkModDll
        chkModDll.Text     = "BloodshedModToolkit";   // 버전은 OnLoad 에서 설정
        chkModDll.Checked  = true;
        chkModDll.Location = new System.Drawing.Point(14, 118);
        chkModDll.Size     = new System.Drawing.Size(320, 22);

        // pnlSeparator
        pnlSeparator.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
        pnlSeparator.Location    = new System.Drawing.Point(14, 150);
        pnlSeparator.Size        = new System.Drawing.Size(492, 2);

        // progressBar
        progressBar.Location = new System.Drawing.Point(14, 162);
        progressBar.Size     = new System.Drawing.Size(492, 22);
        progressBar.Minimum  = 0;
        progressBar.Maximum  = 100;
        progressBar.Style    = System.Windows.Forms.ProgressBarStyle.Continuous;

        // lblStatus
        lblStatus.Text      = "Ready.";
        lblStatus.Location  = new System.Drawing.Point(14, 190);
        lblStatus.Size      = new System.Drawing.Size(492, 20);
        lblStatus.ForeColor = System.Drawing.SystemColors.GrayText;

        // btnInstall
        btnInstall.Text     = "Install";
        btnInstall.Location = new System.Drawing.Point(210, 220);
        btnInstall.Size     = new System.Drawing.Size(100, 30);
        btnInstall.Font     = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
        btnInstall.Click   += btnInstall_Click;

        Controls.AddRange(new System.Windows.Forms.Control[]
        {
            lblGamePath, txtGamePath, btnBrowse,
            lblInstallItems, chkBepInEx, chkModDll,
            pnlSeparator, progressBar, lblStatus, btnInstall
        });

        ResumeLayout(false);
        PerformLayout();
    }
}
