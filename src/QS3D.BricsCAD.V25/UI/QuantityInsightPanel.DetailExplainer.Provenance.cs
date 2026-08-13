using System.Windows;
using System.Windows.Controls;
namespace QS3D.BricsCAD.V25.UI
{
    public partial class QuantityInsightPanel
    {
        private Border BuildQuantityDetailProvenance()
        {
            var border = new Border { Margin = new Thickness(0d, 5d, 0d, 0d) };
            var stack = new StackPanel();
            border.Child = stack;
            var title = CaptionText(); title.Text = "NGUỒN / PROVENANCE"; title.FontWeight = FontWeights.SemiBold; stack.Children.Add(title);
            _quantityDetailElementIds = CaptionText(true); stack.Children.Add(_quantityDetailElementIds);
            _quantityDetailSourceHandles = CaptionText(true); stack.Children.Add(_quantityDetailSourceHandles);
            _quantityDetailDrawingFingerprint = CaptionText(true); stack.Children.Add(_quantityDetailDrawingFingerprint);
            return border;
        }
    }
}
