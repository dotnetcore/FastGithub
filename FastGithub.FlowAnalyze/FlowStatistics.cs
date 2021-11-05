namespace FastGithub.FlowAnalyze
{
    /// <summary>
    /// 流量统计
    /// </summary>
    public record FlowStatistics
    {
        /// <summary>
        /// 获取总读上行
        /// </summary>
        public long TotalRead { get; init; }

        /// <summary>
        /// 获取总下行
        /// </summary>
        public long TotalWrite { get; init; }

        /// <summary>
        /// 获取读取速率
        /// </summary>
        public double ReadRate { get; init; }

        /// <summary>
        /// 获取写入速率
        /// </summary>
        public double WriteRate { get; init; }
    }
}
