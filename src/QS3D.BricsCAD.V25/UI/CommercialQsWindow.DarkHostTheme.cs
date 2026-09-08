using System;
using System.Windows;
using System.Windows.Media;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class CommercialQsWindow
    {
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            ApplyDarkHostSelectionResources();
        }

        private void ApplyDarkHostSelectionResources()
        {
            Resources[SystemColors.HighlightBrushKey] = FindDarkHostBrush("BgSelectedBrush", Color.FromRgb(15, 60, 82));
            Resources[SystemColors.HighlightTextBrushKey] = FindDarkHostBrush("TextPrimaryBrush", Color.FromRgb(230, 237, 243));
            Resources[SystemColors.InactiveSelectionHighlightBrushKey] = FindDarkHostBrush("BgSelectedBrush", Color.FromRgb(15, 60, 82));
            Resources[SystemColors.InactiveSelectionHighlightTextBrushKey] = FindDarkHostBrush("TextPrimaryBrush", Color.FromRgb(230, 237, 243));
        }

        private Brush FindDarkHostBrush(string key, Color fallback)
        {
            return TryFindResource(key) as Brush ?? new SolidColorBrush(fallback);
        }
    }
}
