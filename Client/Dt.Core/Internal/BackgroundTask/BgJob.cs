﻿#region 文件描述
/******************************************************************************
* 创建: Daoting
* 摘要: 
* 日志: 2017-12-06 创建
******************************************************************************/
#endregion

#region 引用命名
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
#endregion

namespace Dt.Core
{
    /// <summary>
    /// 后台作业，公共部分
    /// </summary>
    public static partial class BgJob
    {
        const string _stubType = "StubType";

        /// <summary>
        /// 后台任务运行入口
        /// 此方法不可使用任何UI和外部变量，保证可独立运行！！！
        /// </summary>
        /// <returns></returns>
        public static async Task Run()
        {
            // 打开状态库
            bool notRun = AtState.OpenDbBackground();

            // 因后台任务独立运行，存根类型需要从State库获取！
            Stub stub = null;
            string tpName = AtState.GetCookie(_stubType);
            if (!string.IsNullOrEmpty(tpName))
            {
                Type tp = Type.GetType(tpName);
                if (tp != null)
                    stub = Activator.CreateInstance(tp) as Stub;
            }

            if (stub == null)
                return;

            if (notRun)
            {
                // 前端没运行，完全后台启动
                // 避免涉及UI
                stub.LogSetting.TraceEnabled = false;
                Serilogger.Init();
            }

            var bgJob = stub.ServiceProvider.GetService<IBackgroundJob>();
            if (bgJob != null)
            {
                Log.Debug("开始后台任务");
                try
                {
                    await bgJob.Run();
                    Log.Debug("后台任务结束");
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "后台任务运行异常");
                }
            }
            else
            {
                Unregister();
                Log.Warning("后台任务无处理内容，已注销！");
            }
        }
    }
}