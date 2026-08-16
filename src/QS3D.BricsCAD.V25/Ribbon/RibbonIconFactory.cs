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
        UpdateStatus,
        Recognition,
        RecognitionAuto,
        Takeoff,
        Inspect,
        Draw,
        Transform,
        Measure,
        Section,
        Locate,
        Highlight,
        Focus,
        Isolate,
        Restore,
        Model3d,
        Wall,
        Structure,
        Opening,
        Door,
        Room,
        View3d,
        Orbit,
        Workspace,
        Quantity,
        Excel,
        Schedule,
        Rebar,
        Compare,
        Diff,
        Release
    }

    internal static class RibbonIconFactory
    {
        public static ImageSource Create(RibbonIconKind kind, int pixelSize)
        {
            if (pixelSize <= 0) throw new ArgumentOutOfRangeException(nameof(pixelSize));

            var accent = FrozenBrush(34, 137, 245);
            var accentDark = FrozenBrush(13, 77, 172);
            var accentSoft = FrozenBrush(103, 178, 255);
            var light = FrozenBrush(224, 238, 255);
            var mid = FrozenBrush(151, 174, 201);
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
                    group.Children.Add(Stroke(Brushes.White, 1.6, Geometry.Parse("M20,22 L21.7,23.7 24.7,20.3")));
                    break;

                case RibbonIconKind.Recognition:
                    AddRecognitionFrame(group, accent, accentDark, light);
                    group.Children.Add(Stroke(accent, 2.2, new EllipseGeometry(new Point(22, 22), 5, 5)));
                    group.Children.Add(Stroke(accent, 2.2, new LineGeometry(new Point(25.5, 25.5), new Point(29, 29))));
                    break;

                case RibbonIconKind.RecognitionAuto:
                    AddRecognitionFrame(group, accent, accentDark, light);
                    group.Children.Add(Fill(green, new EllipseGeometry(new Point(24, 23), 6, 6)));
                    group.Children.Add(Stroke(Brushes.White, 2, Geometry.Parse("M21,23 L23,25 27,20")));
                    break;

                case RibbonIconKind.Takeoff:
                    group.Children.Add(Fill(accentDark, new RectangleGeometry(new Rect(5, 4, 21, 25), 2, 2)));
                    group.Children.Add(Fill(light, new RectangleGeometry(new Rect(8, 8, 15, 17), 1, 1)));
                    group.Children.Add(Stroke(accent, 1.5, new LineGeometry(new Point(11, 12), new Point(20, 12))));
                    group.Children.Add(Stroke(accent, 1.5, new LineGeometry(new Point(11, 16), new Point(20, 16))));
                    group.Children.Add(Stroke(accent, 1.5, new LineGeometry(new Point(11, 20), new Point(17, 20))));
                    group.Children.Add(Fill(green, new EllipseGeometry(new Point(24, 24), 5, 5)));
                    group.Children.Add(Stroke(Brushes.White, 1.8, Geometry.Parse("M21.5,24 L23.2,25.7 26.7,21.8")));
                    break;

                case RibbonIconKind.Inspect:
                    AddWireCube(group, accent, accentDark, light, 4, 5, 18);
                    group.Children.Add(Fill(dark, new EllipseGeometry(new Point(22, 22), 6.5, 6.5)));
                    group.Children.Add(Stroke(accentSoft, 2.4, new EllipseGeometry(new Point(22, 22), 5, 5)));
                    group.Children.Add(Stroke(accentSoft, 2.4, new LineGeometry(new Point(25.5, 25.5), new Point(29, 29))));
                    break;

                case RibbonIconKind.Draw:
                    group.Children.Add(Stroke(accentSoft, 2.2, Geometry.Parse("M5,25 L10,8 L25,5")));
                    group.Children.Add(Fill(accent, new EllipseGeometry(new Point(5, 25), 3, 3)));
                    group.Children.Add(Fill(accentDark, new EllipseGeometry(new Point(10, 8), 3, 3)));
                    group.Children.Add(Fill(green, new EllipseGeometry(new Point(25, 5), 3, 3)));
                    group.Children.Add(Fill(light, Geometry.Parse("M10,24 L23,11 27,15 14,28 9,29 Z")));
                    group.Children.Add(Fill(accent, Geometry.Parse("M23,11 L26,8 30,12 27,15 Z")));
                    break;

                case RibbonIconKind.Transform:
                    group.Children.Add(Fill(accentDark, new RectangleGeometry(new Rect(10, 10, 12, 12), 2, 2)));
                    group.Children.Add(Stroke(accentSoft, 2.4, new LineGeometry(new Point(16, 4), new Point(16, 28))));
                    group.Children.Add(Stroke(accentSoft, 2.4, new LineGeometry(new Point(4, 16), new Point(28, 16))));
                    group.Children.Add(Fill(accent, Geometry.Parse("M16,2 L12,7 20,7 Z")));
                    group.Children.Add(Fill(accent, Geometry.Parse("M30,16 L25,12 25,20 Z")));
                    group.Children.Add(Fill(accent, Geometry.Parse("M16,30 L12,25 20,25 Z")));
                    group.Children.Add(Fill(accent, Geometry.Parse("M2,16 L7,12 7,20 Z")));
                    break;

                case RibbonIconKind.Measure:
                    group.Children.Add(Fill(accent, Geometry.Parse("M4,23 L23,4 29,10 10,29 Z")));
                    group.Children.Add(Fill(light, Geometry.Parse("M8,23 L23,8 25,10 10,25 Z")));
                    for (var i = 0; i < 5; i++)
                    {
                        var offset = 10 + i * 3.3;
                        group.Children.Add(Stroke(accentDark, 1.1,
                            new LineGeometry(new Point(offset, 20 - (offset - 10)), new Point(offset + 2, 22 - (offset - 10)))));
                    }
                    break;

                case RibbonIconKind.Section:
                    AddWireCube(group, mid, accentDark, light, 5, 5, 21);
                    group.Children.Add(Fill(accent, Geometry.Parse("M3,17 L27,9 29,14 5,22 Z")));
                    group.Children.Add(Stroke(Brushes.White, 1.3, new LineGeometry(new Point(8, 19), new Point(25, 13))));
                    break;

                case RibbonIconKind.Locate:
                    group.Children.Add(Stroke(accent, 2.4, new EllipseGeometry(new Point(16, 16), 9, 9)));
                    group.Children.Add(Stroke(accentDark, 2, new LineGeometry(new Point(16, 3), new Point(16, 11))));
                    group.Children.Add(Stroke(accentDark, 2, new LineGeometry(new Point(16, 21), new Point(16, 29))));
                    group.Children.Add(Stroke(accentDark, 2, new LineGeometry(new Point(3, 16), new Point(11, 16))));
                    group.Children.Add(Stroke(accentDark, 2, new LineGeometry(new Point(21, 16), new Point(29, 16))));
                    group.Children.Add(Fill(green, new EllipseGeometry(new Point(16, 16), 3, 3)));
                    break;

                case RibbonIconKind.Highlight:
                    group.Children.Add(Fill(accentDark, new RectangleGeometry(new Rect(7, 8, 16, 17), 2, 2)));
                    group.Children.Add(Stroke(accentSoft, 2, new RectangleGeometry(new Rect(5, 6, 20, 21), 3, 3)));
                    AddSparkle(group, light, new Point(26, 6), 4);
                    AddSparkle(group, green, new Point(25, 24), 3);
                    break;

                case RibbonIconKind.Focus:
                    group.Children.Add(Stroke(accentSoft, 2.5, Geometry.Parse("M4,11 L4,5 10,5 M22,5 L28,5 28,11 M28,21 L28,27 22,27 M10,27 L4,27 4,21")));
                    group.Children.Add(Fill(accent, new EllipseGeometry(new Point(16, 16), 6, 6)));
                    group.Children.Add(Fill(light, new EllipseGeometry(new Point(16, 16), 2.5, 2.5)));
                    break;

                case RibbonIconKind.Isolate:
                    group.Children.Add(Stroke(mid, 1.5, Geometry.Parse("M4,6 L10,6 M14,6 L20,6 M24,6 L28,6 M28,6 L28,12 M28,16 L28,22 M28,26 L28,28 M28,28 L22,28 M18,28 L12,28 M8,28 L4,28 M4,28 L4,22 M4,18 L4,12 M4,8 L4,6")));
                    group.Children.Add(Fill(accent, new RectangleGeometry(new Rect(10, 10, 12, 12), 2, 2)));
                    group.Children.Add(Stroke(light, 1.5, new RectangleGeometry(new Rect(12, 12, 8, 8), 1, 1)));
                    break;

                case RibbonIconKind.Restore:
                    group.Children.Add(Fill(mid, new RectangleGeometry(new Rect(7, 10, 8, 8), 1, 1)));
                    group.Children.Add(Fill(accentDark, new RectangleGeometry(new Rect(17, 14, 8, 8), 1, 1)));
                    group.Children.Add(Stroke(accent, 3.2, Geometry.Parse("M6,23 C9,29 20,31 27,23 C30,19 29,13 26,10")));
                    group.Children.Add(Fill(accent, Geometry.Parse("M22,9 L29,8 28,15 Z")));
                    break;

                case RibbonIconKind.Model3d:
                    AddSolidCube(group, accent, accentDark, accentSoft, new Point(16, 16), 11);
                    AddSparkle(group, light, new Point(26, 6), 4);
                    break;

                case RibbonIconKind.Wall:
                    group.Children.Add(Fill(accentDark, Geometry.Parse("M6,7 L21,4 27,8 12,11 Z")));
                    group.Children.Add(Fill(light, Geometry.Parse("M6,7 L12,11 12,27 6,23 Z")));
                    group.Children.Add(Fill(mid, Geometry.Parse("M12,11 L27,8 27,24 12,27 Z")));
                    group.Children.Add(Fill(accent, Geometry.Parse("M6,7 L9,8.5 9,24.5 6,23 Z")));
                    break;

                case RibbonIconKind.Structure:
                    group.Children.Add(Fill(mid, new RectangleGeometry(new Rect(5, 5, 22, 4), 1, 1)));
                    group.Children.Add(Fill(accentDark, new RectangleGeometry(new Rect(7, 9, 4, 17), 1, 1)));
                    group.Children.Add(Fill(accentDark, new RectangleGeometry(new Rect(21, 9, 4, 17), 1, 1)));
                    group.Children.Add(Fill(accent, new RectangleGeometry(new Rect(4, 26, 10, 3), 1, 1)));
                    group.Children.Add(Fill(accent, new RectangleGeometry(new Rect(18, 26, 10, 3), 1, 1)));
                    group.Children.Add(Stroke(light, 1.3, Geometry.Parse("M8,7 L16,15 24,7 M16,15 L8,7")));
                    break;

                case RibbonIconKind.Opening:
                    group.Children.Add(Fill(mid, new RectangleGeometry(new Rect(4, 5, 24, 22), 2, 2)));
                    group.Children.Add(Fill(dark, new RectangleGeometry(new Rect(10, 10, 12, 17), 1, 1)));
                    group.Children.Add(Stroke(accent, 2.2, new RectangleGeometry(new Rect(9, 9, 14, 18), 1, 1)));
                    group.Children.Add(Fill(green, new EllipseGeometry(new Point(25, 8), 4, 4)));
                    group.Children.Add(Stroke(Brushes.White, 1.5, Geometry.Parse("M23,8 L24.5,9.5 27,6.5")));
                    break;

                case RibbonIconKind.Door:
                    group.Children.Add(Stroke(mid, 3, Geometry.Parse("M7,27 L7,5 25,5 25,27")));
                    group.Children.Add(Fill(accent, Geometry.Parse("M10,8 L21,10 21,27 10,24 Z")));
                    group.Children.Add(Fill(accentDark, new EllipseGeometry(new Point(18, 18), 1.4, 1.4)));
                    group.Children.Add(Stroke(accentSoft, 1.4, Geometry.Parse("M10,24 C18,24 23,20 25,15")));
                    break;

                case RibbonIconKind.Room:
                    AddWireCube(group, accentSoft, accentDark, light, 5, 4, 23);
                    group.Children.Add(Fill(accent, new RectangleGeometry(new Rect(9, 11, 3, 13)));
                    group.Children.Add(Fill(accent, new RectangleGeometry(new Rect(18, 8, 3, 16)));
                    break;

                case RibbonIconKind.View3d:
                    group.Children.Add(Stroke(accentSoft, 2, Geometry.Parse("M3,16 C7,9 11,6 16,6 C21,6 25,9 29,16 C25,23 21,26 16,26 C11,26 7,23 3,16 Z")));
                    AddSolidCube(group, accent, accentDark, light, new Point(16, 16), 6);
                    break;

                case RibbonIconKind.Orbit:
                    group.Children.Add(Fill(accentDark, new EllipseGeometry(new Point(16, 16), 6, 6)));
                    group.Children.Add(Stroke(accentSoft, 2.3, Geometry.Parse("M5,14 C7,6 18,2 26,8 C29,10 30,14 29,17")));
                    group.Children.Add(Fill(accent, Geometry.Parse("M25,5 L30,8 26,11 Z")));
                    group.Children.Add(Stroke(accent, 2.3, Geometry.Parse("M27,20 C23,28 12,30 5,24 C2,21 2,17 3,14")));
                    group.Children.Add(Fill(accentSoft, Geometry.Parse("M7,27 L2,24 6,21 Z")));
                    break;

                case RibbonIconKind.Workspace:
                    group.Children.Add(Fill(accentDark, new RectangleGeometry(new Rect(4, 5, 24, 22), 2, 2)));
                    group.Children.Add(Fill(light, new RectangleGeometry(new Rect(7, 8, 8, 16), 1, 1)));
                    group.Children.Add(Fill(accentSoft, new RectangleGeometry(new Rect(18, 8, 7, 7), 1, 1)));
                    group.Children.Add(Fill(accent, new RectangleGeometry(new Rect(18, 18, 7, 6), 1, 1)));
                    break;

                case RibbonIconKind.Quantity:
                    group.Children.Add(Fill(accentDark, new RectangleGeometry(new Rect(5, 6, 22, 21), 2, 2)));
                    group.Children.Add(Fill(light, new RectangleGeometry(new Rect(8, 9, 16, 15), 1, 1)));
                    group.Children.Add(Fill(accent, new RectangleGeometry(new Rect(10, 19, 3, 3)));
                    group.Children.Add(Fill(accent, new RectangleGeometry(new Rect(15, 15, 3, 7)));
                    group.Children.Add(Fill(accent, new RectangleGeometry(new Rect(20, 11, 3, 11)));
                    group.Children.Add(Fill(green, new EllipseGeometry(new Point(25, 25), 4.5, 4.5)));
                    break;

                case RibbonIconKind.Excel:
                    group.Children.Add(Fill(green, new RectangleGeometry(new Rect(5, 4, 22, 24), 2, 2)));
                    group.Children.Add(Fill(light, new RectangleGeometry(new Rect(11, 7, 13, 18), 1, 1)));
                    for (var i = 0; i < 3; i++)
                    {
                        group.Children.Add(Stroke(mid, 1, new LineGeometry(new Point(11, 12 + i * 4), new Point(24, 12 + i * 4))));
                    }
                    group.Children.Add(Stroke(mid, 1, new LineGeometry(new Point(17.5, 7), new Point(17.5, 25))));
                    group.Children.Add(Stroke(Brushes.White, 2, Geometry.Parse("M7.5,12 L10.5,18 M10.5,12 L7.5,18")));
                    break;

                case RibbonIconKind.Schedule:
                    group.Children.Add(Fill(accentDark, new RectangleGeometry(new Rect(4, 5, 24, 23), 2, 2)));
                    group.Children.Add(Fill(light, new RectangleGeometry(new Rect(7, 10, 18, 15), 1, 1)));
                    group.Children.Add(Fill(accent, new RectangleGeometry(new Rect(4, 5, 24, 6), 2, 2)));
                    for (var i = 0; i < 3; i++)
                        group.Children.Add(Stroke(mid, 1, new LineGeometry(new Point(7, 15 + i * 4), new Point(25, 15 + i * 4))));
                    group.Children.Add(Fill(green, new EllipseGeometry(new Point(23, 23), 4, 4)));
                    break;

                case RibbonIconKind.Rebar:
                    group.Children.Add(Stroke(accentDark, 3, Geometry.Parse("M6,25 L6,10 C6,6 10,5 13,5 L25,5")));
                    group.Children.Add(Stroke(accent, 3, Geometry.Parse("M10,28 L10,14 C10,10 14,9 17,9 L28,9")));
                    group.Children.Add(Stroke(light, 1.3, new LineGeometry(new Point(5, 18), new Point(11, 18))));
                    group.Children.Add(Stroke(light, 1.3, new LineGeometry(new Point(17, 4), new Point(17, 10))));
                    group.Children.Add(Fill(green, new EllipseGeometry(new Point(27, 24), 4, 4)));
                    break;

                case RibbonIconKind.Compare:
                    group.Children.Add(Fill(mid, new RectangleGeometry(new Rect(4, 6, 14, 20), 2, 2)));
                    group.Children.Add(Fill(accent, new RectangleGeometry(new Rect(14, 8, 14, 20), 2, 2)));
                    group.Children.Add(Stroke(light, 1.5, new LineGeometry(new Point(9, 12), new Point(14, 12))));
                    group.Children.Add(Stroke(light, 1.5, new LineGeometry(new Point(19, 14), new Point(24, 14))));
                    group.Children.Add(Fill(green, new EllipseGeometry(new Point(16, 25), 4, 4)));
                    break;

                case RibbonIconKind.Diff:
                    group.Children.Add(Fill(accentDark, Geometry.Parse("M4,6 L14,6 14,26 4,26 Z")));
                    group.Children.Add(Fill(accent, Geometry.Parse("M18,6 L28,6 28,26 18,26 Z")));
                    group.Children.Add(Stroke(green, 2.2, new LineGeometry(new Point(11, 16), new Point(21, 16))));
                    group.Children.Add(Fill(green, Geometry.Parse("M21,12 L28,16 21,20 Z")));
                    break;

                case RibbonIconKind.Release:
                    group.Children.Add(Fill(accentDark, Geometry.Parse("M16,3 L27,7 26,17 C25,24 20,28 16,30 C12,28 7,24 6,17 L5,7 Z")));
                    group.Children.Add(Fill(accent, Geometry.Parse("M16,6 L24,9 23,17 C22,21 19,24 16,26 C13,24 10,21 9,17 L8,9 Z")));
                    group.Children.Add(Stroke(Brushes.White, 2.4, Geometry.Parse("M11,16 L15,20 22,12")));
                    group.Children.Add(Fill(green, new EllipseGeometry(new Point(25, 24), 4, 4)));
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

        private static void AddRecognitionFrame(DrawingGroup group, Brush accent, Brush accentDark, Brush light)
        {
            group.Children.Add(Stroke(accent, 2.2, Geometry.Parse("M3,11 L3,4 10,4 M22,4 L29,4 29,11 M29,21 L29,28 22,28 M10,28 L3,28 3,21")));
            group.Children.Add(Fill(accentDark, new RectangleGeometry(new Rect(10, 10, 12, 12), 2, 2)));
            group.Children.Add(Stroke(light, 1.4, new RectangleGeometry(new Rect(12, 12, 8, 8), 1, 1)));
        }

        private static void AddWireCube(DrawingGroup group, Brush front, Brush side, Brush top, double x, double y, double size)
        {
            var d = size * 0.26;
            group.Children.Add(Stroke(front, 1.7, Geometry.Parse(
                $"M{x},{y + d} L{x + size - d},{y + d} L{x + size - d},{y + size} L{x},{y + size} Z")));
            group.Children.Add(Stroke(top, 1.5, Geometry.Parse(
                $"M{x},{y + d} L{x + d},{y} L{x + size},{y} L{x + size - d},{y + d}")));
            group.Children.Add(Stroke(side, 1.5, Geometry.Parse(
                $"M{x + size - d},{y + d} L{x + size},{y} L{x + size},{y + size - d} L{x + size - d},{y + size}")));
            group.Children.Add(Stroke(front, 1.2, new LineGeometry(new Point(x + d, y), new Point(x + d, y + size - d))));
        }

        private static void AddSolidCube(DrawingGroup group, Brush front, Brush side, Brush top, Point center, double radius)
        {
            var x = center.X;
            var y = center.Y;
            group.Children.Add(Fill(front, Geometry.Parse(
                $"M{x - radius},{y - radius * 0.2} L{x},{y + radius * 0.35} L{x},{y + radius} L{x - radius},{y + radius * 0.45} Z")));
            group.Children.Add(Fill(side, Geometry.Parse(
                $"M{x},{y + radius * 0.35} L{x + radius},{y - radius * 0.2} L{x + radius},{y + radius * 0.45} L{x},{y + radius} Z")));
            group.Children.Add(Fill(top, Geometry.Parse(
                $"M{x - radius},{y - radius * 0.2} L{x},{y - radius * 0.75} L{x + radius},{y - radius * 0.2} L{x},{y + radius * 0.35} Z")));
        }

        private static void AddSparkle(DrawingGroup group, Brush brush, Point center, double radius)
        {
            var x = center.X;
            var y = center.Y;
            group.Children.Add(Fill(brush, Geometry.Parse(
                $"M{x},{y - radius} L{x + radius * 0.35},{y - radius * 0.35} L{x + radius},{y} L{x + radius * 0.35},{y + radius * 0.35} L{x},{y + radius} L{x - radius * 0.35},{y + radius * 0.35} L{x - radius},{y} L{x - radius * 0.35},{y - radius * 0.35} Z")));
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
