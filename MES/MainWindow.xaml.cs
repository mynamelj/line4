using MES.View;
using MES.ViewModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace MES
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        private DispatcherTimer timer;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //timer = new DispatcherTimer();
            //timer.Interval = TimeSpan.FromSeconds(60);
            //timer.Tick += Timer_Tick;
            //timer.Start();
        }
        private void Timer_Tick(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                this.WindowState = WindowState.Normal;
            }
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var tabControl = sender as System.Windows.Controls.TabControl;
            if (tabControl == null) return;

            var selectedTab = tabControl.SelectedItem as TabItem;
            if (selectedTab == null) return;

            // 获取当前选中的 Tab 标题
            string header = selectedTab.Header.ToString();

            // 根据选中的 Tab Header 来触发 ViewModel 中的相应操作
            switch (header)
            {
                case "物料批次号上线":
                  
                    break;
                default:
                    break;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var result = System.Windows.MessageBox.Show("确认关闭软件？", "Info", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.Cancel)
            {
                e.Cancel = true;
            }
            else
            {
                Environment.Exit(0);
            }
        }
    }
}