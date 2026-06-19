using MES.Manager;
using MES.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.Lifetime;
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
    /// ProductTypeView.xaml 的交互逻辑
    /// </summary>
    public partial class ProductTypeView : UserControl
    {
        public ProductTypeView()
        {
            InitializeComponent();
        }

        private void Button_PreviewMouseDown_1(object sender, MouseButtonEventArgs e)
        {
            Stopwatch sw = new Stopwatch();
            sw.Restart();
            isLeave = true;
            Task.Run(() =>
            {
                while (isLeave)
                {
                    if (sw.ElapsedMilliseconds > 2000)
                    {
                        SetHelper.IsAdmin = true;
                        Dispatcher.Invoke(() =>
                        {
                            (this.DataContext as ProductTypeViewModel).CopyCommand.Execute(null);
                        });
                        SetHelper.IsAdmin = false;
                        sw.Reset();
                        break;
                    }
                    else
                    {
                        continue;
                    }
                }
            });

        }

        private void Button_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Stopwatch sw = new Stopwatch();
            sw.Restart();
            isLeave1 = true;
            Task.Run(() =>
            {
                while (isLeave1)
                {
                    if (sw.ElapsedMilliseconds > 2000)
                    {
                        SetHelper.IsAdmin = true;
                        Dispatcher.Invoke(() =>
                        {
                            (this.DataContext as ProductTypeViewModel).CopyOriginalCommand.Execute(null);
                        });
                        SetHelper.IsAdmin = false;
                        sw.Reset();
                        break;
                    }
                    else
                    {
                        continue;
                    }
                }
            });
        }


        bool isLeave = true;
        bool isLeave1 = true;
        private void Button_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            isLeave = false;
        }

        private void orbtn_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            isLeave1 = false;
        }
    }
}
