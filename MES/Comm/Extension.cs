using MES.Manager;
using MES.SetModel;
using Microsoft.Xaml.Behaviors.Media;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MES
{
    public class Extension
    {

        public enum PictureType
        {
            /// <summary>
            /// 计算图
            /// </summary>
            Detection,
            /// <summary>
            /// 原图
            /// </summary>
            Original,
        }

        public enum EnumLogType
        {
            log,
            ex,
        }

        public static void WriteConfig(string key,string value)
        {
            try
            {
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var appsetting = config.AppSettings.Settings;
                if (appsetting[key]==null)
                {
                    appsetting.Add(key, value);
                }
                else
                {
                    appsetting[key].Value = value;
                }
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection(key);
            }
            catch (Exception ex)
            {
                SetHelper.ListPLCMessage.ShowInfoQueue(ex.ToString(), false, "ex");
            }
        }

        public static string ReadConfig(string key)
        {
            try
            {
                string value = ConfigurationManager.AppSettings[key] ?? "";
                return value;
            }
            catch (Exception ex)
            {
                SetHelper.ListPLCMessage.ShowInfoQueue(ex.ToString(), false, "ex");
            }
            return "";
        }

    }

   

    
}
