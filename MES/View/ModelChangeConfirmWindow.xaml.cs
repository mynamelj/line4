using System.Windows;

namespace MES.View
{
    /// <summary>
    /// ModelChangeConfirmWindow.xaml 的交互逻辑
    /// 简单窗口：当进站机型厂商(奇瑞/小鹏)与上一次不同时弹出提示，需用户点击确认按钮关闭。
    /// </summary>
    public partial class ModelChangeConfirmWindow : Window
    {
        public ModelChangeConfirmWindow(string message)
        {
            InitializeComponent();
            txtMessage.Text = message;
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
