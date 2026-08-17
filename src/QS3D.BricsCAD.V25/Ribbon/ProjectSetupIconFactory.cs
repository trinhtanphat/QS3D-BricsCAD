using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace QS3D.BricsCAD.V25.Ribbon
{
    /// <summary>
    /// Vector icons dedicated to the three owner-reference Project Setup buttons.
    /// The geometry is generated in-repository at 16/32 px so no proprietary BLT assets are copied.
    /// </summary>
    internal enum ProjectSetupIconKind
    {
        ProjectInformation,
        FloorSettings,
        ProjectProperties
    }

    internal static class ProjectSetupIconFactory
    {
        public static ImageSource Create(ProjectSetupIconKind kind, int pixelSize)
        {
            if (pixelSize <= 0) throw new ArgumentOutOfRangeException(nameof(pixelSize));

            var blue = FrozenBrush(38, 124, 210);
            var blueDark = FrozenBrush(20, 73, 132);
            var blueLight = FrozenBrush(154, 207, 255);
            var paper = FrozenBrush(230, 238, 246);
            var line = FrozenBrush(94, 118, 144);
            var green = FrozenBrush(72, 181, 87);
            var orange = FrozenBrush(232, 151, 46);
            var white = Brushes.White;

            var group = new DrawingGroup();
            switch (kind)
            {
                case ProjectSetupIconKind.ProjectInformation:
                    AddDocument(group, paper, blueDark);
                    group.Children.Add(Fill(blueLight, new RectangleGeometry(new Rect(9, 8, 11, 2), 0.8, 0.8)));
                    group.Children.Add(Fill(line, new RectangleGeometry(new Rect(9, 13, 9, 1.5), 0.5, 0.5)));
                    group.Children.Add(Fill(line, new RectangleGeometry(new Rect(9, 17, 7, 1.5), 0.5, 0.5)));
                    group.Children.Add(Fill(blue, new EllipseGeometry(new Point(23.5, 23.5), 6.2, 6.2)));
                    group.Children.Add(Fill(white, new EllipseGeometry(new Point(23.5, 20.2), 1.15, 1.15)));
                    group.Children.Add(Fill(white, new RectangleGeometry(new Rect(22.35, 22.2, 2.3, 5.2), 1, 1)));
                    break;

                case ProjectSetupIconKind.FloorSettings:
                    group.Children.Add(Fill(blueDark, new RectangleGeometry(new Rect(4, 7, 24, 3), 1, 1)));
                    group.Children.Add(Fill(blue, new RectangleGeometry(new Rect(6, 14, 22, 3), 1, 1)));
                    group.Children.Add(Fill(blueLight, new RectangleGeometry(new Rect(8, 21, 20, 3), 1, 1)));
                    group.Children.Add(Stroke(green, 2.2, new LineGeometry(new Point(5, 5), new Point(5, 27))));
                    group.Children.Add(Fill(green, new EllipseGeometry(new Point(5, 8.5), 2.1, 2.1)));
                    group.Children.Add(Fill(green, new EllipseGeometry(new Point(5, 15.5), 2.1, 2.1)));
                    group.Children.Add(Fill(green, new EllipseGeometry(new Point(5, 22.5), 2.1, 2.1)));
                    group.Children.Add(Stroke(orange, 1.6, new LineGeometry(new Point(23, 5), new Point(23, 27))));
                    group.Children.Add(Fill(orange, new EllipseGeometry(new Point(23, 8.5), 1.6, 1.6)));
                    group.Children.Add(Fill(orange, new EllipseGeometry(new Point(23, 15.5), 1.6, 1.6)));
                    group.Children.Add(Fill(orange, new EllipseGeometry(new Point(23, 22.5), 1.6, 1.6)));
                    break;

                case ProjectSetupIconKind.ProjectProperties:
                    AddDocument(group, paper, blueDark);
                    AddSlider(group, line, blue, 10, 11, 19, 14);
                    AddSlider(group, line, green, 10, 16, 19, 12);
                    AddSlider(group, line, orange, 10, 21, 19, 17);
                    AddGearBadge(group, blueDark, white, new Point(24, 24));
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

        private static void AddDocument(DrawingGroup group, Brush paper, Brush outline)
        {
            group.Children.Add(Fill(paper, new RectangleGeometry(new Rect(6, 4, 19, 24), 2, 2)));
            group.Children.Add(Stroke(outline, 1.5, new RectangleGeometry(new Rect(6, 4, 19, 24), 2, 2)));
            group.Children.Add(Fill(outline, new RectangleGeometry(new Rect(9, 5.5, 10, 2), 0.7, 0.7)));
        }

        private static void AddSlider(DrawingGroup group, Brush track, Brush knob, double x1, double y, double x2, double knobX)
        {
            group.Children.Add(Stroke(track, 1.5, new LineGeometry(new Point(x1, y), new Point(x2, y))));
            group.Children.Add(Fill(knob, new EllipseGeometry(new Point(knobX, y), 2.1, 2.1)));
        }

        private static void AddGearBadge(DrawingGroup group, Brush gear, Brush center, Point origin)
        {
            group.Children.Add(Fill(gear, new EllipseGeometry(origin, 5.8, 5.8)));
            for (var i = 0; i < 8; i++)
            {
                var angle = i * Math.PI / 4.0;
                var x1 = origin.X + Math.Cos(angle) * 5.0;
                var y1 = origin.Y + Math.Sin(angle) * 5.0;
                var x2 = origin.X + Math.Cos(angle) * 7.0;
                var y2 = origin.Y + Math.Sin(angle) * 7.0;
                group.Children.Add(Stroke(gear, 2.2, new LineGeometry(new Point(x1, y1), new Point(x2, y2))));
            }
            group.Children.Add(Fill(center, new EllipseGeometry(origin, 2.0, 2.0)));
        }

        private static SolidColorBrush FrozenBrush(byte r, byte g, byte b)
        {
            var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
            brush.Freeze();
            return brush;
        }

        private static GeometryDrawing Fill(Brush brush, Geometry geometry) =>
            new GeometryDrawing(brush, null, geometry);

        private static GeometryDrawing Stroke(Brush brush, double thickness, Geometry geometry)
        {
            var pen = new Pen(brush, thickness);
            pen.Freeze();
            return new GeometryDrawing(null, pen, geometry);
        }
    }
}
