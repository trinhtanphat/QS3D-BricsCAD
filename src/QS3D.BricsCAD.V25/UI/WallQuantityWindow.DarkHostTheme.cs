using System.Windows;
using System.Windows.Media;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class WallQuantityWindow
    {
        private static readonly bool _wallQuantityDarkHostGuardRegistered = RegisterWallQuantityDarkHostGuard();
        private bool _wallQuantityDarkHostApplied;

        private static bool RegisterWallQuantityDarkHostGuard()
        {
            EventManager.RegisterClassHandler(typeof(WallQuantityWindow), FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnWallQuantityDarkHostLoaded), true);
            return true;
        }

        private static void OnWallQuantityDarkHostLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is WallQuantityWindow window) window.ApplyWallQuantityDarkHostTheme();
        }

        private void ApplyWallQuantityDarkHostTheme()
        {
            if (_wallQuantityDarkHostApplied) return;
            _wallQuantityDarkHostApplied = true;
            if (TryFindResource("BgSelectedBrush") is Brush bg)
            {
                PinWallQuantitySelectionResource(SystemColors.HighlightBrushKey, bg);
                PinWallQuantitySelectionResource(SystemColors.InactiveSelectionHighlightBrushKey, bg);
            }
            if (TryFindResource("TextBrush") is Brush fg)
            {
                PinWallQuantitySelectionResource(SystemColors.HighlightTextBrushKey, fg);
                PinWallQuantitySelectionResource(SystemColors.InactiveSelectionHighlightTextBrushKey, fg);
            }
        }

        private void PinWallQuantitySelectionResource(object key, Brush brush)
        {
            Resources[key] = brush;
            WallList.Resources[key] = brush;
            TakeoffGrid.Resources[key] = brush;
        }
    }
}
