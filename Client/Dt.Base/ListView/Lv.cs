﻿#region 文件描述
/******************************************************************************
* 创建: Daoting
* 摘要: 
* 日志: 2018-11-15 创建
******************************************************************************/
#endregion

#region 引用命名
using Dt.Base.ListView;
using Dt.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
#endregion

namespace Dt.Base
{
    /// <summary>
    /// 列表控件
    /// </summary>
    [ContentProperty(Name = nameof(View))]
    public partial class Lv : Control, IViewItemHost, IMenuHost
    {
        #region 静态内容
        public readonly static DependencyProperty DataProperty = DependencyProperty.Register(
            "Data",
            typeof(INotifyList),
            typeof(Lv),
            new PropertyMetadata(null, OnDataChanged));

        public readonly static DependencyProperty ViewProperty = DependencyProperty.Register(
            "View",
            typeof(object),
            typeof(Lv),
            new PropertyMetadata(null, OnViewChanged));

        public readonly static DependencyProperty ViewModeProperty = DependencyProperty.Register(
            "ViewMode",
            typeof(ViewMode),
            typeof(Lv),
            new PropertyMetadata(ViewMode.List, OnViewModeChanged));

        public readonly static DependencyProperty ViewExProperty = DependencyProperty.Register(
            "ViewEx",
            typeof(Type),
            typeof(Lv),
            new PropertyMetadata(null, OnViewExChanged));

        public readonly static DependencyProperty PhoneViewModeProperty = DependencyProperty.Register(
            "PhoneViewMode",
            typeof(ViewMode?),
            typeof(Lv),
            new PropertyMetadata(null, OnPhoneViewModeChanged));

        public readonly static DependencyProperty SelectionModeProperty = DependencyProperty.Register(
            "SelectionMode",
            typeof(SelectionMode),
            typeof(Lv),
            new PropertyMetadata(SelectionMode.Single, OnSelectionModeChanged));

        public readonly static DependencyProperty ItemHeightProperty = DependencyProperty.Register(
            "ItemHeight",
            typeof(double),
            typeof(Lv),
            new PropertyMetadata(0d, OnReload));

        public static readonly DependencyProperty ShowGroupHeaderProperty = DependencyProperty.Register(
            "ShowGroupHeader",
            typeof(bool),
            typeof(Lv),
            new PropertyMetadata(true, OnReload));

        public static readonly DependencyProperty ShowItemBorderProperty = DependencyProperty.Register(
            "ShowItemBorder",
            typeof(bool),
            typeof(Lv),
            new PropertyMetadata(true, OnReload));

        public static readonly DependencyProperty EnteredBrushProperty = DependencyProperty.Register(
            "EnteredBrush",
            typeof(Brush),
            typeof(Lv),
            new PropertyMetadata(new SolidColorBrush(Color.FromArgb(0x19, 0xff, 0xff, 0x00))));

        public static readonly DependencyProperty PressedBrushProperty = DependencyProperty.Register(
            "PressedBrush",
            typeof(Brush),
            typeof(Lv),
            new PropertyMetadata(new SolidColorBrush(Color.FromArgb(0x19, 0x00, 0x00, 0x00))));

        public readonly static DependencyProperty GroupTemplateProperty = DependencyProperty.Register(
            "GroupTemplate",
            typeof(DataTemplate),
            typeof(Lv),
            new PropertyMetadata(null, OnReload));

        public readonly static DependencyProperty PageDataProperty = DependencyProperty.Register(
            "PageData",
            typeof(PageData),
            typeof(Lv),
            new PropertyMetadata(null, OnPageDataChanged));

        public readonly static DependencyProperty FilterProperty = DependencyProperty.Register(
            "Filter",
            typeof(Predicate<object>),
            typeof(Lv),
            new PropertyMetadata(null));

        public readonly static DependencyProperty GroupNameProperty = DependencyProperty.Register(
            "GroupName",
            typeof(string),
            typeof(Lv),
            new PropertyMetadata(null, OnDataViewPropertyChanged));

        public readonly static DependencyProperty SortDescProperty = DependencyProperty.Register(
            "SortDesc",
            typeof(SortDescription),
            typeof(Lv),
            new PropertyMetadata(null, OnDataViewPropertyChanged));

        public static readonly DependencyProperty AutoFocusProperty = DependencyProperty.Register(
            "AutoFocus",
            typeof(bool),
            typeof(Lv),
            new PropertyMetadata(true));

        public static readonly DependencyProperty AutoCreateColProperty = DependencyProperty.Register(
            "AutoCreateCol",
            typeof(bool),
            typeof(Lv),
            new PropertyMetadata(false, OnAutoCreateColChanged));

        static void OnAutoCreateColChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Lv lv = (Lv)d;
            if ((bool)e.NewValue)
            {
                // 初始化空列
                lv.ViewMode = ViewMode.Table;
                lv.View = new Cols();
            }
        }

        public readonly static DependencyProperty MinItemWidthProperty = DependencyProperty.Register(
            "MinItemWidth",
            typeof(double),
            typeof(Lv),
            new PropertyMetadata(160d, OnReload));

        public static readonly DependencyProperty HasSelectedProperty = DependencyProperty.Register(
            "HasSelected",
            typeof(bool),
            typeof(Lv),
            new PropertyMetadata(false));

