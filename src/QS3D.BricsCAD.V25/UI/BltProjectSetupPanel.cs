using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Minimal owner-reference Project Information canvas. It intentionally stays presentation-only:
    /// no project creation, mutation, regeneration, save or read-side binding occurs here.
    /// </summary>
    internal sealed class BltProjectSetupPanel : UserControl
    {
        public const string ProjectPlaceholderText =
            "(Chưa xây dựng — Thông tin dự án / Thuộc tính dự án)";

        private readonly TextBlock _placeholder;

        public BltProjectSetupPanel()
        {
            SnapsToDevicePixels = true;
            UseLayoutRounding = true;
            Background = new SolidColorBrush(Color.FromRgb(20, 20, 20));

            _placeholder = new TextBlock
            {
                Text = ProjectPlaceholderText,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 13,
                FontWeight = FontWeights.Normal,
                Foreground = new SolidColorBrush(Color.FromRgb(174, 174, 174)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(24)
            };

            var root = new Grid
            {
                Background = Background
            };
            root.Children.Add(_placeholder);
            Content = root;
        }

        public void ShowProjectInformation()
        {
            _placeholder.Text = ProjectPlaceholderText;
        }
    }
}
