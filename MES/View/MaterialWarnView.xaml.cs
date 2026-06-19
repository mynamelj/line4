using MES.Manager;
using MES.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MES.View
{
    /// <summary>
    /// PopupSeqView.xaml 的交互逻辑
    /// </summary>
    public partial class MaterialWarnView : Window
    {
        public string Name { get; set; }
        public int Number { get; set; }
        public string Msg { get; set; }
        public static bool IsOpened { get; set; }
        public MaterialWarnView(string msg)
        {
            Msg = msg;
            InitializeComponent();
            MsgText.Text = Msg;     
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            IsOpened = false;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            IsOpened = true;
            MsgText.Text = Msg;
        }
    }
}