        static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Lv lv = (Lv)d;

            // 重新指定数据源时，清除分页数据源
            var pd = lv.PageData;
            if (pd != null && !pd.Loading)
                lv.ClearValue(PageDataProperty);

            if (lv._dataView != null)
                lv._dataView.Unload();

            if (e.NewValue == null)
            {
                lv._dataView = null;
                lv.ClearAllRows();
            }
            else if (e.NewValue is Table tbl)
            {
                if (lv.AutoCreateCol)
                    lv.OnAutoCreateCol(tbl);
                lv._dataView = new LvDataView(lv, tbl);
            }
            else if (e.NewValue is INotifyList ls)
            {
                if (lv.AutoCreateCol && ls.Count > 0)
                    lv.OnAutoCreateProp(ls[0].GetType());
                lv._dataView = new LvDataView(lv, ls);
            }
            lv.OnDataChanged();
        }

        static void OnViewChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Cols cols = e.NewValue as Cols;
            if (cols != null)
                cols.FixWidth();

            if (e.OldValue == null)
            {
                // 初次设置View
                ((Lv)d).LoadPanel();
            }
            else
            {
                ((Lv)d).Reload();
            }
        }

        static void OnViewModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((Lv)d).LoadPanel();
        }

        static void OnPhoneViewModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (AtSys.IsPhoneUI)
                ((Lv)d).LoadPanel();
        }

        static void OnViewExChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Lv lv = (Lv)d;
            Type tp = (Type)e.NewValue;
            if (tp == null)
            {
                lv._exMethod = null;
                lv._styleMethod = null;
            }
            else
            {
                // 提取静态公共方法
                var mis = tp.GetMethods(BindingFlags.Static | BindingFlags.Public);
                lv._exMethod = new Dictionary<string, MethodInfo>(StringComparer.OrdinalIgnoreCase);
                foreach (var mi in mis)
                {
                    var pis = mi.GetParameters();
                    if (pis.Length == 1
                        && (pis[0].ParameterType == typeof(ViewItem) || pis[0].ParameterType == typeof(LvItem))
                        && mi.ReturnType != typeof(void))
                    {
                        lv._exMethod[mi.Name] = mi;
                        continue;
                    }

                    if (mi.ReturnType == typeof(void)
                        && pis.Length == 1
                        && pis[0].ParameterType == typeof(ViewItem)
                        && mi.Name == "SetStyle")
                    {
                        lv._styleMethod = mi;
                    }
                }
            }
            lv.Reload();
        }

        static void OnDataViewPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Lv lv = (Lv)d;
            if (lv._dataView != null)
                lv._dataView.Refresh();
        }

        static void OnReload(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((Lv)d).Reload();
        }

        static void OnSelectionModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Lv lv = (Lv)d;
            if (lv._panel != null)
            {
                foreach (var row in lv.Rows)
                {
                    row.ClearValue(LvItem.IsSelectedProperty);
                    row.ValueChanged = null;
                }

                if (lv._selectedRows.Count > 0)
                {
                    try
                    {
                        lv._selectedRows.CollectionChanged -= lv.OnSelectedItemsChanged;
                        lv._selectedRows.Clear();
                        lv.HasSelected = false;
                    }
                    finally
                    {
                        lv._selectedRows.CollectionChanged += lv.OnSelectedItemsChanged;
                    }
                }
                lv._panel.Reload();
            }
        }

        static async void OnPageDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Lv lv = (Lv)d;
            if (lv.Scroll != null)
                lv.Scroll.ViewChanged -= lv.OnScrollViewChanged;

            var pd = (PageData)e.NewValue;
            if (pd != null)
            {
                if (lv.ExistLocalValue(DataProperty))
                {
                    // 防止清空数据源时删除分页
                    pd.Loading = true;
                    lv.ClearValue(DataProperty);
                    pd.Loading = false;
                }
                pd.SetOwner(lv);
                await pd.GotoFirstPage();

                if (lv.Scroll != null)
                    lv.Scroll.ViewChanged += lv.OnScrollViewChanged;
            }
        }
        #endregion

        #region 成员变量
        Border _root;
        LvPanel _panel;
        LvDataView _dataView;
        readonly List<LvItem> _rows;
        readonly ObservableCollection<LvItem> _selectedRows;
        Dictionary<string, MethodInfo> _exMethod;
        MethodInfo _styleMethod;
        bool _updatingView;
        SizedPresenter _sizedPresenter;
        #endregion

        #region 构造方法
        public Lv()
        {
            DefaultStyleKey = typeof(Lv);

            _rows = new List<LvItem>();
            _selectedRows = new ObservableCollection<LvItem>();
            _selectedRows.CollectionChanged += OnSelectedItemsChanged;
#if !UWP
            Loaded += OnLoaded;
#endif
        }
        #endregion

        #region 事件
        /// <summary>
        /// 单击行/项目事件
        /// </summary>
        public event EventHandler<ItemClickArgs> ItemClick;

        /// <summary>
        /// 选择变化事件
        /// </summary>
        public event SelectionChangedEventHandler SelectionChanged;

        /// <summary>
        /// 切换数据源事件
        /// </summary>
        public event EventHandler<object> DataChanged;

        /// <summary>
        /// 行加载完毕事件，行变化时始终触发(增删排序分组过滤)，如用于自动滚动到底部
        /// </summary>
        public event EventHandler LoadedRows;
        #endregion

        #region 属性
        /// <summary>
        /// 获取设置数据源对象，需实现INotifyList接口，Table 和 Nl 为常用类型
        /// </summary>
        public INotifyList Data
        {
            get { return (INotifyList)GetValue(DataProperty); }
            set { SetValue(DataProperty, value); }
        }

        /// <summary>
        /// 获取设置行/项目模板，DataTemplate、DataTemplateSelector 或 Cols列定义
        /// </summary>
        public object View
        {
            get { return GetValue(ViewProperty); }
            set { SetValue(ViewProperty, value); }
        }

        /// <summary>
        /// 获取设置视图类型：列表、表格、磁贴，默认List
        /// </summary>
        public ViewMode ViewMode
        {
            get { return (ViewMode)GetValue(ViewModeProperty); }
            set { SetValue(ViewModeProperty, value); }
        }

        /// <summary>
        /// 获取设置视图扩展，包括定义行/项目样式和扩展列
        /// </summary>
        public Type ViewEx
        {
            get { return (Type)GetValue(ViewExProperty); }
            set { SetValue(ViewExProperty, value); }
        }

        /// <summary>
        /// 获取设置Phone模式下的视图类型，null时Win,Phone两模式统一采用ViewMode，默认null
        /// </summary>
        public ViewMode? PhoneViewMode
        {
            get { return (ViewMode?)GetValue(PhoneViewModeProperty); }
            set { SetValue(PhoneViewModeProperty, value); }
        }

        /// <summary>
        /// 获取设置选择模式，默认Single
        /// </summary>
        public SelectionMode SelectionMode
        {
            get { return (SelectionMode)GetValue(SelectionModeProperty); }
            set { SetValue(SelectionModeProperty, value); }
        }

        /// <summary>
        /// 获取设置行/项目高度，0时以第一项高度为准，NaN时自动调整高度(性能差)，默认0
        /// </summary>
        public double ItemHeight
        {
            get { return (double)GetValue(ItemHeightProperty); }
            set { SetValue(ItemHeightProperty, value); }
        }

        /// <summary>
        /// 获取设置顶部是否显示分组导航，默认true
        /// </summary>
        public bool ShowGroupHeader
        {
            get { return (bool)GetValue(ShowGroupHeaderProperty); }
            set { SetValue(ShowGroupHeaderProperty, value); }
        }

        /// <summary>
        /// 获取设置是否显示行/项目分割线，默认true
        /// </summary>
        public bool ShowItemBorder
        {
            get { return (bool)GetValue(ShowItemBorderProperty); }
            set { SetValue(ShowItemBorderProperty, value); }
        }

        /// <summary>
        /// 获取设置鼠标进入行/项目时的背景色
        /// </summary>
        public Brush EnteredBrush
        {
            get { return (Brush)GetValue(EnteredBrushProperty); }
            set { SetValue(EnteredBrushProperty, value); }
        }

        /// <summary>
        /// 获取设置点击行/项目时的背景色
        /// </summary>
        public Brush PressedBrush
        {
            get { return (Brush)GetValue(PressedBrushProperty); }
            set { SetValue(PressedBrushProperty, value); }
        }

        /// <summary>
        /// 获取设置分组模板
        /// </summary>
        public DataTemplate GroupTemplate
        {
            get { return (DataTemplate)GetValue(GroupTemplateProperty); }
            set { SetValue(GroupTemplateProperty, value); }
        }

        /// <summary>
        /// 获取设置分页数据源对象
        /// </summary>
        public PageData PageData
        {
            get { return (PageData)GetValue(PageDataProperty); }
            set { SetValue(PageDataProperty, value); }
        }

        /// <summary>
        /// 获取设置过滤回调
        /// </summary>
        public Predicate<object> Filter
        {
            get { return (Predicate<object>)GetValue(FilterProperty); }
            set { SetValue(FilterProperty, value); }
        }

        /// <summary>
        /// 获取设置分组列名
        /// </summary>
        public string GroupName
        {
            get { return (string)GetValue(GroupNameProperty); }
            set { SetValue(GroupNameProperty, value); }
        }

        /// <summary>
        /// 获取设置排序条件
        /// </summary>
        public SortDescription SortDesc
        {
            get { return (SortDescription)GetValue(SortDescProperty); }
            set { SetValue(SortDescProperty, value); }
        }

        /// <summary>
        /// 获取设置加载后是否自动为输入焦点，默认true
        /// </summary>
        public bool AutoFocus
        {
            get { return (bool)GetValue(AutoFocusProperty); }
            set { SetValue(AutoFocusProperty, value); }
        }

        /// <summary>
        /// 获取设置是否根据数据源自动生成列
        /// </summary>
        public bool AutoCreateCol
        {
            get { return (bool)GetValue(AutoCreateColProperty); }
            set { SetValue(AutoCreateColProperty, value); }
        }

        /// <summary>
        /// 获取设置项目的最小宽度，默认160，只磁贴视图有效！
        /// </summary>
        public double MinItemWidth
        {
            get { return (double)GetValue(MinItemWidthProperty); }
            set { SetValue(MinItemWidthProperty, value); }
        }

        /// <summary>
        /// 获取是否有选择行
        /// </summary>
        public bool HasSelected
        {
            get { return (bool)GetValue(HasSelectedProperty); }
            private set { SetValue(HasSelectedProperty, value); }
        }

        /// <summary>
        /// 获取当前选择的行列表
        /// </summary>
        public IEnumerable<object> SelectedItems
        {
            get
            {
                return from row in _selectedRows
                       select row.Data;
            }
        }

        /// <summary>
        /// 获取设置当前选定行，设置null时清空选择，多选时为返回最后选择行
        /// </summary>
        public object SelectedItem
        {
            get
            {
                var row = _selectedRows.LastOrDefault();
                if (row != null)
                    return row.Data;
                return null;
            }
            set
            {
                // 清空选择
                if (value == null)
                {
                    if (_selectedRows.Count > 0)
                        _selectedRows.Clear();
                    return;
                }

                // 行是否存在
                var selectedRow = (from row in _rows
                                   where row.Data == value
                                   select row).FirstOrDefault();
                if (selectedRow == null)
                    return;

                // 挑出取消选择的行
                bool exist = false;
                List<object> removes = new List<object>();
                foreach (var row in _selectedRows)
                {
                    if (row != selectedRow)
                    {
                        row.IsSelected = false;
                        removes.Add(row.Data);
                    }
                    else
                    {
                        exist = true;
                    }
                }
                // 无变化
                if (removes.Count == 0 && exist)
                    return;

                try
                {
                    _selectedRows.CollectionChanged -= OnSelectedItemsChanged;
                    if (_selectedRows.Count > 0)
                        _selectedRows.Clear();
                    _selectedRows.Add(selectedRow);
                    selectedRow.IsSelected = true;
                    HasSelected = true;
                }
                finally
                {
                    _selectedRows.CollectionChanged += OnSelectedItemsChanged;
                }

                if (SelectionChanged != null)
                {
                    List<object> adds = new List<object>();
                    if (!exist)
                        adds.Add(value);
                    SelectionChanged(this, new SelectionChangedEventArgs(removes, adds));
                }
            }
        }

        /// <summary>
        /// 获取设置选定行的索引，-1无选定行，设置-1清空选择
        /// </summary>
        public int SelectedIndex
        {
            get
            {
                var row = _selectedRows.LastOrDefault();
                if (row != null)
                    return _rows.IndexOf(row);
                return -1;
            }
            set
            {
                // 清空选择
                if (value == -1)
                {
                    if (_selectedRows.Count > 0)
                        _selectedRows.Clear();
                    return;
                }

                // 超出范围
                if (value < 0 || value >= _rows.Count)
                    return;

                // 挑出取消选择的行
                bool exist = false;
                var selectedRow = _rows[value];
                List<object> removes = new List<object>();
                foreach (var row in _selectedRows)
                {
                    if (row != selectedRow)
                    {
                        row.IsSelected = false;
                        removes.Add(row);
                    }
                    else
                    {
                        exist = true;
                    }
                }
                // 无变化
                if (removes.Count == 0 && exist)
                    return;

                try
                {
                    _selectedRows.CollectionChanged -= OnSelectedItemsChanged;
                    if (_selectedRows.Count > 0)
                        _selectedRows.Clear();
                    _selectedRows.Add(selectedRow);
                    selectedRow.IsSelected = true;
                    HasSelected = true;
                }
                finally
                {
                    _selectedRows.CollectionChanged += OnSelectedItemsChanged;
                }

                if (SelectionChanged != null)
                {
                    List<object> adds = new List<object>();
                    if (!exist)
                        adds.Add(value);
                    SelectionChanged(this, new SelectionChangedEventArgs(removes, adds));
                }
            }
        }

        /// <summary>
        /// 获取选择的行数
        /// </summary>
        public int SelectedCount
        {
            get { return _selectedRows.Count; }
        }

        /// <summary>
        /// 获取当前选定的Row
        /// </summary>
        public Row SelectedRow
        {
            get { return SelectedItem as Row; }
        }

        /// <summary>
        /// 获取Table数据源
        /// </summary>
        public Table Table
        {
            get { return GetValue(DataProperty) as Table; }
        }
        #endregion

        #region 内部属性
        /// <summary>
        /// 所有视图行
        /// </summary>
        internal List<LvItem> Rows
        {
            get { return _rows; }
        }

        /// <summary>
        /// 所有分组行
        /// </summary>
        internal List<GroupRow> GroupRows { get; set; }

        /// <summary>
        /// 包含分组时，按顺序排列的所有分组行和视图行，true 分组行，false视图行
        /// </summary>
        internal List<bool> MapRows { get; set; }

        /// <summary>
        /// 是否为表格视图
        /// </summary>
        internal bool IsTableView
        {
            get { return View is Cols && CurrentViewMode == ViewMode.Table; }
        }

        /// <summary>
        /// 是否采用虚拟行
        /// </summary>
        internal bool IsVir
        {
            get { return !(View is DataTemplateSelector || double.IsNaN(ItemHeight)); }
        }

        internal ViewMode CurrentViewMode
        {
            get { return AtSys.IsPhoneUI ? (PhoneViewMode.HasValue ? PhoneViewMode.Value : ViewMode) : ViewMode; }
        }

        internal ScrollViewer Scroll { get; private set; }

        /// <summary>
        /// 滚动栏是否在内部
        /// </summary>
        internal bool IsInnerScroll
        {
            get { return _root.Child is ScrollViewer; }
        }

        internal Cols Cols
        {
            get { return View as Cols; }
        }

        internal IList<LvItem> SelectedRows
        {
            get { return _selectedRows; }
        }
        #endregion

        #region 外部方法
        /// <summary>
        /// 获取当前选定的实体
        /// </summary>
        /// <typeparam name="TEntity">实体类型</typeparam>
        /// <returns></returns>
