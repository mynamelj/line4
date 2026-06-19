using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using TsHardWare.Controller;
using System.IO.Ports;

namespace TsHardWare
{
    public class RTUController : IController
    {
        private int port;
        int baudRate;
        Parity parity;
        int dataBits;
        StopBits stopBits;
        public RTUController(int _port, int _baudRate, Parity _parity, int _dataBits, StopBits _stopBits)
        {
            port = _port;
            baudRate = _baudRate;
            parity = _parity;
            dataBits = _dataBits;
            stopBits = _stopBits;
        }

        private SerialPort serialPort;



        public bool Connect()
        {
            serialPort = new SerialPort();
            serialPort.Parity = parity;
            serialPort.BaudRate = baudRate;
            serialPort.DataBits = dataBits;
            serialPort.StopBits = stopBits;
            serialPort.PortName = "COM" + port.ToString();
            try
            {
                serialPort.Open();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public void Close()
        {
            if (serialPort != null)
            {
                serialPort.Close();
            }
        }

        public int Write(byte[] byteData, int length = -1)
        {
            try
            {
                if (serialPort == null || byteData == null || serialPort.IsOpen == false)
                    return -1;
                DiscardBuff();//请缓存
                if (length == -1)
                {
                    serialPort.Write(byteData, 0, byteData.Length);
                    return byteData.Length;
                }
                else
                {
                    serialPort.Write(byteData, 0, length);
                    return length;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }



        public int Read(ref byte[] byteData, int length = -1)
        {
            try
            {
                if (serialPort != null && serialPort.IsOpen)
                {
                    int readLength = serialPort.BytesToRead;
                    if (readLength > 0)
                    {
                        byteData = new byte[readLength];
                        if (length == -1)
                        {
                            if (serialPort.Read(byteData, 0, readLength) > 0)
                            {
                                DiscardBuff();
                                return readLength;
                            }
                        }
                        byteData = new byte[length];
                        if (serialPort.Read(byteData, 0, length) > 0)
                        {
                            return length;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }

            return -1;
        }

        public bool IsConnect()
        {
            return serialPort != null && serialPort.IsOpen;
        }

        public void DiscardBuff()
        {
            if (serialPort != null && serialPort.IsOpen)
            {
                serialPort.DiscardInBuffer();
                serialPort.DiscardOutBuffer();
            }
        }
    }
}
