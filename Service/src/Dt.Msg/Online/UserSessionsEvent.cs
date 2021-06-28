﻿#region 文件描述
/******************************************************************************
* 创建: Daoting
* 摘要: 
* 日志: 2019-09-04 创建
******************************************************************************/
#endregion

#region 引用命名
using Dt.Core;
using Dt.Core.Caches;
using Dt.Core.EventBus;
using System.Collections.Generic;
using System.Threading.Tasks;
#endregion

namespace Dt.Msg
{
    /// <summary>
    /// 获取用户会话信息的事件
    /// </summary>
    public class UserSessionsEvent : IEvent
    {
        public string CacheKey { get; set; }

        public long UserID { get; set; }
    }

    public class UserSessionsHandler : IRemoteHandler<UserSessionsEvent>
    {
        public Task Handle(UserSessionsEvent p_event)
        {
            var ls = Online.GetSessions(p_event.UserID);
            if (ls != null && ls.Count > 0)
            {
                var result = new List<Dict>();
                foreach (var ci in ls)
                {
                    result.Add(new Dict
                    {
                        { "userid", ci.UserID },
                        { "svcid", Glb.ID },
                        { "starttime", ci.StartTime.ToString() },
                        { "platform", ci.Platform },
                        { "version", ci.Version },
                        { "devicename", ci.DeviceName },
                        { "devicemodel", ci.DeviceModel },
                    });
                }
                var msg = Kit.Serialize(result);
                return Redis.Db.HashSetAsync(p_event.CacheKey, p_event.UserID, msg);
            }
            return Task.CompletedTask;
        }
    }
}
