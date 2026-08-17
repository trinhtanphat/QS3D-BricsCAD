using System;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace QS3D.BricsCAD.V25.Ribbon
{
    /// <summary>
    /// Compact WPF-vector rendering of the current repository-owned QS3D red-X / green-V mark
    /// from assets/branding/qs3d-logo.svg. The Ribbon variant omits the wordmark because text is
    /// not legible at 16-32 px, while preserving the exact red/green identity on a dark QS3D tile.
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
            // Keep the compact host-facing brand glyph aligned with the canonical SVG and the
            // Workspace mark: X = #E84A4A, V/check = #52BE6C. The dark tile/border remain
            // deliberately neutral so the status pair stays recognizable after 16 px downsample.
            var background = FrozenBrush(Color.FromRgb(18, 24, 34));
            var border = FrozenBrush(Color.FromRgb(57, 69, 86));
            var statusRed = FrozenBrush(Color.FromRgb(232, 74, 74));
            var statusGreen = FrozenBrush(Color.FromRgb(82, 190, 108));

            var group = new DrawingGroup();
            var borderPen = new Pen(border, 0.8)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round,
                LineJoin = PenLineJoin.Round
            };
            borderPen.Freeze();

            var tile = new RectangleGeometry(new Rect(1, 1, 30, 30), 5.5, 5.5);
            tile.Freeze();
            group.Children.Add(new GeometryDrawing(background, borderPen, tile));

            // Original QS3D compact identity: a saturated red X paired with a green V/check.
            // The paths intentionally have generous separation so both remain distinct at 16 px.
            group.Children.Add(Stroke(
                statusRed,
                3.4,
                Geometry.Parse("M6.5,8 L13.5,19 M13.5,8 L6.5,19")));
            group.Children.Add(Stroke(
                statusGreen,
                3.6,
                Geometry.Parse("M17,14 L21,19 L27,8")));

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
