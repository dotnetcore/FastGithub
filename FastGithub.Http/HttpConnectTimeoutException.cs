using System;
using System.Net;

namespace FastGithub.Http
{
    /// <summary>
    /// http连接超时异常
    /// </summary>
    sealed class HttpConnectTimeoutException : Exception
    {
        /// <summary>
        /// http连接超时异常
        /// </summary>
        /// <param name="address">连接的ip</param>
        public HttpConnectTimeoutException(IPAddress address)
            : base(address.ToString())
        {

        }
    }
}
