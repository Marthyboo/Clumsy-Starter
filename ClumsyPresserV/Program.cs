using System;
using System.Windows.Forms;

namespace ClumsyPresserV
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Check for admin rights before starting the application
            AdminManager.EnsureAdmin();

            // Only continue if we have admin rights
            if (AdminManager.IsRunAsAdmin())
            {
                Application.Run(new MainForm());
            }
        }
    }
}