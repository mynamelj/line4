using MES.Manager;
using MES.SetModel;
using MES.ViewModel;
using System.Collections;
using System.Collections.ObjectModel;

internal static class Program
{
    private static int checks;

    [STAThread]
    private static int Main()
    {
        try
        {
            TagFormatting();
            MonitorLifecycle().GetAwaiter().GetResult();
            WindowTests.Run(Check);
            Console.WriteLine($"PASS: {checks} PLC monitor assertions (no PLC connection)");
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

    private static void TagFormatting()
    {
        Check(PlcMonitorRow.GetRuntimeTagName("信号") == "信号_1", "implicit runtime suffix matches the PLC driver");
        Check(PlcMonitorRow.GetRuntimeTagName("信号_说明") == "信号_说明_1", "descriptive underscore matches the PLC driver");
        Check(PlcMonitorRow.GetRuntimeTagName("信号_2") == "信号_2", "explicit suffix preserved");
        Check(PlcMonitorRow.FormatValue(new short[] { 1, -2 }) == "1, -2", "integer array formatted");
        Check(PlcMonitorRow.FormatValue(new BitArray(new[] { true, false })) == "True, False", "bit array formatted");
        Check(PlcMonitorRow.FormatValue(null) == "null", "failed read formatted");
    }

    private static async Task MonitorLifecycle()
    {
        var tags = new ObservableCollection<PLCTag>
        {
            new PLCTag { TagName = "信号", DataType = "int", TagAddress = "0" },
            new PLCTag { TagName = "信号_2", DataType = "int", TagAddress = "2" }
        };
        SetHelper.PLCSetting = new PLCSettingModel
        {
            ListGroup = new ObservableCollection<PLCGroup>
            {
                new PLCGroup { GroupName = "读取信号组", GroupType = "ReadGroup", ListTag = tags },
                new PLCGroup { GroupName = "写入信号组", GroupType = "WriteGroup" },
                new PLCGroup { GroupName = "报警信号组", GroupType = "AlarmGroup" },
                new PLCGroup { GroupName = "数据采集信号组", GroupType = "DataCollectionGroup" }
            }
        };
        using var monitor = new PlcMonitorViewModel();
        Check(monitor.Groups.Count == 4, "all configured signal groups appear, including alarm and data collection");
        Check(monitor.Groups[2].Title == "报警信号组", "configured group names preserved");
        var group = monitor.SelectedGroup;
        Check(group.Rows.Select(row => row.Tag).SequenceEqual(tags), "all tags visible without station configuration");
        Check(tags[0].TagName == "信号", "displayed tag names remain unchanged");

        int reads = 0;
        SetHelper.siemens.Read = (kind, name) =>
        {
            Interlocked.Increment(ref reads);
            return name == "信号_2" ? (object)(short)42 : (short)11;
        };
        var polling = monitor.StartAsync();
        await Until(() => group.Rows[1].Value == "42");
        Check(reads == 2 && group.Rows[0].Value == "11", "poll every tag in the selected group");
        SetHelper.siemens.Read = (kind, name) => throw new InvalidOperationException("disconnected");
        await Until(() => group.Rows.All(row => row.Value == "null"));
        Check(group.Rows.All(row => row.Value == "null"), "read exception clears stale values");

        SetHelper.PLCSetting = new PLCSettingModel();
        await Until(() => !ReferenceEquals(group, monitor.SelectedGroup));
        Check(monitor.Groups.Count == 0 && monitor.SelectedGroup == null, "empty configuration clears groups and selection");
        monitor.Dispose();
        await polling;
        int stoppedReads = reads;
        await monitor.StartAsync();
        Check(reads == stoppedReads, "closed monitor cannot restart polling");
    }

    private static async Task Until(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!condition())
        {
            if (DateTime.UtcNow > deadline) throw new TimeoutException("Monitor did not reach expected state.");
            await Task.Delay(10);
        }
    }
}
