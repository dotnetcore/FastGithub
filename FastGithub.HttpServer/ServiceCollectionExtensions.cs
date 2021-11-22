using FastGithub.HttpServer;
using Microsoft.Extensions.DependencyInjection;
namespace FastGithub
{
    /// <summary>
    /// http反向代理的服务注册扩展
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 添加http反向代理
        /// </summary>
        /// <param name="services"></param> 
        /// <returns></returns>
        public static IServiceCollection AddReverseProxy(this IServiceCollection services)
        {
            return services
                .AddMemoryCache()
                .AddHttpForwarder()
                .AddSingleton<CertService>()
                .AddSingleton<ICaCertInstaller, CaCertInstallerOfMacOS>()
                .AddSingleton<ICaCertInstaller, CaCertInstallerOfWindows>()
                .AddSingleton<ICaCertInstaller, CaCertInstallerOfLinuxRedHat>()
                .AddSingleton<ICaCertInstaller, CaCertInstallerOfLinuxDebian>()
                .AddSingleton<HttpProxyMiddleware>()
                .AddSingleton<RequestLoggingMiddleware>()
                .AddSingleton<HttpReverseProxyMiddleware>();
        }
    }
}
