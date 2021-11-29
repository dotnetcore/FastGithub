using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;

namespace FastGithub.Configuration
{
    /// <summary>
    /// 监听器
    /// </summary>
    public static class GlobalListener
    {
        private static readonly IPGlobalProperties global = IPGlobalProperties.GetIPGlobalProperties();
        private static readonly HashSet<int> tcpListenPorts = GetListenPorts(global.GetActiveTcpListeners);
        private static readonly HashSet<int> udpListenPorts = GetListenPorts(global.GetActiveUdpListeners);

        /// <summary>
        /// ssh端口
        /// </summary>
        public static int SshPort { get; } = GetAvailableTcpPort(22);

        /// <summary>
        /// git端口
        /// </summary>
        public static int GitPort { get; } = GetAvailableTcpPort(9418);

        /// <summary>
        /// http端口
        /// </summary>
        public static int HttpPort { get; } = OperatingSystem.IsWindows() ? GetAvailableTcpPort(80) : GetAvailableTcpPort(3880);

        /// <summary>
        /// https端口
        /// </summary>
        public static int HttpsPort { get; } = OperatingSystem.IsWindows() ? GetAvailableTcpPort(443) : GetAvailableTcpPort(38443);

        /// <summary>
        /// 获取已监听的端口
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        private static HashSet<int> GetListenPorts(Func<IPEndPoint[]> func)
        {
            var hashSet = new HashSet<int>();
            try
            {
                foreach (var endpoint in func())
                {
                    hashSet.Add(endpoint.Port);
                }
            }
            catch (Exception)
            {
            }
            return hashSet;
        }

        /// <summary>
        /// 是可以监听TCP
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public static bool CanListenTcp(int port)
        {
            return tcpListenPorts.Contains(port) == false;
        }

        /// <summary>
        /// 是可以监听UDP
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public static bool CanListenUdp(int port)
        {
            return udpListenPorts.Contains(port) == false;
        }

        /// <summary>
        /// 是可以监听TCP和Udp
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public static bool CanListen(int port)
        {
            return CanListenTcp(port) && CanListenUdp(port);
        }

        /// <summary>
        /// 获取可用的随机Tcp端口
        /// </summary>
        /// <param name="minPort"></param> 
        /// <returns></returns>
        public static int GetAvailableTcpPort(int minPort)
        {
            return GetAvailablePort(CanListenTcp, minPort);
        }

        /// <summary>
        /// 获取可用的随机Udp端口
        /// </summary>
        /// <param name="minPort"></param> 
        /// <returns></returns>
        public static int GetAvailableUdpPort(int minPort)
        {
            return GetAvailablePort(CanListenUdp, minPort);
        }

        /// <summary>
        /// 获取可用的随机端口
        /// </summary>
        /// <param name="minPort"></param> 
        /// <returns></returns>
        public static int GetAvailablePort(int minPort)
        {
            return GetAvailablePort(CanListen, minPort);
        }

        /// <summary>
        /// 获取可用端口
        /// </summary>
        /// <param name="canFunc"></param>
        /// <param name="minPort"></param>
        /// <returns></returns>
        /// <exception cref="FastGithubException"></exception>
        private static int GetAvailablePort(Func<int, bool> canFunc, int minPort)
        {
            for (var port = minPort; port < IPEndPoint.MaxPort; port++)
            {
                if (canFunc(port) == true)
                {
                    return port;
                }
            }
            throw new FastGithubException("当前无可用的端口");
        }
    }
}
