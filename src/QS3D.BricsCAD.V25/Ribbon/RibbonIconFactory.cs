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
        Update,
        UpdateOnClose,
        UpdateStatus,
        QuantitySettings,
        QuantityCalculate,
        QuantityExport,
        QuantityView,
        QuantityExplain,
        QuantityCompare
    }

    internal static class RibbonIconFactory
    {
        public static ImageSource Create(RibbonIconKind kind, int pixelSize)
        {
            if (pixelSize <= 0) throw new ArgumentOutOfRangeException(nameof(pixelSize));

            var accent = FrozenBrush(34, 137, 245);
            var accentDark = FrozenBrush(13, 77, 172);
            var accentLight = FrozenBrush(105, 183, 255);
            var light = FrozenBrush(224, 238, 255);
            var dark = FrozenBrush(31, 42, 58);
            var green = FrozenBrush(53, 196, 122);
            var orange = FrozenBrush(245, 156, 43);

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

                case RibbonIconKind.QuantitySettings:
                    // BLT3D reference: blue calculation sheet with a small settings cog.
                    group.Children.Add(Fill(light, new RectangleGeometry(new Rect(4, 3, 21, 26), 1.5, 1.5)));
                    group.Children.Add(Stroke(accentDark, 1.5, new RectangleGeometry(new Rect(4, 3, 21, 26), 1.5, 1.5)));
                    group.Children.Add(Fill(accent, new RectangleGeometry(new Rect(7, 6, 15, 5), 0.8, 0.8)));
                    group.Children.Add(Stroke(accent, 1.35, new LineGeometry(new Point(7, 15), new Point(21, 15))));
                    group.Children.Add(Stroke(accent, 1.35, new LineGeometry(new Point(7, 19), new Point(21, 19))));
                    group.Children.Add(Stroke(accent, 1.35, new LineGeometry(new Point(7, 23), new Point(17, 23))));
                    group.Children.Add(Stroke(accent, 1.2, new LineGeometry(new Point(11, 13), new Point(11, 25))));
                    group.Children.Add(Stroke(accent, 1.2, new LineGeometry(new Point(16, 13), new Point(16, 25))));
                    group.Children.Add(Fill(accentDark, new EllipseGeometry(new Point(25, 24), 5.2, 5.2)));
                    for (var i = 0; i < 8; i++)
                    {
                        var angle = i * Math.PI / 4.0;
                        var x1 = 25 + Math.Cos(angle) * 4.6;
                        var y1 = 24 + Math.Sin(angle) * 4.6;
                        var x2 = 25 + Math.Cos(angle) * 6.1;
                        var y2 = 24 + Math.Sin(angle) * 6.1;
                        group.Children.Add(Stroke(accentDark, 2.1, new LineGeometry(new Point(x1, y1), new Point(x2, y2))));
                    }
                    group.Children.Add(Fill(light, new EllipseGeometry(new Point(25, 24), 2.1, 2.1)));
                    group.Children.Add(Fill(orange, new EllipseGeometry(new Point(28.4, 20.6), 1.25, 1.25)));
                    break;

                case RibbonIconKind.QuantityCalculate:
                    // BLT3D reference: blue isometric quantity cube with orange engine accent.
                    group.Children.Add(Fill(accentLight, Geometry.Parse("M16,3 L28,9 L16,15 L4,9 Z")));
                    group.Children.Add(Fill(accentDark, Geometry.Parse("M4,9 L16,15 L16,29 L4,23 Z")));
                    group.Children.Add(Fill(accent, Geometry.Parse("M16,15 L28,9 L28,23 L16,29 Z")));
                    group.Children.Add(Stroke(light, 1.05, new LineGeometry(new Point(16, 15), new Point(16, 28))));
                    group.Children.Add(Stroke(orange, 2.8, Geometry.Parse("M20,25 L24,18 L27,20 L29,13")));
                    group.Children.Add(Fill(orange, Geometry.Parse("M27,12 L31,12 L29,16 Z")));
                    break;

                case RibbonIconKind.QuantityExport:
                    // BLT3D reference: clean blue isometric export cube.
                    group.Children.Add(Fill(accentLight, Geometry.Parse("M16,3 L28,9 L16,15 L4,9 Z")));
                    group.Children.Add(Fill(accentDark, Geometry.Parse("M4,9 L16,15 L16,29 L4,23 Z")));
                    group.Children.Add(Fill(accent, Geometry.Parse("M16,15 L28,9 L28,23 L16,29 Z")));
                    group.Children.Add(Stroke(light, 1.05, new LineGeometry(new Point(16, 15), new Point(16, 28))));
                    group.Children.Add(Stroke(light, 1.0, new LineGeometry(new Point(7, 10), new Point(16, 14))));
                    break;

                case RibbonIconKind.QuantityView:
                    // BLT3D reference: compact blue quantity bar chart.
                    group.Children.Add(Stroke(accentDark, 1.7, new LineGeometry(new Point(4, 27), new Point(29, 27))));
                    group.Children.Add(Fill(accentDark, new RectangleGeometry(new Rect(6, 19, 4, 8), 0.7, 0.7)));
                    group.Children.Add(Fill(accent, new RectangleGeometry(new Rect(12, 14, 4, 13), 0.7, 0.7)));
                    group.Children.Add(Fill(accentDark, new RectangleGeometry(new Rect(18, 9, 4, 18), 0.7, 0.7)));
                    group.Children.Add(Fill(accent, new RectangleGeometry(new Rect(24, 5, 4, 22), 0.7, 0.7)));
                    group.Children.Add(Stroke(accentLight, 1.1, Geometry.Parse("M6,16 L14,11 20,7 27,4")));
                    break;

                case RibbonIconKind.QuantityExplain:
                    // BLT3D reference: blue calculation/explanation sheet.
                    group.Children.Add(Fill(light, new RectangleGeometry(new Rect(5, 3, 23, 26), 1.5, 1.5)));
                    group.Children.Add(Stroke(accentDark, 1.5, new RectangleGeometry(new Rect(5, 3, 23, 26), 1.5, 1.5)));
                    group.Children.Add(Fill(accent, new RectangleGeometry(new Rect(8, 6, 17, 4), 0.7, 0.7)));
                    group.Children.Add(Fill(accentDark, new RectangleGeometry(new Rect(8, 13, 3, 3), 0.5, 0.5)));
                    group.Children.Add(Fill(accentDark, new RectangleGeometry(new Rect(8, 19, 3, 3), 0.5, 0.5)));
                    group.Children.Add(Fill(accentDark, new RectangleGeometry(new Rect(8, 25, 3, 2), 0.5, 0.5)));
                    group.Children.Add(Stroke(accent, 1.35, new LineGeometry(new Point(13, 14.5), new Point(24, 14.5))));
                    group.Children.Add(Stroke(accent, 1.35, new LineGeometry(new Point(13, 20.5), new Point(24, 20.5))));
                    group.Children.Add(Stroke(accent, 1.35, new LineGeometry(new Point(13, 26), new Point(22, 26))));
                    break;

                case RibbonIconKind.QuantityCompare:
                    // BLT3D reference: blue balance scales with orange pivot accents.
                    group.Children.Add(Fill(orange, new EllipseGeometry(new Point(16, 5), 2.3, 2.3)));
                    group.Children.Add(Stroke(accentDark, 2.2, new LineGeometry(new Point(16, 7), new Point(16, 26))));
                    group.Children.Add(Stroke(accent, 2.0, new LineGeometry(new Point(6, 10), new Point(26, 10))));
                    group.Children.Add(Fill(orange, new EllipseGeometry(new Point(16, 10), 2.0, 2.0)));
                    group.Children.Add(Stroke(accentDark, 1.5, new LineGeometry(new Point(8, 10), new Point(5, 18))));
                    group.Children.Add(Stroke(accentDark, 1.5, new LineGeometry(new Point(8, 10), new Point(11, 18))));
                    group.Children.Add(Stroke(accentDark, 1.5, new LineGeometry(new Point(24, 10), new Point(21, 18))));
                    group.Children.Add(Stroke(accentDark, 1.5, new LineGeometry(new Point(24, 10), new Point(27, 18))));
                    group.Children.Add(Fill(accent, Geometry.Parse("M3,18 L13,18 C12,22 10,24 8,24 C6,24 4,22 3,18 Z")));
                    group.Children.Add(Fill(accent, Geometry.Parse("M19,18 L29,18 C28,22 26,24 24,24 C22,24 20,22 19,18 Z")));
                    group.Children.Add(Fill(accentDark, Geometry.Parse("M10,28 L22,28 L20,25 L12,25 Z")));
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
