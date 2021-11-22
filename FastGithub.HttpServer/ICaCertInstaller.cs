namespace FastGithub.HttpServer
{
    /// <summary>
    /// CA证书安装器
    /// </summary>
    interface ICaCertInstaller
    {
        /// <summary>
        /// 是否支持
        /// </summary>
        /// <returns></returns>
        bool IsSupported();

        /// <summary>
        /// 安装ca证书
        /// </summary>
        /// <param name="caCertFilePath">证书文件路径</param>
        void Install(string caCertFilePath);
    }
}
