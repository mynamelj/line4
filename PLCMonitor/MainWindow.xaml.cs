using MES.Manager;
using MES.SetModel;
using MES.ViewModel;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Net;
using System.Security.Permissions;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PLCMonitor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : UserControl
    {
        public MainWindow()
        {
            SetHelper.products = SetHelper.ReadSys<ObservableCollection<ProductTypeModel>>(SetHelper.productpath) ?? new ObservableCollection<ProductTypeModel>();
            SetHelper.NowProduct = SetHelper.GetProductType(2) ?? new ProductTypeModel();
            SetHelper.SetFilePath(SetHelper.NowProduct.ProductName);
            SetHelper.PLCSetting = SetHelper.ReadSys<PLCSettingModel>(SetHelper.plcpath, "PLC");
            InitializeComponent();
        }
    }
}