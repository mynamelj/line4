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
        public static int StationIndex { get; private set; } = -1;
        public static string InitializationError { get; private set; }
        public static bool BlocksConfigurationChange => Service?.HasPending == true || InitializationError != null;
        public static bool TryStopForConfiguration()
        {
            if (InitializationError != null || (Service != null && !Service.TryStopForConfiguration())) return false;
            Stop();
            return true;
        }
        public static bool IsStation(int index) => index >= 0 && index < SetHelper.StationNumber.numberGroups.Count
            && (MeshinaSettings.IsStationName(SetHelper.StationNumber.numberGroups[index].Name)
                || (index < SetHelper.MesSetting.ListGroup.Count && MeshinaSettings.IsStationName(SetHelper.MesSetting.ListGroup[index].StationID)));
        public static bool IsOfflineOnly => SetHelper.StationNumber.numberGroups.Count > 0
            && Enumerable.Range(0, SetHelper.StationNumber.numberGroups.Count).All(IsStation);

        public static void Initialize()
        {
            Stop();
            Service = null; InitializationError = null; StationIndex = -1;
            var stations = Enumerable.Range(0, SetHelper.StationNumber.numberGroups.Count).Where(IsStation).ToArray();
            if (stations.Length == 0) return;
            StationIndex = stations[0];
            try
            {
                if (stations.Length != 1) throw new InvalidDataException("一个啮合检测目录只支持配置一个2020Meshina工位");
                string configPath = Path.Combine(SetHelper.mainpath, "meshina.json");
                var settings = MeshinaSettings.Load(configPath);
                string runtimeDirectory = Path.Combine(Environment.CurrentDirectory, "data", "meshina");
                var station = SetHelper.MesSetting.ListGroup[StationIndex];
                var api = SetHelper.ApiSetting.ListGroup[StationIndex];
                string context = string.Join("|", SetHelper.NowProduct.ProductID, station.Line, station.StationID,
                    station.MachineID, api.BaseUrl, api.CheckINApi, api.CheckOutApi, settings.DataDirectory);
                var reader = new MdbReader(settings.Provider, settings.RequiredFields);
                reader.ValidateProvider();
                Service = new MeshinaStationService(settings, new MdbPoller(settings.DataDirectory),
                    reader, new MeshinaMesGateway(StationIndex),
                    new MeshinaStateStore(Path.Combine(runtimeDirectory, "current.json")), context, Log)
                { ArchiveDirectory = Path.Combine(runtimeDirectory, "history") };
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

        public static async Task ScanAsync(int index, string sn)
        {
            if (index != StationIndex || Service == null || InitializationError != null)
            { Log("啮合流程未就绪，未接收扫码：" + InitializationError); return; }
            try
            {
                await Service.ScanAsync(sn, () => new MeshinaJob
                {
                    CheckInRequest = "".GetCheckInModel(index, sn.Trim(), false),
                    CheckOutRequest = new List<DC_Info>().GetSNCheckoutModel(new List<CompList>(), sn.Trim(), "", index, 0)
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

        private static void Log(string message)
        {
            SetHelper.ListMesMessage.ShowInfoQueue("[2020Meshina] " + message);
        }
    }
}
