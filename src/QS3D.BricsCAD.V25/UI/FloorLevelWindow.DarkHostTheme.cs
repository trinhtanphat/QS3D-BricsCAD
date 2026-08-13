using System.Windows;
using System.Windows.Media;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Presentation-only host-theme guard for Floor/Level Manager selection chrome.
    /// </summary>
    public partial class FloorLevelWindow
    {
        private static readonly bool _floorDarkHostGuardRegistered = RegisterFloorDarkHostGuard();
        private bool _floorDarkHostApplied;

        private static bool RegisterFloorDarkHostGuard()
        {
            EventManager.RegisterClassHandler(
                typeof(FloorLevelWindow),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnFloorDarkHostLoaded),
                true);
            return true;
        }

        private static void OnFloorDarkHostLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is FloorLevelWindow window)
                window.ApplyFloorDarkHostTheme();
        }

        private void ApplyFloorDarkHostTheme()
        {
            if (_floorDarkHostApplied)
                return;

            _floorDarkHostApplied = true;

            if (TryFindResource("BgSelectedBrush") is Brush selectionBrush)
            {
                PinFloorSelectionResource(SystemColors.HighlightBrushKey, selectionBrush);
                PinFloorSelectionResource(SystemColors.InactiveSelectionHighlightBrushKey, selectionBrush);
            }

            if (TryFindResource("TextBrush") is Brush selectionTextBrush)
            {
                PinFloorSelectionResource(SystemColors.HighlightTextBrushKey, selectionTextBrush);
                PinFloorSelectionResource(SystemColors.InactiveSelectionHighlightTextBrushKey, selectionTextBrush);
            }
        }

        private void PinFloorSelectionResource(object key, Brush brush)
        {
            Resources[key] = brush;
            FloorList.Resources[key] = brush;
        }
    }
}
