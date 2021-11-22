using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;

namespace FastGithub.Configuration
{
    /// <summary>
    /// 反向代理端口
    /// </summary>
    public static class ReverseProxyPort
    {
        /// <summary>
        /// ssh端口
        /// </summary>
        public static int Ssh { get; }

        /// <summary>
        /// http端口
        /// </summary>
        public static int Http { get; }

        /// <summary>
        /// https端口
        /// </summary>
        public static int Https { get; }

        /// <summary>
        /// 反向代理端口
        /// </summary>
        static ReverseProxyPort()
        {
            var ports = new TcpListenerPortCollection();
            Ssh = OperatingSystem.IsWindows() ? ports.GetAvailablePort(22) : ports.GetAvailablePort(3822);
            Http = OperatingSystem.IsWindows() ? ports.GetAvailablePort(80) : ports.GetAvailablePort(3880);
            Https = OperatingSystem.IsWindows() ? ports.GetAvailablePort(443) : ports.GetAvailablePort(38443);
        }

        /// <summary>
        /// 已监听的tcp端口集合
        /// </summary>
        private class TcpListenerPortCollection
        {
            private readonly HashSet<int> tcpPorts = new();

            /// <summary>
            /// 已监听的tcp端口集合
            /// </summary>
            public TcpListenerPortCollection()
            {
                var tcpListeners = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
                foreach (var endpoint in tcpListeners)
                {
                    this.tcpPorts.Add(endpoint.Port);
                }
            }

            /// <summary>
            /// 获取可用的随机Tcp端口
            /// </summary>
            /// <param name="minValue"></param> 
            /// <returns></returns>
            public int GetAvailablePort(int minValue)
            {
                for (var port = minValue; port < IPEndPoint.MaxPort; port++)
                {
                    if (this.tcpPorts.Contains(port) == false)
                    {
                        return port;
                    }
                }
                throw new FastGithubException("当前无可用的端口");
            }
        }
    }
}
