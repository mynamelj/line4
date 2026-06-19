using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Newtonsoft.Json.Linq;

namespace MES.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public class TimeoutViewModel
    {
        public string Password { get; set; } = "123456";
        private readonly string passwordPath;

        public TimeoutViewModel()
        {
            passwordPath = System.AppDomain.CurrentDomain.BaseDirectory;
            if (System.IO.File.Exists(passwordPath + "misc.json"))
            {
                try
                {
                    string json = System.IO.File.ReadAllText(passwordPath + "misc.json");
                    var jo = JObject.Parse(json);
                    string savedPassword = jo["Password"]?.ToString();
                    if (!string.IsNullOrEmpty(savedPassword))
                    {
                        Password = savedPassword;
                    }
                    
                }
                catch (Exception ex)
                {
                    MessageBox.Show("读取密码文件失败: " + ex.Message);
                }
            }
        }

        public ICommand ConfirmCommand => new RelayCommand<object>((obj) =>
        {
            
            var passwordBox = obj as PasswordBox;
            if (passwordBox != null)
            {
                string inputPassword = passwordBox.Password;

                if (inputPassword == Password)
                {
                    var window = Window.GetWindow(passwordBox);
                    if (window != null)
                    {
                        window.DialogResult = true;
                    }
                }
                else
                {
                    MessageBox.Show("密码错误！");
                }
            }
        });
    }
}
