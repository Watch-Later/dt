﻿#region 文件描述
/******************************************************************************
* 创建: Daoting
* 摘要: 
* 日志: 2013-12-16 创建
******************************************************************************/
#endregion

#region 引用命名
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
#endregion

namespace Dt.Core
{
    /// <summary>
    /// 系统可视树管理类，三层：桌面层/页面层、对话框层、提示信息层
    /// uwp的完整UI树：ScrollViewer - Border - app UI
    /// uno的完整UI树：Grid - Border - app UI
    /// app UI：Grid - 桌面层/页面层
    ///              - 对话框层
    ///              - 提示信息层
    /// </summary>
    internal static class SysVisual
    {
        #region 成员变量
        //**************************************************************************
        // 响应式设计：三种布局方式对应三种界面宽度
        // 1. 界面宽度 <= 640px，PhoneUI模式，4"到6"设备 或 缩小的窗口，只一列面板
        // 2. 界面宽度在 641px ~ 1007px，7"到12"设备 或 缩小的窗口，最多两列面板
        // 3. 界面宽度 >= 1008px，13"及更大设备，最多三列面板
        // Win中的LayoutManager已对后两种宽度自动处理
        //**************************************************************************

        /// <summary>
        /// PhoneUI模式的最大宽度
        /// </summary>
        const double _maxPhoneUIWidth = 640;

        /// <summary>
        /// 对话框面板
        /// </summary>
        static readonly Canvas _dlgCanvas;

        /// <summary>
        /// 提示信息面板
        /// </summary>
        static readonly StackPanel _notifyPanel;
        static readonly ItemList<NotifyInfo> _notifyList;
        static PointerEventHandler _pressedHandler = new PointerEventHandler(OnPanelPointerPressed);

        /// <summary>
        /// 内容元素，桌面、Frame、登录页面等，在最底层
        /// </summary>
        static UIElement _rootContent;
        #endregion

        #region 静态构造
        static SysVisual()
        {
            // 根Grid，背景主蓝
            RootGrid = new Grid { Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0xFF, 0x1B, 0xA1, 0xE2)) };

            // 桌面层/页面层，此层调整为动态添加！为uno节省级数！

            // 对话框层
            _dlgCanvas = new Canvas();
            RootGrid.AddHandler(UIElement.PointerPressedEvent, _pressedHandler, true);
            RootGrid.Children.Add(_dlgCanvas);

            // 提示信息层
            _notifyList = new ItemList<NotifyInfo>();
            _notifyList.ItemsChanged += OnNotifyItemsChanged;
            _notifyPanel = new StackPanel();
            _notifyPanel.Spacing = 10;
            RootGrid.Children.Add(_notifyPanel);

#if IOS
            // 状态栏边距
            StatusBarHeight = (int)UIKit.UIApplication.SharedApplication.StatusBarFrame.Height;
            RootGrid.Padding = new Thickness(0, StatusBarHeight, 0, 0);
#elif ANDROID
            // Android上已设置不占用顶部状态栏和底部导航栏，但 Window.Bounds 包含顶部状态栏高度！
            var res = Android.App.Application.Context.Resources;
            int resourceId = res.GetIdentifier("status_bar_height", "dimen", "android");
            if (resourceId > 0)
                StatusBarHeight = (int)(res.GetDimensionPixelSize(resourceId) / res.DisplayMetrics.Density);
#endif

#if WIN
            // WinUI中Window.Current为null
            MainWin = new Window { Title = Kit.Stub.Title };
#else
            // uno中若新创建，Window.Bounds始终为(0, 0)！
            MainWin = Window.Current;
#endif
            MainWin.Content = RootGrid;
            MainWin.Activate();

#if WIN
            // 支持UI自适应
            MainWin.SizeChanged += OnWindowSizeChanged;
            Kit.IsPhoneUI = MainWin.Bounds.Width < _maxPhoneUIWidth;
#elif WASM
            if (Kit.HostOS == HostOS.Windows
                || Kit.HostOS == HostOS.Mac
                || Kit.HostOS == HostOS.Linux)
            {
                // 支持UI自适应
                MainWin.SizeChanged += OnWindowSizeChanged;
                // wasm上Window有内容且激活后Bounds才有效，其它平台一直有效！
                Kit.IsPhoneUI = MainWin.Bounds.Width < _maxPhoneUIWidth;
            }
            else
            {
                // ios android 不支持UI自适应
                Kit.IsPhoneUI = true;
            }
#else
            // ios android 不支持UI自适应
            Kit.IsPhoneUI = true;
#endif

