using MES.Comm;
using MES.Manager;
using MES.SetModel;
using System.IO;
using System.Linq;
using System.Windows;

namespace MES.View
{
    /// <summary>
    /// QualityPasswordView.xaml 的交互逻辑
    /// 用于质量人员开箱前的密码验证，账户密码保存在 configs/user.json 中，UserName为Quality
    /// </summary>
    public partial class QualityPasswordView : Window
    {
        private const string QualityUserName = "Quality";
        private const string DefaultPassword = "123456";

        public bool IsValidated { get; private set; } = false;

        public QualityPasswordView()
        {
            InitializeComponent();
            txtUserName.Text = QualityUserName;
            EnsureQualityUserExists();
        }

        private UserModel GetQualityUser()
        {
            return SetHelper.Users?.FirstOrDefault(x => x.UserName == QualityUserName);
        }

        private void EnsureQualityUserExists()
        {
            if (SetHelper.Users == null)
            {
                SetHelper.Users = new System.Collections.Generic.List<UserModel>();
            }

            var user = GetQualityUser();
            if (user == null)
            {
                user = new UserModel() { UserName = QualityUserName, Password = DefaultPassword };
                SetHelper.Users.Add(user);
                SaveUsers();
            }
        }

        private void SaveUsers()
        {
            string json = JSON.ToJsonFormat(SetHelper.Users);
            File.WriteAllText(SetHelper.userpath, json);
        }

        private void ChkChangePassword_Checked(object sender, RoutedEventArgs e)
        {
            panelNewPassword.Visibility = Visibility.Visible;
            panelConfirmPassword.Visibility = Visibility.Visible;
            panelSavePassword.Visibility = Visibility.Visible;
        }

        private void ChkChangePassword_Unchecked(object sender, RoutedEventArgs e)
        {
            panelNewPassword.Visibility = Visibility.Collapsed;
            panelConfirmPassword.Visibility = Visibility.Collapsed;
            panelSavePassword.Visibility = Visibility.Collapsed;
            txtNewPassword.Password = "";
            txtConfirmPassword.Password = "";
        }

        private bool ValidatePasswordAndMaybeChange()
        {
            var user = GetQualityUser();
            if (user == null)
            {
                MessageBox.Show("未找到质量人员账户");
                return false;
            }

            if (txtPassword.Password != user.Password)
            {
                MessageBox.Show("密码错误");
                return false;
            }

            if (chkChangePassword.IsChecked == true)
            {
                string newPassword = txtNewPassword.Password;
                string confirmPassword = txtConfirmPassword.Password;

                if (string.IsNullOrWhiteSpace(newPassword))
                {
                    MessageBox.Show("新密码不能为空");
                    return false;
                }

                if (newPassword != confirmPassword)
                {
                    MessageBox.Show("两次输入的新密码不一致");
                    return false;
                }

                user.Password = newPassword;
                SaveUsers();
                MessageBox.Show("密码修改成功");
            }

            return true;
        }

        private void BtnSavePassword_Click(object sender, RoutedEventArgs e)
        {
            var user = GetQualityUser();
            if (user == null)
            {
                MessageBox.Show("未找到质量人员账户");
                return;
            }

            if (txtPassword.Password != user.Password)
            {
                MessageBox.Show("原密码错误");
                return;
            }

            string newPassword = txtNewPassword.Password;
            string confirmPassword = txtConfirmPassword.Password;

            if (string.IsNullOrWhiteSpace(newPassword))
            {
                MessageBox.Show("新密码不能为空");
                return;
            }

            if (newPassword != confirmPassword)
            {
                MessageBox.Show("两次输入的新密码不一致");
                return;
            }

            user.Password = newPassword;
            SaveUsers();
            MessageBox.Show("密码修改成功");

            chkChangePassword.IsChecked = false;
            txtPassword.Password = "";
        }

        private void BtnXPOpenBox_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidatePasswordAndMaybeChange())
            {
                return;
            }

            bool result = SetHelper.siemens.WriteItem(PLCGroupName.WriteGroup, "XP开箱_1", true);
            SetHelper.ListPLCMessage.ShowInfoQueue($"触发XP开箱写true,{(result ? "成功" : "失败")}");

            IsValidated = true;
            DialogResult = true;
            Close();
        }

        private void BtnQROpenBox_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidatePasswordAndMaybeChange())
            {
                return;
            }

            bool result = SetHelper.siemens.WriteItem(PLCGroupName.WriteGroup, "QR开箱_1", true);
            SetHelper.ListPLCMessage.ShowInfoQueue($"触发QR开箱写true,{(result ? "成功" : "失败")}");

            IsValidated = true;
            DialogResult = true;
            Close();
        }

        private void BtnCloseBox_Click(object sender, RoutedEventArgs e)
        {

            bool resultXP = SetHelper.siemens.WriteItem(PLCGroupName.WriteGroup, "XP开箱_1", false);
            SetHelper.ListPLCMessage.ShowInfoQueue($"触发XP开箱写false,{(resultXP ? "成功" : "失败")}");

            bool resultQR = SetHelper.siemens.WriteItem(PLCGroupName.WriteGroup, "QR开箱_1", false);
            SetHelper.ListPLCMessage.ShowInfoQueue($"触发QR开箱写false,{(resultQR ? "成功" : "失败")}");

            IsValidated = true;
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            IsValidated = false;
            DialogResult = false;
            Close();
        }
    }
}
