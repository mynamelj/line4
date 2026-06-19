using MES.Comm;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace System
{
    public static class DataExtend
    {
        public static ushort String2Ushort(this string str)
        {
            ushort u = 0;
            if (ushort.TryParse(str, out u))
            { return u; }
            return 0;
        }
        public static ushort SwapUInt16(this ushort inValue)
        {
            return (UInt16)(((inValue & 0xff00) >> 8) |
                     ((inValue & 0x00ff) << 8));
        }
        public static int String2Int(this string str)
        {
            int u = 0;
            if (int.TryParse(str, out u))
            { return u; }
            return 0;
        }

        public static byte[] String2HexByteArray(this string hexString)
        {
            try
            {
                hexString = hexString.Replace(" ", "");
                byte[] returnBytes = new byte[hexString.Length / 2];
                for (int i = 0; i < returnBytes.Length; i++)
                    returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
                return returnBytes;
            }
            catch (Exception ex)
            {
                //WriteLog.LogError("String2HexByteArray()->EX:" + ex);
                return null;
            }
        }

        /// <summary>
        /// 字符串转16进制字符串
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string StringToHexString(this string s)
        {
            try
            {
                string result = string.Empty;
                for (int i = 0; i < s.Length; i++)//逐字节变为16进制字符，以%隔开
                {
                    result += ((int)s[i]).ToString("X2") + " ";
                }
                return result;
            }
            catch (Exception ex)
            {
                // WriteLog.LogError("StringToHexString()->EX:" + ex);
                return "";
            }
        }

        /// <summary>
        /// byte[]转为16进制字符串
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string ByteToHexStr(this byte[] bytes)
        {
            string returnStr = "";
            if (bytes != null)
            {
                for (int i = 0; i < bytes.Length; i++)
                {
                    returnStr += bytes[i].ToString("X2");
                }
            }
            return returnStr;
        }
        public static Parity String2Parity(this string str)
        {
            Parity u = Parity.Even;
            if (Parity.TryParse(str, out u))
            { return u; }
            return 0;
        }

        public static StopBits String2StopBits(this string str)
        {
            StopBits u = StopBits.One;
            if (StopBits.TryParse(str, out u))
            { return u; }
            return 0;
        }

        public static PLCTagItem String2PLCTagItem(this string str)
        {
            PLCTagItem u = PLCTagItem.DEFAULT;
            if (PLCTagItem.TryParse(str, out u))
            {
                return u;
            }
            return u;
        }

        public static float String2Float(this string str)
        {
            float u = 0;
            if (float.TryParse(str, out u))
            { return u; }
            return 0;
        }
        public static decimal String2Decimal(this string str)
        {
            decimal u = 0;
            if (decimal.TryParse(str, out u))
            { return u; }
            return 0;
        }


        public static float StringToweight(this string _str)
        {
            if (_str == null)
                return 0;
            float intTmp = 0;
            _str = _str.TrimEnd();
            float.TryParse(_str.Substring(7, _str.Length - 8), out intTmp);
            return intTmp;
        }

        //将object类型转换成时间，如果能转的话。
        public static DateTime Obj2DateTime(this object obj)
        {
            if (obj == null)
            {
                return DateTime.MinValue;
            }
            else
            {
                DateTime time = DateTime.Now;
                bool isConvertSeccess = DateTime.TryParse(obj.ToString(), out time);
                //若是转换成功，time中就是转换的值，若失败，则变成日期初始化值。
                if (isConvertSeccess)
                    return time;
                else
                    return DateTime.MinValue;
            }
        }

        public static T Clone<T>(this T t)
        {
            using (Stream objectStream = new MemoryStream())
            {
                //利用 System.Runtime.Serialization序列化与反序列化完成引用对象的复制  
                IFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                formatter.Serialize(objectStream, t);
                objectStream.Seek(0, SeekOrigin.Begin);
                return (T)formatter.Deserialize(objectStream);

            }
        }

        public static string Obj2String(this object obj)
        {
            if (obj == null)
            {
                return "";
            }
            else
            {
                return obj.ToString();
            }
        }

        public static int Obj2Int(this object obj)
        {
            if (obj == null)
            {
                return 0;
            }
            else
            {
                int time = 0;
                string strTmp = obj.ToString();
                strTmp = strTmp.ToUpper() == "TRUE" ? "1" : strTmp;
                strTmp = strTmp.ToUpper() == "FALSE" ? "0" : strTmp;
                bool isConvertSeccess = int.TryParse(strTmp, out time);
                //若是转换成功，time中就是转换的值，若失败，则变成日期初始化值。
                if (isConvertSeccess)
                    return time;
                else
                    return 0;
            }
        }

        public static short Obj2Short(this object obj)
        {
            if (obj == null)
            {
                return 0;
            }
            else
            {
                short time = 0;
                string strTmp = obj.ToString();
                strTmp = strTmp.ToUpper() == "TRUE" ? "1" : strTmp;
                strTmp = strTmp.ToUpper() == "FALSE" ? "0" : strTmp;
                bool isConvertSeccess = short.TryParse(strTmp, out time);
                //若是转换成功，time中就是转换的值，若失败，则变成日期初始化值。
                if (isConvertSeccess)
                    return time;
                else
                    return 0;
            }
        }

        public static ushort Obj2UShort(this object obj)
        {
            if (obj == null)
            {
                return 0;
            }
            else
            {
                ushort time = 0;
                string strTmp = obj.ToString();
                strTmp = strTmp.ToUpper() == "TRUE" ? "1" : strTmp;
                strTmp = strTmp.ToUpper() == "FALSE" ? "0" : strTmp;
                bool isConvertSeccess = ushort.TryParse(strTmp, out time);
                //若是转换成功，time中就是转换的值，若失败，则变成日期初始化值。
                if (isConvertSeccess)
                    return time;
                else
                    return 0;
            }
        }
        public static bool ObjToBool(this object obj)
        {
            if (obj == null)
            {
                return false;
            }
            else
            {
                bool data = false;
                string strTmp = obj.ToString();
                strTmp = strTmp == "1" ? "TRUE" : strTmp;
                strTmp = strTmp == "0" ? "Flase" : strTmp;
                bool isConvertSeccess = bool.TryParse(strTmp, out data);
                //若是转换成功，time中就是转换的值，若失败，则变成日期初始化值。
                if (isConvertSeccess)
                    return data;
                else
                    return data;
            }

        }
        public static byte ObjToByte(this object obj)
        {
            if (obj == null)
            {
                return 0;
            }
            else
            {
                byte data = 0;
                bool isConvertSeccess = byte.TryParse(obj.ToString(), out data);
                //若是转换成功，time中就是转换的值，若失败，则变成日期初始化值。
                if (isConvertSeccess)
                    return data;
                else
                    return data;
            }
        }

        public static byte[] ObjToBytes(this object obj)
        {
            if (obj == null)
            {
                return null;
            }
            else
            {
                int data = 0;
                bool isConvertSeccess = int.TryParse(obj.ToString(), out data);
                //若是转换成功，time中就是转换的值，若失败，则变成日期初始化值。
                if (isConvertSeccess)
                    return BitConverter.GetBytes(data);
                else
                    return null;
            }
        }

        public static byte[] ObjToBytes2(this object obj)
        {
            if (obj == null)
            {
                return null;
            }
            else
            {
                ushort data = 0;
                bool isConvertSeccess = ushort.TryParse(obj.ToString(), out data);
                //若是转换成功，time中就是转换的值，若失败，则变成日期初始化值。
                if (isConvertSeccess)
                    return data.Int16toBytes();
                else
                    return null;
            }
        }

        public static byte[] Int16toBytes(this ushort value)
        {
            byte[] resp = new Byte[2];

            resp[1] = (byte)(value & 0xFF);
            resp[0] = (byte)((value >> 8) & 0xFF);

            return resp;
        }

        public static float ObjToFloat(this object obj)
        {
            if (obj == null)
            {
                return 0.0f;
            }
            else
            {
                float data = 0.0f;
                bool isConvertSeccess = float.TryParse(obj.ToString(), out data);
                //若是转换成功，time中就是转换的值，若失败，则变成日期初始化值。
                if (isConvertSeccess)
                    return data;
                else
                    return data;
            }
        }

        public static decimal ObjToDecimal(this object obj)
        {
            if (obj == null)
            {
                return 0.0M;
            }
            else
            {
                decimal data = 0.0M;
                bool isConvertSeccess = decimal.TryParse(obj.ToString(), out data);
                //若是转换成功，time中就是转换的值，若失败，则变成日期初始化值。
                if (isConvertSeccess)
                    return data;
                else
                    return data;
            }
        }

        public static double ObjToDouble(this object obj)
        {
            if (obj == null)
            {
                return 0.0d;
            }
            else
            {
                double data = 0.0d;
                bool isConvertSeccess = double.TryParse(obj.ToString(), out data);
                //若是转换成功，time中就是转换的值，若失败，则变成日期初始化值。
                if (isConvertSeccess)
                    return data;
                else
                    return data;
            }
        }

        /// <summary>  
        /// 获取拼音  
        /// </summary>  
        /// <param name="str"></param>  
        /// <returns></returns>  
        public static string GetPYString(string str)
        {
            string tempStr = "";
            foreach (char c in str)
            {
                if ((int)c >= 33 && (int)c <= 126)
                {
                    //字母和符号原样保留     
                    tempStr += c.ToString();
                }
                else
                {
                    //累加拼音声母     
                    tempStr += GetPYChar(c.ToString());
                }
            }
            return tempStr;
        }

        public static string ToYMDHMSFormat(this DateTime dt)
        {
            return dt.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public static string ToYMDHMSFFFFormat(this DateTime dt)
        {
            return dt.ToString("yyyy-MM-dd HH:mm:ss.fff");
        }

        public static string ToYMDFormat(this DateTime dt)
        {
            return dt.ToString("yyyy-MM-dd HH:mm:ss.fff");
        }

        public static string ToHMSFormat(this DateTime dt)
        {
            return dt.ToString("HH:mm:ss");
        }

        ///      
        /// 取单个字符的拼音声母     
        ///      
        /// 要转换的单个汉字     
        /// 拼音声母     
        public static string GetPYChar(string c)
        {
            byte[] array = new byte[2];
            array = System.Text.Encoding.Default.GetBytes(c);
            int i = (short)(array[0] - '\0') * 256 + ((short)(array[1] - '\0'));
            if (i < 0xB0A1) return "*";
            if (i < 0xB0C5) return "a";
            if (i < 0xB2C1) return "b";
            if (i < 0xB4EE) return "c";
            if (i < 0xB6EA) return "d";
            if (i < 0xB7A2) return "e";
            if (i < 0xB8C1) return "f";
            if (i < 0xB9FE) return "g";
            if (i < 0xBBF7) return "h";
            if (i < 0xBFA6) return "j";
            if (i < 0xC0AC) return "k";
            if (i < 0xC2E8) return "l";
            if (i < 0xC4C3) return "m";
            if (i < 0xC5B6) return "n";
            if (i < 0xC5BE) return "o";
            if (i < 0xC6DA) return "p";
            if (i < 0xC8BB) return "q";
            if (i < 0xC8F6) return "r";
            if (i < 0xCBFA) return "s";
            if (i < 0xCDDA) return "t";
            if (i < 0xCEF4) return "w";
            if (i < 0xD1B9) return "x";
            if (i < 0xD4D1) return "y";
            if (i < 0xD7FA) return "z";
            return "*";
        }

        /// <summary>
        /// 检测字符串是否为电池条码[字符串只包含数字和字母]
        /// </summary>
        /// <param name="strBarCode"></param>
        /// <returns></returns>
        public static bool IsBarCode(this string strBarCode)
        {
            if (Regex.IsMatch(strBarCode, "^[0-9a-zA-Z\\s\\-]+$"))
            {
                return strBarCode.ToUpper() != "ERROR";
            }
            return false;
        }

        /// <summary>
        /// 获取字符串中的数字 
        /// </summary>
        /// <param name="str">字符串</param>
        /// <returns>数字</returns>
        public static decimal GetNumberDecimal(this string str)
        {
            decimal result = 0;
            if (str != null && str != string.Empty)
            {
                // 正则表达式剔除非数字字符（不包含小数点.）
                //str = Regex.Replace(str, @"[^/d./d]", "");
                str = Regex.Replace(str, @"[^\d.\d]", "");
                // 如果是数字，则转换为decimal类型
                if (Regex.IsMatch(str, @"^[+-]?\d*[.]?\d*$"))
                {
                    result = decimal.Parse(str);
                }
            }
            return result;
        }

        /// <summary>
        /// 获取字符串中的数字
        /// </summary>
        /// <param name="str">字符串</param>
        /// <returns>数字</returns>
        public static int GetNumberInt(this string str)
        {
            int result = 0;
            if (str != null && str != string.Empty)
            {
                // 正则表达式剔除非数字字符（不包含小数点.）
                str = Regex.Replace(str, @"[^\d.\d]", "");
                // 如果是数字，则转换为decimal类型
                if (Regex.IsMatch(str, @"^[+-]?\d*[.]?\d*$"))
                {
                    result = int.Parse(str);
                }
            }
            return result;
        }
    }

    public static class TestExtension
    {
        public static String nameof<T, TT>(this T obj, Expression<Func<T, TT>> propertyAccessor)
        {
            if (propertyAccessor.Body.NodeType == ExpressionType.MemberAccess)
            {
                var memberExpression = propertyAccessor.Body as MemberExpression;
                if (memberExpression == null)
                    return null;
                return memberExpression.Member.Name;
            }
            return null;
        }

        public static String nameof<T, TT>(this Expression<Func<T, TT>> accessor)
        {
            return nameof(accessor.Body);
        }

        public static String nameof<T>(this Expression<Func<T>> accessor)
        {
            return nameof(accessor.Body);
        }

        private static String nameof(System.Linq.Expressions.Expression expression)
        {
            if (expression.NodeType == ExpressionType.MemberAccess)
            {
                var memberExpression = expression as MemberExpression;
                if (memberExpression == null)
                    return null;
                return memberExpression.Member.Name;
            }
            return null;
        }
    }


}
