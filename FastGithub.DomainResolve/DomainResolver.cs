using FastGithub.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.DomainResolve
{
    /// <summary>
    /// 域名解析器
    /// </summary> 
    sealed class DomainResolver : IDomainResolver
    {
        private readonly DnsClient dnsClient;
        private readonly DomainPersistence persistence;
        private readonly IPAddressService addressService;
        private readonly ILogger<DomainResolver> logger;
        private readonly ConcurrentDictionary<DnsEndPoint, IPAddress[]> dnsEndPointAddress = new();

        /// <summary>
        /// 域名解析器
        /// </summary>
        /// <param name="dnsClient"></param>
        /// <param name="persistence"></param>
        /// <param name="addressService"></param>
        /// <param name="logger"></param>
        public DomainResolver(
            DnsClient dnsClient,
            DomainPersistence persistence,
            IPAddressService addressService,
            ILogger<DomainResolver> logger)
        {
            this.dnsClient = dnsClient;
            this.persistence = persistence;
            this.addressService = addressService;
            this.logger = logger;

            foreach (var endPoint in persistence.ReadDnsEndPoints())
            {
                this.dnsEndPointAddress.TryAdd(endPoint, Array.Empty<IPAddress>());
            }
        }


        /// <summary>
        /// 解析ip
        /// </summary>
        /// <param name="endPoint">节点</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<IPAddress> ResolveAnyAsync(DnsEndPoint endPoint, CancellationToken cancellationToken = default)
        {
            await foreach (var address in this.ResolveAllAsync(endPoint, cancellationToken))
            {
                return address;
            }
            throw new FastGithubException($"解析不到{endPoint.Host}的IP");
        }

        /// <summary>
        /// 解析域名
        /// </summary>
        /// <param name="endPoint">节点</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async IAsyncEnumerable<IPAddress> ResolveAllAsync(DnsEndPoint endPoint, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            if (this.dnsEndPointAddress.TryGetValue(endPoint, out var addresses) && addresses.Length > 0)
            {
                foreach (var address in addresses)
                {
                    yield return address;
                }
            }
            else
            {
                if (this.dnsEndPointAddress.TryAdd(endPoint, Array.Empty<IPAddress>()))
                {
                    await this.persistence.WriteDnsEndPointsAsync(this.dnsEndPointAddress.Keys, cancellationToken);
                }

                await foreach (var adddress in this.dnsClient.ResolveAsync(endPoint, fastSort: true, cancellationToken))
                {
                    yield return adddress;
                }
            }
        }

        /// <summary>
        /// 对所有节点进行测速
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task TestAllEndPointsAsync(CancellationToken cancellationToken)
        {
            foreach (var keyValue in this.dnsEndPointAddress)
            {
                var dnsEndPoint = keyValue.Key;
                var oldAddresses = keyValue.Value;

                var newAddresses = await this.addressService.GetAddressesAsync(dnsEndPoint, oldAddresses, cancellationToken);
                if (oldAddresses.SequenceEqual(newAddresses) == false)
                {
                    this.dnsEndPointAddress[dnsEndPoint] = newAddresses;

                    var addressArray = string.Join(", ", newAddresses.Select(item => item.ToString()));
                    this.logger.LogInformation($"{dnsEndPoint.Host}->[{addressArray}]");
                }
            }
        }
    }
}
