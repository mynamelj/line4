using System.IO;
using System.Text;
using MES.MesModel.Request;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace MES.SpecialStations.Meshina
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum MeshinaStage { CheckInSending, CheckInRejected, CheckInUncertain, WaitingForMdb,
        ReadyForCheckOut, CheckOutSending, CheckOutRejected, CheckOutUncertain, Completed, Cancelled }

    public sealed class MeshinaJob
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string ContextKey { get; set; }
        public string SN { get; set; }
        public DateTime ScanTimeUtc { get; set; }
        public List<string> BaselineFiles { get; set; } = new List<string>();
        public MeshinaStage Stage { get; set; }
        public string MdbPath { get; set; }
        public MeshinaMeasurement Measurement { get; set; }
        public SNCheckINModel CheckInRequest { get; set; }
        public SNCheckoutModel CheckOutRequest { get; set; }
        public string Message { get; set; }
    }

    public sealed class MeshinaState
    {
        public int Version { get; set; } = 1;
        public MeshinaJob Current { get; set; }
        public MeshinaJob LastFinished { get; set; }
        public List<string> ProcessedFiles { get; set; } = new List<string>();
    }

    public sealed class MeshinaStateStore
    {
        private readonly string path;
        public MeshinaStateStore(string path) { this.path = path; }
        public MeshinaState Load()
        {
            if (!File.Exists(path)) return new MeshinaState();
            var json = JObject.Parse(File.ReadAllText(path));
            if (json["Version"] == null || !json.ContainsKey("Current") || !json.ContainsKey("LastFinished")
                || json["ProcessedFiles"] == null) throw new InvalidDataException("啮合站恢复文件缺少必要信息，禁止清空任务");
            var state = json.ToObject<MeshinaState>();
            if (state == null || state.Version != 1 || state.ProcessedFiles == null
                || (state.Current != null && (string.IsNullOrWhiteSpace(state.Current.SN)
                    || state.Current.BaselineFiles == null || state.Current.CheckInRequest == null
                    || state.Current.CheckOutRequest == null || !Enum.IsDefined(typeof(MeshinaStage), state.Current.Stage))))
                throw new InvalidDataException("啮合站恢复文件不完整，禁止自动清空任务，请联系技术人员");
            if (state.Current != null)
            {
                var job = state.Current;
                if (job.ScanTimeUtc == default || string.IsNullOrWhiteSpace(job.ContextKey)
                    || string.IsNullOrWhiteSpace(job.Id) || job.CheckInRequest.SN != job.SN)
                    throw new InvalidDataException("啮合站任务身份信息不完整");
                if (job.Stage == MeshinaStage.ReadyForCheckOut || job.Stage == MeshinaStage.CheckOutSending
                    || job.Stage == MeshinaStage.CheckOutRejected || job.Stage == MeshinaStage.CheckOutUncertain)
                {
                    if (string.IsNullOrWhiteSpace(job.MdbPath) || job.Measurement?.Values == null || job.Measurement.Values.Count == 0
                        || job.CheckOutRequest.SNInfo?.Length != 1 || job.CheckOutRequest.SNInfo[0]?.SN != job.SN
                        || job.CheckOutRequest.SNInfo[0].DC_Info == null || job.CheckOutRequest.SNInfo[0].DC_Info.Length == 0)
                        throw new InvalidDataException("啮合站出站任务缺少SN/MDB/检测数据绑定，禁止自动出站");
                }
            }
            return state;
        }
        public void Save(MeshinaState state)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(state, Formatting.Indented));
            string temporary = path + ".tmp";
            using (var file = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            { file.Write(bytes, 0, bytes.Length); file.Flush(true); }
            if (File.Exists(path)) File.Replace(temporary, path, null);
            else File.Move(temporary, path);
        }
    }
}
