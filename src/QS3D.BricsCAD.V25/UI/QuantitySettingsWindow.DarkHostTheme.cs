using System.Windows;
using System.Windows.Media;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Presentation-only host-theme guard for Quantity Settings collection selection.
    /// Keeps stock ListBox/DataGrid containers on QS3D dark active/inactive highlight
    /// resources without changing settings, validation or persistence behavior.
    /// </summary>
    public partial class QuantitySettingsWindow
    {
        private static readonly bool _quantitySettingsDarkHostGuardRegistered = RegisterQuantitySettingsDarkHostGuard();
        private bool _quantitySettingsDarkHostApplied;

        private static bool RegisterQuantitySettingsDarkHostGuard()
        {
            EventManager.RegisterClassHandler(
                typeof(QuantitySettingsWindow),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnQuantitySettingsDarkHostLoaded),
                true);
            return true;
        }

        private static void OnQuantitySettingsDarkHostLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is QuantitySettingsWindow window)
                window.ApplyQuantitySettingsDarkHostTheme();
        }

        private void ApplyQuantitySettingsDarkHostTheme()
        {
            if (_quantitySettingsDarkHostApplied)
                return;

            _quantitySettingsDarkHostApplied = true;

            if (TryFindResource("BgSelectedBrush") is Brush selectionBrush)
            {
                PinQuantitySettingsSelectionResource(SystemColors.HighlightBrushKey, selectionBrush);
                PinQuantitySettingsSelectionResource(SystemColors.InactiveSelectionHighlightBrushKey, selectionBrush);
            }

            if (TryFindResource("TextBrush") is Brush selectionTextBrush)
            {
                PinQuantitySettingsSelectionResource(SystemColors.HighlightTextBrushKey, selectionTextBrush);
                PinQuantitySettingsSelectionResource(SystemColors.InactiveSelectionHighlightTextBrushKey, selectionTextBrush);
            }
        }

        private void PinQuantitySettingsSelectionResource(object key, Brush brush)
        {
            // Root resources cover every stock DataGrid row/cell in this window, including
            // the currently unnamed settings tables. Pin the two named ListBoxes as well so
            // already-realized intersection-category containers refresh immediately.
            Resources[key] = brush;
            PrimaryCategoryList.Resources[key] = brush;
            ReferenceCategoryList.Resources[key] = brush;
        }
    }
}
