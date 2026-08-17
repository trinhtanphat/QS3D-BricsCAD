using System.Windows;
using System.Windows.Media;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Pins the dedicated QS3D Properties palette to QS3D-owned dark selection resources
    /// when BricsCAD supplies brighter active or inactive host selection brushes.
    /// Presentation-only: semantic property edit behavior remains owned by WorkspaceViewModel.
    /// </summary>
    public partial class PropertiesPanel
    {
        private static readonly bool DarkHostThemeGuardRegistered = RegisterDarkHostThemeGuard();
        private bool _darkHostThemeGuardApplied;

        private static bool RegisterDarkHostThemeGuard()
        {
            EventManager.RegisterClassHandler(
                typeof(PropertiesPanel),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnDarkHostThemeLoaded),
                true);
            return true;
        }

        private static void OnDarkHostThemeLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is PropertiesPanel panel)
                panel.ApplyDarkHostThemeGuard();
        }

        private void ApplyDarkHostThemeGuard()
        {
            if (_darkHostThemeGuardApplied) return;
            _darkHostThemeGuardApplied = true;

            if (TryFindResource("BgSelectedBrush") is Brush selectionBrush)
            {
                Resources[SystemColors.HighlightBrushKey] = selectionBrush;
                Resources[SystemColors.InactiveSelectionHighlightBrushKey] = selectionBrush;
            }

            if (TryFindResource("TextBrush") is Brush selectionTextBrush)
            {
                Resources[SystemColors.HighlightTextBrushKey] = selectionTextBrush;
                Resources[SystemColors.InactiveSelectionHighlightTextBrushKey] = selectionTextBrush;
            }
        }
    }
}
