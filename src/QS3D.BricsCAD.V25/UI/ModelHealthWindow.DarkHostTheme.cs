using System.Windows;
using System.Windows.Media;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Presentation-only host-theme guard for Model Health issue-grid selection.
    /// </summary>
    public partial class ModelHealthWindow
    {
        private static readonly bool _modelHealthDarkHostGuardRegistered = RegisterModelHealthDarkHostGuard();
        private bool _modelHealthDarkHostApplied;

        private static bool RegisterModelHealthDarkHostGuard()
        {
            EventManager.RegisterClassHandler(
                typeof(ModelHealthWindow),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnModelHealthDarkHostLoaded),
                true);
            return true;
        }

        private static void OnModelHealthDarkHostLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is ModelHealthWindow window)
                window.ApplyModelHealthDarkHostTheme();
        }

        private void ApplyModelHealthDarkHostTheme()
        {
            if (_modelHealthDarkHostApplied)
                return;

            _modelHealthDarkHostApplied = true;

            if (TryFindResource("BgSelectedBrush") is Brush selectionBrush)
            {
                PinModelHealthSelectionResource(SystemColors.HighlightBrushKey, selectionBrush);
                PinModelHealthSelectionResource(SystemColors.InactiveSelectionHighlightBrushKey, selectionBrush);
            }

            if (TryFindResource("TextBrush") is Brush selectionTextBrush)
            {
                PinModelHealthSelectionResource(SystemColors.HighlightTextBrushKey, selectionTextBrush);
                PinModelHealthSelectionResource(SystemColors.InactiveSelectionHighlightTextBrushKey, selectionTextBrush);
            }
        }

        private void PinModelHealthSelectionResource(object key, Brush brush)
        {
            Resources[key] = brush;
            IssueGrid.Resources[key] = brush;
        }
    }
}
