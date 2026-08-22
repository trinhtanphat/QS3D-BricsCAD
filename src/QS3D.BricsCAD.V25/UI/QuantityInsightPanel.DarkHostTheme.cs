using System.Windows;
using System.Windows.Media;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Presentation-only host-theme guard for Quantity Insight. It keeps the stock
    /// TreeViewItem template on QS3D dark active/inactive selection resources without
    /// changing quantity selection, locate or project semantics.
    /// </summary>
    public partial class QuantityInsightPanel
    {
        private static readonly bool _quantityDarkHostThemeGuardRegistered = RegisterQuantityDarkHostThemeGuard();
        private bool _quantityDarkHostThemeApplied;

        private static bool RegisterQuantityDarkHostThemeGuard()
        {
            EventManager.RegisterClassHandler(
                typeof(QuantityInsightPanel),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnQuantityDarkHostThemeLoaded),
                true);
            return true;
        }

        private static void OnQuantityDarkHostThemeLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is QuantityInsightPanel panel)
                panel.ApplyQuantityDarkHostTheme();
        }

        private void ApplyQuantityDarkHostTheme()
        {
            if (_quantityDarkHostThemeApplied)
                return;

            _quantityDarkHostThemeApplied = true;

            if (TryFindResource("BgSelectedBrush") is Brush selectionBrush)
            {
                PinQuantitySelectionResource(SystemColors.HighlightBrushKey, selectionBrush);
                PinQuantitySelectionResource(SystemColors.InactiveSelectionHighlightBrushKey, selectionBrush);
            }

            if (TryFindResource("TextBrush") is Brush selectionTextBrush)
            {
                PinQuantitySelectionResource(SystemColors.HighlightTextBrushKey, selectionTextBrush);
                PinQuantitySelectionResource(SystemColors.InactiveSelectionHighlightTextBrushKey, selectionTextBrush);
            }
        }

        private void PinQuantitySelectionResource(object key, Brush brush)
        {
            Resources[key] = brush;
            QuantityTree.Resources[key] = brush;
        }
    }
}
