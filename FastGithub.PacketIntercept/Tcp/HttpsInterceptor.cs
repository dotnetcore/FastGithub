using FastGithub.Configuration;
using Microsoft.Extensions.Logging;
using System.Runtime.Versioning;

namespace FastGithub.PacketIntercept.Tcp
{
    /// <summary>
    /// https拦截器
    /// </summary>   
    [SupportedOSPlatform("windows")]
    sealed class HttpsInterceptor : TcpInterceptor
    {
        /// <summary>
        /// https拦截器
        /// </summary>
        /// <param name="logger"></param>
        public HttpsInterceptor(ILogger<HttpsInterceptor> logger)
            : base(443, GlobalListener.HttpsPort, logger)
        {
        }
    }
}
