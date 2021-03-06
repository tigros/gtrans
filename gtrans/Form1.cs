﻿using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace gtrans
{
    public partial class Form1 : Form
    {
        private delegate bool EnumWindowProc(IntPtr hWnd, IntPtr lParam);
        bool gotcha = false;
        bool stoprunning = false;
        IntPtr browserhwnd = IntPtr.Zero;

        public Form1()
        {
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Internet Explorer\MAIN\FeatureControl\FEATURE_BROWSER_EMULATION",
                System.AppDomain.CurrentDomain.FriendlyName, 11001);
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Internet Explorer\MAIN\FeatureControl\FEATURE_BROWSER_EMULATION",
                System.AppDomain.CurrentDomain.FriendlyName.Replace(".exe", ".vshost.exe"), 11001);
            InitializeComponent();
        }

        private string myenc()
        {
            String value = textBox1.Text;
            int limit = 62000;

            StringBuilder sb = new StringBuilder();
            int loops = value.Length / limit;

            for (int i = 0; i <= loops; i++)
            {
                if (i < loops)
                    sb.Append(Uri.EscapeDataString(value.Substring(limit * i, limit)));
                else
                    sb.Append(Uri.EscapeDataString(value.Substring(limit * i)));
            }

            return sb.ToString();
        }

        private ArrayList getlist()
        {
            string enc = textBox1.Text;
            ArrayList a = new ArrayList();
            int pos = 0;

            string s = "";
            int len = enc.Length;
            int limit = 2000;

            while (true)
            {
                if (pos + limit < len)
                {
                    s = enc.Substring(pos, limit);
                    int p = s.LastIndexOf(" ");
                    s = s.Substring(0, p);
                    a.Add(s);
                    pos += p + 1;
                }
                else
                {
                    a.Add(enc.Substring(pos));
                    break;
                }
            }

            return a;
        }

        private void newbrow()
        {
            this.SuspendLayout();
            textBox1.SuspendLayout();
            textBox1.Enabled = false;
            Size savesize = new Size(webBrowser1.Size.Width, webBrowser1.Size.Height); 
            webBrowser1.DocumentCompleted -= new WebBrowserDocumentCompletedEventHandler(webBrowser1_DocumentCompleted);
            this.Controls.Remove(this.webBrowser1);
            webBrowser1.Dispose();
            webBrowser1 = null;
            this.webBrowser1 = new System.Windows.Forms.WebBrowser();
            this.webBrowser1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.webBrowser1.Location = new System.Drawing.Point(1048, 25);
            this.webBrowser1.MinimumSize = new System.Drawing.Size(20, 20);
            this.webBrowser1.Name = "webBrowser1";
            this.webBrowser1.Size = savesize;
            this.webBrowser1.TabIndex = 2;
            this.Controls.Add(this.webBrowser1);
            webBrowser1.DocumentCompleted += new WebBrowserDocumentCompletedEventHandler(webBrowser1_DocumentCompleted);
            webBrowser1.ScriptErrorsSuppressed = true;
            textBox1.Enabled = true;
            textBox1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
            browserhwnd = IntPtr.Zero;
        }

        private bool EnumWindow(IntPtr hWnd, IntPtr lParam)
        {
            GCHandle gcChildhandlesList = GCHandle.FromIntPtr(lParam);

            if (gcChildhandlesList == null || gcChildhandlesList.Target == null)
            {
                return false;
            }

            List<IntPtr> childHandles = gcChildhandlesList.Target as List<IntPtr>;
            childHandles.Add(hWnd);

            StringBuilder sb = new StringBuilder(100);
            IntPtr result = GetClassName(hWnd, sb, sb.Capacity);
            if (result != IntPtr.Zero && sb.ToString() == "Internet Explorer_Server")
                browserhwnd = hWnd;

            return true;
        }

        public void getbrowserhwnd(IntPtr parent)
        {
            List<IntPtr> childHandles = new List<IntPtr>();

            GCHandle gcChildhandlesList = GCHandle.Alloc(childHandles);
            IntPtr pointerChildHandlesList = GCHandle.ToIntPtr(gcChildhandlesList);

            try
            {
                EnumWindowProc childProc = new EnumWindowProc(EnumWindow);
                EnumChildWindows(parent, childProc, pointerChildHandlesList);
            }
            finally
            {
                gcChildhandlesList.Free();
            }
        }

        private void sendctrlv()
        {
            uint WM_COMMAND = 0x0111;
            int pastecmd = 0x01001A;
            PostMessage(browserhwnd, WM_COMMAND, pastecmd, 0);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (button1.Text == "Stop")
            {
                stoprunning = true;
                button1.Text = "Go";
                return;
            }

            stoprunning = false;
            button1.Text = "Stop";

            textBox2.Text = "";
            string inner = "";
            StringBuilder sb = new StringBuilder();
            int count = 0;

            try
            {
                string s = @"https://translate.google.com/#view=home&op=translate&sl=auto&tl=en&text=";
                Uri u = new Uri(s);
                ArrayList al = getlist();
                webBrowser1.DocumentCompleted -= new WebBrowserDocumentCompletedEventHandler(webBrowser1_DocumentCompleted);
                webBrowser1.DocumentCompleted += new WebBrowserDocumentCompletedEventHandler(webBrowser1_DocumentCompleted);
                webBrowser1.ScriptErrorsSuppressed = true;

                foreach (string x in al)
                {
                    if (stoprunning)
                        break;

                    webBrowser1.Url = u;

                    gotcha = false;
                    while (!gotcha)
                    {
                        Application.DoEvents();
                        Thread.Sleep(50);
                    }

                    if (browserhwnd == IntPtr.Zero)
                        getbrowserhwnd(Process.GetCurrentProcess().MainWindowHandle);

                    if (x == "")
                        continue;
                    Clipboard.SetText(x);
                    webBrowser1.Focus();
                    sendctrlv();

                    inner = "";
                    var t1 = new System.Windows.Forms.Timer { Enabled = true, Interval = 2000 };
                    t1.Tick += (o, a) =>
                    {
                        HtmlElementCollection hc = webBrowser1.Document.GetElementsByTagName("span");
                        foreach (HtmlElement element in hc)
                        {
                            if (element.GetAttribute("className") == "tlid-translation translation")
                            {
                                inner = element.InnerText.Trim();
                                if (inner != "")
                                    t1.Stop();
                            }
                        }
                    };

                    while (t1.Enabled && !stoprunning)
                    {
                        Application.DoEvents();
                        Thread.Sleep(50);
                    }
                    t1.Stop();

                    sb.Append(" " + inner);
                    count++;
                    if (count % 10 == 0)
                    {
                        textBox2.AppendText(" " + sb.ToString());
                        sb.Clear();
                        if (count % 100 == 0)
                            newbrow();
                        GC.Collect();
                    }
                }                
                textBox2.AppendText(" " + sb.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

            button1.Text = "Go";
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog of = new OpenFileDialog();
                of.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
                of.FilterIndex = 1;
                of.RestoreDirectory = true;
                if (of.ShowDialog() == DialogResult.OK)
                {
                    textBox1.Text = File.ReadAllText(of.FileName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            gotcha = true;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                SaveFileDialog saveFileDialog1 = new SaveFileDialog();

                saveFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
                saveFileDialog1.FilterIndex = 1;
                saveFileDialog1.RestoreDirectory = true;

                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(saveFileDialog1.FileName, textBox2.Text);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            stoprunning = true;
        }

        [DllImport("user32.dll")]
        static extern bool PostMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);
       
        [DllImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumChildWindows(IntPtr window, EnumWindowProc callback, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    }
}
