using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;

namespace FastGithub.UI
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
            using var mutex = new Mutex(true, "Global\\FastGithub.UI", out var isFirstInstance);
            if (isFirstInstance == false)
            {
                return;
            }

            StartFastGithub();
            SetWebBrowserVersion(9000);

            var app = new Application();
            app.StartupUri = new Uri("MainWindow.xaml", UriKind.Relative);
            app.Run();
        }

        /// <summary>
        /// 程序集加载失败时
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private static Assembly? OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            var name = new AssemblyName(args.Name).Name;
            if (name.EndsWith(".resources"))
            {
                return default;
            }

            var stream = Application.GetResourceStream(new Uri($"Resource/{name}.dll", UriKind.Relative)).Stream;
            var buffer = new byte[stream.Length];
            stream.Read(buffer, 0, buffer.Length);
            return Assembly.Load(buffer);
        }

        /// <summary>
        /// 设置浏览器版本
        /// </summary>
        /// <param name="version"></param>
        private static void SetWebBrowserVersion(int version)
        {
            var registry = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION", true);
            var key = $"{Process.GetCurrentProcess().ProcessName}.exe";
            registry.SetValue(key, version, RegistryValueKind.DWord);
        }

        /// <summary>
        /// 启动fastgithub
        /// </summary>
        /// <returns></returns>
        private static void StartFastGithub()
        {
            const string fileName = "fastgithub.exe";
            if (File.Exists(fileName) == false)
            {
                return;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = $"ParentProcessId={Process.GetCurrentProcess().Id} UdpLoggerPort={UdpLogger.Port}",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            Process.Start(startInfo);
        }
    }
}
