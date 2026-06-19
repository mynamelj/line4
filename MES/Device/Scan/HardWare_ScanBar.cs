using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MES.Device.Control;
using MES.Manager;

namespace TsHardWare
{

    /// <summary>
    /// 条码枪
    /// </summary>
    public class HardWare_ScanBar : HardWareBase
    {

        public delegate void DelegateReadBar(List<string> listBar, int hardIndex);
        public event DelegateReadBar OnReadBar;

        public HardWare_ScanBar(string strIP, int port)
            : base(strIP, port)
        {

        }

        public HardWare_ScanBar(int _port, int _baudRate, Parity _parity, int _dataBits, StopBits _stopBits)
            : base(_port, _baudRate, _parity, _dataBits, _stopBits)
        {

        }

        /// <summary>
        /// 是否运行
        /// </summary>
        public bool isRun = true;

        public static DateTime dateTime1=DateTime.MinValue;
        public static DateTime dateTime2=DateTime.MinValue;
        /// <summary>
        /// 一直读取条码串口或者tcp是否有数据回传
        /// </summary>
        public void StartReading()
        {
            new Thread(() =>
            {
                string strBarCode = "";
                while (isRun)
                {
                    Thread.Sleep(100);
                    byte[] buff = null;
                    byte[] buff1 = null;

                    if (HardWare.Read(ref buff) > 0)
                    {
                        strBarCode += Encoding.Default.GetString(buff);
                        Debug.WriteLine("收到" + HardWareIndex + "扫码枪原始值" + strBarCode);
                        //SetHelper.ListScanMessage.ShowInfoQueue("收到" + HardWareIndex + "扫码枪原始值" + strBarCode);
                        Thread.Sleep(100);
                        HardWare.Read(ref buff1);
                        //有时候一把读不全
                        if (buff1 != null)
                        {
                            strBarCode+= Encoding.Default.GetString(buff1);
                        }
                    }
                    if (strBarCode.Contains(EndLine))    //aaa\r\naaa\r\n
                    {
                        dateTime1 = DateTime.Now;
                        if (Math.Abs((dateTime2 - dateTime1).TotalSeconds) < 2)
                        {
                            SetHelper.ListScanMessage.ShowInfoQueue("请勿连续扫码，间隔两秒以上");
                            strBarCode = "";
                            dateTime2 = dateTime1;
                            continue;
                        }
                        dateTime2 = dateTime1;
                        List<string> listBar = new List<string>();
                        string strLeft = "";
                        listBar = HardWareDataHelper.GetAllDataBySplit(strBarCode, EndLine, ref strLeft);
                        if (listBar != null && listBar.Count != 0 && OnReadBar != null)
                        {
                            OnReadBar.BeginInvoke(listBar, HardWareIndex, null, null);
                            strBarCode = strLeft;
                        }
                        strBarCode = "";
                        strLeft = "";
                    }
                }
            })
            { IsBackground = true }.Start();
        }

        #region 字段
        /// <summary>
        /// 每次读取的延时时间2s
        /// </summary>
        public int ReadBarCodeTimeOut = 5000;

        /// <summary>
        /// 每次读取的尝试次数
        /// </summary>
        public int ReadTryTime = 3;

        /// <summary>
        /// 开命令
        /// </summary>
        public string strOnCmd = "";

        /// <summary>
        /// 关命令
        /// </summary>
        public string strOffCmd;

        /// <summary>
        /// 发送是不是采用16进制协议
        /// </summary>
        public bool OnHex;//发送是不是16进制的

        public bool OffHex;

        /// <summary>
        /// 锁读取
        /// </summary>
        private object lockRead = new object();
        #endregion

        #region 读取设备条码
        /// <summary>
        /// 读取设备条码
        /// </summary>
        /// <param name="BarCode"></param>
        /// <returns></returns>
        public bool ReadEquipData(ref string BarCode, int Multiple = 1)
        {
            if (IsSimulater)//模拟数据
            {
                Thread.Sleep(201);
                BarCode = "SimulaterCode" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
                return true;
            }
            if (HardWare == null)
                throw new Exception("[扫码]-协议基础未初始化!");
            if (strOnCmd == null || strOnCmd == "")
                return false;

            try
            {
                if (HardWare.HareWareType == EnumHardWareTypes.RTU)
                {
                    return ReadEquipDataRTU(Multiple, ref BarCode);
                }
                else if (HardWare.HareWareType == EnumHardWareTypes.TCP)
                {
                    return ReadEquipDataTCP(Multiple, ref BarCode);
                }
                else
                {
                    //WriteLog.LogError("ReadEquipData()->HardWare.HareWareType:" + HardWare.HareWareType.ToString());
                }
            }
            catch (Exception ex)
            {
                //WriteLog.LogError("ReadEquipData()->EX:" + ex);
            }
            return false;
        }

