using MES.Manager;
using MES.SetModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MES.Comm
{
    public class ListenPLC
    {

        /// <summary>
        /// 监听PLC信号
        /// </summary>
        /// <param name="name">PLC组名</param>
        /// <param name="tagItem">标签项</param>
        /// <param name="target">期望的目标值</param>
        /// <param name="token">取消令牌，用于停止监听</param>
        /// <returns>是否成功监听到目标信号</returns>
        public Task ListenAsync(PLCGroupName name, string tagItem, bool target, Action action, CancellationToken token = default)
        {

                return Task.Run(async () =>
                {
                    // 记录上一次读取的值，
                    bool lastValue = !target;

                    while (!token.IsCancellationRequested)
                    {
                        try
                        {
                            object temp = null;
                            bool readSuccess = SetHelper.siemens.ReadItem(name, tagItem, ref temp);

                            if (readSuccess && temp is bool boolValue)
                            {
                                if (boolValue == target && lastValue != target)
                                {
                                        action?.Invoke();
                                }

                                // 更新历史状态
                                lastValue = boolValue;
                            }
                            else if (!readSuccess)
                            {
                                SetHelper.ListPLCMessage.ShowInfoQueue($"读取PLC节点 {tagItem} 失败", false, "PLC_Error");
                            }
                            await Task.Delay(200, token);
                        }
                        catch (OperationCanceledException) { /* 正常取消，无需处理 */ }
                        catch (Exception ex)
                        {
                            SetHelper.ListPLCMessage.ShowInfoQueue($"监听PLC异常: {ex.Message}", false, "ex");
                        }
                    }
                }, token);
         }

        public Task ListenPLCTaskAsync(
        PLCGroupName name,string tagItem,bool target,Func<Task> action, CancellationToken token = default)
        {
            return Task.Run(async () =>
            {
                bool lastValue = !target;

                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        object temp = null;
                        bool readSuccess = SetHelper.siemens.ReadItem(name, tagItem, ref temp);

                        if (readSuccess && temp is bool boolValue)
                        {
                            // 上升沿/目标沿触发
                            if (boolValue == target && lastValue != target)
                            {
                                if (action != null)
                                {
                                    await action();
                                }
                            }

                            lastValue = boolValue;
                        }
                        else if (!readSuccess)
                        {
                            SetHelper.ListPLCMessage.ShowInfoQueue($"读取PLC节点 {tagItem} 失败", false, "PLC_Error");
                        }

                        await Task.Delay(100, token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        SetHelper.ListPLCMessage.ShowInfoQueue($"监听PLC异常: {ex.Message}", false, "ex");
                    }
                }
            }, token);
        }


        /// <summary>
        /// 监听PLC状态变化（带防抖），适用于维修状态等非瞬时跳变信号。
        /// 状态必须持续稳定 <paramref name="debounceDuration"/> 毫秒后，才触发 <paramref name="action"/>。
        /// </summary>
        /// <param name="name">PLC组名</param>
        /// <param name="tagItem">标签项</param>
        /// <param name="action">状态稳定后执行的回调，参数为当前稳定值</param>
        /// <param name="debounceDuration">防抖持续时间（毫秒），默认800ms</param>
        /// <param name="pollInterval">轮询间隔（毫秒），默认200ms</param>
        /// <param name="token">取消令牌</param>
        public Task ListenStateChangeAsync(
            PLCGroupName name,
            string tagItem,
            Action<bool> action,
            CancellationToken token = default,
            int debounceDuration = 800,
            int pollInterval = 200
            )
        {
            return Task.Run(async () =>
            {
                bool? lastConfirmedValue = null; // 上一次稳定确认的值
                bool? pendingValue = null;        // 正在防抖中的候选值
                int stableCount = 0;
                int requiredCount = Math.Max(1, debounceDuration / pollInterval);

                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        object temp = null;
                        bool readSuccess = SetHelper.siemens.ReadItem(name, tagItem, ref temp);

                        if (readSuccess && temp is bool boolValue)
                        {
                            if (boolValue == pendingValue)
                            {
                                // 值持续稳定，累加计数
                                stableCount++;
                                if (stableCount >= requiredCount && boolValue != lastConfirmedValue)
                                {
                                    // 稳定时间达到，且与上次确认值不同，触发回调
                                    lastConfirmedValue = boolValue;
                                    action?.Invoke(boolValue);
                                }
                            }
                            else
                            {
                                // 值发生变化，重新开始防抖计数
                                pendingValue = boolValue;
                                stableCount = 1;
                            }
                        }
                        else if (!readSuccess)
                        {
                            SetHelper.ListPLCMessage.ShowInfoQueue($"读取PLC节点 {tagItem} 失败", false, "PLC_Error");
                        }

                        await Task.Delay(pollInterval, token);
                    }
                    catch (OperationCanceledException) { /* 正常取消，无需处理 */ }
                    catch (Exception ex)
                    {
                        SetHelper.ListPLCMessage.ShowInfoQueue($"监听PLC状态变化异常: {ex.Message}", false, "ex");
                    }
                }
            }, token);
        }



        public Task ListenStateChangeAsync(
            PLCGroupName name,
            string tagItem,
            Action<int> action,
            CancellationToken token = default,
            int debounceDuration = 800,
            int pollInterval = 200)
        {
            return Task.Run(async () =>
            {
                int? lastConfirmedValue = null;
                int? pendingValue = null;
                int stableCount = 0;
                int requiredCount = Math.Max(1, debounceDuration / pollInterval);

                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        object temp = null;
                        bool readSuccess = SetHelper.siemens.ReadItem(name, tagItem, ref temp);

                        if (readSuccess)
                        {
                            int intValue = temp.Obj2Int();

                            if (pendingValue.HasValue && intValue == pendingValue.Value)
                            {
                                stableCount++;
                                if (stableCount >= requiredCount && intValue != lastConfirmedValue)
                                {
                                    lastConfirmedValue = intValue;
                                    stableCount = 0;   // 触发后重置
                                    action?.Invoke(intValue);
                                }
                            }
                            else
                            {
                                pendingValue = intValue;
                                stableCount = 1;
                            }
                        }
                        else
                        {
                            SetHelper.ListPLCMessage.ShowInfoQueue($"读取PLC节点 {tagItem} 失败", false, "PLC_Error");
                        }

                        await Task.Delay(pollInterval, token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        SetHelper.ListPLCMessage.ShowInfoQueue($"监听PLC状态变化异常: {ex.Message}", false, "ex");
                    }
                }
            }, token);
        }




    }
}