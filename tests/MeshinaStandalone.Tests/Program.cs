using MeshinaStandalone;
using System.Globalization;
using System.IO;
internal static partial class Program
{
    private static int assertions;
    private static readonly string Root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cases", Guid.NewGuid().ToString("N"));
    private static async Task<int> Main(string[] args)
    {
        try
        {
            await HappyPath(); await FailedAndUncertainRequests(); await Recovery();
            await FileGuards(); await ConcurrentScans(); await AbortFlow();
            await Integration(args);
            Console.WriteLine($"PASS: {assertions} assertions");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
    private static MeshinaJob Request() => new MeshinaJob { FeedingCheckRequest = new FeedingCheckModel(), CheckOutRequest = new SNCheckoutModel() };
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
        Check(f.Gateway.LastOut.SNInfo[0].DC_Info.Length == 3, "uploads exactly three fields");
        Check(f.Gateway.LastOut.SNInfo[0].DC_Info.Single(x => x.Item == "Outshaft_ToothFi1").Value == "1.23", "invariant decimals");
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
