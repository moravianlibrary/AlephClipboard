using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;

namespace AlephClipboard
{
    public class AlephClipboard : Form
    {
        #region DLL imports for communication with Clipboard
        [DllImport("user32.dll")]
        protected static extern int SetClipboardViewer(int hWndNewViewer);

        [DllImport("user32.dll")]
        public static extern bool ChangeClipboardChain(IntPtr hWndRemove, IntPtr hWndNewNext);

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hwnd, int wMsg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        internal static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll")]
        internal static extern bool CloseClipboard();

        [DllImport("user32.dll")]
        internal static extern bool SetClipboardData(uint uFormat, IntPtr data);

        [DllImport("user32.dll")]
        public static extern uint RegisterClipboardFormat(string format);
        #endregion

        private NotifyIcon trayIcon;
        private ContextMenu trayMenu;
        private RichTextBox richTextBox1;
        IntPtr nextClipboardViewer;
        
        // Constructor
        public AlephClipboard()
        {
            InitializeComponent();

            // Create tray menu with ShowApp and Exit
            trayMenu = new ContextMenu();
            trayMenu.MenuItems.Add("Exit", OnExit);

            trayIcon = new NotifyIcon();
            trayIcon.Text = "ALEPH Clipboard";
            this.Text = trayIcon.Text;
            trayIcon.MouseClick += TrayIcon_Click;
            // nice icons
            Stream icon = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("AlephClipboard.favicon.ico");
            trayIcon.Icon = new Icon(icon);
            this.Icon = trayIcon.Icon;

            // Add menu to tray icon and show it.
            trayIcon.ContextMenu = trayMenu;
            trayIcon.Visible = true;

            // create clipboard chain
            nextClipboardViewer = (IntPtr)SetClipboardViewer((int)this.Handle);
        }

        // Override actions for minimize and close buttons and signals by Clipboard
        protected override void WndProc(ref Message m)
        {
            const int WM_SYSCOMMAND = 0x0112;
            const int SC_MINIMIZE = 0xf020;
            const int SC_CLOSE = 0xF060;

            // defined in winuser.h
            const int WM_DRAWCLIPBOARD = 0x308;
            const int WM_CHANGECBCHAIN = 0x030D;

            switch (m.Msg)
            {
                case WM_DRAWCLIPBOARD:
                    DisplayClipboardData();
                    SendMessage(nextClipboardViewer, m.Msg, m.WParam, m.LParam);
                    break;

                case WM_CHANGECBCHAIN:
                    if (m.WParam == nextClipboardViewer)
                    {
                        nextClipboardViewer = m.LParam;
                    }
                    else
                    {
                        SendMessage(nextClipboardViewer, m.Msg, m.WParam, m.LParam);
                    }
                    break;
                
                case WM_SYSCOMMAND:
                    if (m.WParam.ToInt32() == SC_MINIMIZE || m.WParam.ToInt32() == SC_CLOSE)
                    {
                        Hide();
                    }
                    else
                    {
                        base.WndProc(ref m);
                    }
                    break;          

                default:
                    base.WndProc(ref m);
                    break;
            }
        }


        // Displays aleph data stored in clipboard or message, that no such data found
        void DisplayClipboardData()
        {
            try
            {
                IDataObject iData = new DataObject();
                iData = Clipboard.GetDataObject();

                if (iData.GetDataPresent(DataFormats.Text))
                {
                    string data = (string)iData.GetData(DataFormats.Text);
                    string formatName = (data.StartsWith("FMT   L")) ? "ALEPH_DOC" : string.Empty;
                    formatName = (data.StartsWith("008   L")) ? "ALEPH_TAG" : formatName;
                    
                    if (!string.IsNullOrEmpty(formatName))
                    {
                        // fix line endings
                        if (!data.Contains("\r\n"))
                        {
                            data = data.Replace("\n", "\r\n");
                        }
                        if (!data.EndsWith("\r\n"))
                        {
                            data += "\r\n";
                        }
                    
                        // change to byteArray, allocate global memory, copy byteArray to this memory
                        byte[] dataMemory = Encoding.UTF8.GetBytes(data);
                        OpenClipboard(IntPtr.Zero);
                        IntPtr ptr = Marshal.AllocHGlobal(dataMemory.Length);
                        Marshal.Copy(dataMemory, 0, ptr, dataMemory.Length);
                        uint formatId = RegisterClipboardFormat(formatName);
                        bool success = SetClipboardData(formatId, ptr);
                        CloseClipboard();

                        if (!success)
                        {
                            Marshal.FreeHGlobal(ptr);
                            richTextBox1.ForeColor = Color.Red;
                            richTextBox1.Text = DateTime.Now.ToString("HH:mm:ss") + " - Error occured during saving to clipboard";
                        }
                        else
                        {
                            richTextBox1.ForeColor = Color.Green;
                            richTextBox1.Text = DateTime.Now.ToString("HH:mm:ss") + " - OK";
                            richTextBox1.Text += "\r\n" + data;
                        }
                    }
                    else
                    {
                        richTextBox1.ForeColor = Color.Black;
                        richTextBox1.Text = DateTime.Now.ToString("HH:mm:ss") + " - Text in clipboard is not for Aleph";
                    }
                }
                else
                {
                    richTextBox1.ForeColor = Color.Black;
                    richTextBox1.Text = DateTime.Now.ToString("HH:mm:ss") + " - No text in clipboard";
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }


        #region Basic logic for UI
        // Show/Hide application window on click
        private void TrayIcon_Click(object sender, MouseEventArgs e)
        {
            if (this.Visible == false)
            {
                Show();
            }
            else
            {
                Hide();
            }
        }

        // Hide application immediatelly after load
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Hide();
        }

        // Close application
        private void OnExit(object sender, EventArgs e)
        {
            Application.Exit();
        }

        // Release resources (icons)
        protected override void Dispose(bool isDisposing)
        {
            ChangeClipboardChain(this.Handle, nextClipboardViewer);
            if (isDisposing)
            {
                // Release the icon resources.
                trayIcon.Dispose();
                this.Icon.Dispose();
            }

            base.Dispose(isDisposing);
        }
        #endregion

        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.SuspendLayout();
            // 
            // richTextBox1
            // 
            this.richTextBox1.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.richTextBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBox1.Location = new System.Drawing.Point(0, 0);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.ReadOnly = true;
            this.richTextBox1.Size = new System.Drawing.Size(292, 273);
            this.richTextBox1.TabIndex = 0;
            this.richTextBox1.Text = "";
            this.richTextBox1.WordWrap = false;
            // 
            // SysTrayApp
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(292, 273);
            this.Controls.Add(this.richTextBox1);
            this.Name = "SysTrayApp";
            this.ResumeLayout(false);

        }
        #endregion
    }
}
