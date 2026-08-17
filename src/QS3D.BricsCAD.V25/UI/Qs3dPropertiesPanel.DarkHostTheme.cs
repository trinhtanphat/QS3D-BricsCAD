using System.Windows;
using System.Windows.Media;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Pins the standalone QS3D Properties collection surface to QS3D-owned dark
    /// selection resources when hosted inside a native BricsCAD PaletteSet.
    /// Presentation-only: property scope, selection and edit semantics are unchanged.
    /// </summary>
    public partial class Qs3dPropertiesPanel
    {
        private static readonly bool DarkHostThemeGuardRegistered = RegisterDarkHostThemeGuard();
        private bool _darkHostThemeGuardApplied;

        private static bool RegisterDarkHostThemeGuard()
        {
            EventManager.RegisterClassHandler(
                typeof(Qs3dPropertiesPanel),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnDarkHostThemeLoaded),
                true);
            return true;
        }

        private static void OnDarkHostThemeLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is Qs3dPropertiesPanel panel)
                panel.ApplyDarkHostThemeGuard();
        }

        private void ApplyDarkHostThemeGuard()
        {
            if (_darkHostThemeGuardApplied)
                return;

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
