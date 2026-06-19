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
    public partial class PopupSeqView : Window
    {
        public string Name { get; set; }
        public int Number { get; set; }
        public string Msg { get; set; }
        public PopupSeqView(string name,int number,string msg)
        {
            Name = name;
            Number = number;
            Msg = msg;
            InitializeComponent();
            SetHelper.IsOpen[number] = true;

        }

        private void Window_Closed(object sender, EventArgs e)
        {
            ((PopupSeqViewModel)(this.DataContext)).isRun = false;
            Thread.Sleep(1000);
            SetHelper.IsOpen[Number] = false;
        }


    }
}
