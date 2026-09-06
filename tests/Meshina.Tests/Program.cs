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
            CorruptState();
            Check(MeshinaSettings.IsStationName("2020Meshina") && MeshinaSettings.IsStationName("op2020meshina"), "station aliases");
            Check(!MeshinaSettings.IsStationName("OP2020M") && !MeshinaSettings.IsStationName("OP2030Meshina"), "other stations unchanged");
            if (args.Length > 0)
            {
                var value = new MdbReader(args.Length > 1 ? args[1] : "Microsoft.ACE.OLEDB.12.0").Read(Path.GetFullPath(args[0]));
                Check(value.Values.Count + value.EmptyFields.Count == 20, "real MDB accounts for 20 numeric fields");
                Console.WriteLine("REAL MDB: " + string.Join(", ", value.Values.Select(v => v.Key + "=" + v.Value.ToString(CultureInfo.InvariantCulture))) + "; Result=" + value.Result);
                VerifyMdbGuards(args[0], args.Length > 1 ? args[1] : "Microsoft.ACE.OLEDB.12.0");
            }
            Console.WriteLine($"PASS: {assertions} assertions");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
    private static MeshinaJob Request() => new MeshinaJob { CheckInRequest = new SNCheckINModel(), CheckOutRequest = new SNCheckoutModel() };
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
        Check(f.Service.CanRetry && f.Service.Current.Stage == MeshinaStage.CheckInRejected, "retains failed checkin");
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
        Check(f.Service.NeedsConfirmation && !f.Service.CanRetry, "unknown result cannot blindly retry");
        int calls = f.Gateway.OutCount;
        await f.Service.TickAsync(); await f.Service.RetryAsync();
        Check(f.Gateway.OutCount == calls, "polling and retry cannot resend unknown request");
        await f.Service.ResolveUnknownAsync(false, f.Service.Current.Id);
        f.Gateway.OutOutcome = MeshinaMesOutcome.Accepted;
        await f.Service.RetryAsync();
        Check(ReferenceEquals(payload, f.Gateway.LastOut) && f.Service.Current == null, "retries same payload after confirmation");
        var g = new Fixture();
        g.Gateway.InOutcome = MeshinaMesOutcome.Unknown;
        await g.Service.ScanAsync("B", Request);
        Check(g.Service.NeedsConfirmation, "unknown checkin locked");
        await g.Service.ResolveUnknownAsync(true, g.Service.Current.Id);
        Check(g.Service.Current.Stage == MeshinaStage.WaitingForMdb, "confirmed checkin resumes");
        await g.Service.CancelAsync("previous-job-id");
        Check(g.Service.Current != null, "stale operator confirmation cannot cancel a different job");
        await g.Service.CancelAsync(g.Service.Current.Id);
        Check(g.Service.Current == null && g.Service.LastFinished.Stage == MeshinaStage.Cancelled, "manual cancellation archives job");
    }
    private static async Task Recovery()
    {
        var f = new Fixture();
        await f.Service.ScanAsync("RECOVER", Request);
        f.File("test", DateTime.UtcNow.AddSeconds(1));
        f.Restart();
        await f.Service.TickAsync(); await f.Service.TickAsync();
        Check(f.Service.Current == null && f.Gateway.InCount == 1 && f.Gateway.OutCount == 1, "recovers waiting job without repeating checkin");
        var g = new Fixture();
        await g.Service.ScanAsync("CRASH", Request);
        g.Gateway.OutOutcome = MeshinaMesOutcome.Rejected;
        g.File("test", DateTime.UtcNow.AddSeconds(1));
        await g.Service.TickAsync(); await g.Service.TickAsync();
        var state = g.Store.Load();
        state.Current.Stage = MeshinaStage.CheckOutSending;
        g.Store.Save(state); g.Restart();
        Check(g.Service.Current.Stage == MeshinaStage.CheckOutUncertain, "crash during send requires reconciliation");
        await g.Service.TickAsync();
        Check(g.Gateway.OutCount == 1, "restart does not resend uncertain checkout");
    }
    private static async Task FileGuards()
    {
        var f = new Fixture();
        await f.Service.ScanAsync("A", Request);
        f.File("old-copy", DateTime.UtcNow.AddMinutes(-2), DateTime.UtcNow.AddSeconds(1));
        await f.Service.TickAsync(); await f.Service.TickAsync();
        Check(f.Gateway.OutCount == 0, "rejects old measurement newly copied into directory");
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
        Check(g.Gateway.OutCount == 0 && g.Service.Status.Contains("2个"), "ambiguous files do not auto bind");
    }
    private static async Task ConcurrentScans()
    {
        var f = new Fixture();
        f.Gateway.InWait = new TaskCompletionSource<bool>();
        var first = f.Service.ScanAsync("FIRST", Request);
        await f.Service.ScanAsync("SECOND", Request);
        Check(f.Gateway.InCount == 1 && f.Service.Current.SN == "FIRST", "concurrent scans cannot replace SN");
        Check(!f.Service.TryStopForConfiguration(), "blocks configuration change during checkin");
        f.File("during-checkin", DateTime.UtcNow.AddSeconds(1));
        f.Gateway.InWait.SetResult(true); await first;
        await f.Service.TickAsync(); await f.Service.TickAsync();
        Check(f.Gateway.OutCount == 1, "file created during checkin not lost");
        Check(f.Service.TryStopForConfiguration(), "idle service allows configuration change");
        await f.Service.ScanAsync("AFTER-STOP", Request);
        Check(f.Gateway.InCount == 1, "stopped service refuses scans");
    }
    private static void CorruptState()
    {
        string path = Path.Combine(Root, "corrupt.json");
        File.WriteAllText(path, "{broken");
        bool rejected = false;
        try { new MeshinaStateStore(path).Load(); } catch { rejected = true; }
        Check(rejected, "corrupt state fails closed");
        File.WriteAllText(path, "{}");
        rejected = false;
        try { new MeshinaStateStore(path).Load(); } catch { rejected = true; }
        Check(rejected, "missing state properties fail closed");
    }
    private sealed class Fixture
    {
        public readonly string DirectoryPath = Path.Combine(Root, Guid.NewGuid().ToString("N"));
        public readonly Gateway Gateway = new Gateway();
        public readonly Reader Reader = new Reader();
        public MeshinaStateStore Store;
        public MeshinaStationService Service;
        public Fixture() { Directory.CreateDirectory(DirectoryPath); Store = new MeshinaStateStore(Path.Combine(DirectoryPath, "state.json")); Restart(); }
        public void Restart()
        {
            Service?.Stop();
            var settings = new MeshinaSettings { DataDirectory = DirectoryPath, StablePollCount = 2 };
            Service = new MeshinaStationService(settings, new MdbPoller(DirectoryPath), Reader, Gateway, Store, "test", _ => { })
            { ArchiveDirectory = Path.Combine(DirectoryPath, "history") };
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
        public async Task<MeshinaMesReply> CheckInAsync(SNCheckINModel request)
        {
            InCount++;
            if (InWait != null) await InWait.Task;
            return new MeshinaMesReply { Outcome = InOutcome };
        }
        public Task<MeshinaMesReply> CheckOutAsync(SNCheckoutModel request)
        {
            OutCount++; LastOut = request;
            return Task.FromResult(new MeshinaMesReply { Outcome = OutOutcome });
        }
    }
}
