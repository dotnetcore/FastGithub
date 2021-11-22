using System;
using System.IO;
using System.Net;
using System.Text;
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
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static Task SetListensAsync(string tomlPath, IPEndPoint endpoint, CancellationToken cancellationToken)
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
        public static Task SetLogLevelAsync(string tomlPath, int logLevel, CancellationToken cancellationToken)
        {
            return SetAsync(tomlPath, "log_level", new TomlInteger { Value = logLevel }, cancellationToken);
        }

        /// <summary>
        /// 设置负载均衡模式
        /// </summary>
        /// <param name="tomlPath"></param>
        /// <param name="value"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static Task SetLBStrategyAsync(string tomlPath, string value, CancellationToken cancellationToken)
        {
            return SetAsync(tomlPath, "lb_strategy", new TomlString { Value = value }, cancellationToken);
        }

        /// <summary>
        /// 设置TTL
        /// </summary>
        /// <param name="tomlPath"></param>
        /// <param name="minTTL"></param>
        /// <param name="maxTTL"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task SetMinMaxTTLAsync(string tomlPath, TimeSpan minTTL, TimeSpan maxTTL, CancellationToken cancellationToken)
        {
            var minValue = new TomlInteger { Value = (int)minTTL.TotalSeconds };
            var maxValue = new TomlInteger { Value = (int)maxTTL.TotalSeconds };

            await SetAsync(tomlPath, "cache_min_ttl", minValue, cancellationToken);
            await SetAsync(tomlPath, "cache_neg_min_ttl", minValue, cancellationToken);
            await SetAsync(tomlPath, "cache_max_ttl", maxValue, cancellationToken);
            await SetAsync(tomlPath, "cache_neg_max_ttl", maxValue, cancellationToken);
        }

        /// <summary>
        /// 设置指定键的值
        /// </summary>
        /// <param name="tomlPath"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task SetAsync(string tomlPath, string key, TomlNode value, CancellationToken cancellationToken)
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
