﻿#region 文件描述
/******************************************************************************
* 创建: Daoting
* 摘要: 
* 日志: 2020-07-21 创建
******************************************************************************/
#endregion

#region 引用命名
using Dt.Base.Tools;
using Dt.Core;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#endregion

namespace Dt.Base
{
    /// <summary>
    /// 默认系统回调
    /// </summary>
    internal class DefaultCallback : ICallback
    {
        /// <summary>
        /// 显示登录页面
        /// </summary>
        /// <param name="p_isPopup">是否为弹出式</param>
        public void Login(bool p_isPopup)
        {
            Startup.ShowLogin(p_isPopup);
        }

        /// <summary>
        /// 注销后重新登录
        /// </summary>
        public async void Logout()
        {
            // 注销时清空用户信息
            AtUser.Reset();

            AtState.DeleteCookie("LoginPhone");
            AtState.DeleteCookie("LoginPwd");

            await AtSys.Stub.OnLogout();
            Startup.ShowLogin(false);
        }

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        /// <param name="p_content">消息内容</param>
        /// <param name="p_title">标题</param>
        /// <returns>true表确认</returns>
        public Task<bool> Confirm(string p_content, string p_title)
        {
            var dlg = new Dlg { Title = p_title, IsPinned = true };
            if (AtSys.IsPhoneUI)
            {
                dlg.PhonePlacement = DlgPlacement.CenterScreen;
                dlg.Width = SysVisual.ViewWidth - 40;
            }
            else
            {
                dlg.WinPlacement = DlgPlacement.CenterScreen;
                dlg.ShowWinVeil = true;
                dlg.MinWidth = 300;
                dlg.MaxWidth = SysVisual.ViewWidth / 4;
            }
            Grid grid = new Grid { Margin = new Thickness(10) };
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.0, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            TextBlock tb = new TextBlock { Text = p_content, TextWrapping = TextWrapping.Wrap };
            grid.Children.Add(tb);

            StackPanel spBtn = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 20, 0, 0), HorizontalAlignment = HorizontalAlignment.Right };
            var btn = new Button { Content = "确认", Margin = new Thickness(0, 0, 20, 0) };
            btn.Click += (s, e) => dlg.Close(true);
            spBtn.Children.Add(btn);
            btn = new Button { Content = "取消" };
            btn.Click += (s, e) => dlg.Close(false);
            spBtn.Children.Add(btn);
            Grid.SetRow(spBtn, 1);
            grid.Children.Add(spBtn);
            dlg.Content = grid;
            return dlg.ShowAsync();
        }

        /// <summary>
        /// 显示错误对话框
        /// </summary>
        /// <param name="p_content">消息内容</param>
        /// <param name="p_title">标题</param>
        public void Error(string p_content, string p_title)
        {
            var dlg = new Dlg { Title = p_title, IsPinned = true };
            if (AtSys.IsPhoneUI)
            {
                dlg.PhonePlacement = DlgPlacement.CenterScreen;
                dlg.Width = SysVisual.ViewWidth - 40;
            }
            else
            {
                dlg.WinPlacement = DlgPlacement.CenterScreen;
                dlg.ShowWinVeil = true;
                dlg.MinWidth = 300;
                dlg.MaxWidth = SysVisual.ViewWidth / 4;
            }
            Grid grid = new Grid { Margin = new Thickness(20), VerticalAlignment = VerticalAlignment.Center };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
            grid.Children.Add(new TextBlock { Text = "\uE037", FontFamily = AtRes.IconFont, Foreground = AtRes.RedBrush, FontSize = 30, Margin = new Thickness(0, 0, 10, 0), });
            var tb = new TextBlock { Text = p_content, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(tb, 1);
            grid.Children.Add(tb);
            dlg.Content = grid;
            dlg.Show();
        }

        /// <summary>
        /// 显示监视窗口
        /// </summary>
        public void ShowTraceBox()
        {
            SysTrace.ShowBox();
        }

        /// <summary>
        /// 挂起时的处理，必须耗时小！
        /// 手机或PC平板模式下不占据屏幕时触发，此时不确定被终止还是可恢复
        /// </summary>
        /// <returns></returns>
        public Task OnSuspending()
        {
            // ios在转入后台有180s的处理时间，过后停止所有操作，http连接瞬间自动断开
            // android各版本不同
            return Task.CompletedTask;

            // 取消正在进行的上传
            //Uploader.Cancel();

            // asp.net core2.2时因客户端直接关闭app时会造成服务器端http2连接关闭，该连接下的所有Register推送都结束！！！只能从服务端Abort来停止在线推送
            // 升级道.net 5.0后不再出现该现象！无需再通过服务端Abort
            //if (AtUser.IsLogon && PushHandler.RetryState == PushRetryState.Enable)
            //{
            //    PushHandler.RetryState = PushRetryState.Stop;
            //    await AtMsg.Unregister();
            //}
        }

        /// <summary>
        /// 恢复会话时的处理，手机或PC平板模式下再次占据屏幕时触发
        /// </summary>
        public void OnResuming()
        {
            if (AtUser.IsLogon && !PushHandler.StopRetry)
            {
                // 在线推送可能被停止，重新启动
                PushHandler.RetryTimes = 0;
                _ = PushHandler.Register();
            }
        }
    }
}