using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace FastGithub.HttpServer
{
    /// <summary>
    /// 证书服务
    /// </summary>
    sealed class CertService
    {
        private const string CACERT_PATH = "cacert";
        private const int KEY_SIZE_BITS = 2048;
        private readonly IMemoryCache serverCertCache;
        private readonly IEnumerable<ICaCertInstaller> certInstallers;
        private readonly ILogger<CertService> logger;


        /// <summary>
        /// 获取证书文件路径
        /// </summary>
        public string CaCerFilePath { get; } = OperatingSystem.IsLinux() ? $"{CACERT_PATH}/fastgithub.crt" : $"{CACERT_PATH}/fastgithub.cer";

        /// <summary>
        /// 获取私钥文件路径
        /// </summary>
        public string CaKeyFilePath { get; } = $"{CACERT_PATH}/fastgithub.key";

        /// <summary>
        /// 证书服务
        /// </summary>
        /// <param name="serverCertCache"></param>
        /// <param name="certInstallers"></param>
        /// <param name="logger"></param>
        public CertService(
            IMemoryCache serverCertCache,
            IEnumerable<ICaCertInstaller> certInstallers,
            ILogger<CertService> logger)
        {
            this.serverCertCache = serverCertCache;
            this.certInstallers = certInstallers;
            this.logger = logger;
            Directory.CreateDirectory(CACERT_PATH);
        }

        /// <summary>
        /// 生成CA证书
        /// </summary> 
        public bool CreateCaCertIfNotExists()
        {
            if (File.Exists(this.CaCerFilePath) && File.Exists(this.CaKeyFilePath))
            {
                return false;
            }

            File.Delete(this.CaCerFilePath);
            File.Delete(this.CaKeyFilePath);

            var validFrom = DateTime.Today.AddDays(-1);
            var validTo = DateTime.Today.AddYears(10);
            CertGenerator.GenerateBySelf(new[] { nameof(FastGithub) }, KEY_SIZE_BITS, validFrom, validTo, this.CaCerFilePath, this.CaKeyFilePath);
            return true;
        }

        /// <summary>
        /// 安装和信任CA证书
        /// </summary> 
        public void InstallAndTrustCaCert()
        {
            var installer = this.certInstallers.FirstOrDefault(item => item.IsSupported());
            if (installer != null)
            {
                installer.Install(this.CaCerFilePath);
            }
            else
            {
                this.logger.LogWarning($"请根据你的系统平台手动安装和信任CA证书{this.CaCerFilePath}");
            }

            GitConfigSslverify(false);
        }

        /// <summary>
        /// 设置ssl验证
        /// </summary>
        /// <param name="value">是否验证</param>
        /// <returns></returns>
        public static bool GitConfigSslverify(bool value)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = $"config --global http.sslverify {value.ToString().ToLower()}",
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                });
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 获取颁发给指定域名的证书
        /// </summary>
        /// <param name="domain"></param> 
        /// <returns></returns>
        public X509Certificate2 GetOrCreateServerCert(string? domain)
        {
            var key = $"{nameof(CertService)}:{domain}";
            return this.serverCertCache.GetOrCreate(key, GetOrCreateCert);

            // 生成域名的1年证书
            X509Certificate2 GetOrCreateCert(ICacheEntry entry)
            {
                var domains = GetDomains(domain).Distinct();
                var validFrom = DateTime.Today.AddDays(-1);
                var validTo = DateTime.Today.AddYears(1);

                entry.SetAbsoluteExpiration(validTo);
                return CertGenerator.GenerateByCa(domains, KEY_SIZE_BITS, validFrom, validTo, this.CaCerFilePath, this.CaKeyFilePath);
            }
        }

        /// <summary>
        /// 获取域名
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        private static IEnumerable<string> GetDomains(string? domain)
        {
            if (string.IsNullOrEmpty(domain) == false)
            {
                yield return domain;
                yield break;
            }

            yield return Environment.MachineName;
            yield return IPAddress.Loopback.ToString();
            yield return IPAddress.IPv6Loopback.ToString();
        }
    }
}
