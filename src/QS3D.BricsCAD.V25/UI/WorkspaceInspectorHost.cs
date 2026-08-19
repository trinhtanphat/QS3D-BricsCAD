using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using QS3D.Core.Features;

namespace QS3D.BricsCAD.V25.UI
{
    internal sealed class WorkspaceInspectorHost : Grid
    {
        private readonly ColumnDefinition _centerColumn;
        private readonly ColumnDefinition _primarySeparatorColumn;
        private readonly ColumnDefinition _primaryColumn;
        private readonly ColumnDefinition _secondarySeparatorColumn;
        private readonly ColumnDefinition _secondaryColumn;
        private readonly ContentPresenter _centerPresenter;
        private readonly ScrollViewer _primaryScroll;
        private readonly ContentPresenter _primaryPresenter;
        private readonly ScrollViewer _secondaryScroll;
        private readonly ContentPresenter _secondaryPresenter;
        private UIElement _focusFallback;
        private InteractionSurfaceSnapshot? _snapshot;

        public WorkspaceInspectorHost()
        {
            _centerColumn = new ColumnDefinition { Width = new GridLength(1d, GridUnitType.Star), MinWidth = InspectorHostLayoutPlanner.MinimumCenterWidth };
            _primarySeparatorColumn = new ColumnDefinition { Width = new GridLength(0d) };
            _primaryColumn = new ColumnDefinition { Width = new GridLength(0d) };
            _secondarySeparatorColumn = new ColumnDefinition { Width = new GridLength(0d) };
            _secondaryColumn = new ColumnDefinition { Width = new GridLength(0d) };
            ColumnDefinitions.Add(_centerColumn);
            ColumnDefinitions.Add(_primarySeparatorColumn);
            ColumnDefinitions.Add(_primaryColumn);
            ColumnDefinitions.Add(_secondarySeparatorColumn);
            ColumnDefinitions.Add(_secondaryColumn);

            _centerPresenter = new ContentPresenter { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
            Children.Add(_centerPresenter);
            SetColumn(_centerPresenter, 0);
            _focusFallback = _centerPresenter;

            AddSeparator(1);
            _primaryPresenter = new ContentPresenter { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
            _primaryScroll = CreateInspectorScroll(_primaryPresenter);
            Children.Add(_primaryScroll);
            SetColumn(_primaryScroll, 2);

            AddSeparator(3);
            _secondaryPresenter = new ContentPresenter { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
            _secondaryScroll = CreateInspectorScroll(_secondaryPresenter);
            Children.Add(_secondaryScroll);
            SetColumn(_secondaryScroll, 4);

            SizeChanged += OnHostSizeChanged;
        }

        public object CenterContent
        {
            get => _centerPresenter.Content;
            set
            {
                _centerPresenter.Content = value;
                if (value is UIElement element) _focusFallback = element;
            }
        }

        public void Apply(
            InteractionSurfaceSnapshot snapshot,
            object primaryContent,
            object secondaryContent,
            double preferredPrimaryWidth = InspectorHostLayoutPlanner.DefaultPrimaryWidth,
            double preferredSecondaryWidth = InspectorHostLayoutPlanner.DefaultSecondaryWidth)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));

            _snapshot = snapshot;
            _primaryPresenter.Content = snapshot.PrimaryInspector == null ? null : primaryContent;
            _secondaryPresenter.Content = snapshot.SecondaryInspector == null ? null : secondaryContent;
            ApplyLayout(snapshot, preferredPrimaryWidth, preferredSecondaryWidth);
        }

        public void ClearInspectors()
        {
            var restoreFocus = ContainsKeyboardFocus(_primaryScroll) || ContainsKeyboardFocus(_secondaryScroll);
            _snapshot = null;
            _primaryPresenter.Content = null;
            _secondaryPresenter.Content = null;
            CollapseInspectorColumns();
            if (restoreFocus) RestoreCenterFocus();
        }

