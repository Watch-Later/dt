#region 文件描述
/******************************************************************************
* 创建: Daoting
* 摘要: 
* 日志: 2014-07-03 创建
******************************************************************************/
#endregion

#region 引用命名
using Dt.Cells.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
#endregion

namespace Dt.Cells.UI
{
    /// <summary>
    /// 单元格面板
    /// </summary>
    internal partial class CellItem : Panel
    {
        static Rect _rcEmpty = new Rect();
        TextBlock _tb;
        FrameworkElement _editor;
        CellOverflowLayout _overflowLayout;
        Rect? _cachedClip;

        ConditionalFormatView _conditionalView;
        CustomDrawingObject _customDrawingObject;
        FrameworkElement _customDrawingObjectView;
        DataBarDrawingObject _dataBarObject;
        IconDrawingObject _iconObject;
        Sparkline _sparkInfo;
        BaseSparklineView _sparklineView;
        StrikethroughView _strikethroughView;
        FilterButton _filterButton;
        FilterButtonInfo _filterButtonInfo;
        Type _cachedValueType;
        InvalidDataPresenterInfo _dataValidationInvalidPresenterInfo;
        bool _lastUnderline;

        public CellItem(RowItem p_rowItem)
        {
            OwnRow = p_rowItem;
        }

        public RowItem OwnRow { get; }

        public int Row
        {
            get { return OwnRow.Row; }
        }

        public int Column { get; set; }

        public CellLayout CellLayout { get; set; }

        public Cell BindingCell { get; private set; }

        public SheetView SheetView
        {
            get { return OwnRow.OwnPanel.Sheet; }
        }

        public double ZoomFactor
        {
            get { return (double)OwnRow.OwnPanel.Sheet.ZoomFactor; }
        }

        public CellOverflowLayout CellOverflowLayout
        {
            get { return _overflowLayout; }
            set
            {
                if (!object.Equals(_overflowLayout, value))
                {
                    _overflowLayout = value;
                    InvalidateMeasure();
                }
            }
        }

        public FilterButtonInfo FilterButtonInfo
        {
            get { return _filterButtonInfo; }
            set
            {
                if (_filterButtonInfo != value)
                {
                    _filterButtonInfo = value;
                    InvalidateMeasure();
                }
            }
        }

        public void ApplyState()
        {
            Background = BindingCell.ActualBackground;
            if (_filterButton != null)
                _filterButton.ApplyState();
        }

        public void CleanUpBeforeDiscard()
        {
            if (_customDrawingObjectView != null)
                Children.Remove(_customDrawingObjectView);

            if (_dataValidationInvalidPresenterInfo != null)
            {
                OwnRow.OwnPanel.RemoveDataValidationInvalidDataPresenterInfo(_dataValidationInvalidPresenterInfo);
                _dataValidationInvalidPresenterInfo = null;
            }

            DettachSparklineEvents();
        }

        public bool HasEditingElement()
        {
            return (_editor != null);
        }

        public void SetEditingElement(FrameworkElement p_editor)
        {
            if (_editor != p_editor)
                _editor = p_editor;
        }

