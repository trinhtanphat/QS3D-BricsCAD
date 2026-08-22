using System.Windows;
using System.Windows.Controls;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class QuantityInsightPanel
    {
        private Grid BuildQuantityDetailMetrics()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1d, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            AddQuantityDetailMetric(grid, "gross", "Bê tông gộp");
            AddQuantityDetailMetric(grid, "deduction", "Trừ giao cắt");
            AddQuantityDetailMetric(grid, "net", "Bê tông còn");
            AddQuantityDetailMetric(grid, "formwork", "Cốp pha");
            AddQuantityDetailMetric(grid, "length", "Chiều dài");
            AddQuantityDetailMetric(grid, "outer", "Chu vi ngoài");
            AddQuantityDetailMetric(grid, "inner", "Chu vi trong");
            AddQuantityDetailMetric(grid, "door", "Diện tích cửa");
            AddQuantityDetailMetric(grid, "side", "Diện tích hông");
            AddQuantityDetailMetric(grid, "bottom", "Diện tích đáy");
            AddQuantityDetailMetric(grid, "top", "Diện tích đỉnh");
            AddQuantityDetailMetric(grid, "other", "Diện tích khác");
            AddQuantityDetailMetric(grid, "density", "Khối lượng riêng");
            AddQuantityDetailMetric(grid, "mass", "Khối lượng");
            return grid;
        }

        private void AddQuantityDetailMetric(Grid grid, string key, string label)
        {
            var row = grid.RowDefinitions.Count;
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var left = CaptionText();
            left.Text = label;
            left.Margin = new Thickness(0d, 1d, 0d, 1d);
            Grid.SetRow(left, row);
            grid.Children.Add(left);
            var right = new TextBlock { FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(8d, 1d, 0d, 1d) };
            right.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");
            Grid.SetRow(right, row);
            Grid.SetColumn(right, 1);
            grid.Children.Add(right);
            _quantityDetailValues[key] = right;
        }
    }
}
