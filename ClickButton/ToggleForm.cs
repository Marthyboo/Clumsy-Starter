using System;
using System.Drawing;
using System.Windows.Forms;

public class ToggleForm : Form
{
    private CheckBox toggleCheckBox;
    private ClickButton mainForm;
    private TextBox keyTextBox;
    private Button setKeybindButton;
    private Button resetButton;
    private Button exitButton;

    public ToggleForm(ClickButton form)
    {
        this.mainForm = form;

        // Make the toggle form close when main form closes
        form.FormClosing += (s, e) => this.Close();

        // Add FormClosing event handler
        this.FormClosing += ToggleForm_FormClosing;

        this.toggleCheckBox = new CheckBox
        {
            Text = "Hide Indicator",
            AutoSize = true,
            Location = new Point(10, 10),
            Checked = !form.IsIndicatorVisible()
        };
        this.toggleCheckBox.CheckedChanged += ToggleCheckBox_CheckedChanged;

        keyTextBox = new TextBox
        {
            Location = new Point(10, 40),
            Width = 100,
            ReadOnly = true,
            Text = form.GetKeybind()
        };

        setKeybindButton = new Button
        {
            Text = "Set Keybind",
            Location = new Point(120, 38),
            Width = 100
        };
        setKeybindButton.Click += SetKeybindButton_Click;

        resetButton = new Button
        {
            Text = "Reset to Default",
            Location = new Point(10, 70),
            Width = 100
        };
        resetButton.Click += ResetButton_Click;

        // Exit button
        exitButton = new Button
        {
            Text = "Force Exit",
            Location = new Point(120, 70),
            Width = 100,
            BackColor = Color.FromArgb(255, 80, 80),  // Light red color
            ForeColor = Color.White
        };
        exitButton.Click += ExitButton_Click;

        this.Controls.AddRange(new Control[] {
            toggleCheckBox,
            keyTextBox,
            setKeybindButton,
            resetButton,
            exitButton
        });

        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;
        this.Text = "Settings";
        this.Size = new Size(240, 150);
        this.StartPosition = FormStartPosition.CenterScreen;

        // Enable form dragging
        this.MouseDown += ToggleForm_MouseDown;
        this.MouseMove += ToggleForm_MouseMove;
        this.MouseUp += ToggleForm_MouseUp;
    }

    private void ToggleForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        // If it's a user closing the form (clicking X)
        if (e.CloseReason == CloseReason.UserClosing)
        {
            // Show confirmation dialog
            var result = MessageBox.Show(
                "Are you sure you want to exit the application?",
                "Confirm Exit",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                // Force exit the application
                Environment.Exit(0);
            }
            else
            {
                // Cancel the close if user clicks No
                e.Cancel = true;
            }
        }
    }

    private void ExitButton_Click(object sender, EventArgs e)
    {
        // Show confirmation dialog
        var result = MessageBox.Show(
            "Are you sure you want to force exit the application?",
            "Confirm Exit",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result == DialogResult.Yes)
        {
            // Force exit the application
            Environment.Exit(0);
        }
    }

    private void ToggleCheckBox_CheckedChanged(object sender, EventArgs e)
    {
        mainForm.ToggleIndicator();
    }

    private bool isSettingKeybind = false;

    private void SetKeybindButton_Click(object sender, EventArgs e)
    {
        if (!isSettingKeybind)
        {
            isSettingKeybind = true;
            setKeybindButton.Text = "Press Key...";
            keyTextBox.Text = "...";
            this.KeyPreview = true;
            this.KeyDown += ToggleForm_KeyDown;
        }
    }

    private void ToggleForm_KeyDown(object sender, KeyEventArgs e)
    {
        if (isSettingKeybind)
        {
            mainForm.SetKeybind(e.KeyCode);
            keyTextBox.Text = e.KeyCode.ToString();
            setKeybindButton.Text = "Set Keybind";
            isSettingKeybind = false;
            this.KeyDown -= ToggleForm_KeyDown;
        }
    }

    private void ResetButton_Click(object sender, EventArgs e)
    {
        mainForm.ResetToDefault();
        keyTextBox.Text = mainForm.GetKeybind();
        toggleCheckBox.Checked = !mainForm.IsIndicatorVisible();
    }

    private bool dragging = false;
    private int dragCursorX;
    private int dragCursorY;
    private int dragFormX;
    private int dragFormY;

    private void ToggleForm_MouseDown(object sender, MouseEventArgs e)
    {
        dragging = true;
        dragCursorX = Cursor.Position.X;
        dragCursorY = Cursor.Position.Y;
        dragFormX = this.Left;
        dragFormY = this.Top;
    }

    private void ToggleForm_MouseMove(object sender, MouseEventArgs e)
    {
        if (dragging)
        {
            int deltaX = Cursor.Position.X - dragCursorX;
            int deltaY = Cursor.Position.Y - dragCursorY;
            this.Left = dragFormX + deltaX;
            this.Top = dragFormY + deltaY;
        }
    }

    private void ToggleForm_MouseUp(object sender, MouseEventArgs e)
    {
        dragging = false;
    }
}