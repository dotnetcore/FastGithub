using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.PacketIntercept
{
    /// <summary>
    /// tcp拦截后台服务
    /// </summary>
    [SupportedOSPlatform("windows")]
    sealed class TcpInterceptHostedService : BackgroundService
    {
        private readonly IEnumerable<ITcpInterceptor> tcpInterceptors;
        private readonly ILogger<TcpInterceptHostedService> logger;
        private readonly IHost host;

        /// <summary>
        /// tcp拦截后台服务
        /// </summary>
        /// <param name="tcpInterceptors"></param>
        /// <param name="logger"></param>
        /// <param name="host"></param>
        public TcpInterceptHostedService(
            IEnumerable<ITcpInterceptor> tcpInterceptors,
            ILogger<TcpInterceptHostedService> logger,
            IHost host)
        {
            this.tcpInterceptors = tcpInterceptors;
            this.logger = logger;
            this.host = host;
        }

        /// <summary>
        /// https后台
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                var tasks = this.tcpInterceptors.Select(item => item.InterceptAsync(stoppingToken));
                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 995)
            {
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "tcp拦截器异常");
                await this.host.StopAsync(stoppingToken);
            }
        }
    }
}
