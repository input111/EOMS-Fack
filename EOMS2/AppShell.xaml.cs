using Microsoft.Maui.Controls;
using System;

namespace EOMS2
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // 注册路由
            Routing.RegisterRoute(nameof(DelayRequestPage), typeof(DelayRequestPage));
            Routing.RegisterRoute(nameof(Views.PhotoManagerPage), typeof(Views.PhotoManagerPage));
            Routing.RegisterRoute(nameof(shezhi), typeof(shezhi));

            // 添加导航事件处理
            this.Navigating += OnShellNavigating;
        }

        private void OnShellNavigating(object sender, ShellNavigatingEventArgs e)
        {
            if (e.Source == ShellNavigationSource.Push)
            {
                // 添加简单的过渡动画
                this.FlyoutBehavior = FlyoutBehavior.Flyout;
            }
        }

    }
}