using System;
using System.IO;
using System.Text;

namespace FastGithub.WinForm
{

    public class FakeConsole : TextWriter
    {
        private readonly Action<string> _Write;
        private readonly Action<string> _WriteLine;

 
        public FakeConsole(Action<string> write, Action<string> writeLine)
        {
            _Write = write;
            _WriteLine = writeLine;
        }

        public FakeConsole(Action<string> write)
        {
            _Write = write;
            _WriteLine = write;
        }

        public override Encoding Encoding => Encoding.Unicode;

        public override void Write(string? value)
        {
            if (value is null)
            {
                return;
            }

            _Write(value);
        }

        public override void WriteLine(string? value)
        {
            if (value is null)
            {
                return;
            }
            _WriteLine(value);
        }
    }
}