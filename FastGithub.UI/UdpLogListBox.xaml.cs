using System;
using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace FastGithub.UI
{
    /// <summary>
    /// UdpLogListBox.xaml 的交互逻辑
    /// </summary>
    public partial class UdpLogListBox : UserControl
    {
        private readonly int maxLogCount = 100;
        public ObservableCollection<UdpLog> LogList { get; } = new ObservableCollection<UdpLog>();

        public UdpLogListBox()
        {
            InitializeComponent();

            this.DataContext = this;
            this.InitUdpLoggerAsync();
        }

        private async void InitUdpLoggerAsync()
        {
            while (this.Dispatcher.HasShutdownStarted == false)
            {
                try
                {
                    var log = await UdpLogger.GetUdpLogAsync();
                    if (log != null)
                    {
                        this.LogList.Insert(0, log);
                        if (this.LogList.Count > this.maxLogCount)
                        {
                            this.LogList.RemoveAt(this.maxLogCount);
                        }
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        private void MenuItem_Copy_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (this.listBox.SelectedValue is UdpLog udpLog)
            {
                udpLog.SetToClipboard();
            }
        }

        private void MenuItem_Clear_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.LogList.Clear();
        }
    }
}
