using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Applies production-safe WPF defaults only when a QS3D control has not
    /// explicitly selected a different value through a style, binding, template,
    /// animation, or local value.
    /// </summary>
    internal static class ProductionUiPolish
    {
        private static int _registered;

        internal static void EnsureRegistered()
        {
            if (Interlocked.Exchange(ref _registered, 1) != 0)
            {
                return;
            }

            EventManager.RegisterClassHandler(
                typeof(Window),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnQs3dRootLoaded),
                true);
            EventManager.RegisterClassHandler(
                typeof(UserControl),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnQs3dRootLoaded),
                true);
            EventManager.RegisterClassHandler(
                typeof(DataGrid),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnDataGridLoaded),
                true);
            EventManager.RegisterClassHandler(
                typeof(ListBox),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnListBoxLoaded),
                true);
            EventManager.RegisterClassHandler(
                typeof(TreeView),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnTreeViewLoaded),
                true);
        }

        private static void OnQs3dRootLoaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is FrameworkElement root) || !IsQs3dRoot(root))
            {
                return;
            }

            SetIfDefault(root, FrameworkElement.UseLayoutRoundingProperty, true);
            SetIfDefault(root, UIElement.SnapsToDevicePixelsProperty, true);
            SetIfDefault(root, TextOptions.TextFormattingModeProperty, TextFormattingMode.Display);
        }

        private static void OnDataGridLoaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is DataGrid grid) || !IsOwnedByQs3dRoot(grid))
            {
                return;
            }

            SetIfDefault(grid, DataGrid.EnableRowVirtualizationProperty, true);
            SetIfDefault(grid, DataGrid.EnableColumnVirtualizationProperty, true);
            ApplyItemVirtualizationDefaults(grid, virtualizeWhenGrouping: true);
        }

        private static void OnListBoxLoaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is ListBox listBox) || !IsOwnedByQs3dRoot(listBox))
            {
                return;
            }

            ApplyItemVirtualizationDefaults(listBox, virtualizeWhenGrouping: true);
        }

        private static void OnTreeViewLoaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is TreeView treeView) || !IsOwnedByQs3dRoot(treeView))
            {
                return;
            }

            ApplyItemVirtualizationDefaults(treeView, virtualizeWhenGrouping: false);
        }

        private static void ApplyItemVirtualizationDefaults(
            ItemsControl control,
            bool virtualizeWhenGrouping)
        {
            SetIfDefault(control, ScrollViewer.CanContentScrollProperty, true);
            SetIfDefault(control, VirtualizingPanel.IsVirtualizingProperty, true);
            SetIfDefault(
                control,
                VirtualizingPanel.VirtualizationModeProperty,
                VirtualizationMode.Recycling);

            if (virtualizeWhenGrouping)
            {
                SetIfDefault(
                    control,
                    VirtualizingPanel.IsVirtualizingWhenGroupingProperty,
                    true);
            }
        }

        private static bool IsQs3dRoot(FrameworkElement element)
        {
            return (element is Window || element is UserControl)
                && element.GetType().Assembly == typeof(ProductionUiPolish).Assembly;
        }

        private static bool IsOwnedByQs3dRoot(DependencyObject target)
        {
            DependencyObject current = target;

            while (current != null)
            {
                if (current is FrameworkElement element && IsQs3dRoot(element))
                {
                    return true;
                }

                if (current is Visual)
                {
                    current = VisualTreeHelper.GetParent(current);
                }
                else
                {
                    current = LogicalTreeHelper.GetParent(current);
                }
            }

            return false;
        }

        private static void SetIfDefault(
            DependencyObject target,
            DependencyProperty property,
            object value)
        {
            ValueSource source = DependencyPropertyHelper.GetValueSource(target, property);
            if (source.BaseValueSource != BaseValueSource.Default || source.IsExpression)
            {
                return;
            }

            if (!Equals(target.GetValue(property), value))
            {
                target.SetValue(property, value);
            }
        }
    }
}
