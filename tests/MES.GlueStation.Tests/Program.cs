using MES.Manager;

internal static class Program
{
    private static int checks;

    private static async Task<int> Main()
    {
        try
        {
            LocationsAndRouting();
            LoadedGlueSelection();
            ModelGlueSelection();
            await FeedingChecks();
            Console.WriteLine($"PASS: {checks} glue station assertions");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static void Check(bool condition, string description)
    {
        if (!condition) throw new Exception("FAILED: " + description);
        checks++;
    }

    private static void LocationsAndRouting()
    {
        Check(!GlueStationRules.IsGlueStation("0"), "ordinary station needs no glue");
        Check(GlueStationRules.IsGlueStation("1") && GlueStationRules.IsGlueStation("2"),
            "single and dual configurations both enable glue checks");
        Check(!GlueStationRules.GetLocations("Glue", "0").Any(), "ordinary station has no loading option");
        Check(GlueStationRules.GetLocations("Glue", "1").SequenceEqual(new[] { "Glue" }), "single glue keeps existing name");
        Check(GlueStationRules.GetLocations("Glue", "2").SequenceEqual(new[] { "Glue" }),
            "dual glue keeps one real station option; type is selected separately");

        var stations = new[] { "Other", "OP-Glue", "OtherGlue" };
        Check(GlueStationRules.ResolveStationIndex("OP-Glue", stations) == 1,
            "loading and unloading resolve actual station, including hyphens");
        foreach (var location in new[] { "OP-Glue-1", "OP-Glue-2" })
            Check(GlueStationRules.ResolveStationIndex(location, stations) == -1,
                "appended suffix is not resolved to a station: " + location);
        Check(GlueStationRules.ResolveStationIndex("OtherGlue", stations) == 2,
            "filtered dropdown position does not become MES station index");
        Check(GlueStationRules.ResolveStationIndex("OtherGlue", stations.Reverse()) == 0,
            "station resolution follows current configuration");
        Check(GlueStationRules.ResolveStationIndex("Unknown-1", stations) == -1, "unknown station rejected");
        Check(GlueStationRules.ResolveStationIndex(null, stations) == -1, "missing station rejected");
        Check(GlueStationRules.ResolveStationIndex("Glue-1", new[] { "Glue", "Glue-1" }) == 1,
            "actual station name is matched literally");
    }

    private static void LoadedGlueSelection()
    {
        Check(GlueStationRules.MatchesStation("Glue", "Glue"), "loaded glue matches real station name");
        foreach (var location in new[] { "Glue-1", "Glue-2" })
            Check(!GlueStationRules.ShouldReplace(location, GlueStationRules.SecondType, "Glue", "2", GlueStationRules.SecondType),
                "suffixed location is not treated as loaded glue at base station: " + location);
        Check(GlueStationRules.ShouldReplace("Glue", null, "Glue", "1", null), "single glue matches station only");
        Check(!GlueStationRules.ShouldReplace("Other", null, "Glue", "1", null), "single glue leaves other stations alone");
        Check(!GlueStationRules.ShouldReplace("Glue", null, "Glue", "2", GlueStationRules.FirstType),
            "dual glue does not treat null MesMsg as first type");
        Check(!GlueStationRules.ShouldReplace("Glue", "unknown", "Glue", "2", GlueStationRules.FirstType),
            "dual glue does not convert an unknown MesMsg to first type");
        foreach (var newType in new[] { "小鹏1", "小鹏2", "奇瑞1", "奇瑞2" })
        {
            var records = new[]
            {
                (Location: "Glue", Type: "小鹏1"),
                (Location: "Glue", Type: "小鹏2"),
                (Location: "Glue", Type: "奇瑞1"),
                (Location: "Glue", Type: "奇瑞2"),
                (Location: "Other", Type: newType)
            };
            var replaced = records.Where(record =>
                GlueStationRules.ShouldReplace(record.Location, record.Type, "Glue", "2", newType)).ToList();
            Check(replaced.Count == 1 && replaced[0].Location == "Glue"
                && replaced[0].Type == newType,
                "replacement selects only same station and type: " + newType);
        }
        foreach (var location in new[] { "Other", "Other-1", "Other-2", "Glue-3", "Glue-extra", null, "" })
            Check(!GlueStationRules.MatchesStation(location, "Glue"), "unrelated location excluded: " + location);
        Check(!GlueStationRules.MatchesStation("Glue-2", "Glue"), "station matching does not strip suffixes");
    }

    private static void ModelGlueSelection()
    {
        Check(GlueStationRules.GetGlueType("小鹏", GlueStationRules.FirstType) == "小鹏1", "XiaoPeng first type persisted");
        Check(GlueStationRules.GetGlueType("小鹏", GlueStationRules.SecondType) == "小鹏2", "XiaoPeng second type persisted");
        Check(GlueStationRules.GetGlueType("奇瑞", GlueStationRules.FirstType) == "奇瑞1", "QiRui first type persisted");
        Check(GlueStationRules.GetGlueType("奇瑞", GlueStationRules.SecondType) == "奇瑞2", "QiRui second type persisted");
        Check(GlueStationRules.GetGlueType(null, GlueStationRules.FirstType) == null, "missing model rejected");
        Check(GlueStationRules.GetGlueType("unknown", GlueStationRules.FirstType) == null, "unknown model rejected");
        Check(GlueStationRules.GetGlueType("小鹏", null) == null, "missing type rejected");
        Check(GlueStationRules.GetGlueType("小鹏", "unknown") == null, "unknown type rejected");

        var records = new[]
        {
            (Location: "Glue", Type: "小鹏1", Code: "XP1"),
            (Location: "Glue", Type: "小鹏2", Code: "XP2"),
            (Location: "Glue", Type: "奇瑞1", Code: "QR1"),
            (Location: "Glue", Type: "奇瑞2", Code: "QR2"),
            (Location: "Other", Type: "奇瑞1", Code: "OTHER"),
            (Location: "Glue", Type: GlueStationRules.FirstType, Code: "LEGACY1"),
            (Location: "Glue", Type: GlueStationRules.SecondType, Code: "LEGACY2"),
            (Location: "Glue", Type: (string)null, Code: "UNTYPED"),
            (Location: "Glue", Type: "unknown", Code: "UNKNOWN")
        };
        foreach (int productId in new[] { 1, 24, 25, 26 })
        {
            var selected = records.Where(record => GlueStationRules.MatchesStation(record.Location, "Glue")
                && GlueStationRules.MatchesProduct(record.Type, "2", productId)).Select(record => record.Code).ToArray();
            var expected = productId == 25 ? new[] { "QR1", "QR2" } : new[] { "XP1", "XP2" };
            Check(selected.SequenceEqual(expected), "check-in and checkout select only model glue: " + productId);
            foreach (var onlyGlue in records.Take(4))
            {
                var codes = new[] { onlyGlue }.Where(record => GlueStationRules.MatchesProduct(record.Type, "2", productId));
                Check(codes.Any() == expected.Contains(onlyGlue.Code), "one type allowed only for matching model");
            }
            Check(GlueStationRules.MatchesProduct(null, "1", productId), "single glue remains independent of model");
            Check(GlueStationRules.MatchesProduct("legacy", "1", productId), "single glue accepts existing records");
        }
    }

    private static async Task FeedingChecks()
    {
        var calls = new List<string>();
        Task<(bool, string)> Pass(string code)
        {
            calls.Add(code);
            return Task.FromResult((true, "PASS"));
        }

        var response = await GlueStationRules.CheckAsync(Array.Empty<string>(), Pass);
        Check(!response.Success && calls.Count == 0, "no glue blocks machining without MES calls");
        response = await GlueStationRules.CheckAsync(new[] { "", " " }, Pass);
        Check(!response.Success && calls.Count == 0, "empty records do not count as loaded glue");
        foreach (var onlyGlue in new[] { "GLUE-SLOT1", "GLUE-SLOT2", "LEGACY-GLUE" })
        {
            calls.Clear();
            response = await GlueStationRules.CheckAsync(new[] { onlyGlue }, Pass);
            Check(response.Success && calls.SequenceEqual(new[] { onlyGlue }),
                "one glue is sufficient and checked exactly once: " + onlyGlue);
        }

        calls.Clear();
        response = await GlueStationRules.CheckAsync(new[] { "GLUE1", "GLUE2" }, Pass);
        Check(response.Success && calls.SequenceEqual(new[] { "GLUE1", "GLUE2" }), "two glue codes get separate MES requests");

        foreach (string failingCode in new[] { "GLUE1", "GLUE2", "BOTH" })
        {
            calls.Clear();
            response = await GlueStationRules.CheckAsync(new[] { "GLUE1", "GLUE2" }, code =>
            {
                calls.Add(code);
                return Task.FromResult((failingCode != "BOTH" && code != failingCode, "MES reject"));
            });
            Check(!response.Success, "any failed check blocks machining: " + failingCode);
            Check(calls.SequenceEqual(new[] { "GLUE1", "GLUE2" }), "all loaded glue checked despite rejection");
            Check(response.Message.Contains("MES reject") && response.Message.Contains(failingCode == "BOTH" ? "GLUE2" : failingCode),
                "failure identifies glue code and MES reason");
        }

        calls.Clear();
        response = await GlueStationRules.CheckAsync(new[] { "GLUE1", "GLUE2" }, code =>
        {
            if (code == "GLUE1") throw new InvalidOperationException("transport failed");
            return Pass(code);
        });
        Check(!response.Success && response.Message.Contains("GLUE1") && response.Message.Contains("transport failed"),
            "exception fails aggregate result");
        Check(calls.SequenceEqual(new[] { "GLUE2" }), "exception does not skip remaining glue");

        var firstReply = new TaskCompletionSource<(bool, string)>();
        calls.Clear();
        var pending = GlueStationRules.CheckAsync(new[] { "GLUE1", "GLUE2" }, code =>
        {
            calls.Add(code);
            return code == "GLUE1" ? firstReply.Task : Task.FromResult((true, "PASS"));
        });
        Check(!pending.IsCompleted && calls.SequenceEqual(new[] { "GLUE1" }), "wait for first MES reply before second request");
        firstReply.SetResult((false, "FIRST FAIL"));
        response = await pending;
        Check(!response.Success && calls.SequenceEqual(new[] { "GLUE1", "GLUE2" }),
            "later success cannot overwrite earlier failure");
    }
}
