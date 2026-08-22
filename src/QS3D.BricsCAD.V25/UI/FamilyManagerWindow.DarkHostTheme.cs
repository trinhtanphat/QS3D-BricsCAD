using System.Windows;
using System.Windows.Media;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Presentation-only host-theme guard for Family Manager collection selection.
    /// Keeps stock ListViewItem templates on QS3D dark active/inactive selection
    /// resources without changing Family or project behavior.
    /// </summary>
    public partial class FamilyManagerWindow
    {
        private static readonly bool _familyManagerDarkHostGuardRegistered = RegisterFamilyManagerDarkHostGuard();
        private bool _familyManagerDarkHostApplied;

        private static bool RegisterFamilyManagerDarkHostGuard()
        {
            EventManager.RegisterClassHandler(
                typeof(FamilyManagerWindow),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnFamilyManagerDarkHostLoaded),
                true);
            return true;
        }

        private static void OnFamilyManagerDarkHostLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is FamilyManagerWindow window)
            {
                window.ApplyFamilyManagerDarkHostTheme();
                window.ConfigureFamilyTemplateUiAndCatalog();
            }
        }

        private void ApplyFamilyManagerDarkHostTheme()
        {
            if (_familyManagerDarkHostApplied)
                return;

            _familyManagerDarkHostApplied = true;

            if (TryFindResource("BgSelectedBrush") is Brush selectionBrush)
            {
                PinFamilyManagerSelectionResource(SystemColors.HighlightBrushKey, selectionBrush);
                PinFamilyManagerSelectionResource(SystemColors.InactiveSelectionHighlightBrushKey, selectionBrush);
            }

            if (TryFindResource("TextBrush") is Brush selectionTextBrush)
            {
                PinFamilyManagerSelectionResource(SystemColors.HighlightTextBrushKey, selectionTextBrush);
                PinFamilyManagerSelectionResource(SystemColors.InactiveSelectionHighlightTextBrushKey, selectionTextBrush);
            }
        }

        private void PinFamilyManagerSelectionResource(object key, Brush brush)
        {
            Resources[key] = brush;
            FamilyList.Resources[key] = brush;
            PropertyList.Resources[key] = brush;
        }
    }
}
