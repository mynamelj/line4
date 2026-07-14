using Autofac;
using DAL;
using MES.Manager;
using MES.Service;
using MES.SetModel;
using MES.View;
using MES.ViewModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using static MES.Service.MiscService;


namespace MES
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private const string SingleInstanceMutexName = @"Global\MES_Line4_SingleInstance";
        private static Mutex _singleInstanceMutex;
        private bool _ownsSingleInstanceMutex;

        public static class AppContainer
        {
            public static IContainer Instance { get; set; }
        }
        public static IContainer Container { get; private set; }
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
            _singleInstanceMutex = new Mutex(true, SingleInstanceMutexName, out var createdNew);
            _ownsSingleInstanceMutex = createdNew;

            if (!createdNew)
            {
                MessageBox.Show("程序已经在运行中，请勿重复打开。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown();
                return;
            }

            //  misc.json 配置文件
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string filePath = Path.Combine(baseDirectory, "configs\\misc.json");

            try
            {
                if (!File.Exists(filePath))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                    string jsonString = JsonConvert.SerializeObject(new ObservableCollection<SNPrefix>
                    {
                        new SNPrefix { Name = "XP2020outputShaftSNPrefix", Value = "1"},
                        new SNPrefix { Name = "XP2020differentialSNPrefix", Value = "2" },
                        new SNPrefix { Name = "QR2020outputShaftSNPrefix", Value = "3" },
                        new SNPrefix { Name = "QR2020differentialSNPrefix", Value = "4" },
                        new SNPrefix { Name = "XP2030inputShaftSNPrefix", Value = "5" },
                        new SNPrefix { Name = "XP2030intermediateShaftSNPrefix", Value = "6" },
                        new SNPrefix { Name = "QR2030inputShaftSNPrefix", Value = "7" },
                        new SNPrefix { Name = "QR2030intermediateShaftSNPrefix", Value = "8" }
                    }, Formatting.Indented);

                    File.WriteAllText(filePath, jsonString, Encoding.UTF8);
                }

            }
            catch (Exception ex)
            {
                throw new Exception($"Newtonsoft 保存 JSON 失败: {ex.Message}", ex);
            }

            // 创建 Autofac 容器构建器
            var builder = new ContainerBuilder();

            builder.RegisterType<MiscService>().As<IMiscService>().SingleInstance();

            builder.RegisterType<WindowService>().As<IWindowService>().SingleInstance();

            builder.RegisterType<MesMainViewModel>().InstancePerDependency();

            builder.RegisterType<MesMainView>().InstancePerDependency();
            builder.RegisterType<TestView>().InstancePerDependency();
            builder.RegisterType<MiscView>().InstancePerDependency();
            builder.RegisterType<MiscViewModel>().InstancePerDependency();
            builder.RegisterType<MainWindowViewModel>().InstancePerDependency();
            // 注册 MainWindow 本身

            builder.RegisterType<MainWindow>().AsSelf();


            Container = builder.Build();
            AppContainer.Instance = Container;
            var mainWindow = Container.Resolve<MainWindow>();

            mainWindow.Show();

            base.OnStartup(e);
        }


        protected override void OnExit(ExitEventArgs e)
        {
            if (_ownsSingleInstanceMutex)
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

                _singleInstanceMutex?.ReleaseMutex();
            }

            _singleInstanceMutex?.Dispose();
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
