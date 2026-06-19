using DAL;
using MES.Manager;
using MES.SetModel;
using Newtonsoft.Json.Linq;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace MES
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // 最早期的全局异常捕获绑定
        public App()
        {
            // 订阅全局未处理异常事件
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                GlobalExceptionHandler.HandleException((Exception)args.ExceptionObject);
            };

            Application.Current.DispatcherUnhandledException += (sender, args) =>
            {
                GlobalExceptionHandler.HandleException(args.Exception);
                args.Handled = true;
            };

            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                GlobalExceptionHandler.HandleException(args.Exception);
                args.SetObserved();
            };
        }

        //  OnStartup用来处理程序启动后的json
        protected override void OnStartup(StartupEventArgs e)
        {
            string name = Environment.MachineName;
            string processName = Process.GetCurrentProcess().ProcessName;
            int currentProcessCount = Process.GetProcessesByName(processName).Length;

            if (name != "XC-PC-F72600BD"&&name != "XC-PC-F7278065")
            {
                if (currentProcessCount > 1)
                {
                    Environment.Exit(0);
                    return;
                }
            }
            else
            {
                if (currentProcessCount > 2) // 2150转线需要双开程序
                {
                    Environment.Exit(0);
                    return;
                }
            }

            //  misc.json 配置文件
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string filePath = Path.Combine(baseDirectory, "misc.json");

            if (!File.Exists(filePath))
            {
                List<string> ExcludedDatas = new List<string>() { "HeatTemp_1", "CoverPressDisplace_1", "CoverPressForce_1" };
                Dictionary<string, string> RuleDict = new Dictionary<string, string>()
            {
                { "OP2020#1", "1033211DJ1" },
                { "OP2020#2", "1033235DJ1" },
                { "OP2030",   "1033270DJ1" },
            };

                var jo = new JObject();
                jo["Password"] = "123456";
                jo["返修读取数据"] = JToken.FromObject(ExcludedDatas);
                jo["扫码匹配规则"] = JToken.FromObject(RuleDict);

                try
                {
                    // 防止中文在生产环境环境乱码
                    File.WriteAllText(filePath, jo.ToString(), System.Text.Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"无法创建配置文件 misc.json: {ex.Message}", "初始化错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try 
            {
                SetHelper.siemens.WriteItem(SetModel.PLCGroupName.WriteGroup, "操作权限_2", false);
                SetHelper.siemens.WriteItem(SetModel.PLCGroupName.WriteGroup, "操作权限_1", false);
            }
            catch 
            {
                throw;
            }
            base.OnExit(e);
        }
        public class GlobalExceptionHandler
        {
            public static void HandleException(Exception ex)
            {
                // Log the exception details
                // NLogModule.GetInstance().Error(...);
                MessageBox.Show($"{ex}", "全局异常处理", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}