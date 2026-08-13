using System.Windows;
using System.Windows.Media;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class RoomFinishScheduleWindow
    {
        private static readonly bool _darkHostGuardRegistered = RegisterDarkHostGuard();
        private bool _darkHostApplied;

        private static bool RegisterDarkHostGuard()
        {
            EventManager.RegisterClassHandler(typeof(RoomFinishScheduleWindow), FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnDarkHostLoaded), true);
            return true;
        }

        private static void OnDarkHostLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is RoomFinishScheduleWindow window) window.ApplyDarkHostTheme();
        }

        private void ApplyDarkHostTheme()
        {
            if (_darkHostApplied) return;
            _darkHostApplied = true;
            if (TryFindResource("BgSelectedBrush") is Brush bg)
            {
                PinSelectionResource(SystemColors.HighlightBrushKey, bg);
                PinSelectionResource(SystemColors.InactiveSelectionHighlightBrushKey, bg);
            }
            if (TryFindResource("TextBrush") is Brush fg)
            {
                PinSelectionResource(SystemColors.HighlightTextBrushKey, fg);
                PinSelectionResource(SystemColors.InactiveSelectionHighlightTextBrushKey, fg);
            }
        }

        private void PinSelectionResource(object key, Brush brush)
        {
            Resources[key] = brush;
            ScheduleGrid.Resources[key] = brush;
        }
    }
}
