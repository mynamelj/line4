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
    public partial class OP1010View : Window
    {
        public string Name { get; set; }
        public int Number { get; set; }
        public string Msg { get; set; }
        public OP1010View(string msg, string name)
        {
            Msg = msg;
            InitializeComponent();
            MsgText.Text = Msg;
            txtTitle.Text = name;
        }

        private void Window_Closed(object sender, EventArgs e)
        {

        }


    }
}
