using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using MES.Device.Control;

namespace TsHardWare
{
    public class HardWareBase
    {
        public string ID;

        /// <summary>
        /// 结束符-内部会自动替换\\-->\
        /// </summary>
        private string endLine = "\r\n";

        public string EndLine
        {
            get { return endLine; }
            set { endLine = value.Replace(@"\\", @"\"); }
        }

        /// <summary>
        /// 硬件配置基础协议**************这个内部决定是tcp还是rtu
        /// </summary>
        internal ProtocolBase HardWare;

        public HardWareBase()
        {
        }

        public HardWareBase(string strIP, int port)
        {
            HardWare = new ProtocolBase(EnumHardWareTypes.TCP);
            HardWare.SetPara(strIP, port);
        }

        public HardWareBase(int _port, int _baudRate, Parity _parity, int _dataBits, StopBits _stopBits)
        {
            HardWare = new ProtocolBase(EnumHardWareTypes.RTU);
            HardWare.SetPara(_port, _baudRate, _parity, _dataBits, _stopBits);
        }

        /// <summary>
        /// 名称
        /// </summary>
        public string Name;

        /// <summary>
        /// 是否采用模拟数据
        /// </summary>
        public bool IsSimulater = false;
        
        /// <summary>
        /// 设备编号,从0开始的
        /// </summary>
        public int HardWareIndex = 0;

        /// <summary>
        /// 各模块设备编号,从0开始的
        /// </summary>
        public int Index = 0;

        /// <summary>
        /// 是否连接
        /// </summary>
        public bool IsConnect 
        {
            get 
            {
                if (IsSimulater)
                {
                    return true;
                }
                return HardWare != null && HardWare.IsConn; 
            }
        }

        /// <summary>
        /// 连接
        /// </summary>
        /// <returns></returns>
        public bool Connect()
        {
            if (IsSimulater)
            {
                return true;
            }
            return HardWare != null && HardWare.Connect();
        }

        /// <summary>
        /// 关闭
        /// </summary>
        public void Close()
        {
            if (!IsSimulater)
            {
                if (HardWare != null)
                    HardWare.Close();
            }
        }
    }
}
