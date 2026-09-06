using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using MES.Manager;

namespace MES.SpecialStations.Meshina
{
    public partial class MeshinaPanel : UserControl
    {
        private readonly DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        private bool busy;
        public MeshinaPanel()
        {
            InitializeComponent();
            timer.Tick += (_, __) => Refresh();
            Loaded += (_, __) => { Refresh(); timer.Start(); };
            Unloaded += (_, __) => timer.Stop();
        }
        private void Refresh()
        {
            Visibility = MeshinaRuntime.IsOfflineOnly ? Visibility.Visible : Visibility.Collapsed;
            var service = MeshinaRuntime.Service;
            StatusText.Text = MeshinaRuntime.InitializationError ?? service?.Status ?? "啮合流程未启动";
            var job = service?.Current ?? service?.LastFinished;
            JobText.Text = job == null ? "" : $"SN：{job.SN}　扫码时间：{job.ScanTimeUtc.ToLocalTime():yyyy-MM-dd HH:mm:ss}"
                + (string.IsNullOrEmpty(job.MdbPath) ? "" : $"\n检测文件：{System.IO.Path.GetFileName(job.MdbPath)}");
            RetryButton.IsEnabled = !busy && service?.CanRetry == true;
            int index = 0;
            if (job == null || index < 0 || SetHelper.resultModel == null || index >= SetHelper.resultModel.Length) return;
            var result = SetHelper.resultModel[index];
            result.CheckInSN = job.SN;
            result.CheckOutSN = job.MdbPath == null ? "" : job.SN;
            result.Result1 = job.Stage == MeshinaStage.FeedingCheckSending ? "处理中"
                : job.Stage == MeshinaStage.FeedingCheckRejected ? "NG"
                : "OK";
            result.Result3 = job.Stage == MeshinaStage.Completed ? "OK"
                : job.Stage == MeshinaStage.CheckOutRejected ? "NG"
                : job.Stage == MeshinaStage.CheckOutSending ? "处理中" : "";
        }
        private async void RetryClick(object sender, RoutedEventArgs e) => await ExecuteAsync(s => s.RetryAsync());
        private async Task ExecuteAsync(Func<MeshinaStationService, Task> action)
        {
            if (busy || MeshinaRuntime.Service == null) return;
            busy = true; Refresh();
            try { await action(MeshinaRuntime.Service); }
            finally { busy = false; Refresh(); }
        }
    }
}
