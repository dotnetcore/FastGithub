using FastGithub.Configuration;
using Microsoft.Extensions.Logging;
using System.Runtime.Versioning;

namespace FastGithub.PacketIntercept.Tcp
{
    /// <summary>
    /// git拦截器
    /// </summary>   
    [SupportedOSPlatform("windows")]
    sealed class GitInterceptor : TcpInterceptor
    {
        /// <summary>
        /// git拦截器
        /// </summary>
        /// <param name="logger"></param>
        public GitInterceptor(ILogger<HttpInterceptor> logger)
            : base(9418, GlobalListener.GitPort, logger)
        {
        }
    }
}
