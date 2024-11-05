using System;
using System.Security.Principal;
using System.Windows.Forms;
using System.Diagnostics;

namespace ClumsyPresserV
{
    public static class AdminManager
    {
        public static bool IsRunAsAdmin()
        {
            WindowsIdentity id = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(id);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public static void RestartAsAdmin()
        {
            try
            {
                // Get the executable path
                string exePath = Application.ExecutablePath;

                // Create a new process start info
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    WorkingDirectory = Environment.CurrentDirectory,
                    FileName = exePath,
                    Verb = "runas" // This is what requests admin rights
                };

                // Start the new process
                Process.Start(startInfo);

                // Close the current process
                Application.Exit();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error restarting as admin: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
        }

        public static void EnsureAdmin()
        {
            if (!IsRunAsAdmin())
            {
                DialogResult result = MessageBox.Show(
                    "This application requires administrator privileges to function properly.\n\nDo you want to restart as administrator?",
                    "Admin Rights Required",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    RestartAsAdmin();
                }
                else
                {
                    Application.Exit();
                }
            }
        }
    }
}