        private void ApplyLayout(InteractionSurfaceSnapshot snapshot, double preferredPrimaryWidth, double preferredSecondaryWidth)
        {
            var previousPrimaryVisible = _primaryColumn.ActualWidth > 0d;
            var previousSecondaryVisible = _secondaryColumn.ActualWidth > 0d;
            var availableWidth = ActualWidth > 0d ? ActualWidth : InspectorHostLayoutPlanner.MinimumCenterWidth
                + preferredPrimaryWidth + preferredSecondaryWidth + (InspectorHostLayoutPlanner.SeparatorWidth * 2d);
            var layout = InspectorHostLayoutPlanner.Plan(snapshot, availableWidth, preferredPrimaryWidth, preferredSecondaryWidth);

            _primarySeparatorColumn.Width = new GridLength(layout.PrimaryVisible ? layout.SeparatorWidth : 0d);
            _primaryColumn.Width = new GridLength(layout.PrimaryVisible ? layout.PrimaryWidth : 0d);
            _primaryColumn.MinWidth = layout.PrimaryVisible ? InspectorHostLayoutPlanner.MinimumInspectorWidth : 0d;
            _primaryColumn.MaxWidth = layout.PrimaryVisible ? InspectorHostLayoutPlanner.MaximumInspectorWidth : 0d;
            _primaryScroll.Visibility = layout.PrimaryVisible ? Visibility.Visible : Visibility.Collapsed;

            _secondarySeparatorColumn.Width = new GridLength(layout.SecondaryVisible ? layout.SeparatorWidth : 0d);
            _secondaryColumn.Width = new GridLength(layout.SecondaryVisible ? layout.SecondaryWidth : 0d);
            _secondaryColumn.MinWidth = layout.SecondaryVisible ? InspectorHostLayoutPlanner.MinimumInspectorWidth : 0d;
            _secondaryColumn.MaxWidth = layout.SecondaryVisible ? InspectorHostLayoutPlanner.MaximumInspectorWidth : 0d;
            _secondaryScroll.Visibility = layout.SecondaryVisible ? Visibility.Visible : Visibility.Collapsed;

            if ((previousPrimaryVisible && !layout.PrimaryVisible && ContainsKeyboardFocus(_primaryScroll))
                || (previousSecondaryVisible && !layout.SecondaryVisible && ContainsKeyboardFocus(_secondaryScroll)))
            {
                RestoreCenterFocus();
            }
        }

        private void CollapseInspectorColumns()
        {
            _primarySeparatorColumn.Width = new GridLength(0d);
            _primaryColumn.Width = new GridLength(0d);
            _primaryColumn.MinWidth = 0d;
            _primaryColumn.MaxWidth = 0d;
            _primaryScroll.Visibility = Visibility.Collapsed;
            _secondarySeparatorColumn.Width = new GridLength(0d);
            _secondaryColumn.Width = new GridLength(0d);
            _secondaryColumn.MinWidth = 0d;
            _secondaryColumn.MaxWidth = 0d;
            _secondaryScroll.Visibility = Visibility.Collapsed;
        }

        private void OnHostSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var snapshot = _snapshot;
            if (snapshot == null) return;

            ApplyLayout(snapshot, _primaryColumn.ActualWidth > 0d ? _primaryColumn.ActualWidth : InspectorHostLayoutPlanner.DefaultPrimaryWidth,
                _secondaryColumn.ActualWidth > 0d ? _secondaryColumn.ActualWidth : InspectorHostLayoutPlanner.DefaultSecondaryWidth);
        }

        private void AddSeparator(int column)
        {
            var separator = new Border
            {
                Background = Brushes.Transparent,
                IsHitTestVisible = false
            };
            Children.Add(separator);
            SetColumn(separator, column);
        }

        private static ScrollViewer CreateInspectorScroll(ContentPresenter presenter)
        {
            return new ScrollViewer
            {
                Content = presenter,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                CanContentScroll = true,
                Focusable = false,
                Visibility = Visibility.Collapsed
            };
        }

        private static bool ContainsKeyboardFocus(DependencyObject root)
        {
            var focused = Keyboard.FocusedElement as DependencyObject;
            if (focused == null) return false;
            var current = focused;
            while (current != null)
            {
                if (ReferenceEquals(current, root)) return true;
                current = VisualTreeHelper.GetParent(current);
            }
            return false;
        }

        private void RestoreCenterFocus()
        {
            if (_focusFallback.Focusable) _focusFallback.Focus();
            else _centerPresenter.Focus();
        }
    }
}
