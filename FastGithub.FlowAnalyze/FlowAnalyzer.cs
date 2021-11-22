using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace FastGithub.FlowAnalyze
{
    sealed class FlowAnalyzer : IFlowAnalyzer
    {
        private const int INTERVAL_SECONDS = 5;
        private readonly FlowQueues readQueues = new(INTERVAL_SECONDS);
        private readonly FlowQueues writeQueues = new(INTERVAL_SECONDS); 

        /// <summary>
        /// 收到数据
        /// </summary>
        /// <param name="flowType"></param>
        /// <param name="length"></param>
        public void OnFlow(FlowType flowType, int length)
        {
            if (flowType == FlowType.Read)
            {
                this.readQueues.OnFlow(length);
            }
            else
            {
                this.writeQueues.OnFlow(length);
            }
        }

        /// <summary>
        /// 获取流量分析
        /// </summary>
        /// <returns></returns>
        public FlowStatistics GetFlowStatistics()
        {
            return new FlowStatistics
            {
                TotalRead = this.readQueues.TotalBytes,
                TotalWrite = this.writeQueues.TotalBytes,
                ReadRate = this.readQueues.GetRate(),
                WriteRate = this.writeQueues.GetRate()
            };
        }

        private class FlowQueues
        {
            private int cleaning = 0;
            private long totalBytes = 0L; 
            private record QueueItem(long Ticks, int Length);
            private readonly ConcurrentQueue<QueueItem> queues = new();

            private readonly int intervalSeconds;

            public long TotalBytes => this.totalBytes;

            public FlowQueues(int intervalSeconds)
            {
                this.intervalSeconds = intervalSeconds;
            }

            public void OnFlow(int length)
            {
                Interlocked.Add(ref this.totalBytes, length);
                this.CleanInvalidRecords();
                this.queues.Enqueue(new QueueItem(Environment.TickCount64, length));
            }

            public double GetRate()
            {
                this.CleanInvalidRecords();
                return (double)this.queues.Sum(item => item.Length) / this.intervalSeconds;
            }

            /// <summary>
            /// 清除无效记录
            /// </summary>
            /// <returns></returns>
            private bool CleanInvalidRecords()
            {
                if (Interlocked.CompareExchange(ref this.cleaning, 1, 0) != 0)
                {
                    return false;
                }

                var ticks = Environment.TickCount64;
                while (this.queues.TryPeek(out var item))
                {
                    if (ticks - item.Ticks < this.intervalSeconds * 1000)
                    {
                        break;
                    }
                    else
                    {
                        this.queues.TryDequeue(out _);
                    }
                }

                Interlocked.Exchange(ref this.cleaning, 0);
                return true;
            }
        }
    }
}
