using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TsHardWare;


namespace TsHardWare
{
    public class TCPController : IController
    {
        private int connOutTime = 5000;


        public int ConnOutTime
        {
            get { return connOutTime; }
        }
        //private IPEndPoint _endPoint = null;
        internal string IP;
        internal int Port;

        private TcpClient tcpClient = null;
        public string guid = Guid.NewGuid().ToString();
        public TCPController(string serIP, int serPort)
        {
            IP = serIP; Port = serPort;

            //_endPoint = new IPEndPoint(IPAddress.Parse(serIP), serPort);
        }

        private bool connectTmp;
        protected Exception exception;
        private int _timeout = 10000;

        public bool Connect()
        {
            connectTmp = false;
            exception = null;
            Thread thread = new Thread(new ThreadStart(BeginConnect));
            thread.IsBackground = true; // 作为后台线程处理
            // 不会占用机器太长的时间
            thread.Start();

            // 等待如下的时间
            thread.Join(connOutTime);

            if (connectTmp == true)
            {
                // 如果成功就返回TcpClient对象
                thread.Abort();
                return connectTmp;
            }
            if (exception != null)
            {
                // 如果失败就抛出错误
                thread.Abort();
                return false;
            }
            else
            {
                // 同样地抛出错误
                thread.Abort();
                string message = string.Format("TcpClient connection to {0}:{1} timed out",
                  IP, Port);
                return false;
            }
        }
        internal static bool PingCheck(string ip, int timeOut)
        {
            Ping ping = new Ping();
            PingReply pr = ping.Send(ip, timeOut);
            if (pr.Status == IPStatus.Success)
                return true;
            else
                return false;
        }
        protected void BeginConnect()
        {
            try
            {
                if (PingCheck(IP, 3000))
                {
                    Debug.Print("建立新的tcp" + IP);
                    tcpClient = new TcpClient();
                    tcpClient.Connect(IP, (int)Port);
                    this.tcpClient.SendTimeout = this._timeout;

                    this.tcpClient.ReceiveTimeout = this._timeout;

                    // 标记成功，返回调用者
                    connectTmp = true;
                    Debug.Print("建立新的tcp成功" + IP);
                }
            }
            catch (Exception ex)
            {
                // 标记失败
                exception = ex;
            }
        }

        public void Close()
        {
            if (IsConnect())
            {
                tcpClient.Close();
            }
        }

        public int Write(byte[] byteData, int length)
        {
            try
            {
                if (tcpClient == null)
                    return -1;
                if (length == -1)
                {
                    tcpClient.Client.Send(byteData);
                    return byteData.Length;
                }
                tcpClient.Client.Send(byteData, 0, byteData.Length, SocketFlags.None);
                return length;
            }
            catch (Exception ex)
            {
                Debug.Print(string.Format("TCP 发送EX{0}", ex.ToString()));
                return -1;
            }
        }

        public int Read(ref byte[] byteData, int length = -1)
        {
            try
            {
                if (tcpClient == null || !tcpClient.Connected)
                    return -1;
                int index = 0;
                if (byteData != null)
                {
                    //do
                    //{
                    int len = tcpClient.Client.Receive(byteData, 0, byteData.Length, SocketFlags.None);
                    // int len = netStream.Read(byteData, 0, byteData.Length);
                    if (len == 0)
                        return -1;
                    else
                        index += len;
                    //} while (index < byteData.Length);
                    return byteData.Length;
                }
                else
                {
                    int byteL = 0;

                    long len = tcpClient.Available;

                    if (len == 0)
                    {
                        return -1;
                    }

                    Debug.Print("条码长度" + len.ToString());
                    if (len > int.MaxValue)
                    {
                        byteL = int.MaxValue;
                    }
                    else
                    {
                        byteL = (int)len;
                    }
                    byteData = new byte[byteL];

                    do
                    {
                        int lenRtn = tcpClient.Client.Receive(byteData, 0, byteData.Length, SocketFlags.None);
                        if (lenRtn == 0)
                            return -1;
                        else
                            index += lenRtn;
                    }
                    while (index < byteData.Length);
                    return byteData.Length;
                }
            }
            catch (Exception ex)
            {
                Debug.Print(string.Format("TCP 接受EX{0}", ex.ToString()));
                return -1;
            }

        }

        public bool IsConnect()
        {
            return tcpClient != null && tcpClient.Client != null && tcpClient.Client.Connected;
        }

        public void DiscardBuff()
        {
            ;
        }
    }
}
