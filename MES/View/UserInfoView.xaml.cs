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
using MES.ViewModel;

namespace MES.View
{
    /// <summary>
    /// UserInfoView.xaml 的交互逻辑
    /// </summary>
    public partial class UserInfoView : UserControl
    {
        UserInfoViewModel vm;
        public UserInfoView()
        {
            InitializeComponent();
            vm = this.DataContext as UserInfoViewModel;
        }

        private void txtpwd_TextChanged(object sender, TextChangedEventArgs e)
        {
            password.Password = txtpwd.Text;
        }

        private void password_PasswordChanged(object sender, RoutedEventArgs e)
        {
            vm.Password = password.Password;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            vm.Password = password.Password;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            vm.Password = password.Password;
        }
    }
}
