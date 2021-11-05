namespace FastGithub.HttpServer
{
    /// <summary>
    /// 请求日志特性
    /// </summary>
    public interface IRequestLoggingFeature
    {
        /// <summary>
        /// 是否启用
        /// </summary>
        bool Enable { get; set; }
    }
}
