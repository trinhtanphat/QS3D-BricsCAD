using System.Collections.Generic;
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
        }

        private static void OnQs3dRootLoaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is FrameworkElement root) || !IsQs3dRoot(root))
            {
                return;
            }

            // A QS3D Window/UserControl can contain many nested QS3D UserControls.
            // Only the outermost loaded QS3D root traverses its visual tree so each
            // control is processed once and BricsCAD-owned visual trees stay untouched.
            if (HasQs3dRootAncestor(root))
            {
                return;
            }

            ApplyDpiDefaults(root);
            ApplyVirtualizationDefaults(root);
        }

        private static void ApplyDpiDefaults(FrameworkElement root)
        {
            SetIfDefault(root, FrameworkElement.UseLayoutRoundingProperty, true);
            SetIfDefault(root, UIElement.SnapsToDevicePixelsProperty, true);
            SetIfDefault(root, TextOptions.TextFormattingModeProperty, TextFormattingMode.Display);
        }

        private static void ApplyVirtualizationDefaults(DependencyObject root)
        {
            var pending = new Stack<DependencyObject>();
            pending.Push(root);

            while (pending.Count > 0)
            {
                DependencyObject current = pending.Pop();
                ApplyControlDefaults(current);

                int childCount = VisualTreeHelper.GetChildrenCount(current);
                for (int index = childCount - 1; index >= 0; index--)
                {
                    pending.Push(VisualTreeHelper.GetChild(current, index));
                }
            }
        }

        private static void ApplyControlDefaults(DependencyObject current)
        {
            if (current is DataGrid grid)
            {
                SetIfDefault(grid, DataGrid.EnableRowVirtualizationProperty, true);
                SetIfDefault(grid, DataGrid.EnableColumnVirtualizationProperty, true);
                ApplyItemVirtualizationDefaults(grid, virtualizeWhenGrouping: true);
                return;
            }

            if (current is ListBox listBox)
            {
                ApplyItemVirtualizationDefaults(listBox, virtualizeWhenGrouping: true);
                return;
            }

            if (current is TreeView treeView)
            {
                ApplyItemVirtualizationDefaults(treeView, virtualizeWhenGrouping: false);
            }
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

        private static bool HasQs3dRootAncestor(DependencyObject target)
        {
            DependencyObject? current = GetParent(target);
            while (current != null)
            {
                if (current is FrameworkElement element && IsQs3dRoot(element))
                {
                    return true;
                }

                current = GetParent(current);
            }

            return false;
        }

        private static DependencyObject? GetParent(DependencyObject target)
        {
            if (target is Visual)
            {
                return VisualTreeHelper.GetParent(target);
            }

            return LogicalTreeHelper.GetParent(target);
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

            if (!object.Equals(target.GetValue(property), value))
            {
                target.SetValue(property, value);
            }
        }
    }
}
