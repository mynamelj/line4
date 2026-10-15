using MES.Manager;
using MES.MesModel.Response;
using Newtonsoft.Json;

internal static class FoolproofTests
{
    public static void Run(Action<bool, string> check)
    {
        const string json = "{\"EventID\":\"SN_CheckIn\",\"Result\":\"PASS\",\"Msg\":\"FD\",\"SN\":\"FD1010DYH\",\"CarrierID\":\"\"}";
        var response = JsonConvert.DeserializeObject<SNCheckINResponse>(json);
        check(FoolproofTestRules.IsFoolproofTest(response.Result, response.MSG.ToString()), "MES sample recognizes PASS with Msg FD");
        check(response.MSG.ToString() == "FD" && response.SN == "FD1010DYH", "MES sample preserves Msg and SN");
        check(FoolproofTestRules.CheckInResult == 7, "foolproof PLC result is 7");
        check(FoolproofTestRules.IsFoolproofTest("fdcsj"), "MES result comparison ignores case");
        foreach (string result in new[] { "PASS", "REPAIR", "FAIL", "prefix_FDCSJ", "", null })
            check(!FoolproofTestRules.IsFoolproofTest(result), "only FDCSJ identifies foolproof parts: " + result);

        check(FoolproofTestRules.IsFoolproofTest("pass", "fd"), "MES result and message comparison ignores case");
        foreach (string message in new[] { "DJ1", "prefix_FD", "FDCSJ", "", null })
            check(!FoolproofTestRules.IsFoolproofTest("PASS", message), "normal PASS message is not foolproof: " + message);
        foreach (string result in new[] { "FAIL", "REPAIR", "FDCSJ", "", null })
            check(!FoolproofTestRules.IsFoolproofTest(result, "FD"), "FD message without PASS is not accepted: " + result);

        // Mixed checkout group: normal/foolproof tags, overlapping item names and station numbers.
        string[] tags = { "Force_1", "fd_Force_1", "fd_Force_Max_1", "FD_Status_1", "fd_Force_2", "fd_Force_10", "Force_10", "Other_fd_Value_1" };
        var foolproof = tags.Where(tag => FoolproofTestRules.ShouldReadCheckOutTag(tag, "1", 7)).ToArray();
        check(foolproof.SequenceEqual(new[] { "fd_Force_1", "fd_Force_Max_1", "FD_Status_1" }),
            "foolproof station 1 reads only its prefixed tags");
        check(foolproof.Select(FoolproofTestRules.GetMesItemName).SequenceEqual(new[] { "Force", "Force_Max", "Status" }),
            "MES item names strip fd_ and station suffix while preserving internal underscores");
        foreach (int result in new[] { 1, 2, 3, 4, 5, 6 })
        {
            var normal = tags.Where(tag => FoolproofTestRules.ShouldReadCheckOutTag(tag, "1", result)).ToArray();
            check(normal.SequenceEqual(new[] { "Force_1", "Other_fd_Value_1" }),
                "non-foolproof result excludes prefixed tags: " + result);
        }
        check(FoolproofTestRules.GetMesItemName("Other_fd_Value_1") == "Other_fd_Value", "fd_ inside an item is preserved");
        check(FoolproofTestRules.GetMesItemName("fd_fd_Force_10") == "fd_Force", "only the first fd_ prefix is removed");
        check(!tags.Any(tag => FoolproofTestRules.ShouldReadCheckOutTag(tag, "3", 7)), "missing foolproof configuration selects no normal tags");
    }
}
