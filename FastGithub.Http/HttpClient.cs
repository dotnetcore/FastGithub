using FastGithub.Configuration;
using FastGithub.DomainResolve;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.Http
{
    /// <summary>
    /// 表示http客户端
    /// </summary>
    public class HttpClient : HttpMessageInvoker
    {
        /// <summary>
        /// 插入的UserAgent标记
        /// </summary>
        private readonly static ProductInfoHeaderValue userAgent = new(new ProductHeaderValue(nameof(FastGithub), "1.0"));

        /// <summary>
        /// http客户端
        /// </summary>
        /// <param name="domainConfig"></param>
        /// <param name="domainResolver"></param>
        public HttpClient(DomainConfig domainConfig, IDomainResolver domainResolver)
            : this(new HttpClientHandler(domainConfig, domainResolver), disposeHandler: true)
        {
        }

        /// <summary>
        /// http客户端
        /// </summary> 
        /// <param name="handler"></param>
        /// <param name="disposeHandler"></param>
        public HttpClient(HttpMessageHandler handler, bool disposeHandler)
            : base(handler, disposeHandler)
        {
        }

        /// <summary>
        /// 发送请求
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Headers.UserAgent.Contains(userAgent))
            {
                throw new FastGithubException($"由于{request.RequestUri}实际指向了{nameof(FastGithub)}自身，{nameof(FastGithub)}已中断本次转发");
            }
            request.Headers.UserAgent.Add(userAgent);
            var response = await base.SendAsync(request, cancellationToken);
            response.Headers.Server.TryParseAdd(nameof(FastGithub));
            return response;
        }
    }
}