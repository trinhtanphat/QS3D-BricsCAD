using System.Windows;
using System.Windows.Media;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Presentation-only host-theme guard for Material Catalog selection chrome.
    /// </summary>
    public partial class MaterialCatalogWindow
    {
        private static readonly bool _materialCatalogDarkHostGuardRegistered = RegisterMaterialCatalogDarkHostGuard();
        private bool _materialCatalogDarkHostApplied;

        private static bool RegisterMaterialCatalogDarkHostGuard()
        {
            EventManager.RegisterClassHandler(
                typeof(MaterialCatalogWindow),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnMaterialCatalogDarkHostLoaded),
                true);
            return true;
        }

        private static void OnMaterialCatalogDarkHostLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is MaterialCatalogWindow window)
                window.ApplyMaterialCatalogDarkHostTheme();
        }

        private void ApplyMaterialCatalogDarkHostTheme()
        {
            if (_materialCatalogDarkHostApplied)
                return;

            _materialCatalogDarkHostApplied = true;

            if (TryFindResource("BgSelectedBrush") is Brush selectionBrush)
            {
                PinMaterialCatalogSelectionResource(SystemColors.HighlightBrushKey, selectionBrush);
                PinMaterialCatalogSelectionResource(SystemColors.InactiveSelectionHighlightBrushKey, selectionBrush);
            }

            if (TryFindResource("TextBrush") is Brush selectionTextBrush)
            {
                PinMaterialCatalogSelectionResource(SystemColors.HighlightTextBrushKey, selectionTextBrush);
                PinMaterialCatalogSelectionResource(SystemColors.InactiveSelectionHighlightTextBrushKey, selectionTextBrush);
            }
        }

        private void PinMaterialCatalogSelectionResource(object key, Brush brush)
        {
            Resources[key] = brush;
            MaterialList.Resources[key] = brush;
        }
    }
}