        public FrameworkElement GetEditingElement()
        {
            if (_editor == null)
                _editor = new EditingElement();
            TextBox textBox = _editor as TextBox;

            StyleInfo info = BindingCell.Worksheet.GetActualStyleInfo(BindingCell.Row.Index, BindingCell.Column.Index, BindingCell.SheetArea, true);
            if (info == null || textBox == null)
                return _editor;

            // 存在公式
            string text = string.Empty;
            string formula = string.Empty;
            using (((IUIActionExecuter)BindingCell.Worksheet).BeginUIAction())
            {
                int index = BindingCell.Row.Index;
                int column = BindingCell.Column.Index;
                formula = BindingCell.Formula;
                if (formula == null)
                {
                    object[,] objArray = BindingCell.Worksheet.FindFormulas(index, column, 1, 1);
                    if (objArray.GetLength(0) > 0)
                    {
                        string str3 = objArray[0, 1].ToString();
                        int length = str3.Length;
                        if (((length > 2) && str3.StartsWith("{")) && str3.EndsWith("}"))
                        {
                            formula = str3.Substring(1, length - 2);
                        }
                    }
                }
            }
            if (!string.IsNullOrEmpty(formula))
            {
                text = "=" + formula;
                goto Label_0293;
            }
            if (BindingCell.Value == null)
            {
                _cachedValueType = null;
                goto Label_0293;
            }

            // 存在格式化
            _cachedValueType = BindingCell.Value.GetType();
            var preferredEditingFormatter = new GeneralFormatter().GetPreferredEditingFormatter(BindingCell.Value);
            if ((preferredEditingFormatter != null) && (info.Formatter is AutoFormatter))
            {
                try
                {
                    text = preferredEditingFormatter.Format(BindingCell.Value);
                    goto Label_01F7;
                }
                catch
                {
                    text = BindingCell.Text;
                    goto Label_01F7;
                }
            }

            text = BindingCell.Text;
        Label_01F7:
            var formatter2 = info.Formatter;
            if (formatter2 is GeneralFormatter formatter3)
            {
                switch (formatter3.GetFormatType(BindingCell.Value))
                {
                    case NumberFormatType.Number:
                    case NumberFormatType.Text:
                        formatter2 = new GeneralFormatter();
                        break;
                }
            }
            if ((formatter2 != null) && !(formatter2 is AutoFormatter))
            {
                text = formatter2.Format(BindingCell.Value);
            }
            if (text != null && text.StartsWith("=") && SheetView.CanUserEditFormula)
            {
                text = "'" + text;
            }

        Label_0293:
            textBox.Text = text;
            if (info.FontSize > 0.0)
            {
                textBox.FontSize = info.FontSize * ZoomFactor;
            }
            else
            {
                textBox.ClearValue(TextBlock.FontSizeProperty);
            }
            textBox.FontStyle = info.FontStyle;
            textBox.FontWeight = info.FontWeight;
            textBox.FontStretch = info.FontStretch;
            if (info.IsFontFamilySet() && (info.FontFamily != null))
            {
                textBox.FontFamily = info.FontFamily;
            }
            else if (info.IsFontThemeSet())
            {
                string fontTheme = info.FontTheme;
                IThemeSupport worksheet = BindingCell.Worksheet;
                if (worksheet != null)
                {
                    textBox.FontFamily = worksheet.GetThemeFont(fontTheme);
                }
            }
            else
            {
                textBox.ClearValue(Control.FontFamilyProperty);
            }

            Brush foreground = null;
            if (info.IsForegroundSet())
            {
                foreground = info.Foreground;
            }
            else if (info.IsForegroundThemeColorSet())
            {
                string fname = info.ForegroundThemeColor;
                if ((!string.IsNullOrEmpty(fname) && (BindingCell.Worksheet != null)) && (BindingCell.Worksheet.Workbook != null))
                {
                    foreground = new SolidColorBrush(BindingCell.Worksheet.Workbook.GetThemeColor(fname));
                }
            }
            if (foreground != null)
            {
                textBox.Foreground = foreground;
            }
            else
            {
                textBox.Foreground = new SolidColorBrush(Colors.Black);
            }

            textBox.VerticalContentAlignment = info.VerticalAlignment.ToVerticalAlignment();
            if (!string.IsNullOrEmpty(formula))
            {
                textBox.TextAlignment = Windows.UI.Xaml.TextAlignment.Left;
            }
            else if (!BindingCell.ActualWordWrap)
            {
                switch (BindingCell.ToHorizontalAlignment())
                {
                    case HorizontalAlignment.Left:
                    case HorizontalAlignment.Stretch:
                        textBox.TextAlignment = Windows.UI.Xaml.TextAlignment.Left;
                        break;

                    case HorizontalAlignment.Center:
                        textBox.TextAlignment = Windows.UI.Xaml.TextAlignment.Center;
                        break;

                    case HorizontalAlignment.Right:
                        textBox.TextAlignment = Windows.UI.Xaml.TextAlignment.Right;
                        break;
                }
            }
            else
            {
                textBox.TextAlignment = Windows.UI.Xaml.TextAlignment.Left;
            }

            textBox.Margin = GetDefaultPaddingForEdit(textBox.FontSize);
            textBox.TextWrapping = TextWrapping.Wrap;
            return _editor;
        }

