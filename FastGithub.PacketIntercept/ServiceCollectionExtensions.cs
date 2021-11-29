using FastGithub.PacketIntercept;
using FastGithub.PacketIntercept.Dns;
using FastGithub.PacketIntercept.Tcp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Runtime.Versioning;

namespace FastGithub
{
    /// <summary>
    /// 服务注册扩展
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 注册数据包拦截器
        /// </summary>
        /// <param name="services"></param> 
        /// <returns></returns>
        [SupportedOSPlatform("windows")]
        public static IServiceCollection AddPacketIntercept(this IServiceCollection services)
        {
            services.AddSingleton<IDnsConflictSolver, HostsConflictSolver>();
            services.AddSingleton<IDnsConflictSolver, ProxyConflictSolver>();
            services.TryAddSingleton<IDnsInterceptor, DnsInterceptor>();
            services.AddHostedService<DnsInterceptHostedService>();

            services.AddSingleton<ITcpInterceptor, SshInterceptor>();
            services.AddSingleton<ITcpInterceptor, GitInterceptor>();
            services.AddSingleton<ITcpInterceptor, HttpInterceptor>();
            services.AddSingleton<ITcpInterceptor, HttpsInterceptor>();
            services.AddHostedService<TcpInterceptHostedService>();

            return services;
        }
    }
}
