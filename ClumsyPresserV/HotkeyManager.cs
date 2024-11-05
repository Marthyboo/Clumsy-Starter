using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace ClumsyPresserV
{
    public class HotkeyManager
    {
        private ComboBox hotkeySelector;
        private Label hotkeyLabel;
        private Keys triggerKey = Keys.Tab;
        private bool isChangingHotkey = false;
        private Label statusLabel;
        private Form parentForm;

        public Keys CurrentHotkey => triggerKey;

        public HotkeyManager(Form parent, Label statusLabel)
        {
            this.parentForm = parent;
            this.statusLabel = statusLabel;
            SetupHotkeyControls();
            LoadHotkey();
        }

        private void SetupHotkeyControls()
        {
            hotkeyLabel = new Label
            {
                Location = new System.Drawing.Point(10, 140),
                Size = new System.Drawing.Size(100, 20),
                Text = "Hotkey:"
            };

            hotkeySelector = new ComboBox
            {
                Location = new System.Drawing.Point(110, 140),
                Size = new System.Drawing.Size(120, 20),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            var commonKeys = new Keys[]
            {
                // Original keys
                Keys.Tab,
                Keys.Space,
                Keys.F1, Keys.F2, Keys.F3, Keys.F4, Keys.F5,
                Keys.F6, Keys.F7, Keys.F8, Keys.F9, Keys.F10,
                Keys.F11, Keys.F12,
                Keys.Insert, Keys.Delete,
                Keys.Home, Keys.End,
                Keys.PageUp, Keys.PageDown,

                // Number keys
                Keys.D0, Keys.D1, Keys.D2, Keys.D3, Keys.D4,
                Keys.D5, Keys.D6, Keys.D7, Keys.D8, Keys.D9,

                // Numpad keys
                Keys.NumPad0, Keys.NumPad1, Keys.NumPad2, Keys.NumPad3, Keys.NumPad4,
                Keys.NumPad5, Keys.NumPad6, Keys.NumPad7, Keys.NumPad8, Keys.NumPad9,
                Keys.Multiply, Keys.Add, Keys.Subtract, Keys.Divide,

                // Letter keys
                Keys.A, Keys.B, Keys.C, Keys.D, Keys.E, Keys.F, Keys.G, Keys.H, Keys.I,
                Keys.J, Keys.K, Keys.L, Keys.M, Keys.N, Keys.O, Keys.P, Keys.Q, Keys.R,
                Keys.S, Keys.T, Keys.U, Keys.V, Keys.W, Keys.X, Keys.Y, Keys.Z,

                // Special keys
                Keys.OemMinus, Keys.Oemplus,
                Keys.OemOpenBrackets, Keys.OemCloseBrackets,
                Keys.OemPipe, Keys.OemSemicolon, Keys.OemQuotes,
                Keys.Oemcomma, Keys.OemPeriod, Keys.OemQuestion,
                Keys.Pause, Keys.PrintScreen,
                
                // Arrow keys
                Keys.Left, Keys.Right, Keys.Up, Keys.Down
            };

            // Create a dictionary to map keys to display names
            var keyDisplayNames = new System.Collections.Generic.Dictionary<Keys, string>
            {
                { Keys.D0, "0" }, { Keys.D1, "1" }, { Keys.D2, "2" }, { Keys.D3, "3" }, { Keys.D4, "4" },
                { Keys.D5, "5" }, { Keys.D6, "6" }, { Keys.D7, "7" }, { Keys.D8, "8" }, { Keys.D9, "9" },
                { Keys.NumPad0, "Numpad 0" }, { Keys.NumPad1, "Numpad 1" }, { Keys.NumPad2, "Numpad 2" },
                { Keys.NumPad3, "Numpad 3" }, { Keys.NumPad4, "Numpad 4" }, { Keys.NumPad5, "Numpad 5" },
                { Keys.NumPad6, "Numpad 6" }, { Keys.NumPad7, "Numpad 7" }, { Keys.NumPad8, "Numpad 8" },
                { Keys.NumPad9, "Numpad 9" },
                { Keys.Multiply, "Numpad *" }, { Keys.Add, "Numpad +" },
                { Keys.Subtract, "Numpad -" }, { Keys.Divide, "Numpad /" },
                { Keys.OemMinus, "-" }, { Keys.Oemplus, "=" },
                { Keys.OemOpenBrackets, "[" }, { Keys.OemCloseBrackets, "]" },
                { Keys.OemPipe, "\\" }, { Keys.OemSemicolon, ";" },
                { Keys.OemQuotes, "'" }, { Keys.Oemcomma, "," },
                { Keys.OemPeriod, "." }, { Keys.OemQuestion, "/" }
            };

            // Add items to the combo box with custom display names
            foreach (var key in commonKeys)
            {
                string displayName;
                if (keyDisplayNames.TryGetValue(key, out displayName))
                {
                    hotkeySelector.Items.Add(new KeyValuePair<Keys, string>(key, displayName));
                }
                else
                {
                    hotkeySelector.Items.Add(new KeyValuePair<Keys, string>(key, key.ToString()));
                }
            }

            hotkeySelector.DisplayMember = "Value";
            hotkeySelector.ValueMember = "Key";

            // Find and select the current trigger key
            for (int i = 0; i < hotkeySelector.Items.Count; i++)
            {
                var item = (KeyValuePair<Keys, string>)hotkeySelector.Items[i];
                if (item.Key == triggerKey)
                {
                    hotkeySelector.SelectedIndex = i;
                    break;
                }
            }

            hotkeySelector.SelectedIndexChanged += HotkeySelector_SelectedIndexChanged;

            parentForm.Controls.Add(hotkeyLabel);
            parentForm.Controls.Add(hotkeySelector);
        }

        private void HotkeySelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!isChangingHotkey)
            {
                isChangingHotkey = true;
                var selectedPair = (KeyValuePair<Keys, string>)hotkeySelector.SelectedItem;

                var result = MessageBox.Show(
                    $"Change hotkey to {selectedPair.Value}?",
                    "Confirm Hotkey Change",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    triggerKey = selectedPair.Key;
                    statusLabel.Text = $"Hotkey changed to {selectedPair.Value}";
                    SaveHotkey();
                }
                else
                {
                    // Find and select the previous trigger key
                    for (int i = 0; i < hotkeySelector.Items.Count; i++)
                    {
                        var item = (KeyValuePair<Keys, string>)hotkeySelector.Items[i];
                        if (item.Key == triggerKey)
                        {
                            hotkeySelector.SelectedIndex = i;
                            break;
                        }
                    }
                }
                isChangingHotkey = false;
            }
        }

        public void SaveHotkey()
        {
            Properties.Settings.Default.TriggerKey = triggerKey.ToString();
            Properties.Settings.Default.Save();
        }

        private void LoadHotkey()
        {
            string savedKey = Properties.Settings.Default.TriggerKey;
            if (!string.IsNullOrEmpty(savedKey))
            {
                try
                {
                    triggerKey = (Keys)Enum.Parse(typeof(Keys), savedKey);
                    // Find and select the saved trigger key
                    for (int i = 0; i < hotkeySelector.Items.Count; i++)
                    {
                        var item = (KeyValuePair<Keys, string>)hotkeySelector.Items[i];
                        if (item.Key == triggerKey)
                        {
                            hotkeySelector.SelectedIndex = i;
                            break;
                        }
                    }
                }
                catch
                {
                    triggerKey = Keys.Tab;
                    // Select the default Tab key
                    for (int i = 0; i < hotkeySelector.Items.Count; i++)
                    {
                        var item = (KeyValuePair<Keys, string>)hotkeySelector.Items[i];
                        if (item.Key == triggerKey)
                        {
                            hotkeySelector.SelectedIndex = i;
                            break;
                        }
                    }
                }
            }
        }
    }
}