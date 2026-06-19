using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TsHardWare
{

    /// <summary>
    /// 设备接口
    /// </summary>
    public interface IController
    {
        /// <summary>
        /// 是否连接
        /// </summary>
        /// <returns></returns>
        bool IsConnect();

        /// <summary>
        /// 连接
        /// </summary>
        /// <returns></returns>
        bool Connect();

        /// <summary>
        /// 关闭
        /// </summary>
        void Close();

        /// <summary>
        /// 写信息
        /// </summary>
        /// <param name="byteData"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        int Write(byte[] byteData, int length);

        /// <summary>
        /// 读信息
        /// </summary>
        /// <param name="byteData"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        int Read(ref byte[] byteData, int length);

        /// <summary>
        /// 清缓存
        /// </summary>
        /// <returns></returns>
        void DiscardBuff();
    }

}
