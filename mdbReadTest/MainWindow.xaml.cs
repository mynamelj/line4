using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace MdbReadTest
{
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        private bool busy;

        public MainWindow()
        {
            InitializeComponent();
            timer.Tick += async (s, e) => { if (AutoBox.IsChecked == true) await RefreshAsync(); };
            Loaded += async (s, e) => { timer.Start(); await RefreshAsync(); };
            Closed += (s, e) => timer.Stop();
        }

        private async void ReadClick(object sender, RoutedEventArgs e) => await RefreshAsync();

        private async Task RefreshAsync()
        {
            if (busy) return;
            busy = true;
            ReadButton.IsEnabled = false;
            string directory = DirectoryBox.Text.Trim();
            string provider = ProviderBox.SelectedIndex == 0 ? null : (string)((ComboBoxItem)ProviderBox.SelectedItem).Content;
            StatusText.Text = "正在只读访问最新 MDB…";
            try
            {
                var data = await Task.Run(() => DatabaseReader.ReadLatest(directory, provider));
                string selectedTable = (TablesTabs.SelectedItem as TabItem)?.Tag as string;
                TablesTabs.Items.Clear();
                foreach (var table in data.Tables)
                {
                    var tab = new TabItem
                    {
                        Header = table.TableName + "（" + table.Rows.Count + " 行）",
                        Tag = table.TableName,
                        Content = new DataGrid
                        {
                            ItemsSource = table.DefaultView, IsReadOnly = true, AutoGenerateColumns = true,
                            CanUserAddRows = false, CanUserDeleteRows = false,
                            EnableRowVirtualization = true, EnableColumnVirtualization = true
                        }
                    };
                    TablesTabs.Items.Add(tab);
                    if (table.TableName == selectedTable) TablesTabs.SelectedItem = tab;
                }
                if (TablesTabs.SelectedIndex < 0 && TablesTabs.Items.Count > 0) TablesTabs.SelectedIndex = 0;
                FileText.Text = data.Path + "\n文件修改时间：" + data.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss");
                StatusText.Foreground = Brushes.DarkGreen;
                StatusText.Text = $"{DateTime.Now:HH:mm:ss} 读取成功 · {data.Provider} · {data.Tables.Count} 张表，共 {data.Tables.Sum(t => t.Rows.Count)} 行。按最后修改时间选择最新文件，只读原文件。";
            }
            catch (Exception ex)
            {
                TablesTabs.Items.Clear();
                FileText.Text = "读取目录：" + directory;
                StatusText.Foreground = Brushes.Firebrick;
                StatusText.Text = $"{DateTime.Now:HH:mm:ss} 读取失败 · {ex.GetType().Name} (0x{ex.HResult:X8})\n{ex.Message}\n开启自动刷新时会继续重试。只读访问不能绕过对方的独占锁。";
            }
            finally { busy = false; ReadButton.IsEnabled = true; }
        }
    }
}
