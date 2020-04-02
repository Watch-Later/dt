#region 文件描述
/******************************************************************************
* 创建: Daoting
* 摘要: 
* 日志: 2020-03-17 创建
******************************************************************************/
#endregion

#region 引用命名
using Dt.Core;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
#endregion

namespace Dt.Base.FileLists
{
    /// <summary>
    /// 布局面板
    /// </summary>
    public partial class FileListPanel : Panel
    {
        #region 成员变量
        const double PanelMaxHeight = 500;
        readonly FileList _owner;
        readonly List<double> _linesHeight = new List<double>();
        #endregion

        #region 构造方法
        public FileListPanel(FileList p_owner)
        {
            _owner = p_owner;
        }
        #endregion

        protected override Size MeasureOverride(Size availableSize)
        {
            if (Children.Count == 0
                || availableSize.Width == 0
                || availableSize.Height == 0)
                return base.MeasureOverride(availableSize);

            // 单列不自动填充
            if (_owner.ColCount == 1 && _owner.HorizontalAlignment != HorizontalAlignment.Stretch)
                return MeasureOneCol(availableSize);

            double maxWidth = double.IsInfinity(availableSize.Width) ? SysVisual.ViewWidth : availableSize.Width;
            double colWidth = maxWidth / _owner.ColCount;

            double totalHeight = 0;
            double lineHeight = 0;
            _linesHeight.Clear();
            Size itemSize = new Size(colWidth, PanelMaxHeight);
            Size imgSize = new Size(colWidth, _owner.ImageHeight > 0 ? _owner.ImageHeight : colWidth);
            for (int i = 0; i < Children.Count; i++)
            {
                var item = Children[i] as FileItem;
                double height;
                if (item.FileType == FileItemType.Image)
                {
                    item.Measure(imgSize);
                    if (_owner.ImageHeight > 0)
                        height = _owner.ImageHeight;
                    else
                        height = item.DesiredSize.Height;
                }
                else
                {
                    item.Measure(itemSize);
                    height = item.DesiredSize.Height;
                }

                if (height > lineHeight)
                    lineHeight = height;

                // 行尾或最后一项
                if ((i + 1) % _owner.ColCount == 0 || i == Children.Count - 1)
                {
                    totalHeight += lineHeight;
                    _linesHeight.Add(lineHeight);
                    lineHeight = 0;
                }
            }
            return new Size(maxWidth, totalHeight);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (Children.Count == 0)
                return base.ArrangeOverride(finalSize);

            if (_owner.ColCount == 1 && _owner.HorizontalAlignment != HorizontalAlignment.Stretch)
                return ArrangeOneCol(finalSize);

            double totalHeight = 0;
            double colWidth = finalSize.Width / _owner.ColCount;
            for (int i = 0; i < Children.Count; i++)
            {
                var item = Children[i] as FileItem;
                int col = i % _owner.ColCount;
                int row = i / _owner.ColCount;
                item.Arrange(new Rect(col * colWidth, totalHeight, colWidth, _linesHeight[row]));

                if ((i + 1) % _owner.ColCount == 0)
                {
                    // 行尾
                    totalHeight += _linesHeight[row];
                }
            }
            return finalSize;
        }

        /// <summary>
        /// 单列不自动填充
        /// </summary>
        /// <param name="availableSize"></param>
        /// <returns></returns>
        Size MeasureOneCol(Size availableSize)
        {
            double maxWidth = double.IsInfinity(availableSize.Width) ? SysVisual.ViewWidth : availableSize.Width;
            Size itemSize = new Size(maxWidth, PanelMaxHeight);
            Size imgSize = new Size(maxWidth, _owner.ImageHeight > 0 ? _owner.ImageHeight : maxWidth);
            double width = 0;
            double height = 0;
            for (int i = 0; i < Children.Count; i++)
            {
                var item = Children[i] as FileItem;
                item.Measure(item.FileType == FileItemType.Image ? imgSize : itemSize);
                if (item.DesiredSize.Width > width)
                    width = item.DesiredSize.Width;
                height += item.DesiredSize.Height;
            }
            return new Size(width, height);
        }

        Size ArrangeOneCol(Size finalSize)
        {
            double height = 0;
            for (int i = 0; i < Children.Count; i++)
            {
                var item = Children[i] as FileItem;
                item.Arrange(new Rect(0, height, finalSize.Width, item.DesiredSize.Height));
                height += item.DesiredSize.Height;
            }
            return finalSize;
        }
    }
}