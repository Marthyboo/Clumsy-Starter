using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Linq;

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
    private const int WM_KEYUP = 0x0101;
    private static LowLevelKeyboardProc _proc = HookCallback;
    private static IntPtr _hookID = IntPtr.Zero;

    private delegate bool EnumChildProc(IntPtr hWnd, IntPtr lParam);
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    public static bool isActive = false;
    private static Config config;
    public static Keys keybind;
    private FullscreenForm fullscreenForm;
    private Timer checkClumsyTimer;
    private static ClickButton instance;
    private static bool isProcessingKey = false;
    private static DateTime lastKeyPress = DateTime.MinValue;
    private static bool keyState = false;

    public ClickButton()
    {
        instance = this;
        config = Config.Load();
        keybind = config.Keybind;

        // Create and show the fullscreen form
        fullscreenForm = new FullscreenForm();
        fullscreenForm.Show();

        // Hide the main form
        this.Opacity = 0;
        this.ShowInTaskbar = false;

        checkClumsyTimer = new Timer();
        checkClumsyTimer.Interval = 5000;
        checkClumsyTimer.Tick += CheckClumsyTimer_Tick;
        checkClumsyTimer.Start();

        this.FormClosing += OnFormClosing;

        ToggleForm toggleForm = new ToggleForm(this);
        toggleForm.Show();
    }

    public static void Main()
    {
        // First check if Clumsy is running
        if (!IsTargetApplicationRunning())
        {
            MessageBox.Show(
                "Clumsy is not running!\n\nPlease start Clumsy first, then run this application again.",
                "Clumsy Not Found",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return; // Exit immediately
        }

        // Then check for admin rights
        if (!IsRunAsAdmin())
        {
            RelaunchAsAdmin();
            return;
        }

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // Double check Clumsy is still running before creating the form
        if (!IsTargetApplicationRunning())
        {
            MessageBox.Show(
                "Clumsy is not running!\n\nPlease start Clumsy first, then run this application again.",
                "Clumsy Not Found",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        ClickButton form = new ClickButton();
        _hookID = SetHook(_proc);
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
        try
        {
            // Check for Clumsy process first
            if (!System.Diagnostics.Process.GetProcessesByName("clumsy").Any())
            {
                return false;
            }

            // Then verify we can find the window
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
        catch
        {
            return false;
        }
    }

    public void UpdateStatusLabel()
    {
        fullscreenForm?.UpdateStatusLabel(isActive);
    }

    public void SetKeybind(Keys newKey)
    {
        keybind = newKey;
        config.Keybind = newKey;
        config.Save();
        MessageBox.Show($"Keybind set to: {newKey}", "Keybind Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    public string GetKeybind()
    {
        return keybind.ToString();
    }

    public void ToggleIndicator()
    {
        config.ShowIndicator = !config.ShowIndicator;
        fullscreenForm?.SetIndicatorVisibility(config.ShowIndicator);
        config.Save();
    }

    public bool IsIndicatorVisible()
    {
        return config.ShowIndicator;
    }

    public void ResetToDefault()
    {
        // Reset keybind to Tab
        keybind = Keys.Tab;
        config.Keybind = Keys.Tab;

        // Reset indicator to hidden
        config.ShowIndicator = false;
        fullscreenForm?.SetIndicatorVisibility(false);

        // Save the default settings
        config.Save();

        MessageBox.Show("Settings have been reset to default.", "Reset Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
        if (nCode >= 0)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            if (vkCode == (int)keybind)
            {
                if (wParam == (IntPtr)WM_KEYDOWN && !keyState)
                {
                    keyState = true;
                    // Prevent rapid repeated triggers
                    if (!isProcessingKey && (DateTime.Now - lastKeyPress).TotalMilliseconds > 300)
                    {
                        try
                        {
                            isProcessingKey = true;
                            lastKeyPress = DateTime.Now;
                            instance.SimulateTabPress();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error in HookCallback: " + ex.Message);
                        }
                        finally
                        {
                            isProcessingKey = false;
                        }
                    }
                    return (IntPtr)1;
                }
                else if (wParam == (IntPtr)WM_KEYUP)
                {
                    keyState = false;
                    return (IntPtr)1;
                }
            }
        }
        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }

    private void SimulateTabPress()
    {
        try
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
                    System.Threading.Thread.Sleep(30);
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
        catch (Exception ex)
        {
            Console.WriteLine($"Error in SimulateTabPress: {ex.Message}");
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

        // Hide and close the fullscreen form first
        if (fullscreenForm != null)
        {
            fullscreenForm.Hide();
            fullscreenForm.Close();
            fullscreenForm.Dispose();
        }

        // Ensure the application completely exits
        Application.Exit();
        Environment.Exit(0);
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