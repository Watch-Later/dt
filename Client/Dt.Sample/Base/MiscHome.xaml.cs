﻿#region 文件描述
/******************************************************************************
* 创建: Daoting
* 摘要: 
* 日志: 2018-08-23 创建
******************************************************************************/
#endregion

#region 引用命名
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dt.Base;
using Dt.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#endregion

namespace Dt.Sample
{
    public sealed partial class MiscHome : Win
    {
        public MiscHome()
        {
            InitializeComponent();
            _lv.Data = new Nl<CenterInfo>
            {
                new CenterInfo(Icons.汉堡, "基础事件", typeof(RouteEventDemo), null),
                new CenterInfo(Icons.分组, "分隔栏", typeof(SplitterDemo), null),
                new CenterInfo(Icons.详细, "可停靠面板", typeof(DockPanelDemo), "停靠式窗口的布局面板"),
                new CenterInfo(Icons.日历, "流程图", typeof(SketchPage), "任务流程定义示意图"),
                new CenterInfo(Icons.乐谱, "控件事件顺序", typeof(TestInvokeDemo), "测试不同平台主事件的调用顺序"),
            };
        }
    }
}
