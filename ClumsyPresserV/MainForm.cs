using System;
using System.Windows.Forms;
using System.Windows.Automation;
using System.Threading.Tasks;
using System.Diagnostics;
using ClumsyPresserV;

namespace ClumsyPresserV
{
    public partial class MainForm : Form
    {
        private GlobalKeyboardHook _globalKeyboardHook;
        private Label statusLabel;
        private AutomationElement cachedWindow = null;
        private DateTime lastClickTime = DateTime.MinValue;
        private const int CLICK_DELAY_MS = 200;

        private NumericUpDown timerSelector;
        private CheckBox useTimerCheckbox;
        private System.Windows.Forms.Timer autoTimer;
        private Label timerLabel;
        private Stopwatch precisionTimer;
        private Label countdownLabel;
        private HotkeyManager hotkeyManager;
        private bool isProcessing = false;

        // New fields for window handling
        private bool isFullscreen = false;
        private bool wasFullscreen = false;
        private const int WH_SHELL = 10;
        private const int HSHELL_WINDOWACTIVATED = 4;
        private const int HSHELL_RUDEAPPACTIVATED = 32772;

        public MainForm()
        {
            InitializeComponent();
            Task.Run(() => FindClumsyWindow());
            SetupUI();
            SetupKeyboardHook();
            precisionTimer = new Stopwatch();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WH_SHELL)
            {
                if (m.WParam.ToInt32() == HSHELL_WINDOWACTIVATED ||
                    m.WParam.ToInt32() == HSHELL_RUDEAPPACTIVATED)
                {
                    isProcessing = false;
                    lastClickTime = DateTime.MinValue;
                    cachedWindow = null; // Reset window cache on focus change

                    bool currentFullscreen = IsAnyWindowFullscreen();
                    if (currentFullscreen != wasFullscreen)
                    {
                        wasFullscreen = currentFullscreen;
                        isFullscreen = currentFullscreen;
                        isProcessing = false;
                        lastClickTime = DateTime.MinValue;
                        cachedWindow = null; // Reset window cache on fullscreen change
                    }
                }
            }
            base.WndProc(ref m);
        }

        private bool IsAnyWindowFullscreen()
        {
            var screen = Screen.PrimaryScreen;
            var topWindow = AutomationElement.RootElement.FindFirst(
                TreeScope.Children,
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window));

