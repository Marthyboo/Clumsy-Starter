using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

public class FullscreenForm : Form
{
    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_SHOWWINDOW = 0x0040;
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x20;
    private const int WS_EX_LAYERED = 0x80000;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    private Label statusLabel;
    private static Config config;
    private Timer topMostTimer;

    public FullscreenForm()
    {
        config = Config.Load();

        this.statusLabel = new Label
        {
            AutoSize = true,
            Font = new Font("Arial", 16, FontStyle.Bold),
            Location = new Point(10, 10),
            BackColor = Color.Transparent,
            ForeColor = Color.Red,
            Text = "OFF"
        };

        // Form properties
        this.FormBorderStyle = FormBorderStyle.None;
        this.ShowInTaskbar = false;
        this.BackColor = Color.Black;
        this.TransparencyKey = Color.Black;
        this.TopMost = true;
        this.Opacity = 1;

        // Set the form to cover the entire primary screen
        this.StartPosition = FormStartPosition.Manual;
        this.Bounds = Screen.PrimaryScreen.Bounds;

        this.Controls.Add(statusLabel);
        UpdateStatusLabel(false);
        statusLabel.Visible = config.ShowIndicator;

        // Handle window creation
        this.Load += (s, e) =>
        {
            // Set extended window styles
            int exStyle = GetWindowLong(this.Handle, GWL_EXSTYLE);
            exStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_NOACTIVATE;
            SetWindowLong(this.Handle, GWL_EXSTYLE, exStyle);

            // Ensure window stays topmost
            SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
        };

        // Keep checking if we're still topmost
        topMostTimer = new Timer
        {
            Interval = 1000
        };
        topMostTimer.Tick += (s, e) =>
        {
            SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
        };
        topMostTimer.Start();

        // Handle application close
        this.FormClosed += (s, e) =>
        {
            topMostTimer.Stop();
            topMostTimer.Dispose();
            statusLabel.Visible = false;
        };
    }

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            cp.ExStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_NOACTIVATE;
            return cp;
        }
    }

    public void UpdateStatusLabel(bool isActive)
    {
        if (statusLabel.InvokeRequired)
        {
            this.Invoke(new Action(() => UpdateStatusLabel(isActive)));
            return;
        }

        statusLabel.Text = isActive ? "ON" : "OFF";
        statusLabel.ForeColor = isActive ? Color.Green : Color.Red;
        statusLabel.Visible = config.ShowIndicator;
        statusLabel.BringToFront();

        // Ensure we're still topmost
        SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
    }

    public void SetIndicatorVisibility(bool visible)
    {
        if (statusLabel.InvokeRequired)
        {
            this.Invoke(new Action(() => SetIndicatorVisibility(visible)));
            return;
        }

        statusLabel.Visible = visible;
    }

    protected override bool ShowWithoutActivation
    {
        get { return true; }
    }
}
