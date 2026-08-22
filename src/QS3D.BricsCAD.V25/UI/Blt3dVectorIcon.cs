using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Small original vector glyphs used by the BLT3D-reference presentation layer.
    /// They intentionally avoid external/proprietary icon assets and keep Button.Content as the
    /// original string so existing FindButton/content routing remains valid.
    /// </summary>
    internal static class Blt3dVectorIcon
    {
        internal const string Add = "M5,0 L7,0 L7,5 L12,5 L12,7 L7,7 L7,12 L5,12 L5,7 L0,7 L0,5 L5,5 Z";
        internal const string Delete = "M2,3 L10,3 L9,12 L3,12 Z M1,1 L4,1 L4,0 L8,0 L8,1 L11,1 L11,3 L1,3 Z";
        internal const string Bolt = "M7,0 L2,7 L5,7 L4,12 L10,5 L7,5 Z";
        internal const string FolderOpen = "M0,3 L4,3 L5,4 L12,4 L10,11 L1,11 Z M1,1 L5,1 L6,3 L1,3 Z";
        internal const string Reload = "M6,1 C3,1 1,3 1,6 C1,9 3,11 6,11 C8,11 10,10 11,8 L9,8 C8,9 7,9 6,9 C4,9 3,8 3,6 C3,4 4,3 6,3 C7,3 8,3 9,4 L7,4 L10,7 L12,4 L11,4 C10,2 8,1 6,1 Z";
        internal const string Move = "M5,0 L7,0 L7,3 L9,3 L6,6 L3,3 L5,3 Z M5,9 L3,9 L6,6 L9,9 L7,9 L7,12 L5,12 Z M0,5 L3,5 L3,3 L6,6 L3,9 L3,7 L0,7 Z M12,5 L9,5 L9,3 L6,6 L9,9 L9,7 L12,7 Z";
        internal const string Zoom = "M5,1 C2.8,1 1,2.8 1,5 C1,7.2 2.8,9 5,9 C6.1,9 7.1,8.6 7.8,7.9 L11.5,11.6 L12.5,10.6 L8.8,6.9 C9.2,6.3 9,5.6 9,5 C9,2.8 7.2,1 5,1 Z M5,3 C6.1,3 7,3.9 7,5 C7,6.1 6.1,7 5,7 C3.9,7 3,6.1 3,5 C3,3.9 3.9,3 5,3 Z";
        internal const string Eye = "M0,6 C2,2 4,1 6,1 C8,1 10,2 12,6 C10,10 8,11 6,11 C4,11 2,10 0,6 Z M6,3 C4.3,3 3,4.3 3,6 C3,7.7 4.3,9 6,9 C7.7,9 9,7.7 9,6 C9,4.3 7.7,3 6,3 Z M6,4.5 C6.8,4.5 7.5,5.2 7.5,6 C7.5,6.8 6.8,7.5 6,7.5 C5.2,7.5 4.5,6.8 4.5,6 C4.5,5.2 5.2,4.5 6,4.5 Z";
        internal const string EyeOff = "M1,1 L11,11 L12,10 L2,0 Z M0,6 C1,4 2,3 3,2 L4.5,3.5 C3.6,4 3,4.9 3,6 C3,7.7 4.3,9 6,9 C7.1,9 8,8.4 8.5,7.5 L10,9 C8.7,10.4 7.3,11 6,11 C4,11 2,10 0,6 Z M7.5,3.2 C8.1,3.6 8.7,4.2 9,5 L12,6 C11.5,4.8 10.7,3.7 9.8,2.9 Z";
        internal const string Invert = "M2,2 L5,2 L5,0 L8,3 L5,6 L5,4 L2,4 C1.4,4 1,4.4 1,5 L1,6 L0,6 L0,5 C0,3.3 0.8,2 2,2 Z M10,10 L7,10 L7,12 L4,9 L7,6 L7,8 L10,8 C10.6,8 11,7.6 11,7 L11,6 L12,6 L12,7 C12,8.7 11.2,10 10,10 Z";
        internal const string Clear = "M1,2 L2,1 L6,5 L10,1 L11,2 L7,6 L11,10 L10,11 L6,7 L2,11 L1,10 L5,6 Z";
        internal const string Grid = "M0,0 L5,0 L5,5 L0,5 Z M7,0 L12,0 L12,5 L7,5 Z M0,7 L5,7 L5,12 L0,12 Z M7,7 L12,7 L12,12 L7,12 Z";
        internal const string Room = "M1,1 L11,1 L11,11 L1,11 Z M3,3 L9,3 L9,9 L3,9 Z";
        internal const string Beam = "M0,4 L12,4 L12,8 L0,8 Z";
        internal const string Slab = "M0,5 L12,5 L12,8 L0,8 Z M2,3 L10,3 L10,4 L2,4 Z";
        internal const string Column = "M4,0 L8,0 L8,12 L4,12 Z M2,0 L10,0 L10,2 L2,2 Z M2,10 L10,10 L10,12 L2,12 Z";
        internal const string Wall = "M1,1 L11,1 L11,11 L1,11 Z M3,3 L5,3 L5,9 L3,9 Z M7,3 L9,3 L9,9 L7,9 Z";
        internal const string Door = "M2,0 L9,0 L9,12 L2,12 Z M4,2 L7,2 L7,10 L4,10 Z M6,6 L6.8,6 L6.8,6.8 L6,6.8 Z";
        internal const string Stair = "M0,10 L3,10 L3,8 L6,8 L6,6 L9,6 L9,4 L12,4 L12,12 L0,12 Z";
        internal const string Foundation = "M2,0 L10,0 L10,4 L8,4 L8,8 L12,8 L12,12 L0,12 L0,8 L4,8 L4,4 L2,4 Z";
        internal const string Earth = "M0,8 L3,5 L5,7 L8,3 L12,8 L12,12 L0,12 Z";
        internal const string Steel = "M1,0 L11,0 L11,2 L7,2 L7,10 L11,10 L11,12 L1,12 L1,10 L5,10 L5,2 L1,2 Z";
        internal const string Other = "M0,0 L5,0 L5,5 L0,5 Z M7,0 L12,0 L12,5 L7,5 Z M0,7 L5,7 L5,12 L0,12 Z M7,7 L12,7 L12,12 L7,12 Z";

        internal static void Apply(Button? button, string geometryData, double size = 12d)
        {
            if (button == null || string.IsNullOrWhiteSpace(geometryData)) return;
            button.ContentTemplate = CreateTemplate(geometryData, size, typeof(Button));
        }

        internal static void Apply(TreeViewItem? item, string geometryData, double size = 12d)
        {
            if (item == null || string.IsNullOrWhiteSpace(geometryData)) return;
            item.HeaderTemplate = CreateTemplate(geometryData, size, typeof(TreeViewItem));
        }

        private static DataTemplate CreateTemplate(string geometryData, double size, System.Type ancestorType)
        {
            var stack = new FrameworkElementFactory(typeof(StackPanel));
            stack.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
            stack.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);

            var path = new FrameworkElementFactory(typeof(Path));
            path.SetValue(Path.DataProperty, Geometry.Parse(geometryData));
            path.SetValue(FrameworkElement.WidthProperty, size);
            path.SetValue(FrameworkElement.HeightProperty, size);
            path.SetValue(Shape.StretchProperty, Stretch.Uniform);
            path.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 5, 0));
            path.SetBinding(
                Shape.FillProperty,
                new Binding("Foreground")
                {
                    RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, ancestorType, 1)
                });
            stack.AppendChild(path);

            var label = new FrameworkElementFactory(typeof(TextBlock));
            label.SetBinding(TextBlock.TextProperty, new Binding("."));
            label.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            stack.AppendChild(label);

            return new DataTemplate { VisualTree = stack };
        }
    }
}
