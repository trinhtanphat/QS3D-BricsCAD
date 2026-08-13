using System.Windows;
using System.Windows.Media;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class StartCenterWindow
    {
        private static readonly bool _startCenterDarkHostGuardRegistered = RegisterStartCenterDarkHostGuard();
        private bool _startCenterDarkHostApplied;

        private static bool RegisterStartCenterDarkHostGuard()
        {
            EventManager.RegisterClassHandler(typeof(StartCenterWindow), FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnStartCenterDarkHostLoaded), true);
            return true;
        }

        private static void OnStartCenterDarkHostLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is StartCenterWindow window) window.ApplyStartCenterDarkHostTheme();
        }

        private void ApplyStartCenterDarkHostTheme()
        {
            if (_startCenterDarkHostApplied) return;
            _startCenterDarkHostApplied = true;

            if (TryFindResource("BgSelectedBrush") is Brush bg)
            {
                PinStartCenterSelectionResource(SystemColors.HighlightBrushKey, bg);
                PinStartCenterSelectionResource(SystemColors.InactiveSelectionHighlightBrushKey, bg);
            }
            if (TryFindResource("TextBrush") is Brush fg)
            {
                PinStartCenterSelectionResource(SystemColors.HighlightTextBrushKey, fg);
                PinStartCenterSelectionResource(SystemColors.InactiveSelectionHighlightTextBrushKey, fg);
            }
        }

        private void PinStartCenterSelectionResource(object key, Brush brush)
        {
            Resources[key] = brush;
            CommandList.Resources[key] = brush;
            FavoriteList.Resources[key] = brush;
            RecentCommandList.Resources[key] = brush;
            RecentProjectList.Resources[key] = brush;
        }
    }
}
