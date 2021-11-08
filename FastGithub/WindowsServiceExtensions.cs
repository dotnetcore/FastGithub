using FastGithub.DomainResolve;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PInvoke;
using System;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;

namespace FastGithub
{
    /// <summary>
    /// IHostBuilder扩展
    /// </summary>
    static class WindowsServiceExtensions
    {
        /// <summary>
        /// 控制命令
        /// </summary>
        private enum Command
        {
            Start,
            Stop,
        }

        /// <summary>
        /// 使用windows服务
        /// </summary>
        /// <param name="hostBuilder"></param> 
        /// <returns></returns>
        public static IHostBuilder UseWindowsService(this IHostBuilder hostBuilder)
        {
            var contentRoot = Path.GetDirectoryName(Environment.GetCommandLineArgs().First());
            if (contentRoot != null)
            {
                Environment.CurrentDirectory = contentRoot;
                hostBuilder.UseContentRoot(contentRoot);
            }
            return WindowsServiceLifetimeHostBuilderExtensions.UseWindowsService(hostBuilder);
        }

        /// <summary>
        /// 运行主机
        /// </summary>
        /// <param name="host"></param>
        /// <param name="singleton"></param>
        public static void Run(this IHost host, bool singleton = true)
        {
            if (OperatingSystem.IsWindows() && TryGetCommand(out var cmd))
            {
                try
                {
                    UseCommand(cmd);
                }
                catch (Exception ex)
                {
                    var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
                    loggerFactory.CreateLogger(nameof(FastGithub)).LogError(ex.Message);
                }
            }
            else
            {
                using var mutex = new Mutex(true, "Global\\FastGithub", out var firstInstance);
                if (singleton == false || firstInstance)
                {
                    HostingAbstractionsHostExtensions.Run(host);
                }
            }
        }

        /// <summary>
        /// 获取控制指令
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        private static bool TryGetCommand(out Command cmd)
        {
            var args = Environment.GetCommandLineArgs();
            return Enum.TryParse(args.Skip(1).FirstOrDefault(), true, out cmd);
        }

        /// <summary>
        /// 应用控制指令
        /// </summary> 
        /// <param name="cmd"></param>
        [SupportedOSPlatform("windows")]
        private static void UseCommand(Command cmd)
        {
            var binaryPath = Environment.GetCommandLineArgs().First();
            var serviceName = Path.GetFileNameWithoutExtension(binaryPath);
            var state = true;
            if (cmd == Command.Start)
            {
                state = ServiceInstallUtil.InstallAndStartService(serviceName, binaryPath);
            }
            else if (cmd == Command.Stop)
            {
                state = ServiceInstallUtil.StopAndDeleteService(serviceName);
            }

            if (state == false)
            {
                throw new Win32Exception();
            }
        }

    }
}