            if (topWindow != null)
            {
                try
                {
                    var rect = topWindow.Current.BoundingRectangle;
                    return rect.Width >= screen.Bounds.Width && rect.Height >= screen.Bounds.Height;
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }

        private void SetupUI()
        {
            this.SuspendLayout();

            statusLabel = new Label
            {
                Location = new System.Drawing.Point(10, 10),
                Size = new System.Drawing.Size(300, 60),
                Text = "Initializing..."
            };

            useTimerCheckbox = new CheckBox
            {
                Location = new System.Drawing.Point(10, 80),
                Size = new System.Drawing.Size(100, 20),
                Text = "Use Timer",
                Checked = false,
                TabStop = false
            };

            timerLabel = new Label
            {
                Location = new System.Drawing.Point(10, 110),
                Size = new System.Drawing.Size(100, 20),
                Text = "Seconds:"
            };

            timerSelector = new NumericUpDown
            {
                Location = new System.Drawing.Point(110, 110),
                Size = new System.Drawing.Size(60, 20),
                Minimum = 1,
                Maximum = 10,
                Value = 8,
                Enabled = false,
                TabStop = false,
                DecimalPlaces = 1,
                Increment = 0.1M
            };

            countdownLabel = new Label
            {
                Location = new System.Drawing.Point(180, 110),
                Size = new System.Drawing.Size(100, 20),
                Text = ""
            };

            autoTimer = new System.Windows.Forms.Timer
            {
                Enabled = false,
                Interval = 50
            };
            autoTimer.Tick += AutoTimer_Tick;

            useTimerCheckbox.CheckedChanged += (s, e) =>
            {
                timerSelector.Enabled = useTimerCheckbox.Checked;
            };

            hotkeyManager = new HotkeyManager(this, statusLabel);

            this.Controls.AddRange(new Control[]
            {
                statusLabel,
                useTimerCheckbox,
                timerLabel,
                timerSelector,
                countdownLabel
            });

            this.Size = new System.Drawing.Size(350, 230);
            this.ResumeLayout(false);

            UpdateStatus("Ready - Press TAB to start");
        }

        private void UpdateStatus(string message)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => UpdateStatus(message)));
                return;
            }
            statusLabel.Text = message;
        }

        private void AutoTimer_Tick(object sender, EventArgs e)
        {
            if (!precisionTimer.IsRunning) return;

            double remainingSeconds = (double)((decimal)timerSelector.Value * 1000m - (decimal)precisionTimer.ElapsedMilliseconds) / 1000.0;

            if (remainingSeconds <= 0)
            {
                StopTimer();
                ClickClumsyButton();
            }
            else
            {
                if (this.InvokeRequired)
                {
                    this.BeginInvoke(new Action(() => countdownLabel.Text = $"{remainingSeconds:F1}s"));
                }
                else
                {
                    countdownLabel.Text = $"{remainingSeconds:F1}s";
                }
            }
        }

        private void StopTimer()
        {
            autoTimer.Stop();
            precisionTimer.Stop();
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => countdownLabel.Text = ""));
            }
            else
            {
                countdownLabel.Text = "";
            }
        }

        private void FindClumsyWindow()
     {
         try
         {
             AutomationElement desktop = AutomationElement.RootElement;

             // Find all windows
             var allWindows = desktop.FindAll(
                 TreeScope.Children,
                 new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window));

             foreach (AutomationElement window in allWindows)
             {
                 try
                 {
                     string windowName = window.Current.Name.ToLower();
                     if (windowName.StartsWith("clumsy") && windowName.Contains("."))
                     {
                         cachedWindow = window;
                         break;
                     }
                 }
                 catch
                 {
                     continue;
                 }
             }
         }
         catch
         {
             cachedWindow = null;
         }
     }
     
        private void SetupKeyboardHook()
        {
            _globalKeyboardHook = new GlobalKeyboardHook();
            _globalKeyboardHook.KeyboardPressed += OnKeyPressed;
        }

        private void OnKeyPressed(object sender, GlobalKeyboardHookEventArgs e)
        {
            if (e.KeyboardData.Key == hotkeyManager.CurrentHotkey &&
                e.KeyboardState == GlobalKeyboardHook.KeyboardState.KeyDown)
            {
                // Remove the time check since it might be causing the initial delay
                if (!isProcessing)
                {
                    isProcessing = true;
                    // Force window detection on first press
                    if (cachedWindow == null)
                    {
                        FindClumsyWindow();
                    }
                    HandleTabPress();
                    lastClickTime = DateTime.Now;
                    isProcessing = false;
                }
                e.Handled = true;
            }
        }

        private void HandleTabPress()
        {
            try
            {
                // Always try to find the window first
                FindClumsyWindow();

                bool wasStartButton = IsStartButtonVisible();
                if (ClickClumsyButton())
                {
                    if (useTimerCheckbox.Checked && wasStartButton)
                    {
                        precisionTimer.Reset();
                        precisionTimer.Start();
                        autoTimer.Start();
                        UpdateStatus($"Timer started for {timerSelector.Value} seconds");
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error: {ex.Message}");
            }
        }

        private bool IsStartButtonVisible()
        {
            try
            {
                if (cachedWindow == null || !IsWindowValid(cachedWindow)) return false;

                var startButton = cachedWindow.FindFirst(
                    TreeScope.Descendants,
                    new AndCondition(
                        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button),
                        new PropertyCondition(AutomationElement.NameProperty, "Start")
                    ));

                return startButton != null;
            }
            catch
            {
                return false;
            }
        }

        private bool ClickClumsyButton()
        {
            try
            {
                // Always try to find the window first if it's not valid
                if (cachedWindow == null || !IsWindowValid(cachedWindow))
                {
                    FindClumsyWindow();
                }

                if (cachedWindow != null)
                {
                    // Try to find either button immediately
                    AutomationElement button = null;

                    // Try to find Stop button first
                    button = cachedWindow.FindFirst(
                        TreeScope.Descendants,
                        new AndCondition(
                            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button),
                            new PropertyCondition(AutomationElement.NameProperty, "Stop")
                        ));

                    // If Stop button not found, look for Start button
                    if (button == null)
                    {
                        button = cachedWindow.FindFirst(
                            TreeScope.Descendants,
                            new AndCondition(
                                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button),
                                new PropertyCondition(AutomationElement.NameProperty, "Start")
                            ));
                    }

                    if (button != null)
                    {
                        InvokePattern invokePattern = button.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
                        invokePattern.Invoke();
                        UpdateStatus($"Clicked {button.Current.Name}");
                        return true;
                    }
                    else
                    {
                        // If no button found, invalidate the window cache
                        cachedWindow = null;
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error: {ex.Message}");
                cachedWindow = null;
            }
            return false;
        }

        private bool IsWindowValid(AutomationElement element)
        {
            try
            {
                return element.Current.IsEnabled;
            }
            catch
            {
                return false;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            hotkeyManager.SaveHotkey();
            autoTimer?.Stop();
            autoTimer?.Dispose();
            _globalKeyboardHook?.Dispose();
            precisionTimer?.Stop();
            base.OnFormClosing(e);
        }
    }
}
