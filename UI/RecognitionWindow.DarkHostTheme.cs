using System.Windows;
using System.Windows.Media;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class RecognitionWindow
    {
        private static readonly bool _recognitionDarkHostGuardRegistered = RegisterRecognitionDarkHostGuard();
        private bool _recognitionDarkHostApplied;

        private static bool RegisterRecognitionDarkHostGuard()
        {
            EventManager.RegisterClassHandler(typeof(RecognitionWindow), FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnRecognitionDarkHostLoaded), true);
            return true;
        }

        private static void OnRecognitionDarkHostLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is RecognitionWindow window) window.ApplyRecognitionDarkHostTheme();
        }

        private void ApplyRecognitionDarkHostTheme()
        {
            if (_recognitionDarkHostApplied) return;
            _recognitionDarkHostApplied = true;
            if (TryFindResource("BgSelectedBrush") is Brush bg)
            {
                PinRecognitionSelectionResource(SystemColors.HighlightBrushKey, bg);
                PinRecognitionSelectionResource(SystemColors.InactiveSelectionHighlightBrushKey, bg);
            }
            if (TryFindResource("TextBrush") is Brush fg)
            {
                PinRecognitionSelectionResource(SystemColors.HighlightTextBrushKey, fg);
                PinRecognitionSelectionResource(SystemColors.InactiveSelectionHighlightTextBrushKey, fg);
            }
        }

        private void PinRecognitionSelectionResource(object key, Brush brush)
        {
            Resources[key] = brush;
            Grid.Resources[key] = brush;
        }
    }
}
