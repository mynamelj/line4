using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MES.Service
{
    public interface IWindowService
    {
        // 显示普通窗口
        void Show<TWindow>() where TWindow : class;

        // 显示对话框（阻塞），并返回结果
        bool? ShowDialog<TWindow>() where TWindow : class;
    }
}
