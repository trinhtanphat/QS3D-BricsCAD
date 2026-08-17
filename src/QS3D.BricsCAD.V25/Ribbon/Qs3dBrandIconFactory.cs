using System;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace QS3D.BricsCAD.V25.Ribbon
{
    /// <summary>
    /// Compact WPF-vector rendering of the repository-owned QS3D cube mark from
    /// assets/branding/qs3d-logo.svg. The small Ribbon variant intentionally omits the
    /// QS3D/CAD wordmark because text is not legible at 16-32 px; the cube geometry and
    /// approved dark/cyan/blue palette remain recognizable while the final host-facing
    /// image is rasterized to the exact Ribbon pixel size.
    /// </summary>
    internal static class Qs3dBrandIconFactory
    {
        public static ImageSource Create(int pixelSize)
        {
            if (pixelSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(pixelSize));

            // Geometry.Parse consumes path-data decimals using the current thread culture.
            // BricsCAD runs under the user's Windows locale, so guard comma-decimal locales the
            // same way as the semantic Ribbon icon pipeline and immediately restore host culture.
            var thread = Thread.CurrentThread;
            var previousCulture = thread.CurrentCulture;
            try
            {
                thread.CurrentCulture = CultureInfo.InvariantCulture;
                return CreateCore(pixelSize);
            }
            finally
            {
                thread.CurrentCulture = previousCulture;
            }
        }

        private static ImageSource CreateCore(int pixelSize)
        {
            var background = FrozenBrush(Color.FromRgb(6, 19, 35));
            var white = FrozenBrush(Color.FromRgb(234, 247, 255));
            var blue = FrozenBrush(Color.FromRgb(22, 139, 255));
            var cyan = FrozenBrush(Color.FromRgb(51, 197, 255));
            var guide = FrozenBrush(Color.FromRgb(131, 218, 255));

            var group = new DrawingGroup();
            group.Children.Add(new GeometryDrawing(
                background,
                null,
                new RectangleGeometry(new Rect(1, 1, 30, 30), 5.5, 5.5)));

            // Top face of the isometric precision cube.
            group.Children.Add(Stroke(
                white,
                1.1,
                Geometry.Parse("M16,5.75 L24.25,10.5 16,15.25 7.75,10.5 Z")));

            // Left/right outer walls use the approved product blue from the SVG gradient family.
            group.Children.Add(Stroke(
                blue,
                1.55,
                Geometry.Parse("M7.75,10.5 L7.75,20 16,24.875 16,15.25 M24.25,10.5 L24.25,20 16,24.875")));

            // Inner cube detail keeps the recognizable QS3D precision/isometric construction.
            group.Children.Add(Stroke(
                cyan,
                0.9,
                Geometry.Parse(
                    "M10.5,12.06 L16,8.94 21.5,12.06 " +
                    "M10.5,12.06 L10.5,16.81 14.625,19.25 14.625,15.625 12.69,14.5 " +
                    "M21.5,12.06 L21.5,16.81 17.375,19.25 17.375,16.5 20.625,14.625")));

            // Very light alignment ticks from the master mark; they remain subtle at 32 px and
            // disappear naturally at 16 px after rasterization.
            var guidePen = new Pen(guide, 0.45)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round
            };
            guidePen.Freeze();
            group.Children.Add(new GeometryDrawing(
                null,
                guidePen,
                Geometry.Parse("M5.9,10.5 L8.9,10.5 M23.1,10.5 L26.1,10.5 M16,3.9 L16,6.9 M16,23.8 L16,28.1")));

            group.Freeze();

            // BricsCAD's Ribbon reliably consumes the bitmap form used by RibbonIconFactory.
            // Returning the raw DrawingImage here can make the host replace this logo with its
            // missing-image '?' placeholder, even though the vector itself is valid WPF.
            var visual = new DrawingVisual();
            using (var drawing = visual.RenderOpen())
            {
                drawing.PushTransform(new ScaleTransform(pixelSize / 32.0, pixelSize / 32.0));
                drawing.DrawDrawing(group);
                drawing.Pop();
            }

            var image = new RenderTargetBitmap(pixelSize, pixelSize, 96, 96, PixelFormats.Pbgra32);
            image.Render(visual);
            image.Freeze();
            return image;
        }

        private static SolidColorBrush FrozenBrush(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        private static GeometryDrawing Stroke(Brush brush, double thickness, Geometry geometry)
        {
            var pen = new Pen(brush, thickness)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round,
                LineJoin = PenLineJoin.Round
            };
            pen.Freeze();
            geometry.Freeze();
            return new GeometryDrawing(null, pen, geometry);
        }
    }
}
