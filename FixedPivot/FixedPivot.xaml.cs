using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace FixedPivot
{
    public enum HeaderPosition
    {
        Top,
        Bottom,
    }

    [TemplatePart(Name = "HeaderClipper", Type = typeof(ContentControl))]
    [TemplatePart(Name = "LeftHeaderPresenter", Type = typeof(ContentPresenter))]
    [TemplatePart(Name = "PreviousButton", Type = typeof(Button))]
    [TemplatePart(Name = "NextButton", Type = typeof(Button))]
    [TemplatePart(Name = "RightHeaderPresenter", Type = typeof(ContentPresenter))]
    [TemplatePart(Name = "splitView", Type = typeof(SplitView))]
    [TemplatePart(Name = "FixedHeader", Type = typeof(AdaptiveGridView))]
    [TemplatePart(Name = "PivotItemPresenter", Type = typeof(ItemsPresenter))]
    public sealed partial class FixedPivot : Pivot
    {
        private bool _isSelectionChanged = false;
        public List<PivotHeaderModel> Headers { get; private set; }

        public object SplitViewPaneButtomContent
        {
            get => GetValue(SplitViewPaneButtomProperty);
            set => SetValue(SplitViewPaneButtomProperty, value);
        }

        // Using a DependencyProperty as the backing store for SplitViewPaneButtom.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SplitViewPaneButtomProperty =
            DependencyProperty.Register(nameof(SplitViewPaneButtomContent), typeof(object), typeof(FixedPivot), new PropertyMetadata(null));



        public double HeaderHeight
        {
            get { return (double)GetValue(HeaderHeightProperty); }
            set { SetValue(HeaderHeightProperty, value); }
        }

        // Using a DependencyProperty as the backing store for HeaderHeight.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HeaderHeightProperty =
            DependencyProperty.Register(nameof(HeaderHeight), typeof(double), typeof(FixedPivot), new PropertyMetadata(48d));



        public Visibility HeaderTextVisibility
        {
            get { return (Visibility)GetValue(HeaderTextVisibilityProperty); }
            set { SetValue(HeaderTextVisibilityProperty, value); }
        }

        // Using a DependencyProperty as the backing store for HeaderTextVisibility.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HeaderTextVisibilityProperty =
            DependencyProperty.Register(nameof(HeaderTextVisibility), typeof(Visibility), typeof(FixedPivot), new PropertyMetadata(Visibility.Visible));



        public object Commands
        {
            get => GetValue(CommandsProperty);
            set => SetValue(CommandsProperty, value);
        }

        // Using a DependencyProperty as the backing store for Commands.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CommandsProperty =
            DependencyProperty.Register(nameof(Commands), typeof(object), typeof(FixedPivot), new PropertyMetadata(null));


        public HeaderPosition HeaderPosition
        {
            get { return (HeaderPosition)GetValue(HeaderPositionProperty); }
            set { SetValue(HeaderPositionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for HeaderPosition.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HeaderPositionProperty =
            DependencyProperty.Register(nameof(HeaderPosition), typeof(HeaderPosition), typeof(FixedPivot), new PropertyMetadata(HeaderPosition.Top, OnHeaderPositionChanged));

        private static void OnHeaderPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as FixedPivot).OnHeaderPositionChanged((HeaderPosition)e.NewValue);
        }

        private void OnHeaderPositionChanged(HeaderPosition newValue)
        {
            if (_fixedHeader == null || _splitView == null)
                return;
            switch (newValue)
            {
                case HeaderPosition.Top:
                    RelativePanel.SetAlignTopWithPanel(_fixedHeader, true);
                    RelativePanel.SetAlignBottomWithPanel(_fixedHeader, false);
                    RelativePanel.SetBelow(_splitView, _fixedHeader);
                    break;
                case HeaderPosition.Bottom:
                    RelativePanel.SetAlignBottomWithPanel(_fixedHeader, true);
                    RelativePanel.SetAlignTopWithPanel(_fixedHeader, false);
                    RelativePanel.SetAbove(_splitView, _fixedHeader);
                    break;
                default:
                    break;
            }
        }

        public double SplitViewPaneLength
        {
            get => (double)GetValue(SplitViewPaneLengthProperty);
            set => SetValue(SplitViewPaneLengthProperty, value);
        }

        // Using a DependencyProperty as the backing store for SplitViewPaneLength.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SplitViewPaneLengthProperty =
            DependencyProperty.Register(nameof(SplitViewPaneLength), typeof(double), typeof(FixedPivot), new PropertyMetadata(300d));


        public double ContentWidth
        {
            get => (double)GetValue(ContentWidthProperty);
            set => SetValue(ContentWidthProperty, value);
        }

        // Using a DependencyProperty as the backing store for ContentWidth.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ContentWidthProperty =
            DependencyProperty.Register(nameof(ContentWidth), typeof(double), typeof(FixedPivot), new PropertyMetadata(double.NaN));



        public double ContentHeight
        {
            get => (double)GetValue(ContentHeightProperty);
            set => SetValue(ContentHeightProperty, value);
        }

        // Using a DependencyProperty as the backing store for ContentHeight.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ContentHeightProperty =
            DependencyProperty.Register(nameof(ContentHeight), typeof(double), typeof(FixedPivot), new PropertyMetadata(double.NaN));
        private SplitView _splitView;
        private AdaptiveGridView _fixedHeader;
        private long _fixedHeaderToken;

        public FixedPivot()
        {
            this.InitializeComponent();
            SelectionChanged += Header_SelectionChanged;
            //Pivot content will out of the window range if not calculate the size
            //TODO: fix pivot content out of the window range with a batter way
            SizeChanged += FixedPivot_SizeChanged;
            Unloaded += FixedPivot_Unloaded;
        }

        private void FixedPivot_Unloaded(object sender, RoutedEventArgs e)
        {
            _fixedHeader.UnregisterPropertyChangedCallback(VisibilityProperty, _fixedHeaderToken);
            SizeChanged -= FixedPivot_SizeChanged;
            SelectionChanged -= Header_SelectionChanged;
            foreach (FixedPivotItem item in Items)
            {
                item.BadgeChanged -= Content_BadgeChanged;
            }
        }

        private void FixedPivot_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_fixedHeader == null || _splitView == null)
            {
                ContentHeight = e.NewSize.Height;
                ContentWidth = e.NewSize.Width;
                return;
            }
            if (e.PreviousSize.Width != e.NewSize.Width)
            {
                RecalculateWidth(e.NewSize.Width);
            }
            if (e.PreviousSize.Height != e.NewSize.Height)
            {
                RecalculateHeight(e.NewSize.Height);
            }
        }

        private void RecalculateWidth(double width)
        {
            if (_splitView.IsPaneOpen)
            {
                ContentWidth = width - _splitView.OpenPaneLength;
            }
            else if (_splitView.DisplayMode == SplitViewDisplayMode.CompactInline)
            {
                ContentWidth = width - _splitView.CompactPaneLength;
            }
            else
            {
                ContentWidth = width;
            }
        }

        private void RecalculateHeight(double height)
        {
            if (_fixedHeader.Visibility == Visibility.Visible)
            {
                ContentHeight = height - _fixedHeader.Height;
            }
            else
            {
                ContentHeight = height;
            }
        }
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            Headers = Items.Where(CheckItem).Select(GetHeader).ToList();
            (GetTemplateChild("HeaderClipper") as UIElement).Visibility = Visibility.Collapsed;
            (GetTemplateChild("LeftHeaderPresenter") as UIElement).Visibility = Visibility.Collapsed;
            (GetTemplateChild("PreviousButton") as UIElement).Visibility = Visibility.Collapsed;
            (GetTemplateChild("NextButton") as UIElement).Visibility = Visibility.Collapsed;
            (GetTemplateChild("RightHeaderPresenter") as UIElement).Visibility = Visibility.Collapsed;
            _fixedHeader = GetTemplateChild("FixedHeader") as AdaptiveGridView;
            _fixedHeader.DataContext = this;
            _fixedHeaderToken = _fixedHeader.RegisterPropertyChangedCallback(VisibilityProperty, OnFixedHeaderVisibilityChanged);
            _splitView = GetTemplateChild("splitView") as SplitView;
            _splitView.DataContext = this;
            var presenter = GetTemplateChild("PivotItemPresenter") as ItemsPresenter;
            presenter.SetBinding(WidthProperty, new Binding
            {
                Source = this,
                Path = new PropertyPath(nameof(ContentWidth)),
                Mode = BindingMode.TwoWay
            });
            presenter.SetBinding(HeightProperty, new Binding
            {
                Source = this,
                Path = new PropertyPath(nameof(ContentHeight)),
                Mode = BindingMode.TwoWay
            });
            OnHeaderPositionChanged(HeaderPosition);
        }

        private void OnFixedHeaderVisibilityChanged(DependencyObject sender, DependencyProperty dp)
        {
            RecalculateHeight(ActualHeight);
        }

        private bool CheckItem(object item)
        {
            var content = (item as FixedPivotItem);
            return content.Visibility == Visibility.Visible;
        }

        private PivotHeaderModel GetHeader(object item)
        {
            var content = (item as FixedPivotItem);
            content.BadgeChanged += Content_BadgeChanged;
            return new PivotHeaderModel
            {
                Badge = content.Badge,
                Icon = content.HeaderIcon,
                Text = content.HeaderText
            };
        }

        private void Content_BadgeChanged(object sender, int e)
        {
            var index = Items.IndexOf(sender as FixedPivotItem);
            Headers[index].Badge = e;
        }

        private void HeaderList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            RequestRefresh();
        }

        public void RequestRefresh()
        {
            (Items[SelectedIndex] as FixedPivotItem).InvokeRefresh();
        }

        private void HeaderList_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (_isSelectionChanged)
            {
                _isSelectionChanged = false;
            }
            else
            {
                ExVisualTreeHelper.GetObject<ScrollViewer>((Items[SelectedIndex] as FixedPivotItem).Content as DependencyObject)?.ChangeView(0, 0, 1);
            }

        }

        private void Header_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _isSelectionChanged = true;
        }
    }
}
