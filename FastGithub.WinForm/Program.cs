using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace FastGithub.WinForm
{
    static class Program
    {
        public static void Main(string[] args)
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var winFormHostBuilder = Host
                .CreateDefaultBuilder(args)
                .UseWinForm<MainForm>()
                .UseWinFormHostLifetime();
            return FastGithub.Program.ConfigureHostBuilder(winFormHostBuilder);
            
        }
    }
}
