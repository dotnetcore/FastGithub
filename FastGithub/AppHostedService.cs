using FastGithub.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub
{
    /// <summary>
    /// app后台服务
    /// </summary>
    sealed class AppHostedService : BackgroundService
    {
        private readonly IHost host;
        private readonly IOptions<AppOptions> appOptions;
        private readonly IOptions<FastGithubOptions> fastGithubOptions;
        private readonly ILogger<AppHostedService> logger;

        public AppHostedService(
            IHost host,
            IOptions<AppOptions> appOptions,
            IOptions<FastGithubOptions> fastGithubOptions,
            ILogger<AppHostedService> logger)
        {
            this.host = host;
            this.appOptions = appOptions;
            this.fastGithubOptions = fastGithubOptions;
            this.logger = logger;
        }

        /// <summary>
        /// 启动完成
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            var version = ProductionVersion.Current;
            this.logger.LogInformation($"{nameof(FastGithub)}启动完成，当前版本为v{version}，访问 https://github.com/dotnetcore/fastgithub 关注新版本");
            return base.StartAsync(cancellationToken);
        }

        /// <summary>
        /// 后台任务
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(1d), stoppingToken);
            await this.CheckFastGithubProxyAsync(stoppingToken);
            await this.WaitForParentProcessExitAsync(stoppingToken);
        }


        /// <summary>
        /// 检测fastgithub代理设置
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task CheckFastGithubProxyAsync(CancellationToken cancellationToken)
        {
            if (OperatingSystem.IsWindows() == false)
            {
                if (await this.UseFastGithubProxyAsync() == false)
                {
                    var httpProxyPort = this.fastGithubOptions.Value.HttpProxyPort;
                    this.logger.LogWarning($"请设置系统自动代理为http://{IPAddress.Loopback}:{httpProxyPort}，或手动代理http/https为{IPAddress.Loopback}:{httpProxyPort}");
                }
            }
        }


        /// <summary>
        /// 应用fastgithub代理
        /// </summary>
        /// <param name="proxyServer"></param>
        /// <param name="httpProxyPort"></param>
        /// <returns></returns>
        private async Task<bool> UseFastGithubProxyAsync()
        {
            var systemProxy = HttpClient.DefaultProxy;
            if (systemProxy == null)
            {
                return false;
            }

            var domain = this.fastGithubOptions.Value.DomainConfigs.Keys.FirstOrDefault();
            if (domain == null)
            {
                return true;
            }

            var destination = new Uri($"https://{domain.Replace('*', 'a')}");
            var proxyServer = systemProxy.GetProxy(destination);
            if (proxyServer == null)
            {
                return false;
            }

            var httpProxyPort = this.fastGithubOptions.Value.HttpProxyPort;
            if (proxyServer.Port != httpProxyPort)
            {
                return false;
            }

            if (IPAddress.TryParse(proxyServer.Host, out var address))
            {
                return IPAddress.IsLoopback(address);
            }

            try
            {
                var addresses = await Dns.GetHostAddressesAsync(proxyServer.Host);
                return addresses.Any(item => IPAddress.IsLoopback(item));
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 等待父进程退出
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task WaitForParentProcessExitAsync(CancellationToken cancellationToken)
        {
            var parentId = this.appOptions.Value.ParentProcessId;
            if (parentId <= 0)
            {
                return;
            }

            try
            {
                Process.GetProcessById(parentId).WaitForExit();
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"获取进程{parentId}异常");
            }
            finally
            {
                this.logger.LogInformation($"正在主动关闭，因为父进程已退出");
                await this.host.StopAsync(cancellationToken);
            }
        }

    }
}
