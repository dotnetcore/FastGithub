using FastGithub.DomainResolve;
using Microsoft.AspNetCore.Connections;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace FastGithub.HttpServer
{
    /// <summary>
    /// github的ssh代理处理者
    /// </summary>
    sealed class SshReverseProxyHandler : ConnectionHandler
    {
        private readonly IDomainResolver domainResolver;
        private readonly DnsEndPoint sshOverHttpsEndPoint = new("ssh.github.com", 443);

        /// <summary>
        /// github的ssh代理处理者
        /// </summary>
        /// <param name="domainResolver"></param>
        public SshReverseProxyHandler(IDomainResolver domainResolver)
        {
            this.domainResolver = domainResolver;
        }

        /// <summary>
        /// ssh连接后
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task OnConnectedAsync(ConnectionContext context)
        {
            var address = await this.domainResolver.ResolveAnyAsync(this.sshOverHttpsEndPoint);
            using var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            await socket.ConnectAsync(address, this.sshOverHttpsEndPoint.Port);
            var targetStream = new NetworkStream(socket, ownsSocket: false);

            var task1 = targetStream.CopyToAsync(context.Transport.Output);
            var task2 = context.Transport.Input.CopyToAsync(targetStream);
            await Task.WhenAny(task1, task2);
        }
    }
}
