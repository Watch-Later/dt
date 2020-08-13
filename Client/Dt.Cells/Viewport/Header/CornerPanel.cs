#region 文件描述
/******************************************************************************
* 创建: Daoting
* 摘要: 
* 日志: 2014-07-03 创建
******************************************************************************/
#endregion

#region 引用命名
using Dt.Cells.Data;
using System.ComponentModel;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
#endregion

namespace Dt.Cells.UI
{
    internal partial class CornerPanel : Panel
    {
        readonly SheetView _view;
        Border _border;
        Path _path;

        public CornerPanel(SheetView p_view)
        {
            _view = p_view;
            _border = new Border
            {
                BorderBrush = BrushRes.浅灰边框,
                BorderThickness = new Thickness(0, 0, 1, 1),
                Background = BrushRes.WhiteBrush,
            };
            Children.Add(_border);

            _path = new Path
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 4, 4, 4),
                Stretch = Stretch.Uniform,
                Fill = BrushRes.浅灰边框,
                IsHitTestVisible = false,
            };
            PathGeometry geometry = new PathGeometry();
            PathFigure pf = new PathFigure();
            pf.Segments.Add(new LineSegment { Point = new Point(9, 0) });
            pf.Segments.Add(new LineSegment { Point = new Point(9, 9) });
            pf.Segments.Add(new LineSegment { Point = new Point(0, 9) });
            pf.Segments.Add(new LineSegment { Point = new Point(9, 0) });
            geometry.Figures.Add(pf);
            _path.Data = geometry;
            Children.Add(_path);
        }

        internal void ApplyState()
        {
            bool selectAll = false;
            Worksheet worksheet = _view.ActiveSheet;
            if (!_view.HideSelectionWhenPrinting
                && !_view.HasSelectedFloatingObject()
                && worksheet.Selections.Count == 1)
            {
                CellRange range = worksheet.Selections[0];
                selectAll = range.Column == -1 && range.Row == -1 && range.RowCount == -1 && range.ColumnCount == -1;
            }

            if (selectAll)
            {
                _path.Fill = BrushRes.醒目蓝色;
            }
            else if (_view.HoverManager.IsMouseOverCornerHeaders)
            {
                _path.Fill = BrushRes.主题蓝色;
            }
            else
            {
                _path.Fill = BrushRes.浅灰边框;
            }
        }

        #region 测量布局
        protected override Size MeasureOverride(Size availableSize)
        {
            _border.Measure(availableSize);
            _path.Measure(availableSize);
            return availableSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            Rect rc = new Rect(new Point(), finalSize);
            _border.Arrange(rc);
            _path.Arrange(rc);
            return finalSize;
        }
        #endregion
    }
}

