using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MES
{
    /// <summary>
    /// 订阅设备状态变更为1运行时，关闭开箱按钮使能
    /// </summary>
    public class StatusManager
    {
        
        public static event Action<object> MessagePublished;

        // 发布消息
        public static void PublishMessage(object message)
        {
            MessagePublished?.Invoke(message);
        }

        // 订阅消息
        public static void Subscribe(Action<object> subscriber)
        {
            MessagePublished += subscriber;
        }

        // 取消订阅
        public static void Unsubscribe(Action<object> subscriber)
        {
            MessagePublished -= subscriber;
        }
    }
}
