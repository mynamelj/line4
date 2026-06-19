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
using MES.Comm;
using MES.Manager;
using MES.ViewModel;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace MES.View
{
    /// <summary>
    /// UserLoginView.xaml 的交互逻辑
    /// </summary>
    public partial class UserLoginView : Window
    {
        public UserLoginView()
        {
            InitializeComponent();
        }



        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //if (txtUserName.Text == SetHelper.UserName && txtPassword.Password == SetHelper.Password)
            //{
            //    SetHelper.IsAdmin = true;
            //    MessageBox.Show("当前为管理员用户");
            //    SetHelper.NowUser = txtUserName.Text;
            //    SetHelper.ListMesMessage.ShowInfoQueue($"用户{txtUserName.Text}已登录",false,"userLogin");
            //    this.Close();
            //    return;
            //}

            var user = SetHelper.Users.FirstOrDefault();

            if (user != null)
            {
                //发送PLC信号
                //if (user.UserName == "Admin")
                //{
                    SetHelper.IsAdmin = true;
                //}
                var result = SetHelper.siemens.WriteItem(SetModel.PLCGroupName.WriteGroup, "操作权限_1", true);
                SetHelper.ListPLCMessage.ShowInfoQueue($"向PLC写操作权限1_{true}_{(result ? "成功" : "失败")}");
                foreach (var item in SetHelper.StationNumber.numberGroups)
                {
                    if (item.Name.Contains("OP5130"))//5130手动工位一带2
                    {
                         result = SetHelper.siemens.WriteItem(SetModel.PLCGroupName.WriteGroup, "操作权限_2", true);
                         SetHelper.ListPLCMessage.ShowInfoQueue($"向PLC写操作权限2_{true}_{(result ? "成功" : "失败")}");
                    }
                };


                SetHelper.NowUser = "MES";
                MessageBox.Show("登录成功");
                SetHelper.ListMesMessage.ShowInfoQueue($"用户{txtUserName.Text}已登录", false, "userLogin");
                this.Close();
            }
            else
            {
                MessageBox.Show("用户名或密码错误");
            }
        }
    }
}
