using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace FastGithub.UI
{
    static class UdpLogger
    {
        private static readonly byte[] buffer = new byte[ushort.MaxValue];
        private static readonly Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp);

        /// <summary>
        /// 获取日志端口
        /// </summary>
        public static int Port { get; } = GetAvailableUdpPort(38457);


        static UdpLogger()
        {
            socket.Bind(new IPEndPoint(IPAddress.Loopback, Port));
        }

        /// <summary>
        /// 获取可用的随机Udp端口
        /// </summary>
        /// <param name="minValue"></param>
        /// <param name="addressFamily"></param>
        /// <returns></returns>
        private static int GetAvailableUdpPort(int minValue, AddressFamily addressFamily = AddressFamily.InterNetwork)
        {
            var hashSet = new HashSet<int>();
            var tcpListeners = IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners();

            foreach (var endpoint in tcpListeners)
            {
                if (endpoint.AddressFamily == addressFamily)
                {
                    hashSet.Add(endpoint.Port);
                }
            }

            for (var port = minValue; port < IPEndPoint.MaxPort; port++)
            {
                if (hashSet.Contains(port) == false)
                {
                    return port;
                }
            }

            throw new ArgumentException("当前无可用的端口");
        }

        public static async Task<UdpLog?> GetUdpLogAsync()
        {
            EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
            var taskCompletionSource = new TaskCompletionSource<int>();
            socket.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref remoteEP, EndReceiveFrom, taskCompletionSource);
            var length = await taskCompletionSource.Task;

            var json = Encoding.UTF8.GetString(buffer, 0, length);
            return JsonConvert.DeserializeObject<UdpLog>(json);
        }

        private static void EndReceiveFrom(IAsyncResult ar)
        {
            EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
            var length = socket.EndReceiveFrom(ar, ref remoteEP);
            var taskCompletionSource = (TaskCompletionSource<int>)ar.AsyncState;
            taskCompletionSource.TrySetResult(length);
        }
    }
}
