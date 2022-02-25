﻿#region 文件描述
/******************************************************************************
* 创建: Daoting
* 摘要: 
* 日志: 2021-06-07 创建
******************************************************************************/
#endregion

#region 引用命名
using Dt.Core;
using Dt.Core.Rpc;
using System;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Collections.Generic;
#endregion

namespace Dt.Base
{
    /// <summary>
    /// 启动控制
    /// </summary>
    public static class Startup
    {
        #region 启动入口
        /// <summary>
        /// 应用程序启动
        /// </summary>
        /// <param name="p_stub">存根类型</param>
        /// <param name="p_launchArgs">启动参数</param>
        /// <param name="p_shareInfo">接收分享的内容描述</param>
        /// <returns></returns>
        public static async Task Launch(Type p_stub, string p_launchArgs = null, ShareInfo p_shareInfo = null)
        {
            if (!string.IsNullOrEmpty(p_launchArgs))
            {
                try
                {
                    // 带参数启动
                    AutoStartOnce = JsonSerializer.Deserialize<AutoStartInfo>(p_launchArgs);
                }
                catch { }
            }

            // 非null表示app已启动过
            if (Kit.Stub != null)
            {
                // 带参数启动
                if (AutoStartOnce != null)
                    ShowAutoStartOnce();
                Kit.MainWin.Activate();

                if (p_shareInfo != null)
                    Kit.Stub.OnReceiveShare(p_shareInfo);
                return;
            }

            // 合并资源，因Dt.Client打包后uno无法在App.xaml中合并资源，故通过代码合并
            var res = Application.Current.Resources;
            // 后合并的样式优先
            res.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("ms-appx:///Dt.Base/Themes/Generic.xaml") });

#if ANDROID || WASM //|| IOS
            // Frame CommandBar AppBarButton采用本地样式及动画
            // 原来在Global.xaml中定义，Frame内部使用NativeFramePresenter承载
            // 目前 uno4.2 dev 版本iOS不能运行
            Uno.UI.FeatureConfiguration.Style.ConfigureNativeFrameNavigation();
#endif

#if WASM
            // 自定义图标字体库，因Global.xaml中前缀无效无法定义
            res["IconFont"] = new FontFamily("DtIcon");
            res["Symbols"] = new FontFamily("Symbols");
#endif

