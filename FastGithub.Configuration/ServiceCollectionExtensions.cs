using FastGithub.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;

namespace FastGithub
{
    /// <summary>
    /// 服务注册扩展
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 添加配置服务
        /// </summary>
        /// <param name="services"></param> 
        /// <returns></returns>
        public static IServiceCollection AddConfiguration(this IServiceCollection services)
        {
            TypeConverterBinder.Bind(val => IPAddress.Parse(val), val => val?.ToString());
            TypeConverterBinder.Bind(val => IPEndPoint.Parse(val), val => val?.ToString());

            services.TryAddSingleton<FastGithubConfig>();
            return services;
        }
    }
}
