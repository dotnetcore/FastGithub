using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.PacketIntercept
{
    /// <summary>
    /// dns拦截后台服务
    /// </summary>
    [SupportedOSPlatform("windows")]
    sealed class DnsInterceptHostedService : BackgroundService
    {
        private readonly IDnsInterceptor dnsInterceptor;
        private readonly IEnumerable<IDnsConflictSolver> conflictSolvers;
        private readonly ILogger<DnsInterceptHostedService> logger;
        private readonly IHost host;

        /// <summary>
        /// dns拦截后台服务
        /// </summary>
        /// <param name="dnsInterceptor"></param>
        /// <param name="conflictSolvers"></param>
        /// <param name="logger"></param>
        /// <param name="host"></param>
        public DnsInterceptHostedService(
            IDnsInterceptor dnsInterceptor,
            IEnumerable<IDnsConflictSolver> conflictSolvers,
            ILogger<DnsInterceptHostedService> logger,
            IHost host)
        {
            this.dnsInterceptor = dnsInterceptor;
            this.conflictSolvers = conflictSolvers;
            this.logger = logger;
            this.host = host;
        }

        /// <summary>
        /// 启动时处理冲突
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {          
            foreach (var solver in this.conflictSolvers)
            {
                await solver.SolveAsync(cancellationToken);
            }
            await base.StartAsync(cancellationToken);
        }

        /// <summary>
        /// 停止时恢复冲突
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            foreach (var solver in this.conflictSolvers)
            {
                await solver.RestoreAsync(cancellationToken);
            }
            await base.StopAsync(cancellationToken);
        }

        /// <summary>
        /// dns后台
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await this.dnsInterceptor.InterceptAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 995)
            {
            }
            catch (Exception ex)
            { 
                this.logger.LogError(ex, "dns拦截器异常");
                await this.host.StopAsync(stoppingToken);
            }
        }
    }
}