        public void HideForEditing()
        {
            Visibility = Visibility.Collapsed;
        }

        public void ShowAfterEdit()
        {
            ClearValue(VisibilityProperty);
        }

        public void Refresh()
        {
            if (_sparklineView != null)
                UpdateSparkline();
            UpdateChildren();
            InvalidateMeasure();
        }

        #region 测量布局
        //*** CellsPanel.Measure -> RowsLayer.Measure -> RowItem.UpdateChildren -> 行列改变时 CellItem.UpdateChildren -> RowItem.Measure -> CellItem.Measure ***//

        public void UpdateChildren()
        {
            // 刷新绑定的Cell
            int row = OwnRow.Row;
            int column = Column;
            if (CellLayout != null)
            {
                row = CellLayout.Row;
                column = CellLayout.Column;
            }
            BindingCell = OwnRow.OwnPanel.CellCache.GetCachedCell(row, column);

            SheetView sheetView = SheetView;
            if (sheetView == null || BindingCell == null)
                return;

            Worksheet sheet = BindingCell.Worksheet;

            // 迷你图
            Sparkline sparkline = sheet.GetSparkline(row, column);
            if (_sparkInfo != sparkline)
            {
                SparkLine = sparkline;
                SynSparklineView();
            }

            // 收集所有DrawingObject
            List<DrawingObject> list = new List<DrawingObject>();
            DrawingObject[] objArray = sheet.GetDrawingObject(row, column, 1, 1);
            if ((objArray != null) && (objArray.Length > 0))
            {
                list.AddRange(objArray);
            }

            IDrawingObjectProvider drawingObjectProvider = DrawingObjectManager.GetDrawingObjectProvider(sheetView.Excel);
            if (drawingObjectProvider != null)
            {
                DrawingObject[] objArray2 = drawingObjectProvider.GetDrawingObjects(sheet, row, column, 1, 1);
                if ((objArray2 != null) && (objArray2.Length > 0))
                {
                    list.AddRange(objArray2);
                }
            }

            _dataBarObject = null;
            _iconObject = null;
            _customDrawingObject = null;
            if (list.Count > 0)
            {
                foreach (DrawingObject obj in list)
                {
                    if (obj is DataBarDrawingObject bar)
                    {
                        _dataBarObject = bar;
                    }
                    else if (obj is IconDrawingObject icon)
                    {
                        _iconObject = icon;
                    }
                    else if (obj is CustomDrawingObject cust)
                    {
                        _customDrawingObject = cust;
                    }
                }
            }

            bool noBarIcon = SynContitionalView();
            bool noCust = SynCustomDrawingObjectView();

            if (sparkline == null && noBarIcon && noCust && !string.IsNullOrEmpty(BindingCell.Text))
            {
                if (_tb == null)
                {
                    _tb = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
                    Children.Add(_tb);
                }
                _tb.Text = BindingCell.Text;
                ApplyStyle();
            }
            else if (_tb != null)
            {
                Children.Remove(_tb);
                _tb = null;
            }
            SynStrikethroughView();

            FilterButtonInfo info = sheetView.GetFilterButtonInfo(row, column, BindingCell.SheetArea);
            if (info != FilterButtonInfo)
            {
                FilterButtonInfo = info;
                SynFilterButton();
            }

            if (OwnRow.OwnPanel.Sheet.HighlightInvalidData)
            {
                if (_dataValidationInvalidPresenterInfo == null)
                {
                    DataValidator actualDataValidator = BindingCell.ActualDataValidator;
                    if ((actualDataValidator != null) && !actualDataValidator.IsValid(sheetView.ActiveSheet, Row, Column, BindingCell.Value))
                    {
                        InvalidDataPresenterInfo info2 = new InvalidDataPresenterInfo
                        {
                            Row = Row,
                            Column = Column
                        };
                        _dataValidationInvalidPresenterInfo = info2;
                        OwnRow.OwnPanel.AddDataValidationInvalidDataPresenterInfo(_dataValidationInvalidPresenterInfo);
                    }
                }
                else if (_dataValidationInvalidPresenterInfo != null)
                {
                    DataValidator validator2 = BindingCell.ActualDataValidator;
                    if ((validator2 == null) || validator2.IsValid(sheetView.ActiveSheet, Row, Column, BindingCell.Value))
                    {
                        OwnRow.OwnPanel.RemoveDataValidationInvalidDataPresenterInfo(_dataValidationInvalidPresenterInfo);
                        _dataValidationInvalidPresenterInfo = null;
                    }
                }
            }
            else if (_dataValidationInvalidPresenterInfo != null)
            {
                OwnRow.OwnPanel.RemoveDataValidationInvalidDataPresenterInfo(_dataValidationInvalidPresenterInfo);
                _dataValidationInvalidPresenterInfo = null;
            }

            ApplyState();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (Column == -1
                || availableSize.Width == 0.0
                || availableSize.Height == 0.0)
                return new Size();

            Size sizeOverflow = availableSize;
            if (_overflowLayout != null && _overflowLayout.ContentWidth > availableSize.Width)
                sizeOverflow = new Size(_overflowLayout.ContentWidth, availableSize.Height);

            foreach (UIElement element in Children)
            {
                if (element is TextBlock)
                {
                    element.Measure(sizeOverflow);
                }
                else
                {
                    element.Measure(availableSize);
                }
            }
            return availableSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (Column == -1 || finalSize.Width == 0 || finalSize.Height == 0)
            {
                if (Children.Count > 0)
                {
                    foreach (UIElement elem in Children)
                    {
                        elem.Arrange(_rcEmpty);
                    }
                }
                return finalSize;
            }

            double width = finalSize.Width;
            double height = finalSize.Height;
            Rect? nullable = null;
            double left = 0;
            double top = 0;
            Rect rect = new Rect(left, top, width, height);
            Rect rectOverflow = rect;
            if (_overflowLayout != null && _overflowLayout.ContentWidth > width)
            {
                switch (BindingCell.ToHorizontalAlignment())
                {
                    case HorizontalAlignment.Left:
                        if (CellOverflowLayout != null)
                        {
                            double w = CellOverflowLayout.RightBackgroundWidth;
                            if (w >= 0.0)
                                nullable = new Rect(0.0, 0.0, w, finalSize.Height);
                        }
                        break;

                    case HorizontalAlignment.Right:
                        left -= _overflowLayout.ContentWidth - width;
                        if (CellOverflowLayout != null)
                        {
                            double x = finalSize.Width - CellOverflowLayout.LeftBackgroundWidth;
                            double w = CellOverflowLayout.LeftBackgroundWidth;
                            if (w >= 0.0)
                                nullable = new Rect(x, 0.0, w, finalSize.Height);
                        }
                        break;

                    default:
                        left -= (_overflowLayout.ContentWidth - width) / 2.0;
                        if (CellOverflowLayout != null)
                        {
                            double x = 0.0;
                            if (CellOverflowLayout.LeftBackgroundWidth > 0.0)
                                x = (finalSize.Width / 2.0) - CellOverflowLayout.LeftBackgroundWidth;

                            double w = CellOverflowLayout.BackgroundWidth;
                            if (w >= 0.0)
                                nullable = new Rect(x, 0.0, w, finalSize.Height);
                        }
                        break;
                }
                width = _overflowLayout.ContentWidth;
                rectOverflow = new Rect(left, top, width, height);
            }

            if ((_cachedClip.HasValue != nullable.HasValue) || (_cachedClip.HasValue && (_cachedClip.Value != nullable.Value)))
            {
                _cachedClip = nullable;
                if (nullable.HasValue)
                {
                    RectangleGeometry geometry = new RectangleGeometry();
                    geometry.Rect = nullable.Value;
                    Clip = geometry;
                }
                else
                {
                    ClearValue(ClipProperty);
                }
            }

            foreach (UIElement element in Children)
            {
                if (element != null)
                {
                    if (element is TextBlock)
                    {
                        element.Arrange(rectOverflow);
                    }
                    else
                    {
                        element.Arrange(rect);
                    }
                }
            }
            return finalSize;
        }
        #endregion

