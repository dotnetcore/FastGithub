using FastGithub.DomainResolve;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PInvoke;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;

namespace FastGithub
{
    /// <summary>
    /// IHostBuilder扩展
    /// </summary>
    static class ServiceExtensions
    {
        /// <summary>
        /// 控制命令
        /// </summary>
        private enum Command
        {
            Start,
            Stop,
        }

        [SupportedOSPlatform("linux")]
        [DllImport("libc", SetLastError = true)]
        private static extern uint geteuid();

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
            var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(FastGithub));
            if (UseCommand(logger) == false)
            {
                using var mutex = new Mutex(true, "Global\\FastGithub", out var firstInstance);
                if (singleton == false || firstInstance)
                {
                    HostingAbstractionsHostExtensions.Run(host);
                }
                else
                {
                    logger.LogWarning($"程序将自动关闭：系统已运行其它实例");
                }
            }
        }

        /// <summary>
        /// 使用命令
        /// </summary>
        /// <param name="logger"></param>
        /// <returns></returns>
        private static bool UseCommand(ILogger logger)
        {
            var args = Environment.GetCommandLineArgs();
            if (Enum.TryParse<Command>(args.Skip(1).FirstOrDefault(), true, out var cmd) == false)
            {
                return false;
            }

            var action = cmd == Command.Start ? "启动" : "停止";
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    UseCommandAtWindows(cmd);
                }
                else if (OperatingSystem.IsLinux())
                {
                    UseCommandAtLinux(cmd);
                }
                else
                {
                    return false;
                }
                logger.LogInformation($"服务{action}成功");
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message, $"服务{action}异常");
            }
            return true;
        }

        /// <summary>
        /// 应用控制指令
        /// </summary> 
        /// <param name="cmd"></param>
        [SupportedOSPlatform("windows")]
        private static void UseCommandAtWindows(Command cmd)
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

        /// <summary>
        /// 应用控制指令
        /// </summary> 
        /// <param name="cmd"></param>
        [SupportedOSPlatform("linux")]
        private static void UseCommandAtLinux(Command cmd)
        {
            if (geteuid() != 0)
            {
                throw new UnauthorizedAccessException("无法操作服务：没有root权限");
            }

            var binaryPath = Path.GetFullPath(Environment.GetCommandLineArgs().First());
            var serviceName = Path.GetFileNameWithoutExtension(binaryPath);
            var serviceFilePath = $"/etc/systemd/system/{serviceName}.service";

            if (cmd == Command.Start)
            {
                var serviceBuilder = new StringBuilder()
                    .AppendLine("[Unit]")
                    .AppendLine($"Description={serviceName}")
                    .AppendLine()
                    .AppendLine("[Service]")
                    .AppendLine("Type=notify")
                    .AppendLine($"User={Environment.UserName}")
                    .AppendLine($"ExecStart={binaryPath}")
                    .AppendLine($"WorkingDirectory={Path.GetDirectoryName(binaryPath)}")
                    .AppendLine()
                    .AppendLine("[Install]")
                    .AppendLine("WantedBy=multi-user.target");
                File.WriteAllText(serviceFilePath, serviceBuilder.ToString());

                Process.Start("chcon", $"--type=bin_t {binaryPath}").WaitForExit(); // SELinux
                Process.Start("systemctl", "daemon-reload").WaitForExit();
                Process.Start("systemctl", $"start {serviceName}.service").WaitForExit();
                Process.Start("systemctl", $"enable {serviceName}.service").WaitForExit();
            }
            else if (cmd == Command.Stop)
            {
                Process.Start("systemctl", $"stop {serviceName}.service").WaitForExit();
                Process.Start("systemctl", $"disable {serviceName}.service").WaitForExit();

                if (File.Exists(serviceFilePath))
                {
                    File.Delete(serviceFilePath);
                }
                Process.Start("systemctl", "daemon-reload").WaitForExit();
            }
        }
    }
}
