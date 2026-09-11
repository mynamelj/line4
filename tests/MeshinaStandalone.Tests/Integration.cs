using MeshinaStandalone;
using System.Data.OleDb;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static partial class Program
{
    private static async Task Integration(string[] args)
    {
        var config = Configuration.Load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "configs"));
        var selected = config.Resolve(1);
        Check(selected.system.StationNumber == selected.api.StationNumber, "config matches station number");
        var framing = new BarcodeFramer("\r\n");
        Check(framing.Push("ABC\r").Count == 0, "serial partial packet waits");
        Check(framing.Push("\nDEF\r\nG").SequenceEqual(new[] { "ABC", "DEF" }), "serial packets split into separate SNs");
        Check(framing.Push("HI\r\n").Single() == "GHI", "serial trailing partial packet retained");
        var files = Configuration.FileNames.ToDictionary(n => n, n => File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "configs", n)));
        var stationJson = JObject.Parse(files["stationNumber.json"]);
        stationJson["numberGroups"] = JArray.Parse("[{ 'Number': 8, 'Name': 'OP2030Meshina2' },{ 'Number': 1, 'Name': 'OP2020M' }]");
        files["stationNumber.json"] = stationJson.ToString();
        var sysJson = JObject.Parse(files["sys.json"]);
        var extraSys = (JObject)sysJson["ListGroup"][0].DeepClone(); extraSys["StationNumber"] = 8; extraSys["StationID"] = "STATION8";
        ((JArray)sysJson["ListGroup"]).Insert(0, extraSys); files["sys.json"] = sysJson.ToString();
        var apiJson = JObject.Parse(files["api.json"]);
        var extraApi = (JObject)apiJson["ListGroup"][0].DeepClone(); extraApi["StationNumber"] = 8;
        ((JArray)apiJson["ListGroup"]).Add(extraApi); files["api.json"] = apiJson.ToString();
        var mixed = Configuration.Parse(files);
        Check(mixed.Resolve(8).system.StationID == "STATION8" && mixed.Resolve(1).api.StationNumber == 1, "different JSON array orders resolve by number");
        Check(!mixed.UsesSerial(mixed.Resolve(1).station) && mixed.UsesSerial(mixed.Resolve(8).station), "auto USB and serial selection");
        var job = Configuration.CreateJob(selected.system, selected.api, "OP-TEST");
        job.FeedingCheckRequest.SN = "SCANNED";
        Check(job.FeedingCheckRequest.EventID == "SN_FeedingCheck" && job.CheckOutRequest.EventID == "SN_CheckOut" && job.CheckOutRequest.OPID == "OP-TEST", "request metadata compatibility");
        var transport = new FakeHttp();
        using (var gateway = new MeshinaMesGateway(selected.api, _ => { }, transport))
        {
            var reply = await gateway.FeedingCheckAsync(job.FeedingCheckRequest);
            Check(reply.Outcome == MeshinaMesOutcome.Accepted && transport.Last["SN"].ToString() == "SCANNED", "feeding JSON and PASS response");
            Check(transport.Uri.AbsolutePath.EndsWith("/StandAlone/SN_FeedingCheck"), "configured endpoint path");
            transport.Body = "{\"Result\":\"FAIL\",\"MSG\":\"rejected\"}";
            Check((await gateway.FeedingCheckAsync(job.FeedingCheckRequest)).Outcome == MeshinaMesOutcome.Rejected, "FAIL response classified");
            transport.Body = "not JSON";
            Check((await gateway.FeedingCheckAsync(job.FeedingCheckRequest)).Outcome == MeshinaMesOutcome.Unknown, "invalid response remains unknown");
            transport.Status = HttpStatusCode.InternalServerError; transport.Body = "{\"Result\":\"PASS\"}";
            Check((await gateway.FeedingCheckAsync(job.FeedingCheckRequest)).Outcome == MeshinaMesOutcome.Unknown, "HTTP failure cannot be PASS");
            transport.Throw = true; int before = transport.Calls;
            Check((await gateway.FeedingCheckAsync(job.FeedingCheckRequest)).Outcome == MeshinaMesOutcome.Unknown && transport.Calls == before + 1, "timeout is not automatically retried");
        }
        if (args.Length > 0)
        {
            string provider = "Microsoft.Jet.OLEDB.4.0";
            var reader = new MdbReader(provider);
            var data = reader.Read(args[0]);
            Check(data.Values.Keys.SequenceEqual(new[] { "Fi", "fii", "Fr" }), "real MDB reads only three fields");
            using (var handle = File.Open(args[0], FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                Check(reader.Read(args[0]).Values.Count == 3, "shared-read source without delete sharing remains readable");
            var realDir = Path.Combine(Root, "real"); Directory.CreateDirectory(realDir);
            var realSettings = new MeshinaSettings { DataDirectory = realDir, StablePollCount = 2 };
            var realHttp = new FakeHttp();
            using (var gateway = new MeshinaMesGateway(selected.api, _ => { }, realHttp))
            {
                var service = new MeshinaStationService(realSettings, new MdbPoller(realDir), reader, gateway, _ => { });
                await service.ScanAsync("REAL-SCANNED-SN", () => Configuration.CreateJob(selected.system, selected.api, "OP-TEST"));
                string copied = Path.Combine(realDir, "unrelated-name.mdb"); File.Copy(args[0], copied);
                File.SetCreationTimeUtc(copied, DateTime.UtcNow.AddSeconds(1));
                await service.TickAsync(); await service.TickAsync();
                var sn = realHttp.Last["SNInfo"][0]; var values = (JArray)sn["DC_Info"];
                Check(realHttp.Calls == 2 && service.Current == null && sn["SN"].ToString() == "REAL-SCANNED-SN", "real MDB to HTTP JSON full flow");
                Check(values.Select(v => v["Item"].ToString()).SequenceEqual(new[] { "Outshaft_ToothFi1", "Outshaft_ToothFi2", "Outshaft_ToothFi3" }), "all three MES mappings in exact order");
                Check(values.Select(v => decimal.Parse(v["Value"].ToString(), System.Globalization.CultureInfo.InvariantCulture)).SequenceEqual(data.Values.Values), "mapped values preserve Fi fii Fr order");
                Console.WriteLine("CHECKOUT JSON: " + realHttp.Last.ToString(Formatting.None));
                void Execute(string sql)
                {
                    using var connection = new OleDbConnection($"Provider={provider};Data Source={copied};"); connection.Open();
                    using var command = connection.CreateCommand(); command.CommandText = sql; command.ExecuteNonQuery();
                }
                Execute("UPDATE TJSHEET SET Fi=NULL");
                bool rejected = false; try { reader.Read(copied); } catch (InvalidDataException) { rejected = true; }
                Check(rejected, "real MDB rejects missing Fi");
                Execute("UPDATE TJSHEET SET Fi=0");
                Check(reader.Read(copied).Values["Fi"] == 0, "zero is valid");
                Execute("INSERT INTO TJSHEET SELECT * FROM TJSHEET");
                rejected = false; try { reader.Read(copied); } catch (InvalidDataException) { rejected = true; }
                Check(rejected, "multiple records cannot bind automatically");
                Execute("DELETE FROM TJSHEET");
                rejected = false; try { reader.Read(copied); } catch (InvalidDataException) { rejected = true; }
                Check(rejected, "empty MDB waits for data");
            }
        }
    }
    private sealed class FakeHttp : HttpMessageHandler
    {
        public int Calls; public JObject Last; public Uri Uri;
        public string Body = "{\"Result\":\"PASS\",\"Msg\":\"OK\"}";
        public HttpStatusCode Status = HttpStatusCode.OK;
        public bool Throw;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++; Uri = request.RequestUri; Last = JObject.Parse(await request.Content.ReadAsStringAsync());
            if (Throw) throw new TaskCanceledException("simulated timeout");
            return new HttpResponseMessage(Status) { Content = new StringContent(Body) };
        }
    }
}