        #region 更新可视树
        bool SynContitionalView()
        {
            bool isContentVisible = true;
            if ((_dataBarObject != null) || (_iconObject != null))
            {
                if (_conditionalView == null)
                {
                    _conditionalView = new ConditionalFormatView(BindingCell);
                    Children.Add(_conditionalView);
                    Canvas.SetZIndex(_conditionalView, 500);
                }
                _conditionalView.SetDataBarObject(_dataBarObject);
                if (_iconObject != null)
                {
                    _conditionalView.SetImageContainer();
                    _conditionalView.SetIconObject(_iconObject, SheetView.ZoomFactor, BindingCell);
                }
                bool flag = true;
                if (flag && (_dataBarObject != null))
                {
                    flag = !_dataBarObject.ShowBarOnly;
                }
                if (flag && (_iconObject != null))
                {
                    flag = !_iconObject.ShowIconOnly;
                }
                isContentVisible = flag;
            }
            else if (_conditionalView != null)
            {
                Children.Remove(_conditionalView);
                _conditionalView = null;
            }
            return isContentVisible;
        }

        void SynStrikethroughView()
        {
            bool actualStrikethrough = BindingCell.ActualStrikethrough;
            if (_strikethroughView != null)
            {
                Children.Remove(_strikethroughView);
                _strikethroughView = null;
            }
            if (actualStrikethrough && (_strikethroughView == null))
            {
                _strikethroughView = new StrikethroughView(BindingCell, this);
                _strikethroughView.SetLines(SheetView.ZoomFactor, BindingCell);
                Children.Add(_strikethroughView);
            }
        }