            ApplyNotifyStyle();
        }

        /// <summary>
        /// 静态构造方法中已创建窗口及整个系统可视树，避免重复创建
        /// </summary>
        internal static void Init()
        { }
        #endregion

        #region 基础
        /// <summary>
        /// 主窗口
        /// </summary>
        public static readonly Window MainWin;

        /// <summary>
        /// Window.Content内容，根Grid
        /// </summary>
        public static readonly Grid RootGrid;

        /// <summary>
        /// 获取设置桌面层/页面层的内容元素，桌面、Frame、登录页面，在最底层
        /// </summary>
        public static UIElement RootContent
        {
            get { return _rootContent; }
            set
            {
                if (value != null && value != _rootContent)
                {
                    if (_rootContent != null)
                        RootGrid.Children.Remove(_rootContent);
                    _rootContent = value;
                    SetDefaultStyle(_rootContent as Control);
                    RootGrid.Children.Insert(0, value);
                }
            }
        }

        /// <summary>
        /// PhoneUI模式的根Frame
        /// </summary>
        public static Frame RootFrame
        {
            get { return (Frame)_rootContent; }
        }

        /// <summary>
        /// phone状态栏高度
        /// </summary>
        public static int StatusBarHeight = 0;
        #endregion

        #region 对话框
        /// <summary>
        /// 是否存在对话框
        /// </summary>
        public static bool ExistDlg
        {
            get { return _dlgCanvas.Children.Count > 0; }
        }

        /// <summary>
        /// 将对话框添加到可视树，Canvas作为对话框背景遮罩
        /// </summary>
        /// <param name="p_cvs">对话框遮罩容器</param>
        /// <returns></returns>
        public static bool AddDlg(Canvas p_cvs)
        {
            if (p_cvs == null
                || _dlgCanvas.Children.Contains(p_cvs)
                || p_cvs.Children.Count != 1)
                return false;

            SetDefaultStyle(p_cvs.Children[0] as Control);
            _dlgCanvas.Children.Add(p_cvs);
            return true;
        }

        /// <summary>
        /// 从可视树移除对话框
        /// </summary>
        /// <param name="p_cvs">对话框遮罩容器</param>
        public static void RemoveDlg(Canvas p_cvs)
        {
            _dlgCanvas.Children.Remove(p_cvs);
        }

        /// <summary>
        /// 是否存在某对话框
        /// </summary>
        /// <param name="p_cvs">对话框遮罩容器</param>
        /// <returns></returns>
        public static bool ContainsDlg(Canvas p_cvs)
        {
            return p_cvs != null && _dlgCanvas.Children.Contains(p_cvs);
        }

        /// <summary>
        /// 获取最上面的对话框
        /// </summary>
        /// <returns></returns>
        public static UIElement GetTopDlg()
        {
            Canvas cvs = null;
            int z = -1;
            foreach (var item in _dlgCanvas.Children.OfType<Canvas>())
            {
                int index = Canvas.GetZIndex(item);
                if (index > z)
                {
                    z = index;
                    cvs = item;
                }
            }

            if (cvs != null && cvs.Children.Count > 0)
                return cvs.Children[0];
            return null;
        }

        /// <summary>
        /// 将调整对话框大小的外框添加到可视树
        /// </summary>
        /// <param name="p_border">外框</param>
        public static void AddDlgResizeFlag(UIElement p_border)
        {
            if (p_border != null)
                _dlgCanvas.Children.Add(p_border);
        }

        /// <summary>
        /// 从可视树移除调整大小的外框
        /// </summary>
        /// <param name="p_border">外框</param>
        public static void RemoveDlgResizeFlag(UIElement p_border)
        {
            if (p_border != null)
                _dlgCanvas.Children.Remove(p_border);
        }

        /// <summary>
        /// 对话框个数
        /// </summary>
        public static int DlgCount
        {
            get { return _dlgCanvas.Children.Count; }
        }

        /// <summary>
        /// 始终处理所有点击事件，以便处理点击对话框外部
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void OnPanelPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (_dlgCanvas.Children.Count == 0)
                return;

            // 将对话框按从上层到下层(ZIndex)的顺序保存到临时列表，因循环过程中会删除_dlgCanvas的元素！
            var ls = _dlgCanvas.Children.OfType<Canvas>().OrderByDescending((cvs) => Canvas.GetZIndex(cvs)).ToList();
            var pt = e.GetCurrentPoint(null).Position;
            foreach (var cvs in ls)
            {
                if (cvs.Children.Count > 0
                    && cvs.Children[0] is IDlgPressed dlg
                    && !dlg.OnPressed(pt))
                {
                    break;
                }
            }
        }
        #endregion

        #region Notify
        /// <summary>
        /// 获取提示信息列表
        /// </summary>
        public static ItemList<NotifyInfo> NotifyList => _notifyList;

        /// <summary>
        /// UI添加一条提示信息
        /// </summary>
        /// <param name="p_index"></param>
        /// <param name="p_item"></param>
        public static void InsertNotifyItem(int p_index, Control p_item)
        {
            SetDefaultStyle(p_item);
            _notifyPanel.Children.Insert(p_index, p_item);
        }

        /// <summary>
        /// UI删除一条提示信息
        /// </summary>
        /// <param name="p_index"></param>
        public static void RemoveNotifyItem(int p_index)
        {
            _notifyPanel.Children.RemoveAt(p_index);
        }

        /// <summary>
        /// UI清空所有提示信息
        /// </summary>
        public static void ClearAllNotify()
        {
            _notifyPanel.Children.Clear();
        }

        /// <summary>
        /// 调整提示信息层样式
        /// </summary>
        static void ApplyNotifyStyle()
        {
            if (Kit.IsPhoneUI)
            {
                _notifyPanel.Width = double.NaN;
                _notifyPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
                _notifyPanel.VerticalAlignment = VerticalAlignment.Top;
            }
            else
            {
                _notifyPanel.Width = 240;
                _notifyPanel.HorizontalAlignment = HorizontalAlignment.Right;
                _notifyPanel.VerticalAlignment = VerticalAlignment.Bottom;
            }
        }

        static void OnNotifyItemsChanged(object sender, ItemListChangedArgs e)
        {
            if (e.CollectionChange == CollectionChange.ItemInserted || e.CollectionChange == CollectionChange.ItemChanged)
            {
                var info = ((ItemList<NotifyInfo>)sender)[e.Index];
                _notifyPanel.Children.Insert(e.Index, new NotifyItem(info));
            }
            else if (e.CollectionChange == CollectionChange.ItemRemoved)
            {
                _notifyPanel.Children.RemoveAt(e.Index);
            }
            else
            {
                _notifyPanel.Children.Clear();
            }
        }
        #endregion

        #region UI自适应
        /// <summary>
        /// 系统区域大小变化时UI自适应
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void OnWindowSizeChanged(object sender, Microsoft.UI.Xaml.WindowSizeChangedEventArgs e)
        {
            bool isPhoneUI = e.Size.Width < _maxPhoneUIWidth;
            if (isPhoneUI == Kit.IsPhoneUI)
                return;

            Kit.IsPhoneUI = isPhoneUI;
            ApplyNotifyStyle();

            // 登录之前无UI自适应！有向导对话框时造成关闭
            var tp = RootContent.GetType().Name;
            if (tp != "Frame" && tp != "Desktop")
                return;

            // 调整对话框层
            _dlgCanvas.Children.Clear();
            Kit.Callback.OnUIModeChanged();
        }
        #endregion

        #region 设置默认样式
        static void SetDefaultStyle(Control p_con)
        {
            if (p_con != null)
            {
                // 统一设置默认字体大小
                // 原系统的默认大小：Control为11，TextBlock为14，Frame为15
                p_con.FontSize = 16;
            }
        }
        #endregion
    }

    /// <summary>
    /// 对话框处理点击接口
    /// </summary>
    public interface IDlgPressed
    {
        /// <summary>
        /// 点击对话框
        /// </summary>
        /// <param name="p_point">点击位置点坐标</param>
        /// <returns>是否继续调用下层对话框的 OnPressed</returns>
        bool OnPressed(Point p_point);
    }
}