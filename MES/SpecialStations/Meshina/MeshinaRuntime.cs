using System.IO;
using System.Threading;
using MES.Manager;
using MES.MesModel.Request;

namespace MES.SpecialStations.Meshina
{
    /// <summary>连接应用配置、扫码和后台流程；不通过PLC通用进出站。</summary>
    public static class MeshinaRuntime
    {
        private static CancellationTokenSource cancellation;
        public static MeshinaStationService Service { get; private set; }
        public static TestMdbGenerator TestGenerator { get; private set; }
        public static string InitializationError { get; private set; }
        // 三个线外啮合站均独占PC并固定使用工位0。
        public static string StationName =>
            SetHelper.StationNumber.numberGroups.FirstOrDefault()?.Name ?? "Meshina";
        public static bool IsOfflineOnly => MeshinaSettings.IsStationName(StationName);
        public static bool UsesUsbScanner => MeshinaSettings.IsUsbScannerStationName(StationName);
        public static bool UsesSerialScanner => MeshinaSettings.IsSerialScannerStationName(StationName);

        public static void Initialize()
        {
            Stop();
            Service = null; TestGenerator = null; InitializationError = null;
            if (!IsOfflineOnly) return;
            try
            {
                var settings = MeshinaSettings.Load(Path.Combine(SetHelper.mainpath, "meshina.json"));
                Service = new MeshinaStationService(settings, new MdbPoller(settings.DataDirectory),
                    new MdbReader(settings.Provider, settings.RequiredFields), new MeshinaMesGateway(), Log);
                TestGenerator = new TestMdbGenerator(settings.DataDirectory, settings.Provider);
                cancellation = new CancellationTokenSource();
                var token = cancellation.Token;
                var service = Service;
                _ = Task.Run(() => service.RunAsync(token));
                Log($"无PLC啮合流程已启动，目录:{settings.DataDirectory}；程序{(Environment.Is64BitProcess ? 64 : 32)}位，驱动:{settings.Provider}");
            }
            catch (Exception ex)
            {
                InitializationError = ex.Message;
                Log("初始化失败，禁止扫码进站：" + ex.Message);
            }
        }

        public static async Task ScanAsync(string sn)
        {
            if (Service == null)
            { Log("啮合流程未就绪，未接收扫码：" + InitializationError); return; }
            try
            {
                await Service.ScanAsync(sn, () => new MeshinaJob
                {
                    FeedingCheckRequest = sn.Trim().GetFeedingCheck(0),
                    CheckOutRequest = new List<DC_Info>().GetSNCheckoutModel(new List<CompList>(), sn.Trim(), "", 0, 0)
                });
            }
            catch (Exception ex) { Log("扫码处理失败：" + ex.Message); }
        }

        public static void Stop()
        {
            Service?.Stop();
            cancellation?.Cancel();
            cancellation?.Dispose();
            cancellation = null;
        }

        public static void WriteLog(string message)
        {
            Log(message);
        }

        private static void Log(string message)
        {
            SetHelper.ListMesMessage.ShowInfoQueue("[" + StationName + "] " + message);
        }
    }
}
