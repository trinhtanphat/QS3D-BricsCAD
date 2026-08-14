using System.Windows;
using System.Windows.Controls;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class QuantityInsightPanel
    {
        private void BuildQuantityDetailBody(Panel root)
        {
            _quantityDetailSelector = new ComboBox { Margin = new Thickness(0d, 0d, 0d, 6d), Visibility = Visibility.Collapsed };
            _quantityDetailSelector.SelectionChanged += OnQuantityDetailSelectionChanged;
            root.Children.Add(_quantityDetailSelector);
            _quantityDetailBody = new StackPanel { Visibility = Visibility.Collapsed };
            root.Children.Add(new ScrollViewer
            {
                Content = _quantityDetailBody,
                MaxHeight = 245d,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            });
            _quantityDetailContext = new TextBlock { FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap };
            _quantityDetailCount = CaptionText();
            _quantityDetailBody.Children.Add(_quantityDetailContext);
            _quantityDetailBody.Children.Add(_quantityDetailCount);
            _quantityDetailBody.Children.Add(BuildQuantityDetailMetrics());
            _quantityDetailBody.Children.Add(BuildQuantityDetailProvenance());
        }
    }
}
