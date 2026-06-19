using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using MES.Comm;
using MES.Manager;
using MES.SetModel;
using PropertyChanged;

namespace MES.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public class UserInfoViewModel
    {

        public UserInfoViewModel()
        {
            try
            {
                UserList = new ObservableCollection<UserModel>(SetHelper.Users);

            }
            catch (Exception ex)
            {
                SetHelper.ListPLCMessage.ShowInfoQueue(ex.ToString(), false, "ex");
            }
        }

        public string UserName { get; set; }
        public string Password { get; set; }
        public UserModel SelectUser { get; set; } = new UserModel();

        public ObservableCollection<UserModel> UserList { get; set; } = new ObservableCollection<UserModel>();

        public ICommand PasswordChangeCommand => new RelayCommand<PasswordBox>((p) =>
        {
            Password = p.Password;

        });

        public ICommand SelectionChange => new RelayCommand(() =>
        {
            try
            {
                if (SelectUser != null)
                {
                    UserName = SelectUser.UserName;
                    Password = SelectUser.Password;
                }
            }
            catch (Exception ex)
            {
                SetHelper.ListPLCMessage.ShowInfoQueue(ex.ToString(), false, "ex");
            }
        });


        public ICommand AddCommand => new RelayCommand(() =>
        {
            try
            {
                if (SetHelper.IsAdmin)
                {
                    if(UserList.Any(x=>x.UserName==UserName))
                    {
                        MessageBox.Show($"用户{UserName}已存在");
                        return;
                    }

                    UserList.Add(new UserModel()
                    {
                        UserName = UserName,
                        Password = Password
                    });
                    SaveSys(UserList);
                }
                else
                {
                    MessageBox.Show("权限不足");
                }
            }
            catch (Exception ex)
            {
                SetHelper.ListPLCMessage.ShowInfoQueue(ex.ToString(), false, "ex");
            }
        });

        public ICommand EditCommand => new RelayCommand<UserModel>((user) =>
        {
            try
            {
                if (SetHelper.IsAdmin)
                {
                    user.Password = Password;
                    SaveSys(UserList);
                }
                else
                {
                    MessageBox.Show("权限不足");
                }
            }
            catch (Exception ex)
            {
                SetHelper.ListPLCMessage.ShowInfoQueue(ex.ToString(), false, "ex");
            }
        });

        public ICommand DeleteCommand => new RelayCommand<UserModel>((user) =>
        {
            try
            {
                if (SetHelper.IsAdmin)
                {
                    UserList.Remove(user);
                    SaveSys(UserList);
                }
                else
                {
                    MessageBox.Show("权限不足");
                }
            }
            catch (Exception ex)
            {
                SetHelper.ListPLCMessage.ShowInfoQueue(ex.ToString(), false, "ex");
            }

        });

        private static void SaveSys(ObservableCollection<UserModel> GlueOnLineList)
        {
            string materialjson = JSON.ToJsonFormat(GlueOnLineList);
            File.WriteAllText(SetHelper.userpath, materialjson);
            SetHelper.Users = GlueOnLineList.ToList();

        }
    }
}
