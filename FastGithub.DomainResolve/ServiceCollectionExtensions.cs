using FastGithub.DomainResolve;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FastGithub
{
    /// <summary>
    /// 服务注册扩展
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 注册域名解析相关服务
        /// </summary>
        /// <param name="services"></param> 
        /// <returns></returns>
        public static IServiceCollection AddDomainResolve(this IServiceCollection services)
        {
            services.TryAddSingleton<DnsClient>();
            services.TryAddSingleton<DnscryptProxy>();
            services.TryAddSingleton<PersistenceService>();
            services.TryAddSingleton<IPAddressService>();
            services.TryAddSingleton<IDomainResolver, DomainResolver>();
            services.AddHostedService<DomainResolveHostedService>();
            return services;
        }
    }
}
