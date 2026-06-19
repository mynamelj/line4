using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TsHardWare;

namespace MES.Device.Control
{
    /// <summary>
    /// 设备的异常事件拓展
    /// </summary>
    public class HardWareException : Exception
    {
        public Exception exception;
        public HardWareException(string _exception)
        {
            exception = new Exception(_exception);
        }

    }

    public class ProtocolBase
    {

        public Encoding encoding = Encoding.ASCII;

        #region 硬件的通信方式 TCP还是串口
        private EnumHardWareTypes _hareWareType = EnumHardWareTypes.None;
        /// <summary>
        /// 硬件的通信方式 TCP还是串口
        /// </summary>
        public EnumHardWareTypes HareWareType
        {
            get { return _hareWareType; }
        }
        #endregion

        internal IController Conmunicate;

        public ProtocolBase(EnumHardWareTypes HardWareType)
        {
            _hareWareType = HardWareType;
        }

        /// <summary>
        /// TCP类型初始化
        /// </summary>
        /// <param name="strIP"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public bool SetPara(string strIP, int port)
        {
            if (_hareWareType == EnumHardWareTypes.TCP)
            {
                Conmunicate = new TCPController(strIP, port);
            }
            return true;
        }

        /// <summary>
        /// 串口类型的初始化
        /// </summary>
        /// <param name="_port"></param>
        /// <param name="_baudRate"></param>
        /// <param name="_parity"></param>
        /// <param name="_dataBits"></param>
        /// <param name="_stopBits"></param>
        /// <returns></returns>
        public bool SetPara(int _port, int _baudRate, Parity _parity, int _dataBits, StopBits _stopBits)
        {
            if (_hareWareType == EnumHardWareTypes.RTU)
            {
                Conmunicate = new RTUController(_port, _baudRate, _parity, _dataBits, _stopBits);
                return true;
            }
            return false;
        }

        public int Write(byte[] byteData, int length = -1)
        {
            return Conmunicate.Write(byteData, length);
        }

        public int Read(ref byte[] byteData, int length = -1)
        {
            return Conmunicate.Read(ref byteData, length);
        }

        public void Close()
        {
            Conmunicate.Close();
        }

        public bool Connect()
        {
            return Conmunicate.Connect();
        }

        public bool IsConn
        {
            get { return Conmunicate.IsConnect(); }
        }

        public void DiscardBuff()
        {
            Conmunicate.DiscardBuff();
        }
    }
}
