using Autofac;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MES.Service
{
    public class WindowService : IWindowService
    {
        private readonly ILifetimeScope _lifetimeScope;

        // 注入 Autofac 容器的生命周期作用域
        public WindowService(ILifetimeScope lifetimeScope)
        {
            _lifetimeScope = lifetimeScope;
        }

        public void Show<TWindow>() where TWindow : class
        {
            var window = ResolveWindow<TWindow>();
            window.Show();
        }

        public bool? ShowDialog<TWindow>() where TWindow : class
        {
            var window = ResolveWindow<TWindow>();
            return window.ShowDialog();
        }

        // 从 Autofac 容器中解析窗口
        private Window ResolveWindow<TWindow>() where TWindow : class
        {
            var window = _lifetimeScope.Resolve<TWindow>() as Window;
            if (window == null)
            {
                throw new InvalidOperationException($"类型 {typeof(TWindow).Name} 不是一个有效的窗口。");
            }
            return window;
        }
    }
}
