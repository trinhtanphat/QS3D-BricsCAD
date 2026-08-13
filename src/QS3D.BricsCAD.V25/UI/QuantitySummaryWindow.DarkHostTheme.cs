using System.Windows;
using System.Windows.Media;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Presentation-only host-theme guard for Quantity Summary collection selection.
    /// It keeps the stock ListBox/DataGrid containers on QS3D dark active/inactive
    /// selection resources without changing quantity, Follow3D, export or CAD semantics.
    /// </summary>
    public partial class QuantitySummaryWindow
    {
        private static readonly bool _quantitySummaryDarkHostGuardRegistered = RegisterQuantitySummaryDarkHostGuard();
        private bool _quantitySummaryDarkHostApplied;

        private static bool RegisterQuantitySummaryDarkHostGuard()
        {
            EventManager.RegisterClassHandler(
                typeof(QuantitySummaryWindow),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnQuantitySummaryDarkHostLoaded),
                true);
            return true;
        }

        private static void OnQuantitySummaryDarkHostLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is QuantitySummaryWindow window)
                window.ApplyQuantitySummaryDarkHostTheme();
        }

        private void ApplyQuantitySummaryDarkHostTheme()
        {
            if (_quantitySummaryDarkHostApplied)
                return;

            _quantitySummaryDarkHostApplied = true;

            if (TryFindResource("BgSelectedBrush") is Brush selectionBrush)
            {
                PinQuantitySummarySelectionResource(SystemColors.HighlightBrushKey, selectionBrush);
                PinQuantitySummarySelectionResource(SystemColors.InactiveSelectionHighlightBrushKey, selectionBrush);
            }

            if (TryFindResource("TextBrush") is Brush selectionTextBrush)
            {
                PinQuantitySummarySelectionResource(SystemColors.HighlightTextBrushKey, selectionTextBrush);
                PinQuantitySummarySelectionResource(SystemColors.InactiveSelectionHighlightTextBrushKey, selectionTextBrush);
            }
        }

        private void PinQuantitySummarySelectionResource(object key, Brush brush)
        {
            Resources[key] = brush;
            CategoryList.Resources[key] = brush;
            QuantityGrid.Resources[key] = brush;
        }
    }
}
