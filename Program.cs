using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Threading;

namespace AlephClipboard
{
    static class Program
    {
        private static Mutex m_Mutex;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            bool createdNew;
            m_Mutex = new Mutex(true, "AlephClipboardMutex", out createdNew);
            if (createdNew)
            {
                Form f = new AlephClipboard();
                Application.Run();
            }
            else
            {
                MessageBox.Show("Application is already running.", Application.ProductName,
                  MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }
    }
}
