using CommunityToolkit.Mvvm.ComponentModel;
using MES.Manager;
using MES.SetModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MES.ViewModel
{
    public sealed partial class PlcMonitorRow : ObservableObject
    {
        public PLCTag Tag { get; }
        // 仅用于查询现有 PLC 驱动，不修改界面显示的配置名称。
        public string RuntimeTagName => GetRuntimeTagName(Tag.TagName);
        public PlcMonitorRow(PLCTag tag) { Tag = tag; }
        [ObservableProperty]
        private string _value = "null";
        public static string GetRuntimeTagName(string name)
        {
            // 与 SiemensS7Instrument.InitPara 的名称转换保持一致。
            int index = name?.LastIndexOf('_') ?? -1;
            return index > 0 && index + 1 < name.Length && name[index + 1] >= '1' && name[index + 1] <= '9'
                ? name : name + "_1";
        }
        public static string FormatValue(object value) => value is System.Collections.IEnumerable items && !(value is string)
            ? string.Join(", ", items.Cast<object>().Select(FormatValue)) : value?.ToString() ?? "null";
    }

    public sealed class PlcMonitorGroup
    {
        public PLCGroupName Group { get; }
        public string Title { get; }
        public ObservableCollection<PlcMonitorRow> Rows { get; } = new ObservableCollection<PlcMonitorRow>();
        public PlcMonitorGroup(PLCGroupName group, PLCGroup config)
        {
            Group = group;
            Title = string.IsNullOrWhiteSpace(config.GroupName) ? group.ToString() : config.GroupName;
            var tags = config.ListTag;
            if (tags != null)
                foreach (var tag in tags.Where(x => !string.IsNullOrWhiteSpace(x.TagName)))
                    Rows.Add(new PlcMonitorRow(tag));
        }
    }

    public sealed partial class PlcMonitorViewModel : ObservableObject, IDisposable
    {
        private PLCSettingModel _snapshot;
        private CancellationTokenSource _lifetime;
        private bool _disposed;
        private int _selectionVersion;
        public ObservableCollection<PlcMonitorGroup> Groups { get; } = new ObservableCollection<PlcMonitorGroup>();
        [ObservableProperty]
        private PlcMonitorGroup _selectedGroup;
        partial void OnSelectedGroupChanged(PlcMonitorGroup value)
        {
            Interlocked.Increment(ref _selectionVersion);
            if (value != null) foreach (var row in value.Rows) row.Value = "null";
        }
        public PlcMonitorViewModel()
        {
            Reload();
        }
        private void Reload()
        {
            Interlocked.Increment(ref _selectionVersion);
            _snapshot = SetHelper.PLCSetting;
            Groups.Clear();
            if (_snapshot?.ListGroup != null)
            {
                foreach (var config in _snapshot.ListGroup)
                {
                    if (!Enum.TryParse(config.GroupType, out PLCGroupName group) || !Enum.IsDefined(typeof(PLCGroupName), group)) continue;
                    var item = new PlcMonitorGroup(group, config);
                    Groups.Add(item);
                }
            }
            SelectedGroup = Groups.FirstOrDefault();
        }
        public async Task StartAsync()
        {
            if (_disposed || _lifetime != null) return;
            _lifetime = new CancellationTokenSource();
            var token = _lifetime.Token;
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var cycle = Stopwatch.StartNew();
                    if (!IsCurrentConfiguration()) Reload();
                    var group = SelectedGroup;
                    var rows = group?.Rows.ToList() ?? new List<PlcMonitorRow>();
                    int version = _selectionVersion;
                    foreach (var row in rows)
                    {
                        if (token.IsCancellationRequested || version != _selectionVersion) break;
                        var value = await Task.Run(() =>
                        {
                            if (group == null || token.IsCancellationRequested || version != Volatile.Read(ref _selectionVersion) || !IsCurrentConfiguration()) return null;
                            object result = null;
                            try { return SetHelper.siemens.ReadItem(group.Group, row.RuntimeTagName, ref result) ? result : null; }
                            catch { return null; }
                        });
                        if (!token.IsCancellationRequested && version == _selectionVersion && IsCurrentConfiguration())
                            row.Value = PlcMonitorRow.FormatValue(value);
                    }
                    await Task.Delay(Math.Max(1, 1000 - (int)Math.Min(cycle.ElapsedMilliseconds, 1000)), token);
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        }
        private bool IsCurrentConfiguration() => ReferenceEquals(_snapshot, SetHelper.PLCSetting);

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _lifetime?.Cancel();
            _lifetime?.Dispose();
        }
    }
}
