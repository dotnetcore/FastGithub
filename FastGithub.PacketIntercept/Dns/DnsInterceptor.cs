using DNS.Protocol;
using DNS.Protocol.ResourceRecords;
using FastGithub.Configuration;
using FastGithub.WinDiverts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.PacketIntercept.Dns
{
    /// <summary>
    /// dns拦截器
    /// </summary>   
    [SupportedOSPlatform("windows")]
    sealed class DnsInterceptor : IDnsInterceptor
    {
        private const string DNS_FILTER = "udp.DstPort == 53";

        private readonly FastGithubConfig fastGithubConfig;
        private readonly ILogger<DnsInterceptor> logger;

        private readonly TimeSpan ttl = TimeSpan.FromMinutes(5d);

        /// <summary>
        /// 刷新DNS缓存
        /// </summary>    
        [DllImport("dnsapi.dll", EntryPoint = "DnsFlushResolverCache", SetLastError = true)]
        private static extern void DnsFlushResolverCache();

        /// <summary>
        /// 首次加载驱动往往有异常，所以要提前加载
        /// </summary>
        static DnsInterceptor()
        {
            var handle = WinDivert.WinDivertOpen("false", WinDivertLayer.Network, 0, WinDivertOpenFlags.None);
            WinDivert.WinDivertClose(handle);
        }

        /// <summary>
        /// dns拦截器
        /// </summary>
        /// <param name="fastGithubConfig"></param>
        /// <param name="logger"></param>
        /// <param name="options"></param>
        public DnsInterceptor(
            FastGithubConfig fastGithubConfig,
            ILogger<DnsInterceptor> logger,
            IOptionsMonitor<FastGithubOptions> options)
        {
            this.fastGithubConfig = fastGithubConfig;
            this.logger = logger;

            options.OnChange(_ => DnsFlushResolverCache());
        }

        /// <summary>
        /// DNS拦截
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <exception cref="Win32Exception"></exception>
        /// <returns></returns>
        public async Task InterceptAsync(CancellationToken cancellationToken)
        {
            await Task.Yield();

            var handle = WinDivert.WinDivertOpen(DNS_FILTER, WinDivertLayer.Network, 0, WinDivertOpenFlags.None);
            if (handle == new IntPtr(unchecked((long)ulong.MaxValue)))
            {
                throw new Win32Exception();
            }

            cancellationToken.Register(hwnd =>
            {
                WinDivert.WinDivertClose((IntPtr)hwnd!);
                DnsFlushResolverCache();
            }, handle);

            var packetLength = 0U;
            using var winDivertBuffer = new WinDivertBuffer();
            var winDivertAddress = new WinDivertAddress();

            DnsFlushResolverCache();
            while (cancellationToken.IsCancellationRequested == false)
            {
                if (WinDivert.WinDivertRecv(handle, winDivertBuffer, ref winDivertAddress, ref packetLength) == false)
                {
                    throw new Win32Exception();
                }

                try
                {
                    this.ModifyDnsPacket(winDivertBuffer, ref winDivertAddress, ref packetLength);
                }
                catch (Exception ex)
                {
                    this.logger.LogWarning(ex.Message);
                }
                finally
                {
                    WinDivert.WinDivertSend(handle, winDivertBuffer, packetLength, ref winDivertAddress);
                }
            }
        }

        /// <summary>
        /// 修改DNS数据包
        /// </summary>
        /// <param name="winDivertBuffer"></param>
        /// <param name="winDivertAddress"></param>
        /// <param name="packetLength"></param> 
        unsafe private void ModifyDnsPacket(WinDivertBuffer winDivertBuffer, ref WinDivertAddress winDivertAddress, ref uint packetLength)
        {
            var packet = WinDivert.WinDivertHelperParsePacket(winDivertBuffer, packetLength);
            var requestPayload = new Span<byte>(packet.PacketPayload, (int)packet.PacketPayloadLength).ToArray();

            if (TryParseRequest(requestPayload, out var request) == false ||
                request.OperationCode != OperationCode.Query ||
                request.Questions.Count == 0)
            {
                return;
            }

            var question = request.Questions.First();
            if (question.Type != RecordType.A && question.Type != RecordType.AAAA)
            {
                return;
            }

            var domain = question.Name;
            if (this.fastGithubConfig.IsMatch(question.Name.ToString()) == false)
            {
                return;
            }

            // dns响应数据
            var response = Response.FromRequest(request);
            var loopback = question.Type == RecordType.A ? IPAddress.Loopback : IPAddress.IPv6Loopback;
            var record = new IPAddressResourceRecord(domain, loopback, this.ttl);
            response.AnswerRecords.Add(record);
            var responsePayload = response.ToArray();

            // 修改payload和包长 
            responsePayload.CopyTo(new Span<byte>(packet.PacketPayload, responsePayload.Length));
            packetLength = (uint)((int)packetLength + responsePayload.Length - requestPayload.Length);

            // 修改ip包
            IPAddress destAddress;
            if (packet.IPv4Header != null)
            {
                destAddress = packet.IPv4Header->DstAddr;
                packet.IPv4Header->DstAddr = packet.IPv4Header->SrcAddr;
                packet.IPv4Header->SrcAddr = destAddress;
                packet.IPv4Header->Length = (ushort)packetLength;
            }
            else
            {
                destAddress = packet.IPv6Header->DstAddr;
                packet.IPv6Header->DstAddr = packet.IPv6Header->SrcAddr;
                packet.IPv6Header->SrcAddr = destAddress;
                packet.IPv6Header->Length = (ushort)(packetLength - sizeof(IPv6Header));
            }

            // 修改udp包
            var destPort = packet.UdpHeader->DstPort;
            packet.UdpHeader->DstPort = packet.UdpHeader->SrcPort;
            packet.UdpHeader->SrcPort = destPort;
            packet.UdpHeader->Length = (ushort)(sizeof(UdpHeader) + responsePayload.Length);

            winDivertAddress.Impostor = true;
            winDivertAddress.Direction = winDivertAddress.Loopback
                ? WinDivertDirection.Outbound
                : WinDivertDirection.Inbound;

            WinDivert.WinDivertHelperCalcChecksums(winDivertBuffer, packetLength, ref winDivertAddress, WinDivertChecksumHelperParam.All);
            this.logger.LogInformation($"{domain}->{loopback}");
        }


        /// <summary>
        /// 尝试解析请求
        /// </summary>
        /// <param name="payload"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        static bool TryParseRequest(byte[] payload, [MaybeNullWhen(false)] out Request request)
        {
            try
            {
                request = Request.FromArray(payload);
                return true;
            }
            catch (Exception)
            {
                request = null;
                return false;
            }
        }
    }
}
