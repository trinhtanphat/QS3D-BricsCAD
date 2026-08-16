using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Bounded BLT3D-reference surface for THIẾT LẬP DỰ ÁN → Thuộc tính dự án.
    /// The supplied owner reference explicitly marks this surface as not built yet, so this
    /// window intentionally stays read-only and does not invent ProjectState persistence fields.
    /// </summary>
    public sealed class ProjectPropertiesWindow : Window
    {
        private const string PlaceholderText = "(Chưa xây dựng — Thuộc tính dự án)";

        public ProjectPropertiesWindow()
        {
            Title = "QS3D — Thuộc tính dự án";
            Width = 900;
            Height = 620;
            MinWidth = 640;
            MinHeight = 420;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = new SolidColorBrush(Color.FromRgb(20, 20, 20));
            Content = BuildContent();
        }

        private static UIElement BuildContent()
        {
            var root = new Grid
            {
                Background = new SolidColorBrush(Color.FromRgb(20, 20, 20))
            };

            root.Children.Add(new TextBlock
            {
                Text = PlaceholderText,
                Foreground = new SolidColorBrush(Color.FromRgb(190, 190, 190)),
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center
            });

            return root;
        }
    }
}
