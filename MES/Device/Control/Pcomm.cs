using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace TsHardWare.Controller
{

    /// <summary>
    /// PComm串口
    /// </summary>
    public class PComm
    {
        [DllImport("PCOMM.DLL", EntryPoint = "sio_open")]
        public static extern int sio_open(int port);

        [DllImport("PComm.dll", EntryPoint = "sio_ioctl")]
        public static extern int sio_ioctl(int port, int baud, int mode);

        [DllImport("PComm.dll", EntryPoint = "sio_DTR")]
        public static extern int sio_DTR(int port, int mode);

        [DllImport("PComm.dll", EntryPoint = "sio_RTS")]
        public static extern int sio_RTS(int port, int mode);

        [DllImport("PComm.dll", EntryPoint = "sio_close")]
        public static extern int sio_close(int port);

        [DllImport("PComm.dll", EntryPoint = "sio_read")]
        public static extern int sio_read(int port, ref byte buf, int length);

        [DllImport("PComm.dll", EntryPoint = "sio_write")]
        public static extern int sio_write(int port, ref byte buf, int length);

        [DllImport("PComm.dll", EntryPoint = "sio_SetReadTimeouts")]
        public static extern int sio_SetReadTimeouts(int port, int TotalTimeouts, int IntervalTimeouts);

        [DllImport("PComm.dll", EntryPoint = "sio_SetWriteTimeouts")]
        public static extern int sio_SetWriteTimeouts(int port, int TotalTimeouts, int IntervalTimeouts);

        [DllImport("PComm.dll", EntryPoint = "sio_AbortRead")]
        public static extern int sio_AbortRead(int port);

        [DllImport("PComm.dll", EntryPoint = "sio_AbortWrite")]
        public static extern int sio_AbortWrite(int port);

        [DllImport("PComm.dll", EntryPoint = "sio_getmode")]
        public static extern int sio_getmode(int port);

        [DllImport("PComm.dll", EntryPoint = "sio_getbaud")]
        public static extern int sio_getbaud(int port);

        [DllImport("PComm.dll", EntryPoint = "sio_flowctrl")]
        public static extern int sio_flowctrl(int port, int mode);
        [DllImport("PComm.dll", EntryPoint = "sio_iqueue")]
        public static extern int sio_iqueue(int port);

        [DllImport("PComm.dll", EntryPoint = "sio_lstatus")]
        public static extern int sio_lstatus(int port);



        [DllImport("PComm.dll", EntryPoint = "sio_flush")]
        public static extern int sio_flush(int port, int func);

        // Function return error code
        private const int SIO_OK = 0;
        private const int SIO_BADPORT = -1;
        private const int SIO_OUTCONTROL = -2;
        private const int SIO_NODATA = -4;
        private const int SIO_OPENFAIL = -5;
        private const int SIO_RTS_BY_HW = -6;
        private const int SIO_BADPARAM = -7;
        private const int SIO_WIN32FAIL = -8;
        private const int SIO_BOARDNOTSUPPORT = -9;
        private const int SIO_ABORT_WRITE = -11;
        private const int SIO_WRITETIMEOUT = -12;

        // Self Define function return error code
        private const int ERR_NOANSWER = -101;

        // Baud rate
        private const int B50 = 0x0;
        private const int B75 = 0x1;
        private const int B110 = 0x2;
        private const int B134 = 0x3;
        private const int B150 = 0x4;
        private const int B300 = 0x5;
        private const int B600 = 0x6;
        private const int B1200 = 0x7;
        private const int B1800 = 0x8;
        private const int B2400 = 0x9;
        private const int B4800 = 0xA;
        private const int B7200 = 0xB;
        private const int B9600 = 0xC;
        private const int B19200 = 0xD;
        private const int B38400 = 0xE;
        private const int B57600 = 0xF;
        private const int B115200 = 0x10;
        private const int B230400 = 0x11;
        private const int B460800 = 0x12;
        private const int B921600 = 0x13;

        // Mode setting Data bits define
        private const int BIT_5 = 0x0;
        private const int BIT_6 = 0x1;
        private const int BIT_7 = 0x2;
        private const int BIT_8 = 0x3;

        // Mode setting Stop bits define
        private const int STOP_1 = 0x0;
        private const int STOP_2 = 0x4;

        // Mode setting Parity define
        private const int P_EVEN = 0x18;
        private const int P_ODD = 0x8;
        private const int P_SPC = 0x38;
        private const int P_MRK = 0x28;
        private const int P_NONE = 0x0;

        // Private Key name
        private const string KEY_PORT = "Port";
        private const string KEY_BAUDRATE = "Baud_Rate";
        private const string KEY_PARITY = "Parity";
        private const string KEY_BYTESIZE = "Byte_Size";
        private const string KEY_STOPBITS = "Stop_Bits";
        private const string KEY_BEFOREDELAY = "Before_Delay";
        private const string KEY_BYTEDELAY = "Byte_Delay";
        private const string KEY_READINTERVALTIMEOUT = "Read_Interval_Timeout";
        private const string KEY_AFTERDELAY = "After_Delay";

        // Port param
        private int Gl_Int_Port = 1;
        private int Gl_Int_Baudrate = B9600;
        private int Gl_Int_Parity = P_NONE;
        private int Gl_Int_ByteSize = BIT_8;
        private int Gl_Int_StopBits = STOP_1;

        // Delay param
        private int Gl_Int_BeforeDelay = 0;
        private int Gl_Int_ByteDelay = 0;
        private int Gl_Int_ReadIntervalTimeout = 50;
        private int Gl_Int_AfterDealy = 3000;

        /// <summary>
        /// 解析通讯参数
        /// </summary>
        /// <param name="Hb_CommParam"></param>
        private void AnalyseCommParam(Hashtable Ht_CommParam)
        {
            //Port
            Gl_Int_Port = int.Parse(Ht_CommParam[KEY_PORT].ToString());
            //Baud rate
            if (Ht_CommParam.Contains(KEY_BAUDRATE))
            {
                switch (Ht_CommParam[KEY_BAUDRATE].ToString())
                {
                    case "50": Gl_Int_Baudrate = B50; break;
                    case "75": Gl_Int_Baudrate = B75; break;
                    case "110": Gl_Int_Baudrate = B110; break;
                    case "134": Gl_Int_Baudrate = B134; break;
                    case "150": Gl_Int_Baudrate = B150; break;
                    case "300": Gl_Int_Baudrate = B300; break;
                    case "600": Gl_Int_Baudrate = B600; break;
                    case "1200": Gl_Int_Baudrate = B1200; break;
                    case "1800": Gl_Int_Baudrate = B1800; break;
                    case "2400": Gl_Int_Baudrate = B2400; break;
                    case "4800": Gl_Int_Baudrate = B4800; break;
                    case "7200": Gl_Int_Baudrate = B7200; break;
                    case "9600": Gl_Int_Baudrate = B9600; break;
                    case "19200": Gl_Int_Baudrate = B19200; break;
                    case "38400": Gl_Int_Baudrate = B38400; break;
                    case "57600": Gl_Int_Baudrate = B57600; break;
                    case "115200": Gl_Int_Baudrate = B115200; break;
                    case "230400": Gl_Int_Baudrate = B230400; break;
                    case "460800": Gl_Int_Baudrate = B460800; break;
                    case "921600": Gl_Int_Baudrate = B921600; break;
                    default: Gl_Int_Baudrate = B9600; break;
                }
            }
            //Parity
            if (Ht_CommParam.Contains(KEY_PARITY))
            {
                switch (Ht_CommParam[KEY_PARITY].ToString())
                {
                    case "Even": Gl_Int_Parity = P_EVEN; break;
                    case "Odd": Gl_Int_Parity = P_ODD; break;
                    case "Space": Gl_Int_Parity = P_SPC; break;
                    case "Mark": Gl_Int_Parity = P_MRK; break;
                    case "None": Gl_Int_Parity = P_NONE; break;
                    default: Gl_Int_Parity = P_NONE; break;
                }
            }
            //Byte Size
            if (Ht_CommParam.Contains(KEY_BYTESIZE))
            {
                switch (Ht_CommParam[KEY_BYTESIZE].ToString())
                {
                    case "5": Gl_Int_ByteSize = BIT_5; break;
                    case "6": Gl_Int_ByteSize = BIT_6; break;
                    case "7": Gl_Int_ByteSize = BIT_7; break;
                    case "8": Gl_Int_ByteSize = BIT_8; break;
                    default: Gl_Int_ByteSize = BIT_8; break;
                }
            }
            //Stop Bits
            if (Ht_CommParam.Contains(KEY_STOPBITS))
            {
                switch (Ht_CommParam[KEY_STOPBITS].ToString())
                {
                    case "1": Gl_Int_StopBits = STOP_1; break;
                    case "2": Gl_Int_StopBits = STOP_2; break;
                    default: Gl_Int_StopBits = STOP_1; break;
                }
            }
            //Before Delay
            if (Ht_CommParam.Contains(KEY_BEFOREDELAY))
            {
                int.TryParse(Ht_CommParam[KEY_BEFOREDELAY].ToString(), out Gl_Int_BeforeDelay);
            }
            //Byte Delay
            if (Ht_CommParam.Contains(KEY_BYTEDELAY))
            {
                int.TryParse(Ht_CommParam[KEY_BYTEDELAY].ToString(), out Gl_Int_ByteDelay);
            }
            //Read Interval Timeout
            if (Ht_CommParam.Contains(KEY_READINTERVALTIMEOUT))
            {
                int.TryParse(Ht_CommParam[KEY_READINTERVALTIMEOUT].ToString(), out Gl_Int_ReadIntervalTimeout);
            }
            //After Delay
            if (Ht_CommParam.Contains(KEY_AFTERDELAY))
            {
                int.TryParse(Ht_CommParam[KEY_AFTERDELAY].ToString(), out Gl_Int_AfterDealy);
            }
        }


        /// <summary>
        /// 初始化串口通讯
        /// </summary>
        /// <param name="Hb_CommParam"></param>
        /// <returns>错误码</returns>
        public bool InitComm(Hashtable Ht_CommParam)
        {
            AnalyseCommParam(Ht_CommParam);

            //Open port
            int i_RtnCode = sio_open(Gl_Int_Port);
            if (i_RtnCode != SIO_OK)
            {

                return false;
            }

            //Configure communication parameters
            int mode = Gl_Int_Parity | Gl_Int_ByteSize | Gl_Int_StopBits;
            i_RtnCode = sio_ioctl(Gl_Int_Port, Gl_Int_Baudrate, mode);
            if (i_RtnCode != SIO_OK)
            {
                return false;
            }

            //Flow control
            i_RtnCode = sio_flowctrl(Gl_Int_Port, 0);
            if (i_RtnCode != SIO_OK)
            {
                return false;
            }

            //DTR
            i_RtnCode = sio_DTR(Gl_Int_Port, 1);
            if (i_RtnCode != SIO_OK)
            {
                return false;
            }

            //RTS
            i_RtnCode = sio_RTS(Gl_Int_Port, 1);
            if (i_RtnCode != SIO_OK)
            {
                return false;
            }

            //Set timeout values for sio_read 
            sio_SetReadTimeouts(Gl_Int_Port, Gl_Int_AfterDealy, Gl_Int_ReadIntervalTimeout);
            return true;
        }


        public bool Write(byte[] byteSend)
        {
            int i_RtnCode;
            if (byteSend == null || byteSend.Length == 0)
                return false;
            if (Gl_Int_ByteDelay == 0)
            {
                i_RtnCode = sio_write(Gl_Int_Port, ref byteSend[0], byteSend.Length);
                //WriteLog.LogIr("发送长度:"+ byteSend.Length+",实际长度："+ i_RtnCode);
                if (i_RtnCode < 0)
                {
                    return false;
                }
            }
            else
            {
                for (int i = 0; i < byteSend.Length; i++)
                {
                    i_RtnCode = sio_write(Gl_Int_Port, ref byteSend[i], 1);
                    if (i_RtnCode < 0)
                    {
                        return false;
                    }
                    Thread.Sleep(Gl_Int_ByteDelay);
                }
            }
            return true;
        }

        public bool Write(byte[] byteSend,int offset,int length)
        {
            int i_RtnCode;
            if (Gl_Int_ByteDelay == 0)
            {
                i_RtnCode = sio_write(Gl_Int_Port, ref byteSend[offset], length);
                if (i_RtnCode < 0)
                {
                    return false;
                }
            }
            else
            {
                for (int i = offset; i < offset + length; i++)
                {
                    i_RtnCode = sio_write(Gl_Int_Port, ref byteSend[i], 1);
                    if (i_RtnCode < 0)
                    {
                        return false;
                    }
                    Thread.Sleep(Gl_Int_ByteDelay);
                }
            }
            return true;
        }


        public int DataBufferLength
        {
            get { return sio_iqueue(Gl_Int_Port); }
        }

        public bool Write(string Str_SendFrame)
        {
            int i_RtnCode;

            byte[] buffer = System.Text.Encoding.Default.GetBytes(Str_SendFrame);
          
            if (Gl_Int_ByteDelay == 0)
            {
                i_RtnCode = sio_write(Gl_Int_Port, ref buffer[0], buffer.Length);
                if (i_RtnCode < 0)
                {
                    return false;
                }
            }
            else
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    i_RtnCode = sio_write(Gl_Int_Port, ref buffer[i], 1);
                    if (i_RtnCode < 0)
                    {
                        return false;
                    }
                    Thread.Sleep(Gl_Int_ByteDelay);
                }
            }
            return true;
        }

        public bool Read(int offset,int length, ref byte[] recbuffer)
        {
            recbuffer = new byte[length];
            int i_RtnCode = sio_read(Gl_Int_Port, ref recbuffer[0], recbuffer.Length);
            if (i_RtnCode < 0)
            {
                return false;
            }
            else if (i_RtnCode == 0)
            {
                return false;
            }
            else
                return true;
        }
        public bool IsOpen
        {
            get { return sio_lstatus(Gl_Int_Port) >= 0; }
        }
        /// <summary>
        /// 关闭串口通讯
        /// </summary>
        /// <returns>错误码</returns>
        public int CloseComm()
        {
            int i_RtnCode = sio_close(Gl_Int_Port);
            if (i_RtnCode != SIO_OK)
            {
                return i_RtnCode;
            }
            return 0;
        }

        /// <summary>
        /// 获取通讯错误消息
        /// </summary>
        /// <param name="i_ErrCode">错误码</param>
        /// <returns>错误消息</returns>
        public string GetCommErrMsg(int i_ErrCode)
        {
            switch (i_ErrCode)
            {
                case SIO_OK: return "成功";
                case SIO_BADPORT: return "串口号无效,检测串口号!";
                case SIO_OUTCONTROL: return "主板不是MOXA兼容的智能主板!";
                case SIO_NODATA: return "没有可读的数据!";
                case SIO_OPENFAIL: return "打开串口失败,检查串口是否被占用!";
                case SIO_RTS_BY_HW: return "不能控制串口因为已经通过sio_flowctrl设定为自动H/W流控制";
                case SIO_BADPARAM: return "串口参数错误,检查串口参数!";
                case SIO_WIN32FAIL: return "调用Win32函数失败!";
                case SIO_BOARDNOTSUPPORT: return "串口不支持这个函数!";
                case SIO_ABORT_WRITE: return "用户终止写数据块!";
                case SIO_WRITETIMEOUT: return "写数据超时!";
                case ERR_NOANSWER: return "无应答!";
                default: return i_ErrCode.ToString();
            }
        }

        /// <summary>
        /// 清空缓存
        /// </summary>
        /// <returns></returns>
        internal bool DiscardInBuffer()
        {
            return sio_flush(Gl_Int_Port, 0) >= 0;
        }

        /// <summary>
        /// 清空输出缓存
        /// </summary>
        /// <returns></returns>
        internal bool DiscardOutBuffer()
        {
            return sio_flush(Gl_Int_Port, 1) >= 0;
        }

        /// <summary>
        /// 清空输入输出输出缓存
        /// </summary>
        /// <returns></returns>
        internal bool DiscardIn_OutBuffer()
        {
            return sio_flush(Gl_Int_Port, 2) >= 0;
        }
    }
}
