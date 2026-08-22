using System.Windows;
using System.Windows.Media;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Presentation-only host-theme guard for Audit Log grid selection.
    /// </summary>
    public partial class AuditLogWindow
    {
        private static readonly bool _auditLogDarkHostGuardRegistered = RegisterAuditLogDarkHostGuard();
        private bool _auditLogDarkHostApplied;

        private static bool RegisterAuditLogDarkHostGuard()
        {
            EventManager.RegisterClassHandler(
                typeof(AuditLogWindow),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnAuditLogDarkHostLoaded),
                true);
            return true;
        }

        private static void OnAuditLogDarkHostLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is AuditLogWindow window)
                window.ApplyAuditLogDarkHostTheme();
        }

        private void ApplyAuditLogDarkHostTheme()
        {
            if (_auditLogDarkHostApplied)
                return;

            _auditLogDarkHostApplied = true;

            if (TryFindResource("BgSelectedBrush") is Brush selectionBrush)
            {
                PinAuditLogSelectionResource(SystemColors.HighlightBrushKey, selectionBrush);
                PinAuditLogSelectionResource(SystemColors.InactiveSelectionHighlightBrushKey, selectionBrush);
            }

            if (TryFindResource("TextBrush") is Brush selectionTextBrush)
            {
                PinAuditLogSelectionResource(SystemColors.HighlightTextBrushKey, selectionTextBrush);
                PinAuditLogSelectionResource(SystemColors.InactiveSelectionHighlightTextBrushKey, selectionTextBrush);
            }
        }

        private void PinAuditLogSelectionResource(object key, Brush brush)
        {
            Resources[key] = brush;
            Grid.Resources[key] = brush;
        }
    }
}
