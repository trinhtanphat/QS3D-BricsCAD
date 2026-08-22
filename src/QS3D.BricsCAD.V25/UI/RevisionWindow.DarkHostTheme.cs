using System.Windows;
using System.Windows.Media;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class RevisionWindow
    {
        private static readonly bool _revisionDarkHostGuardRegistered = RegisterRevisionDarkHostGuard();
        private bool _revisionDarkHostApplied;

        private static bool RegisterRevisionDarkHostGuard()
        {
            EventManager.RegisterClassHandler(typeof(RevisionWindow), FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnRevisionDarkHostLoaded), true);
            return true;
        }

        private static void OnRevisionDarkHostLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is RevisionWindow window) window.ApplyRevisionDarkHostTheme();
        }

        private void ApplyRevisionDarkHostTheme()
        {
            if (_revisionDarkHostApplied) return;
            _revisionDarkHostApplied = true;
            if (TryFindResource("BgSelectedBrush") is Brush bg)
            {
                PinRevisionSelectionResource(SystemColors.HighlightBrushKey, bg);
                PinRevisionSelectionResource(SystemColors.InactiveSelectionHighlightBrushKey, bg);
            }
            if (TryFindResource("TextBrush") is Brush fg)
            {
                PinRevisionSelectionResource(SystemColors.HighlightTextBrushKey, fg);
                PinRevisionSelectionResource(SystemColors.InactiveSelectionHighlightTextBrushKey, fg);
            }
        }

        private void PinRevisionSelectionResource(object key, Brush brush)
        {
            Resources[key] = brush;
            Grid.Resources[key] = brush;
            SemanticGrid.Resources[key] = brush;
        }
    }
}
