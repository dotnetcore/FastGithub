using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace FastGithub.HttpServer
{
    abstract class CaCertInstallerOfLinux : ICaCertInstaller
    {
        private readonly ILogger logger;

        /// <summary>
        /// 更新工具文件名
        /// </summary>
        protected abstract string CaCertUpdatePath { get; }

        /// <summary>
        /// 证书根目录
        /// </summary>
        protected abstract string CaCertStorePath { get; }

        [DllImport("libc", SetLastError = true)]
        private static extern uint geteuid();

        public CaCertInstallerOfLinux(ILogger logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// 是否支持
        /// </summary>
        /// <returns></returns>
        public bool IsSupported()
        {
            return OperatingSystem.IsLinux() && File.Exists(this.CaCertUpdatePath);
        }

        /// <summary>
        /// 安装ca证书
        /// </summary>
        /// <param name="caCertFilePath">证书文件路径</param>
        public void Install(string caCertFilePath)
        {
            var destCertFilePath = Path.Combine(this.CaCertStorePath, Path.GetFileName(caCertFilePath));
            if (File.Exists(destCertFilePath) && File.ReadAllBytes(caCertFilePath).SequenceEqual(File.ReadAllBytes(destCertFilePath)))
            {
                return;
            }

            if (geteuid() != 0)
            {
                this.logger.LogWarning($"无法自动安装CA证书{caCertFilePath}：没有root权限");
                return;
            }

            try
            {
                Directory.CreateDirectory(this.CaCertStorePath);
                foreach (var item in Directory.GetFiles(this.CaCertStorePath, "fastgithub.*"))
                {
                    File.Delete(item);
                }
                File.Copy(caCertFilePath, destCertFilePath, overwrite: true);
                Process.Start(this.CaCertUpdatePath).WaitForExit();
                this.logger.LogInformation($"已自动向系统安装CA证书{caCertFilePath}");
            }
            catch (Exception ex)
            {
                File.Delete(destCertFilePath);
                this.logger.LogWarning(ex.Message, "自动安装CA证书异常");
            }
        }
    }
}