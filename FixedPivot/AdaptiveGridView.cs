using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace FixedPivot
{
    public class AdaptiveGridView : GridView
    {
        /// <summary>
        /// Identifies the <see cref="ItemWidth"/> dependency property.
        /// </summary>
        private static readonly DependencyProperty ItemWidthProperty =
            DependencyProperty.Register(nameof(ItemWidth), typeof(double), typeof(AdaptiveGridView), new PropertyMetadata(double.NaN));
        
        
        /// <summary>
        /// Gets the template that defines the panel that controls the layout of items.
        /// </summary>
        /// <remarks>
        /// This property overrides the base ItemsPanel to prevent changing it.
        /// </remarks>
        /// <returns>
        /// An ItemsPanelTemplate that defines the panel to use for the layout of the items.
        /// The default value for the ItemsControl is an ItemsPanelTemplate that specifies
        /// a StackPanel.
        /// </returns>
        public new ItemsPanelTemplate ItemsPanel => base.ItemsPanel;

        private double ItemWidth
        {
            get { return (double)GetValue(ItemWidthProperty); }
            set { SetValue(ItemWidthProperty, value); }
        }

        private static int CalculateColumns(double containerWidth, double itemWidth)
        {
            var columns = (int)Math.Round(containerWidth / itemWidth);
            if (columns == 0)
            {
                columns = 1;
            }

            return columns;
        }

        private bool _isLoaded;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdaptiveGridView"/> class.
        /// </summary>
        public AdaptiveGridView()
        {
            IsTabStop = false;
            SizeChanged += OnSizeChanged;
            Items.VectorChanged += ItemsOnVectorChanged;
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;

            // Define ItemContainerStyle in code rather than using the DefaultStyle
            // to avoid having to define the entire style of a GridView. This can still
            // be set by the enduser to values of their chosing
            var style = new Style(typeof(GridViewItem));
            style.Setters.Add(new Setter(HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
            style.Setters.Add(new Setter(VerticalContentAlignmentProperty, VerticalAlignment.Stretch));
            ItemContainerStyle = style;
        }

        /// <summary>
        /// Prepares the specified element to display the specified item.
        /// </summary>
        /// <param name="obj">The element that's used to display the specified item.</param>
        /// <param name="item">The item to display.</param>
        protected override void PrepareContainerForItemOverride(DependencyObject obj, object item)
        {
            base.PrepareContainerForItemOverride(obj, item);
            var element = obj as FrameworkElement;
            if (element != null)
            {
                var widthBinding = new Binding()
                {
                    Source = this,
                    Path = new PropertyPath(nameof(ItemWidth)),
                    Mode = BindingMode.TwoWay
                };
                element.SetBinding(WidthProperty, widthBinding);
            }
        }

        /// <summary>
        /// Calculates the width of the grid items.
        /// </summary>
        /// <param name="containerWidth">The width of the container control.</param>
        /// <returns>The calculated item width.</returns>
        protected virtual double CalculateItemWidth(double containerWidth)
        {
            return containerWidth / Items.Count;
        }

        /// <summary>
        /// Invoked whenever application code or internal processes (such as a rebuilding layout pass) call
        /// ApplyTemplate. In simplest terms, this means the method is called just before a UI element displays
        /// in your app. Override this method to influence the default post-template logic of a class.
        /// </summary>
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            SetupItemsPanelRoot();
        }

        private void ItemsOnVectorChanged(IObservableVector<object> sender, IVectorChangedEventArgs @event)
        {
            if (!double.IsNaN(ActualWidth))
            {
                // If the item count changes, check if more or less columns needs to be rendered,
                // in case we were having fewer items than columns.
                RecalculateLayout(ActualWidth);
            }
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // If the width of the internal list view changes, check if more or less columns needs to be rendered.
            if (e.PreviousSize.Width != e.NewSize.Width)
            {
                RecalculateLayout(e.NewSize.Width);
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;
            SetupItemsPanelRoot();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = false;
        }

        private void SetupItemsPanelRoot()
        {
            if (_isLoaded)
            {
                var itemsWrapGridPanel = ItemsPanelRoot as ItemsWrapGrid;
                if (itemsWrapGridPanel != null)
                {
                    itemsWrapGridPanel.Orientation = Orientation.Vertical;
                    itemsWrapGridPanel.MaximumRowsOrColumns = 1;
                }
                ScrollViewer.SetHorizontalScrollMode(this, ScrollMode.Disabled);
            }
        }

        private void RecalculateLayout(double containerWidth)
        {
            if (containerWidth > 0)
            {
                var newWidth = CalculateItemWidth(containerWidth);

                if (double.IsNaN(ItemWidth) || Math.Abs(newWidth - ItemWidth) > 1)
                {
                    ItemWidth = newWidth;
                }
            }
        }

    }
}
