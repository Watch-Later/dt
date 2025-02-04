﻿#region 文件描述
/******************************************************************************
* 创建: Daoting
* 摘要: 
* 日志: 2019-08-29 创建
******************************************************************************/
#endregion

#region 引用命名
using Dt.Agent;
using Dt.Core;
using Dt.Core.Rpc;
using System;
using System.IO;
using System.Threading.Tasks;
#endregion

namespace Dt.Cm
{
    /// <summary>
    /// 发布服务Api
    /// </summary>
    [Api]
    public class Publish : BaseApi
    {
        const string _template =
            "<!DOCTYPE html>\n" +
            "<html>\n" +
            "<head>\n" +
            "    <meta charset=\"utf-8\">\n" +
            "    <title>{0}</title>\n" +
            "    <link rel=\"stylesheet\" href=\"../froala.css\">\n" +
            "</head>\n" +
            "<body>\n" +
            "    <div class=\"fr-element fr-view\">\n" +
            "        {1}\n" +
            "    </div>\n" +
            "</body>\n" +
            "</html>";

        /// <summary>
        /// 保存文章，返回文章地址
        /// </summary>
        /// <param name="p_post"></param>
        /// <returns></returns>
        public async Task<string> SavePost(PubPostObj p_post)
        {
            Throw.IfNull(p_post, "待保存的文章对象为null");
            Throw.If(!p_post.IsAdded && !p_post.IsChanged, "文章对象不需要保存");

            if (p_post.IsPublish
                && (string.IsNullOrEmpty(p_post.Title) || string.IsNullOrEmpty(p_post.Content)))
            {
                Throw.Msg("发布的文章标题和内容不能为空");
            }

            string oldUrl = null;
            if (p_post.Cells["title"].IsChanged || p_post.Cells["content"].IsChanged)
            {
                oldUrl = p_post.Url;
                
                if (!string.IsNullOrEmpty(p_post.Title) && !string.IsNullOrEmpty(p_post.Content))
                {
                    string url = null;
                    if (!string.IsNullOrEmpty(p_post.Title) && !string.IsNullOrEmpty(p_post.Content))
                    {
                        url = await BuildHtmlPage(p_post.Title, p_post.Content, Kit.Now.ToString("yyyyMM"));
                    }
                    p_post.Url = url;
                }
            }

            bool suc = await Dp.Save(p_post);
            if (suc && oldUrl != null)
            {
                // 删除旧文件
                await AtFsm.DeleteFile($"g/{oldUrl}");
            }

            Throw.If(!suc, "文章保存失败");
            return p_post.Url;
        }

        /// <summary>
        /// 创建静态页面
        /// </summary>
        /// <param name="p_title">页面标题</param>
        /// <param name="p_content">页面内容</param>
        /// <returns>返回静态页面的路径，生成失败返回null</returns>
        public Task<string> CreatePage(string p_title, string p_content)
        {
            return BuildHtmlPage(p_title, p_content, Kit.Now.ToString("yyyyMM"));
        }

        /// <summary>
        /// 创建测试页面
        /// </summary>
        /// <param name="p_title">页面标题</param>
        /// <param name="p_content">页面内容</param>
        /// <returns>返回页面路径，生成失败返回null</returns>
        public Task<string> CreateTestPage(string p_title, string p_content)
        {
            return BuildHtmlPage(p_title, p_content, "test");
        }

        async Task<string> BuildHtmlPage(string p_title, string p_content, string p_folder)
        {
            if (string.IsNullOrEmpty(p_title)
                || string.IsNullOrEmpty(p_content)
                || string.IsNullOrEmpty(p_folder))
                return null;

            string pageContent = string.Format(_template, p_title, p_content);
            string pageName = $"{Kit.NewID}.html";
            var result = await AtFsm.SaveFile($"g/{p_folder}/{pageName}", pageContent);
            if (string.IsNullOrEmpty(result))
                return $"{p_folder}/{pageName}";
            return null;
        }
    }
}