        /// <summary>
        /// 读取设备条码
        /// </summary>
        /// <param name="BarCode"></param>
        /// <returns></returns>
        public bool ReadEquipDataRTU(int Multiple, ref string BarCode)
        {
            if (IsSimulater)//模拟数据
            {
                Thread.Sleep(201);
                BarCode = "ScanBar" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
                return true;
            }
            if (HardWare == null)
                throw new Exception("[扫码]-协议基础未初始化!");
            if (strOnCmd == null || strOnCmd == "")
                return false;

            lock (lockRead)
            {
                try
                {
                    if (!HardWare.IsConn)
                    {
                        HardWare.Connect();
                    }
                    Stopwatch sw = new Stopwatch();
                    byte[] byteSend = null;
                    if (OnHex)
                    {
                        byteSend = strOnCmd.String2HexByteArray();
                    }
                    else
                    {
                        byteSend = Encoding.ASCII.GetBytes(strOnCmd);
                    }
                    HardWare.DiscardBuff();
                    HardWare.Write(byteSend);//开命令 
                    //Thread.Sleep(20);
                    //SendBarCodeOff();
                    //再发一个测量
                    sw.Reset();
                    sw.Start();
                    string strBarCode = "";
                    byte[] buff = null;
                    while (sw.ElapsedMilliseconds < ReadBarCodeTimeOut)
                    {
                        Thread.Sleep(10);
                        if (HardWare.Read(ref buff) > 0)
                        {
                            strBarCode += Encoding.Default.GetString(buff);
                            // WriteLog.LogScan("扫描枪返回原始值:" + strBarCode);
                        }
                        if (strBarCode.EndsWith(EndLine))    //aaa\r\naaa\r\n
                        {
                            //WriteLog.LogScan("扫描枪返回原始值,带有结束符:" + strBarCode + EndLine);
                            BarCode = HardWareDataHelper.GetMyDataByEndLine(strBarCode, EndLine);
                            SendBarCodeOff();
                            return BarCode != "ERROR" && BarCode != (Multiple == 1 ? "ERROR" : "ERROR,ERROR"); ;
                        }
                    }
                    SendBarCodeOff();
                    return false;
                }
                catch (Exception ex)
                {
                    //WriteLog.LogError("ReadEquipData()->EX:" + ex);
                    return false;
                }
            }
        }

        /// <summary>
        /// 读取设备条码
        /// </summary>
        /// <param name="BarCode"></param>
        /// <returns></returns>
        public bool ReadEquipDataTCP(int Multiple, ref string BarCode)
        {
            if (IsSimulater)//模拟数据
            {
                Thread.Sleep(201);
                BarCode = "ScanBar" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
                return true;
            }
            if (HardWare == null)
                throw new Exception("[扫码]-协议基础未初始化!");
            if (strOnCmd == null || strOnCmd == "")
                return false;

            lock (lockRead)
            {
                try
                {
                    HardWare.Close();
                    if (!HardWare.IsConn)
                    {
                        HardWare.Connect();
                    }
                    Stopwatch sw = new Stopwatch();
                    byte[] byteSend = null;
                    if (OnHex)
                    {
                        byteSend = strOnCmd.String2HexByteArray();
                    }
                    else
                    {
                        byteSend = Encoding.ASCII.GetBytes(strOnCmd);
                    }
                    HardWare.DiscardBuff();
                    HardWare.Write(byteSend);//开命令 
                    Thread.Sleep(20);
                    //SendBarCodeOff();
                    //再发一个测量
                    sw.Reset();
                    sw.Start();
                    string strBarCode = "";
                    byte[] buff = null;
                    while (sw.ElapsedMilliseconds < ReadBarCodeTimeOut)
                    {
                        Thread.Sleep(10);
                        if (HardWare.Read(ref buff) > 0)
                        {
                            strBarCode += Encoding.Default.GetString(buff);
                        }
                        if (strBarCode.EndsWith(EndLine))    //aaa\r\naaa\r\n
                        {
                            BarCode = HardWareDataHelper.GetMyDataByEndLine(strBarCode, EndLine);
                            //SendBarCodeOff();
                            //WriteLog.LogScan(Name + "读取扫码枪返回解析值:" + strBarCode);

                            return BarCode != "ERROR" && BarCode != (Multiple == 1 ? "ERROR" : "ERROR,ERROR"); ;
                        }
                    }
                    //SendBarCodeOff();
                    //rebot();
                    return false;
                }
                catch (Exception ex)
                {
                    //WriteLog.LogError("ReadEquipData()->EX:" + ex);
                    return false;
                }
            }
        }

        #endregion

        #region 扫码枪关闭命令-有的扫码枪需要读去完成后关闭条码枪
        /// <summary>
        /// 扫码枪关闭命令
        /// </summary>
        void SendBarCodeOff()
        {
            if (HardWare != null && strOffCmd != null && strOffCmd.Length != 0)
            {
                byte[] byteSend = null;
                if (OnHex)
                {
                    byteSend = strOffCmd.String2HexByteArray();
                }
                else
                {
                    byteSend = Encoding.ASCII.GetBytes(strOffCmd);
                }
                //WriteLog.LogScan(OnHex + "关命令:" + strOffCmd);
                HardWare.Write(byteSend);
            }
        }
        #endregion
    }
}