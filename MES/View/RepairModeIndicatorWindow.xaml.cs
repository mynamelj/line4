using System.Windows;

namespace MES.View
{
    /// <summary>
    /// RepairModeIndicatorWindow.xaml 的交互逻辑
    /// 简单窗口：屏幕右下角、置顶、透明背景、红色文字，用于提示返修模式开启状态。
    /// </summary>
    public partial class RepairModeIndicatorWindow : Window
    {
        public RepairModeIndicatorWindow()
        {
            InitializeComponent();
            this.Loaded += RepairModeIndicatorWindow_Loaded;
        }

        private void RepairModeIndicatorWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var workArea = SystemParameters.WorkArea;
            this.Left = workArea.Right - this.Width - 10;
            this.Top = workArea.Bottom - this.Height - 10;
        }
    }
}