        bool SynCustomDrawingObjectView()
        {
            bool isContentVisible = true;
            if (_customDrawingObject != null)
            {
                isContentVisible = !_customDrawingObject.ShowDrawingObjectOnly;
                FrameworkElement rootElement = _customDrawingObject.RootElement;
                if (_customDrawingObjectView != rootElement)
                {
                    if (_customDrawingObjectView != null)
                    {
                        Children.Remove(_customDrawingObjectView);
                    }
                    _customDrawingObjectView = rootElement;
                    if (_customDrawingObjectView != null)
                    {
                        Panel parent = _customDrawingObjectView.Parent as Panel;
                        if ((parent != null) && (parent != this))
                        {
                            parent.Children.Remove(_customDrawingObjectView);
                        }
                        if (!Children.Contains(_customDrawingObjectView))
                        {
                            Children.Add(_customDrawingObjectView);
                        }
                    }
                }
            }
            else if (_customDrawingObjectView != null)
            {
                Children.Remove(_customDrawingObjectView);
                _customDrawingObjectView = null;
            }
            return isContentVisible;
        }

        void SynFilterButton()
        {
            if (_filterButtonInfo != null)
            {
                if (_filterButton == null)
                {
                    FilterButton element = new FilterButton(this);
                    element.HorizontalAlignment = HorizontalAlignment.Right;
                    element.VerticalAlignment = VerticalAlignment.Bottom;
                    element.Area = SheetArea.Cells;
                    _filterButton = element;
                    Canvas.SetZIndex(element, 0xbb8);
                    Children.Add(element);
                }
                else
                {
                    _filterButton.ApplyState();
                }
            }
            else if (_filterButton != null)
            {
                Children.Remove(_filterButton);
                _filterButton = null;
            }
        }
        #endregion

