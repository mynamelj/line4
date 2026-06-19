using MES.SetModel;
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

namespace Update
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        string filepath = Environment.CurrentDirectory + "\\sys.json";
        List<string> LocalFile = new List<string>();
        List<string> UpdateFile = new List<string>();
        List<string> FileNames = new List<string>();
        List<string> iPs = new List<string>();

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!File.Exists(filepath)) File.Create(filepath);
                var data = File.ReadAllText(filepath, Encoding.UTF8);
                if (data == "") return;
                UpdateSoftwareModel model = JsonConvert.DeserializeObject<UpdateSoftwareModel>(data);
                SoftWarePath.Text = model.FilePath;
                allAddress.Text = model.IPAddress;

                GetUpdatePath();
                if (!Directory.Exists(SoftWarePath.Text)) Directory.CreateDirectory(SoftWarePath.Text);
                FileNames = new DirectoryInfo(SoftWarePath.Text).GetFiles("*").Where(x => x.Name.Contains("MES")).Select(x => x.Name).ToList();
                iPs = allAddress.Text.Split(',').Where(x => x != "").ToList();

                //MessageBox.Show(Environment.MachineName);
                //Process[] process1 = Process.GetProcesses("CSMES-PC-00E36N");
                //Process process = Process.GetProcesses("CSMES-PC-00E368").Where(x => x.ProcessName == "MES.exe").FirstOrDefault();
                //Process.GetProcessesByName("MES.exe", "CSMES-PC-00E368");


                //if (process != null)
                //    process.Kill();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                //throw;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            UpdateSoftwareModel model = new UpdateSoftwareModel()
            {
                FilePath = SoftWarePath.Text,
                IPAddress = allAddress.Text,
            };
            File.WriteAllText(filepath, JsonConvert.SerializeObject(model));
        }


        public void GetUpdatePath()
        {
            iPs.ForEach((i) =>
            {
                FileNames.ForEach((j) =>
                {
                    string path = $"\\\\{i}\\{SoftWarePath.Text}\\{j}";
                    UpdateFile.Add(path);
                });
            });
        }

        private void Button2_Click(object sender, RoutedEventArgs e)
        {
            int i = 1;
            try
            {
                iPs = allAddress.Text.Split(',').Where(x => x != "").ToList();
                foreach (var ip in iPs)
                {
                    try
                    {
                        foreach (var filename in FileNames)
                        {
                            string oldpath = $"{SoftWarePath.Text}\\{filename}";
                            string newpath = $"\\\\{ip}\\{SoftWarePath.Text.Replace(":", "")}\\{filename}";
                            if (File.Exists(newpath))
                            {
                                File.Delete(newpath);
                            }
                            File.Copy(oldpath, newpath);

                            string sysPath = $"\\\\{ip}\\{SoftWarePath.Text.Replace(":", "")}\\configs\\sys.json";
                            UpdateMesSet(sysPath);
                        }
                        i++;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ip + ex.ToString());
                    }
                }
                MessageBox.Show("更新完成"+i);
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.ToString());
            }
        }

        public void UpdateMesSet(string path)
        {
            try
            {
                string json = File.ReadAllText(path);
                MesSettingModel mes = JsonConvert.DeserializeObject<MesSettingModel>(json);
                mes.IsSimulate = IsSimulate.IsChecked ?? false;

                string jsonTo = JsonConvert.SerializeObject(mes);
                File.WriteAllText(path, jsonTo);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }


        public void conn(string ip, string path1, string processName)
        {
            ConnectionOptions options = new ConnectionOptions();
            options.Locale = ip;
            options.Authentication = AuthenticationLevel.Default;

            ManagementPath path = new ManagementPath(path1);

            ManagementScope scope = new ManagementScope(path, options);

            ObjectQuery query = new ObjectQuery($"Select From Win32_Process Where Name=\"{processName}\"");
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query))
            {
                foreach (var item in searcher.Get())
                {

                }
            }
        }
    }
    //net use [path] [\user: name pwd]
    public class UpdateSoftwareModel
    {
        public string FilePath { get; set; }
        public string IPAddress { get; set; }
    }
}