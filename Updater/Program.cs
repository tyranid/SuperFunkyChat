using System;
using System.Windows.Forms;

namespace Updater
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            MessageBox.Show("No updates available", "No Updates", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }
    }
}
