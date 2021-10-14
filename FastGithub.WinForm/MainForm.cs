using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FastGithub.WinForm
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            console = CreateFakeConsole();
            Controls.Add(console);
            var version = ProductionVersion.Current;
            Text = $"{nameof(FastGithub)}(Version {version})【按Enter键清除日志区域】";
            Icon = new Icon("./app.ico");
        }



        private TextBox console;

        private const int maxLength = short.MaxValue;


        private void Write(string? s)
        {

            Action<string> setValueAction = str =>
            {
                if (console.TextLength > maxLength)
                {
                    Clear();
                }
                console.AppendText(str);
            };
            console.Invoke(setValueAction, s);
        }

        private void WriteLine(string? s)
        {
            Write(s);
            Write(Environment.NewLine);
        }

        private void Clear()
        {
            Action setValueAction = () => console.Clear();
            console.Invoke(setValueAction);
        }

        private TextBox CreateFakeConsole()
        {
            console = new()
            {
                Multiline = true,
                Dock = DockStyle.Fill,
                ScrollBars = ScrollBars.Vertical                
            };
            console.KeyPress += Console_KeyPress;
            Console.SetOut(new FakeConsole(Write, WriteLine));
            return console;

        }

        private void Console_KeyPress(object? sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                Clear();
            }
        }

    }
}
