﻿#region 文件描述
/******************************************************************************
* 创建: Daoting
* 摘要: 
* 日志: 2013-12-16 创建
******************************************************************************/
#endregion

#region 引用命名
using Dt.Base;
using Dt.Base.Report;
using Dt.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#endregion

namespace Dt.Sample
{
    public class DataRptScript : RptScript
    {
        public override Task<Table> GetData(string p_name)
        {
            return Task.Run(() =>
            {
                using (var stream = typeof(RptDesignDemo).Assembly.GetManifestResourceStream($"Dt.Sample.Report.数据源.{p_name}.json"))
                {
                    return Table.Create(stream);
                }
            });
        }
    }

    public class MyRptScript : DataRptScript
    {
        public override Task<Table> GetData(string p_name)
        {
            return Task.Run(() =>
            {
                using (var stream = typeof(RptDesignDemo).Assembly.GetManifestResourceStream($"Dt.Sample.Report.数据源.{p_name}.json"))
                {
                    var tbl = Table.Create(stream);
                    var tgt = Table.Create(tbl);
                    var ls = from row in tbl
                             where row.Str("parentid") == View.Info.Params.Str("parentid")
                             select row;
                    foreach (var row in ls)
                    {
                        tgt.Add(row);
                    }
                    return tgt;
                }
            });
        }

        public override void InitMenu(Menu p_menu)
        {
            Mi mi = new Mi { ID = "后退", Icon = Icons.左 };
            mi.Click += OnBack;
            p_menu.Items.Insert(0, mi);
            p_menu.Items.Add(new Mi { ID = "显示网格", IsCheckable = true, Cmd = View.CmdGridLine });
        }

        void OnBack(object sender, Mi e)
        {
            var ls = View.Tag as Stack<RptInfo>;
            if (ls != null && ls.Count > 0)
                View.LoadReport(ls.Pop());
        }

        public override void OnCellClick(string p_id, IRptCell p_text)
        {
            var row = p_text.Data;
            if (p_id == "flag1")
            {
                if (row.Bool("isgroup"))
                {
                    var info = new MyRptInfo { Name = "脚本", Params = new Dict { { "parentid", row.Str("id") }, { "parentname", row.Str("name") } } };
                    var ls = View.Tag as Stack<RptInfo>;
                    if (ls == null)
                    {
                        ls = new Stack<RptInfo>();
                        View.Tag = ls;
                    }
                    ls.Push(View.Info);
                    View.LoadReport(info);
                }
                else
                {
                    Dlg dlg = new Dlg();
                    var pnl = new StackPanel
                    {
                        Children =
                    {
                        new TextBlock { Text = "id：" + row.Str("id")},
                        new TextBlock { Text = "parentid：" + row.Str("parentid")},
                        new TextBlock { Text = "name：" + row.Str("name")},
                        new TextBlock { Text = "isgroup：" + row.Str("isgroup")},
                    },
                        Margin = new Thickness(20),
                    };
                    dlg.Content = pnl;
                    dlg.Show();
                }
            }
            else if (p_id == "flag2")
            {
                AtKit.Msg(row.Bool("isgroup") ? "分组菜单" : "实体菜单");
            }
        }
    }
}