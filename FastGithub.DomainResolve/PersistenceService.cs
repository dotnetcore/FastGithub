using FastGithub.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.DomainResolve
{
    /// <summary>
    /// 域名持久化
    /// </summary>
    sealed class PersistenceService
    {
        private static readonly string dataFile = "dnsendpoints.json";
        private static readonly SemaphoreSlim dataLocker = new(1, 1);
        private static readonly JsonSerializerOptions jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private readonly FastGithubConfig fastGithubConfig;
        private readonly ILogger<PersistenceService> logger;
        private record EndPointItem(string Host, int Port);


        /// <summary>
        /// 域名持久化
        /// </summary> 
        /// <param name="fastGithubConfig"></param>
        /// <param name="logger"></param>
        public PersistenceService(
            FastGithubConfig fastGithubConfig,
            ILogger<PersistenceService> logger)
        {
            this.fastGithubConfig = fastGithubConfig;
            this.logger = logger;
        }


        /// <summary>
        /// 读取保存的节点
        /// </summary>
        /// <returns></returns>
        public IList<DnsEndPoint> ReadDnsEndPoints()
        {
            if (File.Exists(dataFile) == false)
            {
                return Array.Empty<DnsEndPoint>();
            }

            try
            {
                dataLocker.Wait();

                var utf8Json = File.ReadAllBytes(dataFile);
                var endPointItems = JsonSerializer.Deserialize<EndPointItem[]>(utf8Json, jsonOptions);
                if (endPointItems == null)
                {
                    return Array.Empty<DnsEndPoint>();
                }

                var dnsEndPoints = new List<DnsEndPoint>();
                foreach (var item in endPointItems)
                {
                    if (this.fastGithubConfig.IsMatch(item.Host) == true)
                    {
                        dnsEndPoints.Add(new DnsEndPoint(item.Host, item.Port));
                    }
                }
                return dnsEndPoints;
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex.Message, "读取dns记录异常");
                return Array.Empty<DnsEndPoint>();
            }
            finally
            {
                dataLocker.Release();
            }
        }

        /// <summary>
        /// 保存节点到文件
        /// </summary>
        /// <param name="dnsEndPoints"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task WriteDnsEndPointsAsync(IEnumerable<DnsEndPoint> dnsEndPoints, CancellationToken cancellationToken)
        {
            try
            {
                await dataLocker.WaitAsync(CancellationToken.None);

                var endPointItems = dnsEndPoints.Select(item => new EndPointItem(item.Host, item.Port)).ToArray();
                var utf8Json = JsonSerializer.SerializeToUtf8Bytes(endPointItems, jsonOptions);
                await File.WriteAllBytesAsync(dataFile, utf8Json, cancellationToken);
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex.Message, "保存dns记录异常");
            }
            finally
            {
                dataLocker.Release();
            }
        }
    }
}
