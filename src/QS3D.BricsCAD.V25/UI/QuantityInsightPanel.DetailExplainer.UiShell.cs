using System.Windows;
using System.Windows.Controls;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class QuantityInsightPanel
    {
        private Border BuildQuantityDetailCard()
        {
            var card = new Border
            {
                BorderThickness = new Thickness(1d),
                CornerRadius = new CornerRadius(5d),
                Padding = new Thickness(8d),
                Margin = new Thickness(0d, 6d, 0d, 0d),
                MaxHeight = 335d
            };
            card.SetResourceReference(Border.BackgroundProperty, "Bg1Brush");
            card.SetResourceReference(Border.BorderBrushProperty, "BorderBrush");
            var root = new StackPanel();
            card.Child = root;
            BuildQuantityDetailHeader(root);
            _quantityDetailEmptyHint = CaptionText(true);
            _quantityDetailEmptyHint.Margin = new Thickness(0d, 4d, 0d, 4d);
            root.Children.Add(_quantityDetailEmptyHint);
            BuildQuantityDetailBody(root);
            return card;
        }

        private TextBlock CaptionText(bool wrap = false)
        {
            var text = new TextBlock { FontSize = 10d, TextWrapping = wrap ? TextWrapping.Wrap : TextWrapping.NoWrap };
            text.SetResourceReference(TextBlock.ForegroundProperty, "MutedBrush");
            return text;
        }
    }
}
