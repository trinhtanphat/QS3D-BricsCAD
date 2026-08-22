using System.Windows;
using System.Windows.Media;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Presentation-only host-theme guard for Zone Manager selection chrome.
    /// </summary>
    public partial class ZoneManagerWindow
    {
        private static readonly bool _zoneDarkHostGuardRegistered = RegisterZoneDarkHostGuard();
        private bool _zoneDarkHostApplied;

        private static bool RegisterZoneDarkHostGuard()
        {
            EventManager.RegisterClassHandler(
                typeof(ZoneManagerWindow),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnZoneDarkHostLoaded),
                true);
            return true;
        }

        private static void OnZoneDarkHostLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is ZoneManagerWindow window)
                window.ApplyZoneDarkHostTheme();
        }

        private void ApplyZoneDarkHostTheme()
        {
            if (_zoneDarkHostApplied)
                return;

            _zoneDarkHostApplied = true;

            if (TryFindResource("BgSelectedBrush") is Brush selectionBrush)
            {
                PinZoneSelectionResource(SystemColors.HighlightBrushKey, selectionBrush);
                PinZoneSelectionResource(SystemColors.InactiveSelectionHighlightBrushKey, selectionBrush);
            }

            if (TryFindResource("TextBrush") is Brush selectionTextBrush)
            {
                PinZoneSelectionResource(SystemColors.HighlightTextBrushKey, selectionTextBrush);
                PinZoneSelectionResource(SystemColors.InactiveSelectionHighlightTextBrushKey, selectionTextBrush);
            }
        }

        private void PinZoneSelectionResource(object key, Brush brush)
        {
            Resources[key] = brush;
            ZoneList.Resources[key] = brush;
        }
    }
}
