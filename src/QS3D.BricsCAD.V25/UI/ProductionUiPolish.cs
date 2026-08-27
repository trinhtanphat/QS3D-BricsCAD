using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Applies production-safe WPF defaults that remain safe after the host has started layout.
    ///
    /// Virtualization policy is intentionally not mutated here. This class runs from a Loaded
    /// handler, and WPF forbids changing VirtualizationMode after an ItemsHost has measured.
    /// Item virtualization therefore belongs to Theme.xaml or to explicit pre-layout construction
    /// for dynamically created controls.
    /// </summary>
    internal static class ProductionUiPolish
    {
        private static int _registered;

        internal static void EnsureRegistered()
        {
            if (Interlocked.CompareExchange(ref _registered, 1, 0) != 0)
            {
                return;
            }

            try
            {
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
            catch
            {
                // Registration is process-wide. If WPF is not ready yet, allow the
                // next QS3D initialization attempt to retry instead of latching a
                // permanently half-initialized UI state.
                Interlocked.Exchange(ref _registered, 0);
                throw;
            }
        }

        private static void OnQs3dRootLoaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is FrameworkElement root) || !IsQs3dRoot(root))
            {
                return;
            }

            // A QS3D Window/UserControl can contain many nested QS3D UserControls.
            // Only the outermost loaded QS3D root applies visual polish, while nested roots still
            // get a localization refresh because their content may have been materialized later.
            if (HasQs3dRootAncestor(root))
            {
                UiLocalization.Apply(root);
                return;
            }

            ApplyDpiDefaults(root);
            UiLocalization.RegisterAndApply(root);
        }

        private static void ApplyDpiDefaults(FrameworkElement root)
        {
            SetIfDefault(root, FrameworkElement.UseLayoutRoundingProperty, true);
            SetIfDefault(root, UIElement.SnapsToDevicePixelsProperty, true);
            SetIfDefault(root, TextOptions.TextFormattingModeProperty, TextFormattingMode.Display);
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
