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
            Visibility = MeshinaRuntime.StationIndex >= 0 ? Visibility.Visible : Visibility.Collapsed;
            var service = MeshinaRuntime.Service;
            StatusText.Text = MeshinaRuntime.InitializationError ?? service?.Status ?? "啮合流程未启动";
            var job = service?.Current ?? service?.LastFinished;
            JobText.Text = job == null ? "" : $"SN：{job.SN}　扫码时间：{job.ScanTimeUtc.ToLocalTime():yyyy-MM-dd HH:mm:ss}"
                + (string.IsNullOrEmpty(job.MdbPath) ? "" : $"\n检测文件：{System.IO.Path.GetFileName(job.MdbPath)}");
            RetryButton.IsEnabled = !busy && service?.CanRetry == true;
            AcceptedButton.IsEnabled = NotAcceptedButton.IsEnabled = !busy && service?.NeedsConfirmation == true;
            CancelButton.IsEnabled = !busy && service?.CanCancel == true;
            int index = MeshinaRuntime.StationIndex;
            if (job == null || index < 0 || SetHelper.resultModel == null || index >= SetHelper.resultModel.Length) return;
            var result = SetHelper.resultModel[index];
            result.CheckInSN = job.SN;
            result.CheckOutSN = job.MdbPath == null ? "" : job.SN;
            result.Result1 = job.Stage == MeshinaStage.CheckInSending ? "处理中"
                : job.Stage == MeshinaStage.CheckInRejected ? "NG"
                : job.Stage == MeshinaStage.CheckInUncertain ? "待核实"
                : job.Stage == MeshinaStage.Cancelled ? "已结束" : "OK";
            result.Result3 = job.Stage == MeshinaStage.Completed ? "OK"
                : job.Stage == MeshinaStage.CheckOutRejected ? "NG"
                : job.Stage == MeshinaStage.CheckOutUncertain ? "待核实"
                : job.Stage == MeshinaStage.CheckOutSending ? "处理中" : "";
        }
        private async void RetryClick(object sender, RoutedEventArgs e) => await ExecuteAsync(s => s.RetryAsync());
        private async void AcceptedClick(object sender, RoutedEventArgs e)
        {
            string jobId = MeshinaRuntime.Service?.Current?.Id;
            if (Confirm("请先核实MES中的当前SN记录。确认本次请求已被MES接收？"))
                await ExecuteAsync(s => s.ResolveUnknownAsync(true, jobId));
        }
        private async void NotAcceptedClick(object sender, RoutedEventArgs e)
        {
            string jobId = MeshinaRuntime.Service?.Current?.Id;
            if (Confirm("请先核实MES中的当前SN记录。确认本次请求未被MES接收，可以重新提交？"))
                await ExecuteAsync(s => s.ResolveUnknownAsync(false, jobId));
        }
        private async void CancelClick(object sender, RoutedEventArgs e)
        {
            string jobId = MeshinaRuntime.Service?.Current?.Id;
            if (Confirm("确认已处理MES中的本件状态，并停止本件测量、保存和延迟写入？结束后请移走本件，避免下一件绑定到本件数据。"))
                await ExecuteAsync(s => s.CancelAsync(jobId));
        }
        private bool Confirm(string message)
        {
            if (!SetHelper.IsAdmin)
            {
                StatusText.Text = "请技术人员登录后处理异常恢复";
                return false;
            }
            return MessageBox.Show(message + $"\n当前SN：{MeshinaRuntime.Service?.Current?.SN}", "啮合站异常恢复",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
        }
        private async Task ExecuteAsync(Func<MeshinaStationService, Task> action)
        {
            if (busy || MeshinaRuntime.Service == null) return;
            busy = true; Refresh();
            try { await action(MeshinaRuntime.Service); }
            finally { busy = false; Refresh(); }
        }
    }
}
