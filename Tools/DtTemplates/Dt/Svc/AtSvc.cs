﻿using Dt.Core;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dt
{
    /// <summary>
    /// svc代理 
    /// </summary>
    public class AtSvc
    {
        static string _svcUrl = "https://localhost:1234";
        static List<string> _allTables;

        public static string SvcUrl
        {
            get { return _svcUrl; }
            set
            {
                if (_svcUrl != value)
                {
                    _svcUrl = value;
                    _allTables = null;
                }
            }
        }

        public static async Task<List<string>> GetAllTables()
        {
            if (_allTables == null || _allTables.Count == 0)
            {
                _allTables = await new Rpc().Call<List<string>>(
                    SvcUrl,
                    "SysTools.GetAllTables"
                );

                if (_allTables != null)
                {
                    // 空
                    _allTables.Insert(0, "");
                }
            }
            return _allTables;
        }

        /// <summary>
        /// 生成实体类
        /// </summary>
        /// <param name="p_tblName">表名</param>
        /// <param name="p_clsName">类名，null时按规则生成：移除前后缀，首字母大写</param>
        /// <returns></returns>
        public static Task<string> GetEntityClass(string p_tblName, string p_clsName)
        {
            return new Rpc().Call<string>(
                SvcUrl,
                "SysTools.GetEntityClass",
                p_tblName,
                p_clsName
            );
        }

        public static Task<string> GetFvCells(string p_tblName)
        {
            return new Rpc().Call<string>(
                SvcUrl,
                "SysTools.GetFvCells",
                p_tblName
            );
        }

        public static Task<string> GetLvItemTemplate(string p_tblName)
        {
            return new Rpc().Call<string>(
                SvcUrl,
                "SysTools.GetLvItemTemplate",
                p_tblName
            );
        }

        public static Task<string> GetLvTableCols(string p_tblName)
        {
            return new Rpc().Call<string>(
                SvcUrl,
                "SysTools.GetLvTableCols",
                p_tblName
            );
        }

        public static Task<string> GetSingleTblSql(string p_tblName, string p_title, bool p_blurQuery)
        {
            return new Rpc().Call<string>(
                SvcUrl,
                "SysTools.GetSingleTblSql",
                p_tblName,
                p_title,
                p_blurQuery
            );
        }

        public static Task<string> GetOneToManySql(string p_parentTbl, string p_parentTitle, List<string> p_childTbls, List<string> p_childTitles, bool p_blurQuery)
        {
            return new Rpc().Call<string>(
                SvcUrl,
                "SysTools.GetOneToManySql",
                p_parentTbl,
                p_parentTitle,
                p_childTbls,
                p_childTitles,
                p_blurQuery
            );
        }

        public static Task<bool> ExistParentID(string p_tblName)
        {
            return new Rpc().Call<bool>(
                SvcUrl,
                "SysTools.ExistParentID",
                p_tblName
            );
        }

        public static Task<string> GetManyToManySql(string p_mainTbl, string p_mainTitle, bool p_blurQuery)
        {
            return new Rpc().Call<string>(
                SvcUrl,
                "SysTools.GetManyToManySql",
                p_mainTbl,
                p_mainTitle,
                p_blurQuery
            );
        }
    }
}