        #region 迷你图
        Sparkline SparkLine
        {
            get { return _sparkInfo; }
            set
            {
                if (_sparkInfo != value)
                {
                    DettachSparklineEvents();
                    if (_sparklineView != null)
                    {
                        Children.Remove(_sparklineView);
                        _sparklineView = null;
                    }
                    _sparkInfo = value;
                    AttachSparklineEvents();
                }
            }
        }

        void AttachSparklineEvents()
        {
            if (_sparkInfo != null)
                _sparkInfo.SparklineChanged += new EventHandler(sparkline_SparklineChanged);
        }

        void DettachSparklineEvents()
        {
            if (_sparkInfo != null)
                _sparkInfo.SparklineChanged -= new EventHandler(sparkline_SparklineChanged);
        }

        void sparkline_SparklineChanged(object sender, EventArgs e)
        {
            Sparkline sparkline = sender as Sparkline;
            if ((_sparklineView == null) || (_sparklineView.SparklineType != sparkline.SparklineType))
            {
                if (_sparklineView != null)
                {
                    Children.Remove(_sparklineView);
                    _sparklineView = null;
                }
                SynSparklineView();
            }
            else
            {
                UpdateSparkline();
            }
        }

        void UpdateSparkline()
        {
            if (SheetView != null && _sparklineView != null)
            {
                _sparklineView.Update(new Size(ActualWidth, ActualHeight), (double)SheetView.ZoomFactor);
            }
        }

        void SynSparklineView()
        {
            SheetView sheetView = SheetView;
            if (sheetView == null)
                return;

            if (_sparkInfo != null)
            {
                if (_sparklineView == null)
                {
                    _sparklineView = CreateSparkline(_sparkInfo);
                    _sparklineView.ZoomFactor = OwnRow.OwnPanel.Sheet.ZoomFactor;
                    ((IThemeContextSupport)_sparklineView).SetContext(sheetView.ActiveSheet);
                    Canvas.SetZIndex(_sparklineView, 0x3e8);
                    Children.Add(_sparklineView);
                    _sparklineView.Update(new Size(ActualWidth, ActualHeight), (double)sheetView.ZoomFactor);
                }
            }
            else if (_sparklineView != null)
            {
                DettachSparklineEvents();
                Children.Remove(_sparklineView);
                _sparklineView = null;
            }
        }

        BaseSparklineView CreateSparkline(Sparkline info)
        {
            if (info.SparklineType == SparklineType.Column)
            {
                return new ColumnSparklineView(new ColumnSparklineViewInfo(info));
            }
            if (info.SparklineType == SparklineType.Line)
            {
                return new LineSparklineView(new LineSparklineViewInfo(info));
            }
            return new WinLossSparklineView(new WinLossSparklineViewInfo(info));
        }
        #endregion


