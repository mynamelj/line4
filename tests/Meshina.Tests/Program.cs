using MES.MesModel.Request;
using MES.SpecialStations.Meshina;
using System.Globalization;
using System.IO;
using System.Data.OleDb;

internal static class Program
{
    private static int assertions;
    private static readonly string Root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cases", Guid.NewGuid().ToString("N"));
    private static async Task<int> Main(string[] args)
    {
        try
        {
            await HappyPath();
            await FailedAndUncertainRequests();
            await Recovery();
            await FileGuards();
            await ConcurrentScans();
            await AbortFlow();
            Check(MeshinaSettings.IsStationName("OP2020M") && MeshinaSettings.IsStationName("op2020meshina"), "2020 station aliases");
            Check(MeshinaSettings.IsStationName("2030Meshina1") && MeshinaSettings.IsStationName("OP2030Meshina2"), "2030 meshing stations");
            Check(MeshinaSettings.IsUsbScannerStationName("OP2020M")
                && !MeshinaSettings.IsUsbScannerStationName("2030Meshina1"), "USB scanner station selection");
            Check(MeshinaSettings.IsSerialScannerStationName("2030Meshina1")
                && MeshinaSettings.IsSerialScannerStationName("2030Meshina2"), "serial scanner station selection");
            Check(!MeshinaSettings.IsStationName("OP2020B") && !MeshinaSettings.IsStationName("OP2030Meshina")
                && !MeshinaSettings.IsStationName("2030Meshina3"), "other stations unchanged");
            if (args.Length > 0)
            {
                var value = new MdbReader(args.Length > 1 ? args[1] : "Microsoft.ACE.OLEDB.12.0").Read(Path.GetFullPath(args[0]));
                Check(value.Values.Count + value.EmptyFields.Count == 20, "real MDB accounts for 20 numeric fields");
                Console.WriteLine("REAL MDB: " + string.Join(", ", value.Values.Select(v => v.Key + "=" + v.Value.ToString(CultureInfo.InvariantCulture))) + "; Result=" + value.Result);
                VerifyMdbGuards(args[0], args.Length > 1 ? args[1] : "Microsoft.ACE.OLEDB.12.0");
                await VerifyGeneratedMdb(args.Length > 1 ? args[1] : "Microsoft.ACE.OLEDB.12.0");
            }
            Console.WriteLine($"PASS: {assertions} assertions");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
    private static MeshinaJob Request() => new MeshinaJob { FeedingCheckRequest = new FeedingCheckModel(), CheckOutRequest = new SNCheckoutModel() };
    private static async Task VerifyGeneratedMdb(string provider)
    {
        string directory = Path.Combine(Root, "generated");
        Directory.CreateDirectory(directory);
        var gateway = new Gateway();
        var settings = new MeshinaSettings { DataDirectory = directory, Provider = provider, StablePollCount = 2 };
        var service = new MeshinaStationService(settings, new MdbPoller(directory), new MdbReader(provider), gateway, _ => { });
        await service.ScanAsync("GENERATED-SN", Request);
        string path = new TestMdbGenerator(directory, provider).Create();
        Check(System.IO.Path.GetFileName(path).StartsWith("194-001_Unknown_955555555555555555_")
            && System.IO.Path.GetExtension(path).Equals(".mdb", StringComparison.OrdinalIgnoreCase), "test MDB filename format");
        var data = new MdbReader(provider).Read(path);
        Check(data.Values.Count == 20 && data.EmptyFields.Count == 0, "generated MDB contains all 20 numeric values");
        Check(data.Values["Fi"] == 1.11m && data.Values["Cylindricity"] == 3.44m && data.Result == "PASS",
            "generated MDB contains expected values and PASS result");
        await service.TickAsync();
        await service.TickAsync();
        Check(gateway.OutCount == 1 && gateway.LastOut.SNInfo[0].SN == "GENERATED-SN",
            "generated MDB triggers checkout for current scanned SN");
    }
    private static void VerifyMdbGuards(string sample, string provider)
    {
        string copy = Path.Combine(Root, "reader-test.mdb");
        File.Copy(sample, copy);
        var builder = new OleDbConnectionStringBuilder { Provider = provider, DataSource = copy };
        void Execute(string sql)
        {
            using var connection = new OleDbConnection(builder.ConnectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }
        var reader = new MdbReader(provider);
        string originalResult = reader.Read(copy).Result;
        Execute("UPDATE [TJSHEET] SET [Fi]=12.34, [aave]=1.234");
        var data = reader.Read(copy);
        Check(data.Values["Fi"] == 12.34m && data.Values["aave"] == 1.234m && data.Result == originalResult, "real reader preserves decimal precision and raw Result");
        Check(data.EmptyFields.Contains("dave1") && !data.Values.ContainsKey("dave1"), "real reader skips optional NULL without inventing zero");
        Execute("UPDATE [TJSHEET] SET [Fi]=NULL");
        bool rejected = false;
        try { reader.Read(copy); } catch (InvalidDataException) { rejected = true; }
        Check(rejected, "real reader rejects required NULL");
        Execute("UPDATE [TJSHEET] SET [Fi]=0");
        Execute("INSERT INTO [TJSHEET] SELECT * FROM [TJSHEET]");
        rejected = false;
        try { reader.Read(copy); } catch (InvalidDataException) { rejected = true; }
        Check(rejected, "real reader rejects multiple rows");
        Execute("DELETE FROM [TJSHEET]");
        rejected = false;
        try { reader.Read(copy); } catch (InvalidDataException) { rejected = true; }
        Check(rejected, "real reader rejects empty table");
    }
    private static void Check(bool condition, string label)
    {
        if (!condition) throw new Exception("FAILED: " + label);
        assertions++;
    }
    private static async Task HappyPath()
    {
        var f = new Fixture();
        f.File("old", DateTime.UtcNow.AddMinutes(-1));
        await f.Service.ScanAsync("  SCANNER-SN  ", Request);
        Check(f.Service.Current.SN == "SCANNER-SN" && f.Gateway.InCount == 1, "uses scanner SN");
        await f.Service.ScanAsync("NEXT", Request);
        Check(f.Service.Current.SN == "SCANNER-SN" && f.Gateway.InCount == 1, "blocks next SN");
        await f.Service.TickAsync();
        Check(f.Gateway.OutCount == 0, "ignores baseline");
        string path = f.File("unrelated-code", DateTime.UtcNow.AddSeconds(1));
        await f.Service.TickAsync();
        Check(f.Gateway.OutCount == 0, "waits for stable file");
        await f.Service.TickAsync();
        Check(f.Gateway.OutCount == 1 && f.Service.Current == null, "automatically completes checkout");
        Check(f.Gateway.LastOut.SNInfo[0].SN == "SCANNER-SN", "filename never supplies SN");
        Check(f.Gateway.LastOut.SNInfo[0].DC_Info.Length == 20, "uploads only 20 numeric fields");
        Check(f.Gateway.LastOut.SNInfo[0].DC_Info.Single(x => x.Item == "Fi").Value == "1.23", "invariant decimals");
        Check(f.Service.LastFinished.Measurement.Result == "FAIL" && f.Gateway.LastOut.SNInfo[0].Result == "PASS", "retains but ignores Result");
        Check(f.Service.LastFinished.MdbPath == path, "persists bound MDB");
        await f.Service.TickAsync();
        Check(f.Gateway.OutCount == 1, "does not upload twice");
        f.Restart();
        await f.Service.ScanAsync("NEXT", Request);
        await f.Service.TickAsync(); await f.Service.TickAsync();
        Check(f.Gateway.OutCount == 1, "does not reuse file after restart");
    }
    private static async Task FailedAndUncertainRequests()
    {
        var f = new Fixture();
        f.Gateway.InOutcome = MeshinaMesOutcome.Rejected;
        await f.Service.ScanAsync("A", Request);
        Check(f.Service.CanRetry && f.Service.Current.Stage == MeshinaStage.FeedingCheckRejected, "retains failed checkin");
        f.File("test", DateTime.UtcNow.AddSeconds(1));
        await f.Service.TickAsync(); await f.Service.TickAsync();
        Check(f.Gateway.OutCount == 0, "no checkout before successful checkin");
        f.Gateway.InOutcome = MeshinaMesOutcome.Accepted;
        await f.Service.RetryAsync();
        f.Gateway.OutOutcome = MeshinaMesOutcome.Rejected;
        await f.Service.TickAsync(); await f.Service.TickAsync();
        Check(f.Service.Current.Stage == MeshinaStage.CheckOutRejected && f.Service.Current.Measurement != null, "retains rejected checkout data");
        var payload = f.Gateway.LastOut;
        f.Gateway.OutOutcome = MeshinaMesOutcome.Unknown;
        await f.Service.RetryAsync();
        Check(f.Service.CanRetry, "unknown result retains data for operator retry");
        int calls = f.Gateway.OutCount;
        await f.Service.TickAsync();
        Check(f.Gateway.OutCount == calls, "unknown request is not automatically repeated");
        f.Gateway.OutOutcome = MeshinaMesOutcome.Accepted;
        await f.Service.RetryAsync();
        Check(ReferenceEquals(payload, f.Gateway.LastOut) && f.Service.Current == null, "retries same payload after confirmation");
    }
    private static async Task Recovery()
    {
        var f = new Fixture();
        await f.Service.ScanAsync("A", Request);
        f.Restart();
        Check(f.Service.Current == null, "restart begins an empty single-station session");
    }
    private static async Task FileGuards()
    {
        var f = new Fixture();
        await f.Service.ScanAsync("A", Request);
        string path = f.File("new", DateTime.UtcNow.AddSeconds(1));
        await f.Service.TickAsync();
        File.AppendAllText(path, "still writing");
        await f.Service.TickAsync();
        Check(f.Gateway.OutCount == 0, "size change resets stabilization");
        f.Reader.Fail = true;
        await f.Service.TickAsync();
        Check(f.Service.Current.Stage == MeshinaStage.WaitingForMdb && f.Gateway.OutCount == 0, "read errors retain job");
        f.Reader.Fail = false;
        await f.Service.TickAsync();
        Check(f.Gateway.OutCount == 1, "retries reading incomplete MDB");
        var g = new Fixture();
        await g.Service.ScanAsync("B", Request);
        g.File("one", DateTime.UtcNow.AddSeconds(1)); g.File("two", DateTime.UtcNow.AddSeconds(1));
        await g.Service.TickAsync(); await g.Service.TickAsync();
        Check(g.Gateway.OutCount == 1, "uses first new measurement in time order");
    }
    private static async Task ConcurrentScans()
    {
        var f = new Fixture();
        f.Gateway.InWait = new TaskCompletionSource<bool>();
        var first = f.Service.ScanAsync("FIRST", Request);
        await f.Service.ScanAsync("SECOND", Request);
        Check(f.Gateway.InCount == 1 && f.Service.Current.SN == "FIRST", "concurrent scans cannot replace SN");
        f.File("during-checkin", DateTime.UtcNow.AddSeconds(1));
        f.Gateway.InWait.SetResult(true); await first;
        await f.Service.TickAsync(); await f.Service.TickAsync();
        Check(f.Gateway.OutCount == 1, "file created during checkin not lost");
        f.Service.Stop();
        await f.Service.ScanAsync("AFTER-STOP", Request);
        Check(f.Gateway.InCount == 1, "stopped service refuses scans");
    }
    private static async Task AbortFlow()
    {
        var f = new Fixture();
        await f.Service.ScanAsync("OLD", Request);
        f.File("old", DateTime.UtcNow.AddSeconds(1));
        await f.Service.AbortAsync();
        Check(f.Service.Current == null && f.Service.LastFinished.Stage == MeshinaStage.Cancelled, "abort releases waiting SN");
        await f.Service.ScanAsync("NEW", Request);
        await f.Service.TickAsync(); await f.Service.TickAsync();
        Check(f.Service.Current.SN == "NEW" && f.Gateway.OutCount == 0, "new scan excludes old file already present");
        f.File("new", DateTime.UtcNow.AddSeconds(2));
        await f.Service.TickAsync(); await f.Service.TickAsync();
        Check(f.Gateway.LastOut.SNInfo[0].SN == "NEW", "new gear checks out after abort");

        var g = new Fixture();
        g.Gateway.InWait = new TaskCompletionSource<bool>();
        var scan = g.Service.ScanAsync("PENDING", Request);
        var abort = g.Service.AbortAsync();
        Check(!abort.IsCompleted && !g.Service.CanRetry && !g.Service.CanAbort, "abort waits for in-flight FeedingCheck");
        await g.Service.ScanAsync("TOO-EARLY", Request);
        Check(g.Gateway.InCount == 1, "new scan waits for old request to finish");
        g.Gateway.InWait.SetResult(true);
        await scan; await abort;
        Check(g.Service.Current == null && g.Gateway.OutCount == 0, "late FeedingCheck does not resume aborted job");
        await g.Service.ScanAsync("AFTER", Request);
        Check(g.Service.Current.SN == "AFTER", "scan works after in-flight abort");

        var h = new Fixture();
        await h.Service.ScanAsync("OUT-PENDING", Request);
        h.File("out", DateTime.UtcNow.AddSeconds(1));
        await h.Service.TickAsync();
        h.Gateway.OutWait = new TaskCompletionSource<bool>();
        var checkout = h.Service.TickAsync();
        var abortOut = h.Service.AbortAsync();
        Check(!abortOut.IsCompleted, "abort waits for in-flight checkout");
        h.Gateway.OutWait.SetResult(true);
        await checkout; await abortOut;
        Check(h.Service.Current == null && h.Service.LastFinished.Stage == MeshinaStage.Cancelled, "late checkout response cannot restart old job");
        await h.Service.ScanAsync("NEXT", Request);
        Check(h.Service.Current.SN == "NEXT" && h.Gateway.OutCount == 1, "next SN stays separate from old checkout");
    }
    private sealed class Fixture
    {
        public readonly string DirectoryPath = Path.Combine(Root, Guid.NewGuid().ToString("N"));
        public readonly Gateway Gateway = new Gateway();
        public readonly Reader Reader = new Reader();
        public MeshinaStationService Service;
        public Fixture() { Directory.CreateDirectory(DirectoryPath); Restart(); }
        public void Restart()
        {
            Service?.Stop();
            var settings = new MeshinaSettings { DataDirectory = DirectoryPath, StablePollCount = 2 };
            Service = new MeshinaStationService(settings, new MdbPoller(DirectoryPath), Reader, Gateway, _ => { });
        }
        public string File(string prefix, DateTime measuredUtc, DateTime? createdUtc = null)
        {
            string path = Path.Combine(DirectoryPath, prefix + "_" + measuredUtc.ToLocalTime().ToString("yyyyMMdd_HHmmss") + ".mdb");
            System.IO.File.WriteAllText(path, "test data");
            System.IO.File.SetCreationTimeUtc(path, createdUtc ?? measuredUtc);
            return path;
        }
    }
    private sealed class Reader : IMdbReader
    {
        public bool Fail;
        public MeshinaMeasurement Read(string path)
        {
            if (Fail) throw new IOException("MDB busy or incomplete");
            return new MeshinaMeasurement { Values = MdbReader.NumericFields.ToDictionary(f => f, _ => 1.23m), Result = "FAIL" };
        }
    }
    private sealed class Gateway : IMeshinaMesGateway
    {
        public int InCount, OutCount;
        public MeshinaMesOutcome InOutcome = MeshinaMesOutcome.Accepted, OutOutcome = MeshinaMesOutcome.Accepted;
        public SNCheckoutModel LastOut;
        public TaskCompletionSource<bool> InWait;
        public TaskCompletionSource<bool> OutWait;
        public async Task<MeshinaMesReply> FeedingCheckAsync(FeedingCheckModel request)
        {
            InCount++;
            if (InWait != null) await InWait.Task;
            return new MeshinaMesReply { Outcome = InOutcome };
        }
        public async Task<MeshinaMesReply> CheckOutAsync(SNCheckoutModel request)
        {
            OutCount++; LastOut = request;
            if (OutWait != null) await OutWait.Task;
            return new MeshinaMesReply { Outcome = OutOutcome };
        }
    }
}
