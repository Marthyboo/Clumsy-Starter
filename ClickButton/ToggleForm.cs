// File: ToggleForm.cs
using System;
using System.Windows.Forms;

public class ToggleForm : Form
{
    private CheckBox toggleCheckBox; // CheckBox to toggle the state
    private ClickButton mainForm; // Reference to the ClickButton instance
    private TextBox keyTextBox; // TextBox to display the keybind
    private Button setKeybindButton; // Button to set the keybind

    public ToggleForm(ClickButton form)
    {
        this.mainForm = form; // Store the reference to the ClickButton instance

        this.toggleCheckBox = new CheckBox
        {
            Text = "Hide Indicator", // Checkbox text
            AutoSize = true,
            Location = new System.Drawing.Point(10, 10)
        };
        this.toggleCheckBox.CheckedChanged += ToggleCheckBox_CheckedChanged; // Event handler for checkbox

        // TextBox for displaying the keybind
        keyTextBox = new TextBox
        {
            Location = new System.Drawing.Point(10, 40),
            Width = 200,
            ReadOnly = true // Make it read-only to prevent manual input
        };

        // Button to set the keybind
        setKeybindButton = new Button
        {
            Text = "Set Keybind",
            Location = new System.Drawing.Point(210, 40)
        };
        setKeybindButton.Click += SetKeybindButton_Click; // Event handler for button

        this.Controls.Add(toggleCheckBox);
        this.Controls.Add(keyTextBox);
        this.Controls.Add(setKeybindButton);
        this.FormBorderStyle = FormBorderStyle.FixedToolWindow; // Fixed window style
        this.StartPosition = FormStartPosition.Manual; // Manual position
        this.Location = new System.Drawing.Point(200, 200); // Initial position
        this.TopMost = true; // Always on top
        this.MouseDown += ToggleForm_MouseDown; // Allow dragging
        this.MouseMove += ToggleForm_MouseMove; // Allow dragging
        this.MouseUp += ToggleForm_MouseUp; // Allow dragging
    }

    private void ToggleCheckBox_CheckedChanged(object sender, EventArgs e)
    {
        // Hide or show the indicator based on the checkbox state
        mainForm.statusLabel.Visible = !toggleCheckBox.Checked; // Hide the indicator if checked
    }

    private void SetKeybindButton_Click(object sender, EventArgs e)
    {
        // Open a new form to set the keybind
        using (var keybindForm = new KeybindForm(mainForm))
        {
            keybindForm.ShowDialog();
            keyTextBox.Text = mainForm.GetKeybind(); // Update the TextBox with the selected key
        }
    }

    // Variables for dragging
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