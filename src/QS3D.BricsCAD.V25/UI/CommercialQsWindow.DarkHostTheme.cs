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
            var selectionBrush = TryFindResource("BgSelectedBrush") as Brush
                ?? new SolidColorBrush(Color.FromRgb(15, 60, 82));
            var selectionTextBrush = TryFindResource("TextBrush") as Brush
                ?? new SolidColorBrush(Color.FromRgb(230, 237, 243));

            Resources[SystemColors.HighlightBrushKey] = selectionBrush;
            Resources[SystemColors.InactiveSelectionHighlightBrushKey] = selectionBrush;
            Resources[SystemColors.HighlightTextBrushKey] = selectionTextBrush;
            Resources[SystemColors.InactiveSelectionHighlightTextBrushKey] = selectionTextBrush;
        }
    }
}
