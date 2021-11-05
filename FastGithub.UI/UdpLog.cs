using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Windows;

namespace FastGithub.UI
{
    public class UdpLog
    {
        public DateTime Timestamp { get; set; }

        public LogLevel Level { get; set; }

        public string Message { get; set; } = string.Empty;

        public string SourceContext { get; set; } = string.Empty;

        public string Color => this.Level <= LogLevel.Information ? "#333" : "IndianRed";

        /// <summary>
        /// 复制到剪贴板
        /// </summary>
        public void SetToClipboard()
        {
            Clipboard.SetText($"{this.Timestamp:yyyy-MM-dd HH:mm:ss.fff}\r\n{this.Message}");
        }

    }

}
