using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Tommy;

namespace FastGithub.DomainResolve
{
    /// <summary>
    /// doml配置工具
    /// </summary>
    static class TomlUtil
    {
        /// <summary>
        /// 设置监听地址
        /// </summary>
        /// <param name="tomlPath"></param>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public static Task SetListensAsync(string tomlPath, IPEndPoint endpoint, CancellationToken cancellationToken = default)
        {
            var value = new TomlArray
            {
                endpoint.ToString()
            };
            return SetAsync(tomlPath, "listen_addresses", value, cancellationToken);
        }

        /// <summary>
        /// 设置日志等级
        /// </summary>
        /// <param name="tomlPath"></param>
        /// <param name="logLevel"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static Task SetlogLevelAsync(string tomlPath, int logLevel, CancellationToken cancellationToken)
        {
            return SetAsync(tomlPath, "log_level", new TomlInteger { Value = logLevel });
        }

        /// <summary>
        /// 设置ecs
        /// </summary>
        /// <param name="tomlPath"></param> 
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<bool> SetEdnsClientSubnetAsync(string tomlPath, CancellationToken cancellationToken = default)
        {
            try
            {
                var address = await GetPublicIPAddressAsync(cancellationToken);
                if (address != null)
                {
                    var value = new TomlArray { $"{address}/32" };
                    await SetAsync(tomlPath, "edns_client_subnet", value, cancellationToken);
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 获取公网ip
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private static async Task<IPAddress?> GetPublicIPAddressAsync(CancellationToken cancellationToken)
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(3d) };
            var response = await httpClient.GetStringAsync("https://pv.sohu.com/cityjson?ie=utf-8", cancellationToken);
            var match = Regex.Match(response, @"\d+\.\d+\.\d+\.\d+");
            IPAddress.TryParse(match.Value, out var address);
            return address;
        }

        /// <summary>
        /// 设置指定键的值
        /// </summary>
        /// <param name="tomlPath"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static async Task SetAsync(string tomlPath, string key, TomlNode value, CancellationToken cancellationToken = default)
        {
            var toml = await File.ReadAllTextAsync(tomlPath, cancellationToken);
            var reader = new StringReader(toml);
            var tomlTable = TOML.Parse(reader);
            tomlTable[key] = value;

            var builder = new StringBuilder();
            var writer = new StringWriter(builder);
            tomlTable.WriteTo(writer);
            toml = builder.ToString();

            await File.WriteAllTextAsync(tomlPath, toml, cancellationToken);
        }
    }
}
