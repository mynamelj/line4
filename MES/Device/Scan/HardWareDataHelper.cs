using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TsHardWare
{
    public class HardWareDataHelper
    {
        /// <summary>
        /// 通过结束符返回数据的最后一次的数据
        /// </summary>
        /// <param name="str"></param>
        /// <param name="strLength"></param>
        /// <returns></returns>
        public static string GetMyDataByEndLine(string str, string strEndLine)
        {
            char[] charTmp = new char[strEndLine.Length];
            for (int i = 0; i < strEndLine.Length; i++)
            {
                charTmp[i] = (char)strEndLine[i];
            }
            string[] strSplit = str.Trim().Split(charTmp, StringSplitOptions.RemoveEmptyEntries);

            if (strSplit.Length == 1)
            {
                string strNew = strSplit[strSplit.Length - 1];
                if (strNew.Replace(" ", "") == "") return "ERROR";
                return strNew;
            }
            else if (strSplit.Length > 1)
            {
                for (int i = 0; i < strSplit.Length; i++)
                {
                    string strNew = strSplit[i];
                    if (strNew.Replace(" ", "") != "")
                    {
                        return strNew;
                    }
                }
            }
            return "ERROR";
        }


        internal static List<string> GetAllDataBySplit(string str, string Split, ref string strLeft)
        { 
            char[] charTmp = new char[Split.Length];
            for (int i = 0; i < Split.Length; i++)
            {
                charTmp[i] = (char)Split[i];
            }
            string[] strSplit = str.Trim().Split(charTmp, StringSplitOptions.RemoveEmptyEntries);//4个

            if (strSplit.Length >= 1)
            {
                List<string> listData = new List<string>();
                for (int i = 0; i < strSplit.Length; i++)
                {
                    if (i == strSplit.Length - 1)
                    {
                        if (!str.EndsWith(Split))
                        {
                            strLeft = strSplit[i];
                        }
                        else
                        {
                            strLeft = "";
                            listData.Add(strSplit[i]);
                        }
                    }
                    else
                    {
                        listData.Add(strSplit[i]);
                    }
                }
                return listData;
            }
            return null;
        }
    }
}
