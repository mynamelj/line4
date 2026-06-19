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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MES.View
{
    /// <summary>
    /// CopyToView.xaml 的交互逻辑
    /// </summary>
    public partial class CopyToView : Window
    {
        public CopyToView(MaterailModel materail)
        {
            InitializeComponent();
            CopyToViewModel context = this.DataContext as CopyToViewModel;
            context.Material = materail;
        }

        
    }
}
