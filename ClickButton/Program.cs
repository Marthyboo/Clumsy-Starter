// File: ClickButton.cs
using System;
using System.Diagnostics; // For process management
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

public class ClickButton : Form
{
    [DllImport("user32.dll")]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    private static extern IntPtr FindWindowEx(IntPtr hWndParent, IntPtr hWndChildAfter, string lpszClass, string lpszWindowName);

    [DllImport("user32.dll")]
    private static extern void SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumChildWindows(IntPtr hWndParent, EnumChildProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    private const uint WM_CLICK = 0x00F5;
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private static LowLevelKeyboardProc _proc = HookCallback;
    private static IntPtr _hookID = IntPtr.Zero;

    private delegate bool EnumChildProc(IntPtr hWnd, IntPtr lParam);
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    public static bool isActive = false;
    public static Keys keybind = Keys.Tab;
    public Label statusLabel;
    private Timer checkClumsyTimer;
    private static ClickButton instance;

    public ClickButton()
    {
        instance = this;

        this.statusLabel = new Label
        {
            AutoSize = true,
            Font = new System.Drawing.Font("Arial", 16, System.Drawing.FontStyle.Bold),
            Location = new System.Drawing.Point(10, 10),
            BackColor = System.Drawing.Color.Transparent,
            ForeColor = System.Drawing.Color.Red
        };

        this.Controls.Add(statusLabel);
        this.FormBorderStyle = FormBorderStyle.None;
        this.TopMost = true;
        this.StartPosition = FormStartPosition.Manual;
        this.Location = new System.Drawing.Point(10, 10);
        this.ShowInTaskbar = false;
        this.BackColor = System.Drawing.Color.Lime;
        this.TransparencyKey = System.Drawing.Color.Lime;
        UpdateStatusLabel();

        checkClumsyTimer = new Timer();
        checkClumsyTimer.Interval = 5000;
        checkClumsyTimer.Tick += CheckClumsyTimer_Tick;
        checkClumsyTimer.Start();

        this.FormClosing += OnFormClosing; // Add cleanup on form closing

        ToggleForm toggleForm = new ToggleForm(this);
        toggleForm.Show();
    }

    public static void Main()
    {
        if (!IsRunAsAdmin())
        {
            RelaunchAsAdmin();
            return;
        }

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        if (!IsTargetApplicationRunning())
        {
            MessageBox.Show("The target application (clumsy) is not running. Please start it and try again.", "Application Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        ClickButton form = new ClickButton();
        _hookID = ClickButton.SetHook(_proc);
        Application.Run(form);
        UnhookWindowsHookEx(_hookID);
    }

    private static bool IsRunAsAdmin()
    {
        var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
        var principal = new System.Security.Principal.WindowsPrincipal(identity);
        return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
    }

    private static void RelaunchAsAdmin()
    {
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            UseShellExecute = true,
            FileName = Process.GetCurrentProcess().MainModule.FileName,
            Verb = "runas"
        };
        Process.Start(startInfo);
    }

    private static bool IsTargetApplicationRunning()
    {
        bool isRunning = false;

        EnumWindows((hWnd, lParam) =>
        {
            StringBuilder sb = new StringBuilder(256);
            GetWindowText(hWnd, sb, sb.Capacity);
            if (sb.ToString().ToLower().Contains("clumsy"))
            {
                isRunning = true;
                return false;
            }
            return true;
        }, IntPtr.Zero);

        return isRunning;
    }

    public void UpdateStatusLabel()
    {
        statusLabel.Text = isActive ? "ON" : "OFF";
        statusLabel.ForeColor = isActive ? System.Drawing.Color.Green : System.Drawing.Color.Red;
        statusLabel.Visible = true;
    }

    public void SetKeybind(Keys newKey)
    {
        keybind = newKey;
        MessageBox.Show($"Keybind set to: {newKey}", "Keybind Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    public string GetKeybind()
    {
        return keybind.ToString();
    }

    private static IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using (var curProcess = Process.GetCurrentProcess())
        using (var curModule = curProcess.MainModule)
        {
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            if (vkCode == (int)keybind)
            {
                try
                {
                    instance.SimulateTabPress();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error in HookCallback: " + ex.Message);
                }
                return (IntPtr)1;
            }
        }
        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }

    private void SimulateTabPress()
    {
        isActive = !isActive;
        UpdateStatusLabel();

        IntPtr hWnd = FindClumsyWindow();
        if (hWnd != IntPtr.Zero)
        {
            Console.WriteLine("Found the window: " + hWnd);
            IntPtr buttonHandle = FindButtonByText(hWnd, "Start");
            if (buttonHandle == IntPtr.Zero)
            {
                buttonHandle = FindButtonByText(hWnd, "Stop");
            }


            if (buttonHandle != IntPtr.Zero)
            {
                Console.WriteLine("Found the button: " + buttonHandle);
                SetForegroundWindow(hWnd);
                System.Threading.Thread.Sleep(300);
                PostMessage(buttonHandle, WM_CLICK, IntPtr.Zero, IntPtr.Zero);
                Console.WriteLine("Button click simulated.");
            }
            else
            {
                Console.WriteLine("Button not found.");
            }
        }
        else
        {
            Console.WriteLine("Window not found.");
        }
    }

    private static IntPtr FindButtonByText(IntPtr hWndParent, string buttonText)
    {
        IntPtr foundButton = IntPtr.Zero;
        EnumChildWindows(hWndParent, (hWnd, lParam) =>
        {
            StringBuilder sb = new StringBuilder(256);
            GetWindowText(hWnd, sb, sb.Capacity);
            if (sb.ToString() == buttonText)
            {
                foundButton = hWnd;
                return false;
            }
            return true;
        }, IntPtr.Zero);
        return foundButton;
    }

    private static IntPtr FindClumsyWindow()
    {
        IntPtr foundWindow = IntPtr.Zero;
        EnumWindows((hWnd, lParam) =>
        {
            StringBuilder sb = new StringBuilder(256);
            GetWindowText(hWnd, sb, sb.Capacity);
            if (sb.ToString().ToLower().Contains("clumsy"))
            {
                foundWindow = hWnd;
                return false;
            }
            return true;
        }, IntPtr.Zero);

        return foundWindow;
    }

    private void CheckClumsyTimer_Tick(object sender, EventArgs e)
    {
        if (!IsTargetApplicationRunning())
        {
            MessageBox.Show("The target application (clumsy) has closed. The application will now exit.", "Application Closed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Application.Exit();
        }
    }

    private void OnFormClosing(object sender, FormClosingEventArgs e)
    {
        // Stop and dispose the timer
        if (checkClumsyTimer != null)
        {
            checkClumsyTimer.Stop();
            checkClumsyTimer.Dispose();
        }

        // Unhook the keyboard hook
        if (_hookID != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookID);
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);
}