            try
            {
                // 创建存根、系统初始化，内含创建窗口及整个系统可视树
                Stub stub = (Stub)Activator.CreateInstance(p_stub);
                await Kit.Startup(stub, new DefaultCallback());

                // 连接cm服务，获取全局参数，更新/打开模型库
                if (Kit.IsUsingDtSvc)
                    await InitConfig();

                // 从存根启动，因uno中无法在一个根UI的Loaded事件中切换到另一根UI，所以未采用启动页方式
                await stub.OnStartup();

                // 接收分享
                if (p_shareInfo != null)
                    stub.OnReceiveShare(p_shareInfo);

                // 注册后台任务
                if (stub.EnableBgTask)
                    BgJob.Register();

                InputManager.Init();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        /// <summary>
        /// 连接cm服务，获取全局参数，更新打开模型库
        /// </summary>
        /// <returns></returns>
        static async Task InitConfig()
        {
            // 获取全局参数：服务器时间、所有服务地址、模型文件版本号
            List<object> cfg;
            try
            {
                cfg = await AtCm.GetConfig();
            }
            catch
            {
                throw new Exception("服务器连接失败！");
            }

            if (cfg == null || cfg.Count != 3)
                throw new Exception("获取参数失败！");

            // 服务器时间、初始化服务地址
            Kit.SyncTime((DateTime)cfg[0]);
            Kit.InitSvcUrls(cfg[1]);

            // 更新打开模型库
            await OpenModelDb(cfg[2] as string);
        }

        /// <summary>
        /// 更新打开模型文件
        /// 1. 与本地不同时下载新模型文件；
        /// 2. 打开模型库；
        /// </summary>
        /// <param name="p_ver"></param>
        /// <returns></returns>
        static async Task OpenModelDb(string p_ver)
        {
            // 更新模型文件
            string modelVer = Path.Combine(Kit.DataPath, $"model-{p_ver}.ver");
            if (!File.Exists(modelVer))
            {
                string modelFile = Path.Combine(Kit.DataPath, "model.db");

                // 删除旧版的模型文件和版本号文件
                try { File.Delete(modelFile); } catch { }
                foreach (var file in new DirectoryInfo(Kit.DataPath).GetFiles($"model-*.ver"))
                {
                    try { file.Delete(); } catch { }
                }

                try
                {
                    // 下载模型文件，下载地址如 https://localhost/app-cm/.model
                    using (var response = await BaseRpc.Client.GetAsync($"{Kit.GetSvcUrl("cm")}/.model"))
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (var gzipStream = new GZipStream(stream, CompressionMode.Decompress))
                    using (var fs = File.Create(modelFile, 262140, FileOptions.WriteThrough))
                    {
                        gzipStream.CopyTo(fs);
                        fs.Flush();
                    }

                    // 版本号文件
                    File.Create(modelVer);
                }
                catch (Exception ex)
                {
                    try
                    {
                        File.Delete(modelFile);
                    }
                    catch { }
                    throw new Exception("下载模型文件失败！" + ex.Message);
                }
            }

            // 打开模型库
            try
            {
                AtModel.OpenDb();
            }
            catch (Exception ex)
            {
                throw new Exception("打开模型库失败！" + ex.Message);
            }
        }

        /// <summary>
        /// 启动过程中显示错误信息，此时未加载任何UI
        /// </summary>
        /// <param name="p_error"></param>
        static void ShowError(string p_error)
        {
            var dlg = new Dlg { IsPinned = true, Resizeable = false, HideTitleBar = true, ShowVeil = false, Background = Res.主蓝 };
            if (!Kit.IsPhoneUI)
            {
                dlg.WinPlacement = DlgPlacement.CenterScreen;
                dlg.MinWidth = 300;
                dlg.MaxWidth = Kit.ViewWidth / 4;
                dlg.BorderThickness = new Thickness(0);
            }
            var pnl = new StackPanel { Margin = new Thickness(40), VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
            pnl.Children.Add(new TextBlock { Text = "\uE037", FontFamily = Res.IconFont, Foreground = Res.WhiteBrush, FontSize = 40, Margin = new Thickness(0, 0, 0, 10), HorizontalAlignment = HorizontalAlignment.Center });
            pnl.Children.Add(new TextBlock { Text = p_error, Foreground = Res.WhiteBrush, FontSize = 20, TextWrapping = TextWrapping.Wrap, HorizontalAlignment = HorizontalAlignment.Center });
            dlg.Content = pnl;
            dlg.Show();
        }
        #endregion

        #region 按默认流程启动
        /// <summary>
        /// 按默认流程启动，
        /// <para>1. 记录主页和登录页的类型，以备登录、注销、自动登录、中途登录时用</para>
        /// <para>2. 不使用dt服务时，直接显示主页</para>
        /// <para>3. 已登录过，先自动登录</para>
        /// <para>4. 未登录或登录失败时，根据 p_loginFirst 显示登录页或主页</para>
        /// </summary>
        /// <param name="p_homePageType">主页类型，null时采用默认主页 DefaultHome</param>
        /// <param name="p_loginFirst">是否强制先登录，默认true</param>
        /// <param name="p_loginPageType">登录页类型，null时采用默认登录页 DefaultLogin</param>
        /// <returns></returns>
        public static async Task Run(Type p_homePageType = null, bool p_loginFirst = true, Type p_loginPageType = null)
        {
            _homePageType = p_homePageType;
            _loginPageType = p_loginPageType;

            // 不使用dt服务，直接显示主页
            if (!Kit.IsUsingDtSvc)
            {
                ShowHome();
                return;
            }

            string phone = AtState.GetCookie("LoginPhone");
            string pwd = AtState.GetCookie("LoginPwd");
            if (!string.IsNullOrEmpty(phone) && !string.IsNullOrEmpty(pwd))
            {
                // 自动登录
                var result = await AtCm.LoginByPwd<LoginResult>(phone, pwd);

                // 登录成功
                if (result.IsSuc)
                {
                    Kit.InitUser(result);
                    // 切换到主页
                    ShowHome();
                    // 接收服务器推送
                    PushHandler.Register();
                    return;
                }
            }

            // 未登录或登录失败
            if (p_loginFirst)
            {
                // 强制先登录
                ShowLogin(false);
            }
            else
            {
                // 未登录先显示主页
                ShowHome();
            }
        }
        #endregion

        #region 注册接收推送
        /// <summary>
        /// 注册接收服务器推送
        /// </summary>
        public static void RegisterSysPush()
        {
            PushHandler.Register();
        }

        /// <summary>
        /// 主动停止接收推送
        /// </summary>
        public static void StopSysPush()
        {
            PushHandler.StopRecvPush();
        }
        #endregion

        #region 加载登录页
        /// <summary>
        /// 登录页面类型，null时采用 DefaultLogin
        /// </summary>
        static Type _loginPageType;

        /// <summary>
        /// 当前登录页面类型，未设置时采用 DefaultLogin
        /// </summary>
        public static Type LoginPageType
        {
            get { return _loginPageType == null ? Type.GetType("Dt.App.DefaultLogin,Dt.App") : _loginPageType; }
        }

        /// <summary>
        /// 显示登录页面
        /// </summary>
        /// <param name="p_isPopup">是否为弹出式</param>
        public static void ShowLogin(bool p_isPopup)
        {
            Kit.RunAsync(() =>
            {
                // 外部未指定时采用默认登录页
                Type tp = LoginPageType;
                var page = Activator.CreateInstance(tp) as UIElement;
                if (!p_isPopup)
                {
                    SysVisual.RootContent = page;
                    return;
                }

                // 弹出式登录页面在未登录遇到需要登录的功能时
                var dlg = new Dlg
                {
                    Resizeable = false,
                    HideTitleBar = true,
                    PhonePlacement = DlgPlacement.Maximized,
                    WinPlacement = DlgPlacement.Maximized,
                    Content = page,
                };
                dlg.Show();
            });
        }
        #endregion

        #region 加载主页
        static Type _homePageType;

        /// <summary>
        /// 主页类型
        /// </summary>
        public static Type HomePageType
        {
            get { return _homePageType == null ? Type.GetType("Dt.App.DefaultHome,Dt.App") : _homePageType; }
        }

        /// <summary>
        /// 自启动信息，加载主页前设置有效
        /// </summary>
        public static AutoStartInfo AutoStartOnce { get; set; }

        /// <summary>
        /// 加载根内容 Desktop/Frame 和主页
        /// </summary>
        public static void ShowHome()
        {
            if (Kit.IsPhoneUI)
                LoadRootFrame();
            else
                LoadDesktop();
        }

        /// <summary>
        /// 加载PhoneUI模式的根Frame
        /// </summary>
        internal static void LoadRootFrame()
        {
            SysVisual.RootContent = new Frame();

            // 主页作为根
            if (HomePageType != null)
            {
                Win win = Activator.CreateInstance(HomePageType) as Win;
                if (win != null)
                {
                    if (string.IsNullOrEmpty(win.Title))
                        win.Title = "主页";
                    win.Icon = Icons.主页;
                    win.NaviToHome();
                }
            }

            // 自启动
            AutoStartInfo autoStart = AutoStartOnce != null ? AutoStartOnce : AtState.GetAutoStart();
            if (autoStart != null)
            {
                // 用户设置的自启动
                Win win = CreateAutoStartWin(autoStart);
                if (win != null)
                {
                    win.NaviToHome();
                }
                else if (AutoStartOnce == null)
                {
                    // 用户设置的自启动，启动失败删除cookie
                    AtState.DelAutoStart();
                }
                // 只自启动一次
                AutoStartOnce = null;
            }
        }

        /// <summary>
        /// 加载Windows模式桌面
        /// </summary>
        internal static void LoadDesktop()
        {
            Desktop desktop = new Desktop();

            // 主页
            if (HomePageType != null)
            {
                Win win = Activator.CreateInstance(HomePageType) as Win;
                if (win != null)
                {
                    if (string.IsNullOrEmpty(win.Title))
                        win.Title = "主页";
                    win.Icon = Icons.主页;
                    desktop.HomeWin = win;
                }
            }

            // 自启动
            AutoStartInfo autoStart = AutoStartOnce != null ? AutoStartOnce : AtState.GetAutoStart();
            if (autoStart != null)
            {
                // 用户设置的自启动
                Win win = CreateAutoStartWin(autoStart);
                if (win != null)
                {
                    desktop.ShowNewWin(win);
                }
                else if (AutoStartOnce == null)
                {
                    // 用户设置的自启动，启动失败删除cookie
                    AtState.DelAutoStart();
                }
                // 只自启动一次
                AutoStartOnce = null;
            }

            if (desktop.MainWin == null)
                desktop.MainWin = desktop.HomeWin;
            SysVisual.RootContent = desktop;
        }
        #endregion

        #region 自定义根页面
        /// <summary>
        /// 显示自定义根页面
        /// </summary>
        /// <param name="p_page"></param>
        public static void ShowRoot(UIElement p_page)
        {
            SysVisual.RootContent = p_page;
        }
        #endregion

        #region 自启动
        /// <summary>
        /// 设置自启动
        /// </summary>
        /// <param name="p_win"></param>
        internal static void SetAutoStart(Win p_win)
        {
            AutoStartInfo info = GetAutoStartInfo(p_win);
            AtState.SaveAutoStart(info);
            Kit.Msg(string.Format("{0}已设置自启动！", p_win.Title));
        }

        /// <summary>
        /// 取消自启动
        /// </summary>
        internal static void DelAutoStart()
        {
            AtState.DelAutoStart();
            Kit.Msg("已取消自启动设置！");
        }

        /// <summary>
        /// 创建自启动Win
        /// </summary>
        /// <param name="p_autoStart"></param>
        /// <returns></returns>
        static Win CreateAutoStartWin(AutoStartInfo p_autoStart)
        {
            Win win = null;
            Type type = Type.GetType(p_autoStart.WinType);
            if (type != null)
            {
                try
                {
                    if (string.IsNullOrEmpty(p_autoStart.Params))
                    {
                        win = (Win)Activator.CreateInstance(type);
                    }
                    else
                    {
                        var par = JsonSerializer.Deserialize(p_autoStart.Params, Type.GetType(p_autoStart.ParamsType));
                        win = (Win)Activator.CreateInstance(type, par);
                    }

                    if (win != null)
                    {
                        win.Title = string.IsNullOrEmpty(p_autoStart.Title) ? "自启动" : p_autoStart.Title;
                        Icons icon;
                        if (Enum.TryParse(p_autoStart.Icon, out icon))
                            win.Icon = icon;
                    }
                }
                catch { }
            }
            return win;
        }

        /// <summary>
        /// 获取Win的自启动信息
        /// </summary>
        /// <param name="p_win"></param>
        /// <returns></returns>
        internal static AutoStartInfo GetAutoStartInfo(Win p_win)
        {
            if (p_win == null)
                return null;

            Tabs tabs = (Tabs)p_win.GetValue(Win.MainTabsProperty);
            if (tabs != null
                && tabs.Items.Count > 0
                && ((Tab)tabs.Items[0]).Content is Win win)
            {
                // 设置主区窗口为自启动
                p_win = win;
            }

            AutoStartInfo info = new AutoStartInfo();
            info.WinType = p_win.GetType().AssemblyQualifiedName;
            info.Title = p_win.Title;
            info.Icon = p_win.Icon.ToString();
            if (p_win.Params != null)
            {
                info.Params = JsonSerializer.Serialize(p_win.Params, JsonOptions.UnsafeSerializer);
                info.ParamsType = p_win.Params.GetType().AssemblyQualifiedName;
            }
            return info;
        }

        static void ShowAutoStartOnce()
        {
            Win win = CreateAutoStartWin(AutoStartOnce);
            if (win != null)
            {
                if (Kit.IsPhoneUI)
                    win.NaviToHome();
                else
                    Desktop.Inst.ShowNewWin(win);
            }
            AutoStartOnce = null;
        }
        #endregion
    }
}
