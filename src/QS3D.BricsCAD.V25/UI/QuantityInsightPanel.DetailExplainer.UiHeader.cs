using System.Windows;
using System.Windows.Controls;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class QuantityInsightPanel
    {
        private void BuildQuantityDetailHeader(Panel root)
        {
            var header = new Grid { Margin = new Thickness(0d, 0d, 0d, 6d) };
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1d, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            root.Children.Add(header);
            var titles = new StackPanel();
            header.Children.Add(titles);
            var title = new TextBlock { Text = "CHI TIẾT CẤU KIỆN", FontWeight = FontWeights.SemiBold };
            title.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");
            titles.Children.Add(title);
            var subtitle = CaptionText();
            subtitle.Text = "Canonical detail • read-only";
            titles.Children.Add(subtitle);
            _quantityDetailLocateButton = new Button { Content = "Định vị cấu kiện", Margin = new Thickness(8d, 0d, 0d, 0d), IsEnabled = false };
            _quantityDetailLocateButton.SetResourceReference(FrameworkElement.StyleProperty, "DenseButton");
            _quantityDetailLocateButton.Click += OnQuantityDetailLocateClick;
            Grid.SetColumn(_quantityDetailLocateButton, 1);
            header.Children.Add(_quantityDetailLocateButton);
        }
    }
}
