using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Reflection;
using CefSharp;
using CefSharp.WinForms;
using System.Resources;

namespace Messenger_
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            InitBrowser();
            FormClosing += new FormClosingEventHandler(Form1_Closing);
            title.MouseDown += new MouseEventHandler(title_MouseDown);
            title.MouseUp += new MouseEventHandler(title_MouseUp);
        }

        public ChromiumWebBrowser browser;
        public void InitBrowser()
        {
            using (CefSettings settings = new CefSettings())
            {
                settings.CachePath = Path.GetTempPath() + @"\CEF";
                Cef.Initialize(settings);
            }

            browser = new ChromiumWebBrowser("https://messenger.com");
            Controls.Add(browser);
            browser.Dock = DockStyle.None;
            browser.Width = 520;
            browser.Height = 550;
            browser.Location = new Point(3, 20);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string path = Application.StartupPath + @"\location.dat";

            if (File.Exists(path))
            {
                using (StreamReader f = File.OpenText(path))
                {
                    string s = f.ReadLine();
                    f.Close();

                    int x = int.Parse(s.Split(',')[0]);
                    int y = int.Parse(s.Split(',')[1]);
                    Location = new Point(x, y);
                }
            }


            GlobalKeyboardHook gHook = new GlobalKeyboardHook();
            gHook.KeyDown += new KeyEventHandler(Form1_KeyDown);
            foreach (Keys key in Enum.GetValues(typeof(Keys))) gHook.HookedKeys.Add(key);

            gHook.hook();
        }

        public bool shift = false;
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape && this.ContainsFocus) Application.Exit();
            else if (e.KeyCode == Keys.LShiftKey || e.KeyCode == Keys.RShiftKey)
            {
                shift = true;
                shiftTimer.Enabled = true;
            }
            else if (shift)
            {
                switch (e.KeyCode)
                {
                    case Keys.Tab:
                        if (Visible == true) Visible = false;
                        else Visible = true;
                        break;
                }
            }
        }

        private void ShiftTimer_Tick(object sender, EventArgs e)
        {
            shift = false;
        }

        public int offsetX, offsetY;
        private void DragTimer_Tick(object sender, EventArgs e)
        {
            Location = new Point(Cursor.Position.X - offsetX, Cursor.Position.Y - offsetY);
        }

        private void title_MouseDown(object sender, MouseEventArgs e)
        {
            offsetX = (Cursor.Position.X - Location.X);
            offsetY = (Cursor.Position.Y - Location.Y);
            dragTimer.Enabled = true;
        }
        
        private void title_MouseUp(object sender, MouseEventArgs e)
        {
            dragTimer.Enabled = false;
        }

        private void Form1_Closing(object sender, FormClosingEventArgs e)
        {
            string path = Application.StartupPath + @"\location.dat";

            if (File.Exists(path)) File.Delete(path);
            using (StreamWriter f = File.CreateText(path))
            {
                f.WriteLine(Location.X + "," + Location.Y);
                f.Close();
            }
        }
    }
}
