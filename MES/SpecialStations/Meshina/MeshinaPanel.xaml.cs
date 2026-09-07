using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
            Loaded += (_, __) =>
            {
                Refresh(); timer.Start();
                FocusScannerInput();
            };
            Unloaded += (_, __) => timer.Stop();
        }
        private async void UsbScanInput_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Return && e.Key != Key.Tab) return;
            string sn = UsbScanInput.Text.Trim();
            if (sn.Length == 0) return;
            e.Handled = true;
            UsbScanInput.Clear();
            await MeshinaRuntime.ScanAsync(sn);
            Refresh();
        }
        private void Refresh()
        {
            Visibility = MeshinaRuntime.IsOfflineOnly ? Visibility.Visible : Visibility.Collapsed;
            StationTitle.Text = MeshinaRuntime.StationName + " · 线外啮合";
            UsbScanPanel.Visibility = MeshinaRuntime.UsesUsbScanner ? Visibility.Visible : Visibility.Collapsed;
            var service = MeshinaRuntime.Service;
            StatusText.Text = MeshinaRuntime.InitializationError ?? service?.Status ?? "啮合流程未启动";
            if (service?.Current?.AbortRequested == true) StatusText.Text = "正在终止本次流程，等待当前操作结束…";
            var job = service?.Current ?? service?.LastFinished;
            JobText.Text = job == null ? "" : $"SN：{job.SN}　扫码时间：{job.ScanTimeUtc.ToLocalTime():yyyy-MM-dd HH:mm:ss}"
                + (string.IsNullOrEmpty(job.MdbPath) ? "" : $"\n检测文件：{System.IO.Path.GetFileName(job.MdbPath)}");
            RetryButton.IsEnabled = !busy && service?.CanRetry == true;
            AbortButton.IsEnabled = service?.CanAbort == true;
            TestMdbButton.IsEnabled = !busy && service?.CanGenerateTestMdb == true;
            int index = 0;
            if (job == null || index < 0 || SetHelper.resultModel == null || index >= SetHelper.resultModel.Length) return;
            var result = SetHelper.resultModel[index];
            result.CheckInSN = job.SN;
            result.CheckOutSN = job.MdbPath == null ? "" : job.SN;
            result.Result1 = job.Stage == MeshinaStage.FeedingCheckSending ? "处理中"
                : job.Stage == MeshinaStage.FeedingCheckRejected ? "NG"
                : job.Stage == MeshinaStage.Cancelled ? "已终止"
                : "OK";
            result.Result3 = job.Stage == MeshinaStage.Completed ? "OK"
                : job.Stage == MeshinaStage.Cancelled ? "已终止"
                : job.Stage == MeshinaStage.CheckOutRejected ? "NG"
                : job.Stage == MeshinaStage.CheckOutSending ? "处理中" : "";
        }
        private async void RetryClick(object sender, RoutedEventArgs e) => await ExecuteAsync(s => s.RetryAsync());
        private async void AbortClick(object sender, RoutedEventArgs e)
        {
            var service = MeshinaRuntime.Service;
            if (service == null) return;
            var abort = service.AbortAsync();
            Refresh();
            await abort;
            UsbScanInput.Clear();
            Refresh();
            FocusScannerInput();
        }
        private void TestMdbClick(object sender, RoutedEventArgs e)
        {
            if (busy || MeshinaRuntime.Service?.CanGenerateTestMdb != true || MeshinaRuntime.TestGenerator == null) return;
            if (MessageBox.Show("将生成一份测试MDB，并触发当前扫码SN自动出站。是否继续？", MeshinaRuntime.StationName + "测试",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            try
            {
                string path = MeshinaRuntime.TestGenerator.Create();
                StatusText.Text = "测试MDB已生成：" + System.IO.Path.GetFileName(path);
                MeshinaRuntime.WriteLog("测试MDB已生成：" + path);
            }
            catch (Exception ex)
            {
                StatusText.Text = "生成测试MDB失败：" + ex.Message;
                MeshinaRuntime.WriteLog("生成测试MDB失败：" + ex.Message);
            }
            finally { FocusScannerInput(); }
        }
        private async Task ExecuteAsync(Func<MeshinaStationService, Task> action)
        {
            if (busy || MeshinaRuntime.Service == null) return;
            busy = true; Refresh();
            try { await action(MeshinaRuntime.Service); }
            finally { busy = false; Refresh(); FocusScannerInput(); }
        }

        private void FocusScannerInput()
        {
            if (!MeshinaRuntime.UsesUsbScanner) return;
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() => UsbScanInput.Focus()));
        }
    }
}
