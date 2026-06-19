using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MES.Comm
{
    /// <summary>
    /// 订阅MES出站提示的物料不足数据，发布到物料页面清空显示，并且自动下料
    /// </summary>
    public class MessageManager
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
