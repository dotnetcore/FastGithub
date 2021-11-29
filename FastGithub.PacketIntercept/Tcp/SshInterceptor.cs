using FastGithub.Configuration;
using Microsoft.Extensions.Logging;
using System.Runtime.Versioning;

namespace FastGithub.PacketIntercept.Tcp
{
    /// <summary>
    /// ssh拦截器
    /// </summary>   
    [SupportedOSPlatform("windows")]
    sealed class SshInterceptor : TcpInterceptor
    {
        /// <summary>
        /// ssh拦截器
        /// </summary>
        /// <param name="logger"></param>
        public SshInterceptor(ILogger<HttpInterceptor> logger)
            : base(22, GlobalListener.SshPort, logger)
        {
        }
    }
}
