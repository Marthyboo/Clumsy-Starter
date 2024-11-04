// File: KeybindForm.cs
using System;
using System.Windows.Forms;

public class KeybindForm : Form
{
    private Label instructionLabel;
    private TextBox keyTextBox;
    private Button saveButton;
    private ClickButton mainForm;

    public KeybindForm(ClickButton form)
    {
        this.mainForm = form; // Reference to the main form

        instructionLabel = new Label
        {
            Text = "Press a key to set as the Tab keybind:",
            AutoSize = true,
            Location = new System.Drawing.Point(10, 10)
        };

        keyTextBox = new TextBox
        {
            Location = new System.Drawing.Point(10, 40),
            Width = 220, // Adjusted width
            ReadOnly = true // Make it read-only to prevent manual input
        };

        saveButton = new Button
        {
            Text = "Save",
            Location = new System.Drawing.Point(240, 40),
            Width = 50 // Adjusted width for the button
        };
        saveButton.Click += SaveButton_Click;

        this.Controls.Add(instructionLabel);
        this.Controls.Add(keyTextBox);
        this.Controls.Add(saveButton);
        this.FormBorderStyle = FormBorderStyle.FixedToolWindow;
        this.StartPosition = FormStartPosition.CenterParent;
        this.TopMost = true;

        // Set up key press event
        this.KeyDown += KeybindForm_KeyDown;
        this.KeyPreview = true; // Ensure the form captures key events
        this.ClientSize = new System.Drawing.Size(300, 100); // Adjusted size
    }

    private void KeybindForm_KeyDown(object sender, KeyEventArgs e)
    {
        // Set the keyTextBox to the pressed key
        keyTextBox.Text = e.KeyCode.ToString();
        e.Handled = true; // Prevent further processing of the key
    }

    private void SaveButton_Click(object sender, EventArgs e)
    {
        // Save the keybind to the main form
        if (Enum.TryParse(keyTextBox.Text, out Keys newKey))
        {
            mainForm.SetKeybind(newKey);
            this.Close(); // Close the keybind form
        }
        else
        {
            MessageBox.Show("Invalid key. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}