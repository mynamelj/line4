using MES.Manager;
using MES.Service;
using MES.SetModel;
using System.Collections.ObjectModel;

internal static class Program
{
    private static int checks;

    private static int Main()
    {
        try
        {
            ScanRouting();
            RepairRouting();
            Console.WriteLine($"PASS: {checks} station layout assertions");
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

    private static void Layout(params string[] names)
    {
        SetHelper.StationNumber.numberGroups = new ObservableCollection<NumberGroup>(
            names.Select((name, index) => new NumberGroup { Number = index + 1, Name = name }));
    }

    private static void ScanRouting()
    {
        var service = new MiscService
        {
            SNPrefixes = new ObservableCollection<SNPrefix>
            {
                new SNPrefix { Name = "XP2030inputShaftSNPrefix", Value = "X5" },
                new SNPrefix { Name = "XP2030intermediateShaftSNPrefix", Value = "X6" },
                new SNPrefix { Name = "QR2030inputShaftSNPrefix", Value = "X7" },
                new SNPrefix { Name = "QR2030intermediateShaftSNPrefix", Value = "X8" }
            }
        };
        service.MapSNPrefixDic();

        Layout("OP2020PressFit1", "OP2020PressFit2");
        foreach (int gun in new[] { 0, 1 })
        {
            Check(service.ResolveStationIndex(gun, "X5-SN") == 0, "XP input shaft on either gun -> station 1");
            Check(service.ResolveStationIndex(gun, "X6-SN") == 1, "XP intermediate shaft on either gun -> station 2");
            Check(service.ResolveStationIndex(gun, "X7-SN") == 0, "QR input shaft on either gun -> station 1");
            Check(service.ResolveStationIndex(gun, "x8-SN") == 1, "QR intermediate shaft case insensitive -> station 2");
        }

        Check(StationRules.IsInputOrIntermediatePressFitStation("OP2020PressFit1"), "input press station enables routing");
        Check(StationRules.IsInputOrIntermediatePressFitStation("op2020pressfit2"), "intermediate press station enables routing");
        foreach (string name in new[] { "OP2020Meshing1", "OP2020Meshing2", "OP2030Meshing", "OP2030PressFit", "OP2040ShaftAssy", "OP2045Assy", "OP2030Meshing1", "", null })
            Check(!StationRules.IsInputOrIntermediatePressFitStation(name), "no press pair routing: " + name);

        // Resolving after a configuration change must not reuse the old positions.
        Layout("OP2020PressFit2", "OP2020PressFit1");
        Check(service.ResolveStationIndex(1, "X5-SN") == 1, "input follows current configuration order");
        Check(service.ResolveStationIndex(0, "X6-SN") == 0, "intermediate follows current configuration order");

        Layout("OP2020PressFit1");
        Check(service.ResolveStationIndex(1, "X6-SN") == -1, "missing intermediate station rejected");
        Layout("OP2020PressFit1", "OP2020PressFit1");
        Check(service.ResolveStationIndex(0, "X5-SN") == -1, "duplicate station rejected");
        Layout("OP2020Meshing1", "OP2020Meshing2");
        Check(service.ResolveStationIndex(0, "X5-SN") == -1, "independent meshing cannot receive press scan");

        Layout("OP2020PressFit1", "OP2020PressFit2");
        Check(service.ResolveStationIndex(-1, "X5-SN") == -1, "negative gun index rejected");
        Check(service.ResolveStationIndex(2, "X5-SN") == -1, "unknown gun rejected");
        Check(service.ResolveStationIndex(0, "") == -1, "empty scan rejected");
        Check(service.ResolveStationIndex(0, "UNKNOWN") == -1, "unknown prefix rejected");

        service.SNPrefixes[1].Value = "X5";
        service.MapSNPrefixDic();
        Check(service.ResolveStationIndex(1, "X5-SN") == -1, "same prefix across two stations rejected");
        service.SNPrefixes[1].Value = "X6";
        service.SNPrefixes[2].Value = "X5";
        service.MapSNPrefixDic();
        Check(service.ResolveStationIndex(1, "X5-SN") == 0, "shared XP/QR prefix for same station allowed");
        service.SNPrefixes[0].Value = "X";
        service.SNPrefixes[1].Value = "X6";
        service.MapSNPrefixDic();
        Check(service.ResolveStationIndex(0, "X6-SN") == 1, "longest matching prefix wins");
    }

    private static void RepairRouting()
    {
        Layout("OP2020PressFit1", "OP2020PressFit2");
        var pair = new DataManager();
        pair.TriggerRepairMode(true, "1");
        Check(pair.IsStationInRepair("OP2020PressFit1"), "station 1 sets input repair");
        Check(!pair.IsStationInRepair("OP2020PressFit2"), "input repair does not affect intermediate");
        pair.TriggerRepairMode(true, "2");
        pair.TriggerRepairMode(false, "1");
        Check(SetHelper.IsRepairMode && pair.IsStationInRepair("OP2020PressFit2"), "intermediate keeps repair enabled independently");
        pair.TriggerRepairMode(false, "2");
        Check(!SetHelper.IsRepairMode, "both disabled clears aggregate repair mode");

        Layout("OP2030PressFit", "OP2040ShaftAssy", "OP2045Assy");
        var shaft = new DataManager();
        shaft.TriggerRepairMode(true, "1");
        Check(shaft.IsStationInRepair("OP2030PressFit"), "layout 2 station 1 sets shaft press repair");
        Check(!shaft.IsStationInRepair("OP2020PressFit1"), "layout 2 station 1 cannot set input repair");
        shaft.TriggerRepairMode(false, "2");
        shaft.TriggerRepairMode(false, "3");
        Check(shaft.IsStationInRepair("OP2030PressFit"), "OP2040 and OP2045 cannot clear shaft press repair");
        shaft.TriggerRepairMode(false, "1");
        Check(!SetHelper.IsRepairMode, "shaft station clears own repair mode");
        shaft.TriggerRepairMode(true, "2");
        shaft.TriggerRepairMode(true, "3");
        Check(!SetHelper.IsRepairMode, "OP2040 and OP2045 cannot enable press repair by their positions");
        foreach (string number in new[] { "0", "4", "bad", null })
            shaft.TriggerRepairMode(true, number);
        Check(!SetHelper.IsRepairMode, "invalid station number cannot enable repair");

        Layout("OP2020PressFit2", "OP2020PressFit1");
        var reordered = new DataManager();
        reordered.TriggerRepairMode(true, "1");
        Check(reordered.IsStationInRepair("OP2020PressFit2"), "repair follows configured name rather than fixed position");
        Check(!reordered.IsStationInRepair("OP2020PressFit1"), "reordered station does not set other press station");
        reordered.TriggerRepairMode(false, "1");

        Layout("OP3040Assy");
        var separate = new DataManager();
        separate.TriggerRepairMode(true, "1");
        Check(SetHelper.IsOP3040RepairMode && !SetHelper.IsRepairMode, "OP3040 remains separate from shaft repair");
        separate.TriggerRepairMode(false, "1");
        Check(!SetHelper.IsOP3040RepairMode, "OP3040 mode can be disabled");
    }
}