#if ANDROID
        new
#endif
        public TEntity Selected<TEntity>()
        where TEntity : Entity
        {
            return SelectedItem as TEntity;
        }

        /// <summary>
        /// 刷新数据视图，通常在动态过滤时调用
        /// </summary>
        public void Refresh()
        {
            if (_dataView != null)
                _dataView.Refresh();
        }

        /// <summary>
        /// 切换视图，同时调整多属性时只刷新一次，性能高！
        /// </summary>
        /// <param name="p_view">null时不切换</param>
        /// <param name="p_viewMode">null时不切换</param>
        /// <param name="p_viewEx">null时不切换</param>
        public void ChangeView(object p_view, ViewMode? p_viewMode, Type p_viewEx = null)
        {
            _updatingView = true;
            try
            {
                if (p_view != null)
                    View = p_view;
                if (p_viewEx != null)
                    ViewEx = p_viewEx;

                Scroll.ChangeView(0, 0, null);
                if (p_viewMode.HasValue && ViewMode != p_viewMode.Value)
                {
                    _rows.Clear();
                    if (_selectedRows.Count > 0)
                        _selectedRows.Clear();
                    if (GroupRows != null)
                    {
                        GroupRows.Clear();
                        GroupRows = null;
                        MapRows = null;
                    }
                    ViewMode = p_viewMode.Value;
                    Refresh();
                }
                else if (_panel != null)
                {
                    _panel.Reload();
                }
            }
            finally
            {
                _updatingView = false;
            }
        }

        /// <summary>
        /// 设置为输入焦点
        /// </summary>
        public void SetFocus()
        {
            if (_panel != null)
                _panel.ReceiveFocus();
        }

        /// <summary>
        /// 获取行UI，不支持虚拟行的情况！使用场景少
        /// </summary>
        /// <param name="p_index">行索引</param>
        /// <returns></returns>
        public FrameworkElement GetRowUI(int p_index)
        {
            if (!IsVir && _panel != null)
                return _panel.GetLvRow(p_index);
            return null;
        }
        #endregion

        #region 选择
        /// <summary>
        /// 全选
        /// </summary>
        public void SelectAll()
        {
            if (SelectionMode != SelectionMode.Multiple)
                return;

            List<object> adds = new List<object>();
            try
            {
                _selectedRows.CollectionChanged -= OnSelectedItemsChanged;
                foreach (var row in _rows)
                {
                    if (!row.IsSelected)
                    {
                        adds.Add(row.Data);
                        _selectedRows.Add(row);
                        row.IsSelected = true;
                    }
                }
            }
            finally
            {
                _selectedRows.CollectionChanged += OnSelectedItemsChanged;
            }

            if (adds.Count > 0 && SelectionChanged != null)
                SelectionChanged(this, new SelectionChangedEventArgs(new List<object>(), adds));
        }

        /// <summary>
        /// 清除所有选择行的选择状态
        /// </summary>
        public void ClearSelection()
        {
            if (_selectedRows.Count > 0)
                _selectedRows.Clear();
        }

        /// <summary>
        /// 增加选择行
        /// </summary>
        /// <param name="p_ls"></param>
        public void Select(IList p_ls)
        {
            if (p_ls == null || p_ls.Count == 0)
                return;

            List<object> adds = new List<object>();
            try
            {
                _selectedRows.CollectionChanged -= OnSelectedItemsChanged;
                foreach (var row in _rows)
                {
                    if (!row.IsSelected && p_ls.Contains(row.Data))
                    {
                        adds.Add(row.Data);
                        _selectedRows.Add(row);
                        row.IsSelected = true;
                    }
                }
            }
            finally
            {
                _selectedRows.CollectionChanged += OnSelectedItemsChanged;
            }

            if (adds.Count > 0 && SelectionChanged != null)
                SelectionChanged(this, new SelectionChangedEventArgs(new List<object>(), adds));
        }

        /// <summary>
        /// 取消列表中行的选择状态
        /// </summary>
        /// <param name="p_ls"></param>
        public void RemoveSelection(IList p_ls)
        {
            if (p_ls == null || p_ls.Count == 0)
                return;

            List<object> removes = new List<object>();
            try
            {
                _selectedRows.CollectionChanged -= OnSelectedItemsChanged;
                foreach (var data in p_ls)
                {
                    var item = (from row in _selectedRows
                                where row.Data == data
                                select row).FirstOrDefault();
                    if (item != null)
                    {
                        removes.Add(data);
                        _selectedRows.Remove(item);
                        item.IsSelected = false;
                    }
                }
            }
            finally
            {
                _selectedRows.CollectionChanged += OnSelectedItemsChanged;
            }

            if (removes.Count > 0 && SelectionChanged != null)
                SelectionChanged(this, new SelectionChangedEventArgs(removes, new List<object>()));
        }

        /// <summary>
        /// 删除所有选择行
        /// </summary>
        /// <param name="p_isConfirm"></param>
        /// <returns></returns>
        public void DeleteSelection()
        {
            var data = Data;
            if (data != null && _selectedRows.Count > 0)
            {
                data.RemoveRange((from row in _selectedRows
                                  select row.Data).ToList());
            }
        }
        #endregion

        #region 滚动到可视
        /// <summary>
        /// 滚动到最顶端
        /// </summary>
        public void ScrollTop()
        {
            if (_panel != null)
                _panel.ScrollInto(0);
        }

        /// <summary>
        /// 滚动到最底端
        /// </summary>
        public void ScrollBottom()
        {
            if (_panel != null)
                _panel.ScrollInto(Rows.Count - 1);
        }

        /// <summary>
        /// 将指定行滚动到可视区域
        /// </summary>
        /// <param name="p_index">行索引</param>
        public void ScrollInto(int p_index)
        {
            if (_panel != null)
                _panel.ScrollInto(p_index);
        }

        /// <summary>
        /// 滚动到指定的数据行
        /// </summary>
        /// <param name="p_row"></param>
        public void ScrollInto(object p_row)
        {
            if (_panel != null)
            {
                for (int i = 0; i < _rows.Count; i++)
                {
                    if (_rows[i].Data == p_row)
                    {
                        _panel.ScrollInto(i);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 点击分组导航头链接，滚动到指定的分组
        /// </summary>
        /// <param name="p_group"></param>
        internal void ScrollIntoGroup(GroupRow p_group)
        {
            if (IsInnerScroll)
            {
                // 启用动画会界面抖动！
                // 16为分组行上部的间隔高度
                Scroll.ChangeView(null, p_group.IsFirst ? 0 : p_group.Top + 16, null, true);
            }
            else
            {
                // 不能用p_group计算相对位置，因不可见时被布局在空区域
                var pt = _panel.TransformToVisual(Scroll).TransformPoint(new Point());
                double y = Scroll.VerticalOffset + pt.Y + p_group.Top;
                if (!p_group.IsFirst)
                    y += 16;
                Scroll.ChangeView(null, y, null, true);
            }
        }
        #endregion

        #region 重写方法
        /*********************************************************************************************************/
        // 平台调用顺序不同：
        // UWP：OnApplyTemplate > MeasureOverride > ArrangeOverride > SizeChanged > Loaded
        // Adr：OnApplyTemplate > Loaded > MeasureOverride > ArrangeOverride > SizeChanged
        // iOS：OnApplyTemplate > Loaded > MeasureOverride > SizeChanged > ArrangeOverride
        // uno中OnApplyTemplate时不一定在可视树上，uwp的OnApplyTemplate时已在可视树上
        // 为了动态构造控件内容，uwp在OnApplyTemplate中处理，uno在Loaded时处理 ！
        /*********************************************************************************************************/
#if UWP
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            InitTemplate();
        }
#endif

        protected override Size MeasureOverride(Size availableSize)
        {
            // 准确获取可见区大小！
            if (_panel != null)
            {
                if (!double.IsInfinity(availableSize.Width) && !double.IsInfinity(availableSize.Height))
                {
                    // 外部无ScrollViewer StackPanel的情况
                    _panel.SetMaxSize(availableSize);
                }
                else if (_sizedPresenter != null)
                {
                    // 外部有ScrollViewer时，取父级有效大小，参见win.xaml：win模式在Tabs定义，phone模式在Tab定义
                    _panel.SetMaxSize(_sizedPresenter.AvailableSize);
                }
                else
                {
                    // 无有效大小时以窗口大小为准
                    double width = double.IsInfinity(availableSize.Width) ? SysVisual.ViewWidth : availableSize.Width;
                    double height = double.IsInfinity(availableSize.Height) ? SysVisual.ViewHeight : availableSize.Height;
                    _panel.SetMaxSize(new Size(width, height));
                }
            }
            return base.MeasureOverride(availableSize);
        }
        #endregion

        #region 加载过程
        /// <summary>
        /// 动态构造控件内容
        /// uwp在OnApplyTemplate中调用，Lv已在可视树上
        /// uno在Loaded时调用，OnApplyTemplate时不一定在可视树上
        /// </summary>
        void InitTemplate()
        {
            _root = (Border)GetTemplateChild("Border");

            // win模式查询范围限制在Tabs内，phone模式限制在Tab内
            var scroll = this.FindParentInWin<ScrollViewer>();
            if (scroll == null)
            {
                // 内部滚动栏
                scroll = new ScrollViewer();
                _root.Child = scroll;
            }
            else
            {
                // 参见win.xaml：win模式在Tabs定义，phone模式在Tab定义
                _sizedPresenter = scroll.FindParentInWin<SizedPresenter>();
            }
            scroll.VerticalScrollMode = ScrollMode.Auto;
            scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            // 滚动到顶部或底部时添加分页数据
            if (PageData != null)
                scroll.ViewChanged += OnScrollViewChanged;
            Scroll = scroll;

            LoadPanel();
        }

        /// <summary>
        /// 动态加载面板
        /// </summary>
        void LoadPanel()
        {
            if (_root == null || View == null)
                return;

            if (_panel != null)
            {
                _panel.Unload();
                if (_root.Child == Scroll)
                    Scroll.Content = null;
                else
                    _root.Child = null;
            }

            LvPanel pnl;
            ViewMode mode = CurrentViewMode;
            if (mode == ViewMode.List)
            {
                pnl = new ListPanel(this);
            }
            else if (mode == ViewMode.Table)
            {
                if (View is Cols)
                    pnl = new TablePanel(this);
                else
                    throw new Exception("未提供表格所需的Cols定义！");
            }
            else
            {
                pnl = new TilePanel(this);
            }

            if (_panel != null)
                pnl.SetMaxSize(_panel.GetMaxSize());
            _panel = pnl;

            if (mode == ViewMode.Table)
            {
                Scroll.HorizontalScrollMode = ScrollMode.Auto;
                Scroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
            }
            else
            {
                Scroll.HorizontalScrollMode = ScrollMode.Disabled;
                Scroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            }

            // 内部有滚动栏时，面板放在滚动栏内
            if (_root.Child == Scroll)
                Scroll.Content = _panel;
            else
                _root.Child = _panel;
        }

        /// <summary>
        /// 重新加载面板内容
        /// </summary>
        void Reload()
        {
            if (_panel != null && !_updatingView)
                _panel.Reload();
        }

        /// <summary>
        /// 加载数据行
        /// </summary>
        internal void LoadRows(IEnumerable p_rows)
        {
            _rows.Clear();
            if (_selectedRows.Count > 0)
                _selectedRows.Clear();
            bool existGroup = false;
            if (GroupRows != null)
            {
                // 清空原有分组行
                GroupRows.Clear();
                GroupRows = null;
                MapRows = null;
                existGroup = true;
            }
            int i = 1;
            foreach (var row in p_rows)
            {
                _rows.Add(new LvItem(this, row, i++));
            }

            if (_panel != null)
            {
                _panel.OnRowsChanged(existGroup);
                OnLoadedRows();
            }
        }

        /// <summary>
        /// 加载数据行和分组行
        /// </summary>
        /// <param name="p_groups"></param>
        internal void LoadGroupRows(IList p_groups)
        {
            _rows.Clear();
            if (_selectedRows.Count > 0)
                _selectedRows.Clear();
            int i = 1;

            MapRows = new List<bool>();
            GroupRows = new List<GroupRow>();
            foreach (var group in p_groups.OfType<IList>())
            {
                GroupRows.Add(new GroupRow(this, group));
                MapRows.Add(true);
                foreach (var row in group)
                {
                    _rows.Add(new LvItem(this, row, i++));
                    MapRows.Add(false);
                }
            }
            if (GroupRows.Count > 0)
                GroupRows[0].IsFirst = true;

            if (_panel != null)
            {
                _panel.OnRowsChanged(true);
                OnLoadedRows();
            }
        }

        /// <summary>
        /// 批量插入数据行
        /// </summary>
        /// <param name="p_tbl"></param>
        /// <param name="p_start">开始插入位置</param>
        /// <param name="p_count">共插入行数</param>
        internal void BatchInsertRows(IList p_tbl, int p_start, int p_count)
        {
            if (p_start < 0 || p_start > _rows.Count)
                return;

            int index;
            if (p_start == _rows.Count)
            {
                // 添加到末尾
                index = _rows.Count + 1;
                for (int i = 0; i < p_count; i++)
                {
                    _rows.Add(new LvItem(this, p_tbl[i + p_start], index++));
                    if (MapRows != null)
                        MapRows.Add(false);
                }
            }
            else
            {
                // 插入
                index = p_start;
                for (int i = 0; i < p_count; i++)
                {
                    _rows.Insert(index, new LvItem(this, p_tbl[index], index + 1));
                    if (MapRows != null)
                        MapRows.Insert(index, false);
                    index++;
                }

                // 更新后续行号
                for (int i = index; i < _rows.Count; i++)
                {
                    _rows[i].Index = i + 1;
                }
            }

            if (_panel != null)
            {
                _panel.OnInsertRows(p_start, p_count);
                OnLoadedRows();
            }
        }

        /// <summary>
        /// 清空所有行
        /// </summary>
        internal void ClearAllRows()
        {
            _rows.Clear();
            if (_selectedRows.Count > 0)
                _selectedRows.Clear();
            bool existGroup = false;
            if (GroupRows != null)
            {
                GroupRows.Clear();
                GroupRows = null;
                MapRows = null;
                existGroup = true;
            }
            if (_panel != null)
            {
                _panel.OnRowsChanged(existGroup);
                OnLoadedRows();
            }
        }

        /// <summary>
        /// uno时的处理
        /// uno中OnApplyTemplate时不一定在可视树上，uwp的OnApplyTemplate时已在可视树上
        /// 为了动态构造控件内容，uwp在OnApplyTemplate中处理，uno在Loaded时处理 ！
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            InitTemplate();
        }

        /// <summary>
        /// 滚动到顶部或底部时添加分页数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        async void OnScrollViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            if (e.IsIntermediate || !PageData.HasMorePages)
                return;

            var page = PageData;
            if (page.InsertTop && Scroll.VerticalOffset == 0)
            {
                // 插入顶部
                Scroll.ViewChanged -= OnScrollViewChanged;
                int cnt = 0;
                if (Data != null)
                    cnt = Data.Count;
                await page.GotoNextPage();
                cnt = Data.Count - cnt - 1;

                if (cnt > 0)
                {
                    // 滚动到当前行的位置
                    await Dispatcher.RunAsync(
                        CoreDispatcherPriority.Normal,
                        new DispatchedHandler(() =>
                        {
                            double height = _panel.GetRowVerPos(cnt);
                            Scroll.ChangeView(null, height, null, true);
                        })
                        );
                }
                Scroll.ViewChanged += OnScrollViewChanged;
            }
            else if (!page.InsertTop && Scroll.VerticalOffset == Scroll.ScrollableHeight)
            {
                // 插入底部
                await page.GotoNextPage();
            }
        }

        /// <summary>
        /// 自动生成列
        /// </summary>
        /// <param name="p_tbl"></param>
        void OnAutoCreateCol(Table p_tbl)
        {
            Cols cols = new Cols();
            foreach (var c in p_tbl.Columns)
            {
                cols.Add(new Col { ID = c.ID, Title = c.ID, Width = 200 });
            }
            View = cols;
        }

        void OnAutoCreateProp(Type p_type)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region 选择
        /// <summary>
        /// 选择行集合变化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnSelectedItemsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // HasSelected状态
            bool hasSelected = HasSelected;
            if (_selectedRows.Count == 0 && hasSelected)
                ClearValue(HasSelectedProperty);
            else if (_selectedRows.Count > 0 && !hasSelected)
                HasSelected = true;

            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                // 增加
                if (e.NewItems != null && e.NewItems.Count > 0)
                {
                    List<object> adds = new List<object>();
                    foreach (var row in e.NewItems.OfType<LvItem>())
                    {
                        row.IsSelected = true;
                        adds.Add(row.Data);
                    }
                    SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(new List<object>(), adds));
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                // 删除
                if (e.OldItems != null && e.OldItems.Count > 0)
                {
                    List<object> removals = new List<object>();
                    foreach (var row in e.OldItems.OfType<LvItem>())
                    {
                        row.IsSelected = false;
                        removals.Add(row.Data);
                    }
                    SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(removals, new List<object>()));
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                // 清空
                var removals = new List<object>();
                foreach (var row in _rows)
                {
                    if (row.IsSelected)
                    {
                        removals.Add(row.Data);
                        row.IsSelected = false;
                    }
                }
                if (removals.Count > 0 && SelectionChanged != null)
                    SelectionChanged(this, new SelectionChangedEventArgs(removals, new List<object>()));
            }
        }

        /// <summary>
        /// 单选模式点击时切换选择
        /// </summary>
        /// <param name="p_vr"></param>
        internal void OnToggleSelected(LvItem p_vr)
        {
            List<object> removes = new List<object>();
            try
            {
                _selectedRows.CollectionChanged -= OnSelectedItemsChanged;
                if (_selectedRows.Count > 0)
                {
                    var row = _selectedRows[0];
                    row.IsSelected = false;
                    removes.Add(row.Data);
                    _selectedRows.Clear();
                }

                _selectedRows.Add(p_vr);
                p_vr.IsSelected = true;
                HasSelected = true;
            }
            finally
            {
                _selectedRows.CollectionChanged += OnSelectedItemsChanged;
            }
            SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(removes, new List<object> { p_vr.Data }));
        }
        #endregion

        #region IViewItemHost
        void IViewItemHost.SetItemStyle(ViewItem p_item)
        {
            if (_styleMethod != null)
                _styleMethod.Invoke(null, new object[] { p_item });
        }

        MethodInfo IViewItemHost.GetViewExMethod(string p_colName)
        {
            if (_exMethod != null && _exMethod.TryGetValue(p_colName, out MethodInfo mi))
                return mi;
            return null;
        }
        #endregion

        #region IMenuHost
        /// <summary>
        /// 切换上下文菜单或修改触发事件种类时通知宿主刷新
        /// </summary>
        void IMenuHost.UpdateContextMenu()
        {
            Reload();
        }
        #endregion

        #region 触发事件
        /// <summary>
        /// 触发单击行事件
        /// </summary>
        /// <param name="e"></param>
        internal void OnItemClick(object p_data, object p_oldData)
        {
            ItemClick?.Invoke(this, new ItemClickArgs(p_data, p_oldData));
        }

        /// <summary>
        /// 触发切换数据源事件
        /// </summary>
        void OnDataChanged()
        {
            DataChanged?.Invoke(this, Data);
        }

        /// <summary>
        /// 触发行加载完毕事件
        /// </summary>
        void OnLoadedRows()
        {
            // 共四种行加载情况：LoadRows LoadGroupRows BatchInsertRows ClearAllRows
            // 数据变化 -> 行加载 -> UI完成布局 -> 触发完毕事件
            _panel.UpdateLayout();
            LoadedRows?.Invoke(this, EventArgs.Empty);
        }
        #endregion
    }
}