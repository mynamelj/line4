using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.IO.Ports;

namespace TsHardWare
{
    public class RTUController : IController
    {

        private string port;
        int baudRate;
        Parity parity;
        int dataBits;
        StopBits stopBits;
        public RTUController(string _port, int _baudRate, Parity _parity, int _dataBits, StopBits _stopBits)
        {
            port = _port;
            baudRate = _baudRate;
            parity = _parity;
            dataBits = _dataBits;
            stopBits = _stopBits;
        }

        private SerialPort serial;


        public bool Connect()
        {
            serial = new SerialPort();
            serial.PortName = port;
            serial.BaudRate = baudRate;
            serial.Parity = parity;
            serial.DataBits = dataBits;
            serial.StopBits = stopBits;
            serial.Open();
            

            return serial.IsOpen;
        }

        public void Close()
        {
            if (serial != null)
            {
                serial.Close();
            }
        }

        public int Write(byte[] byteData, int length = -1)
        {
            if (serial == null || byteData == null || serial.IsOpen == false)
                return -1;
            serial.DiscardInBuffer();
            serial.DiscardOutBuffer();//请缓存
            if (length == -1)
            {
                serial.Write(byteData, 0, byteData.Length);
                //WriteLog.LogIr("发送是否成功:" + bl.ToString());
                return byteData.Length;
            }
            else
            {
                serial.Write(byteData, 0, length);
                return length;
            }
        }

        public int Read(ref byte[] byteData, int length = -1)
        {
            if (serial != null && serial.IsOpen)
            {
                int readLength = serial.ReadBufferSize;
                if (readLength > 0)
                {
                    byteData = new byte[readLength];
                    if (length == -1)
                    {
                        if (serial.Read(byteData, 0, readLength) >= 0)
                        {
                            serial.DiscardInBuffer();
                            serial.DiscardOutBuffer();//请缓存
                            return readLength;
                        }
                    }
                    byteData = new byte[length];
                    if (serial.Read(byteData, 0, readLength) >= 0)
                    {
                        return length;
                    }
                }
            }
            return -1;
        }

        public bool IsConnect()
        {
            return serial != null && serial.IsOpen;
        }

        public void DiscardBuff()
        {
            if (serial != null && serial.IsOpen)
            {
                serial.DiscardInBuffer();
                serial.DiscardOutBuffer();
            }
        }
    }
}
