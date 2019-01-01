using Microsoft.Win32;
using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace gtrans
{
    public partial class Form1 : Form
    {
        bool gotcha = false;
        bool stoprunning = false;

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
            string enc = myenc();
            ArrayList a = new ArrayList();
            int pos = 0;

            string s = "";
            int len = enc.Length;
            int limit = 1900;

            while (true)
            {
                if (pos + limit < len)
                {
                    s = enc.Substring(pos, limit);
                    int p = s.LastIndexOf("%20");
                    s = s.Substring(0, p);
                    a.Add(s);
                    pos += p + 3;
                }
                else
                {
                    a.Add(enc.Substring(pos));
                    break;
                }
            }

            return a;
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
                ArrayList al = getlist();
                webBrowser1.DocumentCompleted -= new WebBrowserDocumentCompletedEventHandler(webBrowser1_DocumentCompleted);
                webBrowser1.DocumentCompleted += new WebBrowserDocumentCompletedEventHandler(webBrowser1_DocumentCompleted);
                webBrowser1.ScriptErrorsSuppressed = true;

                foreach (string x in al)
                {
                    if (stoprunning)
                        break;

                    Uri u = new Uri(s + x);
                    webBrowser1.Url = u;

                    gotcha = false;
                    while (!gotcha)
                    {
                        Application.DoEvents();
                        Thread.Sleep(50);
                    }

                    inner = "";
                    var t1 = new System.Windows.Forms.Timer { Enabled = true, Interval = 1000 };
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
                    }
                }
                
                textBox2.AppendText(" " + sb.ToString());
                button1.Text = "Go";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
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
    }
}
