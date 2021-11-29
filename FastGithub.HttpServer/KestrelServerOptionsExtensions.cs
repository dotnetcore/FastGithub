using FastGithub.Configuration;
using FastGithub.HttpServer;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;

namespace FastGithub
{
    /// <summary>
    /// Kestrel扩展
    /// </summary>
    public static class KestrelServerOptionsExtensions
    {
        /// <summary>
        /// 无限制
        /// </summary>
        /// <param name="kestrel"></param>
        public static void NoLimit(this KestrelServerOptions kestrel)
        {
            kestrel.Limits.MaxRequestBodySize = null;
            kestrel.Limits.MinResponseDataRate = null;
            kestrel.Limits.MinRequestBodyDataRate = null;
        }

        /// <summary>
        /// 监听http代理
        /// </summary>
        /// <param name="kestrel"></param>
        public static void ListenHttpProxy(this KestrelServerOptions kestrel)
        {
            var options = kestrel.ApplicationServices.GetRequiredService<IOptions<FastGithubOptions>>().Value;
            var httpProxyPort = options.HttpProxyPort;

            if (GlobalListener.CanListenTcp(httpProxyPort) == false)
            {
                throw new FastGithubException($"tcp端口{httpProxyPort}已经被其它进程占用，请在配置文件更换{nameof(FastGithubOptions.HttpProxyPort)}为其它端口");
            }

            var logger = kestrel.GetLogger();
            kestrel.ListenLocalhost(httpProxyPort);
            logger.LogInformation($"已监听http://localhost:{httpProxyPort}，http代理服务启动完成");
        }

        /// <summary>
        /// 监听ssh反向代理
        /// </summary>
        /// <param name="kestrel"></param>
        public static void ListenSshReverseProxy(this KestrelServerOptions kestrel)
        {
            var sshPort = GlobalListener.SshPort;
            kestrel.ListenLocalhost(sshPort, listen =>
            {
                listen.UseFlowAnalyze();
                listen.UseConnectionHandler<GithubSshReverseProxyHandler>();
            });

            kestrel.GetLogger().LogInformation($"已监听ssh://localhost:{sshPort}，github的ssh反向代理服务启动完成");
        }

        /// <summary>
        /// 监听git反向代理
        /// </summary>
        /// <param name="kestrel"></param>
        public static void ListenGitReverseProxy(this KestrelServerOptions kestrel)
        {
            var gitPort = GlobalListener.GitPort;
            kestrel.ListenLocalhost(gitPort, listen =>
            {
                listen.UseFlowAnalyze();
                listen.UseConnectionHandler<GithubGitReverseProxyHandler>();
            });

            kestrel.GetLogger().LogInformation($"已监听git://localhost:{gitPort}，github的git反向代理服务启动完成");
        }

        /// <summary>
        /// 监听http反向代理
        /// </summary>
        /// <param name="kestrel"></param>
        public static void ListenHttpReverseProxy(this KestrelServerOptions kestrel)
        {
            var httpPort = GlobalListener.HttpPort;
            kestrel.ListenLocalhost(httpPort);

            if (OperatingSystem.IsWindows())
            {
                kestrel.GetLogger().LogInformation($"已监听http://localhost:{httpPort}，http反向代理服务启动完成");
            }
        }

        /// <summary>
        /// 监听https反向代理
        /// </summary>
        /// <param name="kestrel"></param>
        /// <exception cref="FastGithubException"></exception>
        public static void ListenHttpsReverseProxy(this KestrelServerOptions kestrel)
        {
            var certService = kestrel.ApplicationServices.GetRequiredService<CertService>();
            certService.CreateCaCertIfNotExists();
            certService.InstallAndTrustCaCert();

            var httpsPort = GlobalListener.HttpsPort;
            kestrel.ListenLocalhost(httpsPort, listen =>
            {
                if (OperatingSystem.IsWindows())
                {
                    listen.UseFlowAnalyze();
                }
                listen.UseHttps(https =>
                {
                    https.ServerCertificateSelector = (ctx, domain) => certService.GetOrCreateServerCert(domain);
                });
            });

            if (OperatingSystem.IsWindows())
            {
                var logger = kestrel.GetLogger();
                logger.LogInformation($"已监听https://localhost:{httpsPort}，https反向代理服务启动完成");
            }
        }

        /// <summary>
        /// 获取日志
        /// </summary>
        /// <param name="kestrel"></param>
        /// <returns></returns>
        private static ILogger GetLogger(this KestrelServerOptions kestrel)
        {
            var loggerFactory = kestrel.ApplicationServices.GetRequiredService<ILoggerFactory>();
            return loggerFactory.CreateLogger($"{nameof(FastGithub)}.{nameof(HttpServer)}");
        }
    }
}