        void ApplyStyle()
        {
            Windows.UI.Xaml.TextAlignment textAlignment;
            switch (BindingCell.ActualHorizontalAlignment)
            {
                case CellHorizontalAlignment.Center:
                    textAlignment = Windows.UI.Xaml.TextAlignment.Center;
                    break;
                case CellHorizontalAlignment.Right:
                    textAlignment = Windows.UI.Xaml.TextAlignment.Right;
                    break;
                default:
                    textAlignment = Windows.UI.Xaml.TextAlignment.Left;
                    break;
            }
            if (_tb.TextAlignment != textAlignment)
                _tb.TextAlignment = textAlignment;

            VerticalAlignment verAlignment;
            switch (BindingCell.ActualVerticalAlignment)
            {
                case CellVerticalAlignment.Top:
                    verAlignment = VerticalAlignment.Top;
                    break;
                case CellVerticalAlignment.Bottom:
                    verAlignment = VerticalAlignment.Bottom;
                    break;
                default:
                    verAlignment = VerticalAlignment.Center;
                    break;
            }
            if (_tb.VerticalAlignment != verAlignment)
                _tb.VerticalAlignment = verAlignment;

            var foreground = BindingCell.ActualForeground;
            if (foreground != null && foreground != _tb.Foreground)
                _tb.Foreground = foreground;

            var fontStyle = BindingCell.ActualFontStyle;
            if (_tb.FontStyle != fontStyle)
                _tb.FontStyle = fontStyle;

            var fontWeight = BindingCell.ActualFontWeight;
            if (_tb.FontWeight.Weight != fontWeight.Weight)
                _tb.FontWeight = fontWeight;

            var fontStretch = BindingCell.ActualFontStretch;
            if (_tb.FontStretch != fontStretch)
                _tb.FontStretch = fontStretch;

            var fontFamily = BindingCell.ActualFontFamily;
            if (fontFamily != null && _tb.FontFamily.Source != fontFamily.Source)
                _tb.FontFamily = fontFamily;

            bool wrap = BindingCell.ActualWordWrap;
            TextWrapping textWrap = wrap ? TextWrapping.Wrap : TextWrapping.NoWrap;
            if (_tb.TextWrapping != textWrap)
                _tb.TextWrapping = textWrap;

            double fontSize = BindingCell.ActualFontSize * ZoomFactor;
            double fitZoom = -1;
            if (!wrap && BindingCell.ActualShrinkToFit)
            {
                // 自动缩小字体适应单元格宽度
                double textWidth = MeasureHelper.MeasureText(
                    _tb.Text,
                    _tb.FontFamily,
                    fontSize,
                    _tb.FontStretch,
                    _tb.FontStyle,
                    _tb.FontWeight,
                    new Size(double.PositiveInfinity, double.PositiveInfinity),
                    false,
                    null,
                    _tb.UseLayoutRounding,
                    ZoomFactor).Width;
                double cellWidth = BindingCell.Worksheet.GetActualColumnWidth(BindingCell.Column.Index, BindingCell.ColumnSpan, BindingCell.SheetArea) * ZoomFactor;
                cellWidth = MeasureHelper.ConvertExcelCellSizeToTextSize(new Size(cellWidth, double.PositiveInfinity), ZoomFactor).Width;
                cellWidth = Math.Max((double)0.0, (double)(cellWidth - BindingCell.ActualTextIndent * ZoomFactor));
                if (cellWidth < textWidth)
                    fitZoom = cellWidth / textWidth;
            }
            if (fitZoom > 0)
                fontSize *= fitZoom;
            if (_tb.FontSize != fontSize)
                _tb.FontSize = fontSize;

            var margin = MeasureHelper.TextBlockDefaultMargin;
            var indent = BindingCell.ActualTextIndent * ZoomFactor;
            if (indent > 0 && _tb.TextAlignment != Windows.UI.Xaml.TextAlignment.Center)
            {
                if (_tb.TextAlignment == Windows.UI.Xaml.TextAlignment.Left)
                    margin.Left += indent;
                else if (_tb.TextAlignment == Windows.UI.Xaml.TextAlignment.Right)
                    margin.Right += indent;
            }
            if (_tb.Margin != margin)
                _tb.Margin = margin;

            if (BindingCell.ActualUnderline)
            {
                Underline underline = new Underline();
                Run run = new Run();
                run.Text = _tb.Text;
                underline.Inlines.Add(run);
                _tb.Inlines.Clear();
                _tb.Inlines.Add(underline);
                _lastUnderline = true;
            }
            else if (_lastUnderline)
            {
                string str = _tb.Text;
                _tb.Inlines.Clear();
                _tb.Text = str;
            }

            if (BindingCell.ActualStrikethrough)
            {
                foreach (UIElement element in (_tb.Parent as Panel).Children)
                {
                    if (element is StrikethroughView)
                    {
                        StrikethroughView view = element as StrikethroughView;
                        if (view.LineContainer != null)
                        {
                            foreach (var line in view.LineContainer.Children.OfType<Line>())
                            {
                                line.Stroke = _tb.Foreground;
                            }
                        }
                        break;
                    }
                }
            }
        }

