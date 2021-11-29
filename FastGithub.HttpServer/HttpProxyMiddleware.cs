using FastGithub.Configuration;
using FastGithub.DomainResolve;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Forwarder;

namespace FastGithub.HttpServer
{
    /// <summary>
    /// http代理中间件
    /// </summary>
    sealed class HttpProxyMiddleware
    {
        private const string LOCALHOST = "localhost";
        private const int HTTP_PORT = 80;
        private const int HTTPS_PORT = 443;

        private readonly FastGithubConfig fastGithubConfig;
        private readonly IDomainResolver domainResolver;
        private readonly IHttpForwarder httpForwarder;
        private readonly HttpReverseProxyMiddleware httpReverseProxy;

        private readonly HttpMessageInvoker defaultHttpClient;
        private readonly TimeSpan connectTimeout = TimeSpan.FromSeconds(10d);

        static HttpProxyMiddleware()
        {
            // https://github.com/dotnet/aspnetcore/issues/37421
            var authority = typeof(HttpParser<>).Assembly
                .GetType("Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.HttpCharacters")?
                .GetField("_authority", BindingFlags.NonPublic | BindingFlags.Static)?
                .GetValue(null);

            if (authority is bool[] authorityArray)
            {
                authorityArray['-'] = true;
            }
        }

        /// <summary>
        /// http代理中间件
        /// </summary>
        /// <param name="fastGithubConfig"></param>
        /// <param name="domainResolver"></param>
        /// <param name="httpForwarder"></param>
        /// <param name="httpReverseProxy"></param>
        public HttpProxyMiddleware(
            FastGithubConfig fastGithubConfig,
            IDomainResolver domainResolver,
            IHttpForwarder httpForwarder,
            HttpReverseProxyMiddleware httpReverseProxy)
        {
            this.fastGithubConfig = fastGithubConfig;
            this.domainResolver = domainResolver;
            this.httpForwarder = httpForwarder;
            this.httpReverseProxy = httpReverseProxy;

            this.defaultHttpClient = new HttpMessageInvoker(CreateDefaultHttpHandler(), disposeHandler: false);
        }

        /// <summary>
        /// 处理请求
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task InvokeAsync(HttpContext context)
        {
            var host = context.Request.Host;
            if (this.IsFastGithubServer(host) == true)
            {
                var proxyPac = this.CreateProxyPac(host);
                context.Response.ContentType = "application/x-ns-proxy-autoconfig";
                context.Response.Headers.Add("Content-Disposition", $"attachment;filename=proxy.pac");
                await context.Response.WriteAsync(proxyPac);
            }
            else if (context.Request.Method == HttpMethods.Connect)
            {
                var cancellationToken = context.RequestAborted;
                using var connection = await this.CreateConnectionAsync(host, cancellationToken);
                var responseFeature = context.Features.Get<IHttpResponseFeature>();
                if (responseFeature != null)
                {
                    responseFeature.ReasonPhrase = "Connection Established";
                }
                context.Response.StatusCode = StatusCodes.Status200OK;
                await context.Response.CompleteAsync();

                var transport = context.Features.Get<IConnectionTransportFeature>()?.Transport;
                if (transport != null)
                {
                    var task1 = connection.CopyToAsync(transport.Output, cancellationToken);
                    var task2 = transport.Input.CopyToAsync(connection, cancellationToken);
                    await Task.WhenAny(task1, task2);
                }
            }
            else
            {
                await this.httpReverseProxy.InvokeAsync(context, async next =>
                {
                    var destinationPrefix = $"{context.Request.Scheme}://{context.Request.Host}";
                    await this.httpForwarder.SendAsync(context, destinationPrefix, this.defaultHttpClient);
                });
            }
        }

        /// <summary>
        /// 是否为fastgithub服务
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        private bool IsFastGithubServer(HostString host)
        {
            if (host.Port != this.fastGithubConfig.HttpProxyPort)
            {
                return false;
            }

            if (host.Host == LOCALHOST)
            {
                return true;
            }

            return IPAddress.TryParse(host.Host, out var address) && IPAddress.IsLoopback(address);
        }

        /// <summary>
        /// 创建proxypac脚本
        /// </summary>
        /// <param name="proxyHost"></param>
        /// <returns></returns>
        private string CreateProxyPac(HostString proxyHost)
        {
            var buidler = new StringBuilder();
            buidler.AppendLine("function FindProxyForURL(url, host){");
            buidler.AppendLine($"    var fastgithub = 'PROXY {proxyHost}';");
            foreach (var domain in this.fastGithubConfig.GetDomainPatterns())
            {
                buidler.AppendLine($"    if (shExpMatch(host, '{domain}')) return fastgithub;");
            }
            buidler.AppendLine("    return 'DIRECT';");
            buidler.AppendLine("}");
            return buidler.ToString();
        }

        /// <summary>
        /// 创建连接
        /// </summary>
        /// <param name="host"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="AggregateException"></exception>
        private async Task<Stream> CreateConnectionAsync(HostString host, CancellationToken cancellationToken)
        {
            var innerExceptions = new List<Exception>();
            await foreach (var endPoint in this.GetUpstreamEndPointsAsync(host, cancellationToken))
            {
                var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    using var timeoutTokenSource = new CancellationTokenSource(this.connectTimeout);
                    using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutTokenSource.Token);
                    await socket.ConnectAsync(endPoint, linkedTokenSource.Token);
                    return new NetworkStream(socket, ownsSocket: false);
                }
                catch (Exception ex)
                {
                    socket.Dispose();
                    cancellationToken.ThrowIfCancellationRequested();
                    innerExceptions.Add(ex);
                }
            }
            throw new AggregateException($"无法连接到{host}", innerExceptions);
        }

        /// <summary>
        /// 获取目标终节点
        /// </summary>
        /// <param name="host"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async IAsyncEnumerable<EndPoint> GetUpstreamEndPointsAsync(HostString host, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var targetHost = host.Host;
            var targetPort = host.Port ?? HTTPS_PORT;

            if (IPAddress.TryParse(targetHost, out var address) == true)
            {
                yield return new IPEndPoint(address, targetPort);
            }
            else if (this.fastGithubConfig.IsMatch(targetHost) == false)
            {
                yield return new DnsEndPoint(targetHost, targetPort);
            }
            else if (targetPort == HTTP_PORT)
            {
                yield return new IPEndPoint(IPAddress.Loopback, GlobalListener.HttpPort);
            }
            else if (targetPort == HTTPS_PORT)
            {
                yield return new IPEndPoint(IPAddress.Loopback, GlobalListener.HttpsPort);
            }
            else
            {
                var dnsEndPoint = new DnsEndPoint(targetHost, targetPort);
                await foreach (var item in this.domainResolver.ResolveAsync(dnsEndPoint, cancellationToken))
                {
                    yield return new IPEndPoint(item, targetPort);
                }
            }
        }

        /// <summary>
        /// 创建httpHandler
        /// </summary>
        /// <returns></returns>
        private static SocketsHttpHandler CreateDefaultHttpHandler()
        {
            return new()
            {
                Proxy = null,
                UseProxy = false,
                UseCookies = false,
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.None
            };
        }
    }
}