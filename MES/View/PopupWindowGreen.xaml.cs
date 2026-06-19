using MES.Manager;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MES.View
{
    /// <summary>
    /// PopupWindowGreen.xaml 的交互逻辑
    /// </summary>
    public partial class PopupWindowGreen : Window
    {
        Stopwatch sw = new Stopwatch();
        private string Message = "";

        public PopupWindowGreen(string MSG)
        {
            InitializeComponent();
            SetHelper.IsMsgGreenWindowOpen = true;
            sw.Restart();
            CloseWindowTimer();
            Message = MSG;
            this.Loaded += PopupWindow_Loaded;
            this.Closing += PopupWindow_Closing;

            
        }

        private void PopupWindow_Closing(object sender, CancelEventArgs e)
        {
            SetHelper.IsMsgGreenWindowOpen = false;
            sw.Stop();
            sw.Reset();
        }

        private void PopupWindow_Loaded(object sender, RoutedEventArgs e)
        {
            txtMessage.Text = Message;
            this.Activate();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        public void CloseWindowTimer()
        {
            try
            {
                Task.Run(() =>
                {
                    while (SetHelper.IsMsgGreenWindowOpen)
                    {
                        if (sw.ElapsedMilliseconds > 5 * 60 * 1000)
                        {
                            Dispatcher.BeginInvoke(() =>
                            {
                                this.Close();
                            });
                        }
                        Thread.Sleep(1000);
                    }
                });
            }
            catch (Exception ex)
            {

            }
        }
    }
}