using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace QS3D.BricsCAD.V25.Ribbon
{
    internal enum RibbonIconKind
    {
        OpenProject,
        Save,
        SaveAs,
        Settings,
        Objects,
        Qs3dLogo,
        Update,
        UpdateOnClose,
        UpdateStatus
    }

    internal static class RibbonIconFactory
    {
        public static ImageSource Create(RibbonIconKind kind, int pixelSize)
        {
            if (pixelSize <= 0) throw new ArgumentOutOfRangeException(nameof(pixelSize));

            var accent = FrozenBrush(34, 137, 245);
            var accentDark = FrozenBrush(13, 77, 172);
            var light = FrozenBrush(224, 238, 255);
            var dark = FrozenBrush(31, 42, 58);
            var green = FrozenBrush(53, 196, 122);

            var group = new DrawingGroup();
            switch (kind)
            {
                case RibbonIconKind.OpenProject:
                    group.Children.Add(Fill(accentDark, new RectangleGeometry(new Rect(3, 7, 13, 8), 2, 2)));
                    group.Children.Add(Fill(accent, Geometry.Parse("M3,11 L12,11 15,8 29,8 25,27 3,27 Z")));
                    group.Children.Add(Fill(light, new RectangleGeometry(new Rect(7, 15, 14, 8), 1, 1)));
                    group.Children.Add(Stroke(accentDark, 1.4, new RectangleGeometry(new Rect(7, 15, 14, 8), 1, 1)));
                    break;

                case RibbonIconKind.Save:
                    group.Children.Add(Fill(accent, new RectangleGeometry(new Rect(4, 3, 24, 26), 2, 2)));
                    group.Children.Add(Fill(dark, new RectangleGeometry(new Rect(9, 4, 13, 8), 1, 1)));
                    group.Children.Add(Fill(light, new RectangleGeometry(new Rect(9, 17, 14, 9), 1, 1)));
                    group.Children.Add(Fill(accentDark, new RectangleGeometry(new Rect(18, 5, 3, 5))));
                    break;

                case RibbonIconKind.SaveAs:
                    group.Children.Add(Fill(accent, new RectangleGeometry(new Rect(3, 3, 22, 25), 2, 2)));
                    group.Children.Add(Fill(dark, new RectangleGeometry(new Rect(8, 4, 12, 7), 1, 1)));
                    group.Children.Add(Fill(light, new RectangleGeometry(new Rect(8, 16, 12, 9), 1, 1)));
                    group.Children.Add(Fill(accentDark, new EllipseGeometry(new Point(25, 23), 7, 7)));
                    group.Children.Add(Stroke(Brushes.White, 2.2, new LineGeometry(new Point(21, 23), new Point(29, 23))));
                    group.Children.Add(Stroke(Brushes.White, 2.2, new LineGeometry(new Point(25, 19), new Point(25, 27))));
                    break;

                case RibbonIconKind.Settings:
                    group.Children.Add(Fill(accent, new EllipseGeometry(new Point(16, 16), 9, 9)));
                    group.Children.Add(Fill(dark, new EllipseGeometry(new Point(16, 16), 4, 4)));
                    for (var i = 0; i < 8; i++)
                    {
                        var angle = i * Math.PI / 4.0;
                        var x1 = 16 + Math.Cos(angle) * 9;
                        var y1 = 16 + Math.Sin(angle) * 9;
                        var x2 = 16 + Math.Cos(angle) * 13;
                        var y2 = 16 + Math.Sin(angle) * 13;
                        group.Children.Add(Stroke(accent, 4, new LineGeometry(new Point(x1, y1), new Point(x2, y2))));
                    }
                    break;

                case RibbonIconKind.Objects:
                    group.Children.Add(Fill(accent, new EllipseGeometry(new Point(9, 9), 5, 5)));
                    group.Children.Add(Fill(light, new EllipseGeometry(new Point(23, 9), 5, 5)));
                    group.Children.Add(Fill(accentDark, new EllipseGeometry(new Point(9, 23), 5, 5)));
                    group.Children.Add(Fill(accent, new EllipseGeometry(new Point(23, 23), 5, 5)));
                    group.Children.Add(Stroke(accentDark, 1.5, new RectangleGeometry(new Rect(3, 3, 26, 26), 2, 2)));
                    break;

                case RibbonIconKind.Qs3dLogo:
                    var logoGroup = new DrawingGroup();
                    var brandBlue = FrozenBrush(0, 90, 158);
                    var brandGreen = FrozenBrush(54, 165, 54);
                    logoGroup.Children.Add(Fill(
                        brandBlue,
                        Geometry.Parse("M22.2,10.5 L22.2,44.6 L25.75,46.65 L25.75,14.6 L57.7,33.05 L57.7,25.6 L26,7.3 Z")));
                    logoGroup.Children.Add(Fill(
                        brandGreen,
                        Geometry.Parse("M28.95,16.45 L28.95,48.5 L32.5,50.55 L32.5,34 L43.2,40.15 L43.2,36.05 L28.95,27.85 L28.95,20.6 L54.3,35.25 L54.3,46.75 L37.2,36.9 L33.65,40.95 L57.85,54.9 L57.85,33.2 Z")));
                    logoGroup.Transform = new MatrixTransform(
                        0.5714285714,
                        0,
                        0,
                        0.5714285714,
                        -6.8857142851,
                        -1.7714285712);
                    group.Children.Add(logoGroup);
                    break;

                case RibbonIconKind.Update:
                    group.Children.Add(Stroke(accent, 4, Geometry.Parse("M7,13 C9,6 17,3 24,7 C27,9 29,12 29,16")));
                    group.Children.Add(Fill(accent, Geometry.Parse("M24,3 L30,8 23,11 Z")));
                    group.Children.Add(Stroke(accentDark, 4, Geometry.Parse("M25,19 C23,26 15,29 8,25 C5,23 3,20 3,16")));
                    group.Children.Add(Fill(accentDark, Geometry.Parse("M8,29 L2,24 9,21 Z")));
                    break;

                case RibbonIconKind.UpdateOnClose:
                    group.Children.Add(Fill(accent, new EllipseGeometry(new Point(13, 16), 10, 10)));
                    group.Children.Add(Stroke(Brushes.White, 2, new LineGeometry(new Point(13, 10), new Point(13, 17))));
                    group.Children.Add(Stroke(Brushes.White, 2, new LineGeometry(new Point(13, 17), new Point(18, 20))));
                    group.Children.Add(Fill(green, new EllipseGeometry(new Point(24, 22), 6, 6)));
                    group.Children.Add(Stroke(Brushes.White, 2, Geometry.Parse("M21,22 L23,24 27,19")));
                    break;

                case RibbonIconKind.UpdateStatus:
                    group.Children.Add(Fill(accentDark, new RectangleGeometry(new Rect(4, 5, 24, 22), 3, 3)));
                    group.Children.Add(Fill(light, new RectangleGeometry(new Rect(8, 9, 16, 3), 1, 1)));
                    group.Children.Add(Fill(accent, new RectangleGeometry(new Rect(8, 15, 12, 3), 1, 1)));
                    group.Children.Add(Fill(green, new EllipseGeometry(new Point(22, 22), 4, 4)));
                    break;
            }

            group.Freeze();
            var visual = new DrawingVisual();
            using (var drawing = visual.RenderOpen())
            {
                drawing.PushTransform(new ScaleTransform(pixelSize / 32.0, pixelSize / 32.0));
                drawing.DrawDrawing(group);
                drawing.Pop();
            }

            var bitmap = new RenderTargetBitmap(pixelSize, pixelSize, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(visual);
            bitmap.Freeze();
            return bitmap;
        }

        private static SolidColorBrush FrozenBrush(byte r, byte g, byte b)
        {
            var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
            brush.Freeze();
            return brush;
        }

        private static GeometryDrawing Fill(Brush brush, Geometry geometry) => new GeometryDrawing(brush, null, geometry);

        private static GeometryDrawing Stroke(Brush brush, double thickness, Geometry geometry)
        {
            var pen = new Pen(brush, thickness);
            pen.Freeze();
            return new GeometryDrawing(null, pen, geometry);
        }
    }
}
