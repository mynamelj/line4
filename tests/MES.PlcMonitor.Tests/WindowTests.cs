using MES.Manager;
using MES.SetModel;
using MES.View;
using MES.ViewModel;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

internal static class WindowTests
{
    public static void Run(Action<bool, string> check)
    {
        SetHelper.PLCSetting = new PLCSettingModel
        {
            ListGroup = new ObservableCollection<PLCGroup>
            {
                new PLCGroup
                {
                    GroupType = "ReadGroup", GroupName = "读取信号组",
                    ListTag = new ObservableCollection<PLCTag>
                    {
                        new PLCTag { TagName = "信号", DataType = "int", TagAddress = "0" },
                        new PLCTag { TagName = "信号_2", DataType = "int", TagAddress = "2" }
                    }
                },
                new PLCGroup { GroupType = "WriteGroup", GroupName = "写入信号组" }
            }
        };
        int reads = 0;
        SetHelper.siemens.Read = (group, name) => { Interlocked.Increment(ref reads); return (short)42; };
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        // Reproduce the reported startup condition: MainWindow exists but has not been shown.
        var shell = new Window();
        app.MainWindow = shell;
        var trace = new StringWriter();
        var listener = new TextWriterTraceListener(trace);
        PresentationTraceSources.DataBindingSource.Listeners.Add(listener);
        PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Error;
        try
        {
            var window = new PlcMonitorView();
            try
            {
                check(!shell.IsVisible && window.Owner == null, "construct monitor with an unshown main window without setting Owner");
                check(window.DataContext is PlcMonitorViewModel, "parameterless window constructs its own view model");
                window.WindowState = WindowState.Normal;
                window.WindowStartupLocation = WindowStartupLocation.Manual;
                window.Left = -20000;
                window.Top = -20000;
                window.ShowInTaskbar = false;
                window.ShowActivated = false;
                window.Show();
                var model = (PlcMonitorViewModel)window.DataContext;
                PumpUntil(() => model.SelectedGroup.Rows.All(row => row.Value == "42"));
                check(window.IsLoaded && window.IsVisible, "direct Show loads the monitor window");
                var tabs = Find<TabControl>(window);
                var grid = Find<DataGrid>(window);
                check(tabs?.Items.Count == 2 && grid?.Items.Count == 2, "group tabs and all tag rows bind in the actual WPF view");
                check(Find<ComboBox>(window) == null, "monitor has no station selector");
                check(trace.ToString().Length == 0, "monitor bindings have no WPF errors");
            }
            finally { window.Close(); }
            int stoppedReads = reads;
            PumpUntil(() => false, TimeSpan.FromMilliseconds(1100), false);
            check(reads == stoppedReads, "closing actual WPF window stops polling");
        }
        finally
        {
            PresentationTraceSources.DataBindingSource.Listeners.Remove(listener);
            listener.Dispose();
            shell.Close();
            app.Shutdown();
        }
    }

    private static T Find<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is T match) return match;
            var descendant = Find<T>(child);
            if (descendant != null) return descendant;
        }
        return null;
    }

    private static void PumpUntil(Func<bool> condition, TimeSpan? timeout = null, bool requireCondition = true)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(5));
        var frame = new DispatcherFrame();
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(10) };
        timer.Tick += (_, _) =>
        {
            if (condition() || DateTime.UtcNow >= deadline) frame.Continue = false;
        };
        timer.Start();
        try { Dispatcher.PushFrame(frame); }
        finally { timer.Stop(); }
        if (requireCondition && !condition()) throw new TimeoutException("WPF monitor did not reach expected state.");
    }
}