        Thickness GetDefaultPaddingForEdit(double fontSize)
        {
            Thickness excelBlank = MeasureHelper.ExcelCellBlankThickness;
            Thickness textBoxBlank = MeasureHelper.TextBoxBlankThickness;
            double left = excelBlank.Left - textBoxBlank.Left;
            double right = excelBlank.Right - textBoxBlank.Right;
            double top = excelBlank.Top - textBoxBlank.Top;
            return new Thickness(left, top, right, excelBlank.Bottom - textBoxBlank.Bottom);
        }


        internal Size GetPreferredEditorSize(Size maxSize, Size cellContentSize, HorizontalAlignment alignment, float indent)
        {
            if (((OwnRow == null) ? null : OwnRow.OwnPanel) == null)
            {
                return new Size();
            }
            //if (!_owningRow.OwningPresenter.Sheet.CanEditOverflow || (_cellType == null))
            //{
            //    return new Size(cellContentSize.Width, cellContentSize.Height);
            //}
            double num = Math.Min(maxSize.Width, cellContentSize.Width);
            Size size = MeasureHelper.ConvertTextSizeToExcelCellSize(CalcStringSize(maxSize, true, null), ZoomFactor);
            size.Width += 2.0;
            string text = "T";
            Size size2 = CalcStringSize(new Size(2147483647.0, 2147483647.0), false, text);
            size.Width += size2.Width;
            if (((alignment == HorizontalAlignment.Left) || (alignment == HorizontalAlignment.Right)) && (num < (size.Width + indent)))
            {
                size.Width += indent;
            }
            return new Size(Math.Max(num, size.Width), Math.Max(cellContentSize.Height, size.Height));
        }

        internal bool JudgeWordWrap(Size maxSize, Size cellContentSize, HorizontalAlignment alignment, float indent)
        {
            return false;
            //if (((((_owningRow == null) ? null : _owningRow.OwningPresenter) == null) || !_owningRow.OwningPresenter.Sheet.CanEditOverflow) || (_cellType == null))
            //{
            //    return false;
            //}
            //double num = Math.Min(maxSize.Width, cellContentSize.Width);
            //Size size = MeasureHelper.ConvertTextSizeToExcelCellSize(CalcStringSize(new Size(2147483647.0, 2147483647.0), false, null), ZoomFactor);
            //size.Width += 2.0;
            //if (((alignment == HorizontalAlignment.Left) || (alignment == HorizontalAlignment.Right)) && (num < (size.Width + indent)))
            //{
            //    size.Width += indent;
            //}
            //return (maxSize.Width < size.Width);
        }

        Size CalcStringSize(Size maxSize, bool allowWrap, string text = null)
        {
            //if (_cellType.HasEditingElement())
            //{
            //    TextBox editingElement = _cellType.GetEditingElement() as TextBox;
            //    if ((editingElement != null) && !string.IsNullOrEmpty(editingElement.Text))
            //    {
            //        Cell bindingCell = BindingCell;
            //        if (bindingCell != null)
            //        {
            //            FontFamily actualFontFamily = bindingCell.ActualFontFamily;
            //            if (actualFontFamily == null)
            //            {
            //                actualFontFamily = editingElement.FontFamily;
            //            }
            //            object textFormattingMode = null;
            //            double fontSize = bindingCell.ActualFontSize * ZoomFactor;
            //            if (fontSize < 0.0)
            //            {
            //                fontSize = editingElement.FontSize;
            //            }
            //            return MeasureHelper.MeasureText((text == null) ? editingElement.Text : text, actualFontFamily, fontSize, bindingCell.ActualFontStretch, bindingCell.ActualFontStyle, bindingCell.ActualFontWeight, maxSize, allowWrap, textFormattingMode, SheetView.UseLayoutRounding, ZoomFactor);
            //        }
            //    }
            //}
            return new Size();
        }

    }
}

