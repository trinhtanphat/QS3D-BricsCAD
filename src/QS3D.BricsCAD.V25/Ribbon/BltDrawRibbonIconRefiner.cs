using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media;

namespace QS3D.BricsCAD.V25.Ribbon
{
    /// <summary>
    /// Applies a consistent clean-room BLT3D-familiar icon treatment to every QS3D Draw action.
    /// The icons are vector drawings generated in-process: no BLT3D binaries or proprietary
    /// bitmap assets are copied. This pass runs after compact Draw layout and before BIM mirroring,
    /// so MÔ HÌNH BIM receives the same qualified button artwork.
    /// </summary>
    internal static class BltDrawRibbonIconRefiner
    {
        private const string AssemblyName = "BrxMgd";
        private const string DrawTabId = "QS3D_DRAW";

        private enum IconKind
        {
            Point,
            Arc,
            Line,
            Rectangle,
            Polyline,
            Circle,
            Boundary,
            Slope,
            Cut,
            Move,
            Rotate,
            Mirror,
            Copy,
            Break,
            Join,
            Measure,
            Corner,
            Tee,
            Import,
            ImportLight,
            Delete,
            Export
        }

        private static readonly IReadOnlyDictionary<string, IconKind> IconByButtonId =
            new Dictionary<string, IconKind>(StringComparer.OrdinalIgnoreCase)
            {
                ["QS3D_DRAW_BLT_POINT"] = IconKind.Point,
                ["QS3D_DRAW_BLT_ARC"] = IconKind.Arc,
                ["QS3D_DRAW_BLT_LINE"] = IconKind.Line,
                ["QS3D_DRAW_BLT_RECTANGLE"] = IconKind.Rectangle,
                ["QS3D_DRAW_BLT_TRACE"] = IconKind.Polyline,
                ["QS3D_DRAW_BLT_CIRCLE"] = IconKind.Circle,
                ["QS3D_DRAW_BLT_BOUNDARY"] = IconKind.Boundary,
                ["QS3D_DRAW_BLT_SLAB_SLOPE"] = IconKind.Slope,
                ["QS3D_DRAW_BLT_SLAB_CUT"] = IconKind.Cut,
                ["QS3D_DRAW_BLT_MOVE"] = IconKind.Move,
                ["QS3D_DRAW_BLT_ROTATE"] = IconKind.Rotate,
                ["QS3D_DRAW_BLT_MIRROR"] = IconKind.Mirror,
                ["QS3D_DRAW_BLT_COPY"] = IconKind.Copy,
                ["QS3D_DRAW_BLT_BREAK"] = IconKind.Break,
                ["QS3D_DRAW_BLT_JOIN"] = IconKind.Join,
                ["QS3D_DRAW_BLT_DISTANCE"] = IconKind.Measure,
                ["QS3D_DRAW_BLT_CORNER"] = IconKind.Corner,
                ["QS3D_DRAW_BLT_TEE"] = IconKind.Tee,
                ["QS3D_DRAW_BLT_IFC_IMPORT"] = IconKind.Import,
                ["QS3D_DRAW_BLT_IFC_IMPORT_LIGHT"] = IconKind.ImportLight,
                ["QS3D_DRAW_BLT_IFC_DELETE"] = IconKind.Delete,
                ["QS3D_DRAW_BLT_IFC_EXPORT"] = IconKind.Export
            };

        public static bool TryInitialize()
        {
            try
            {
                var control = FindRibbonControl();
                if (control == null)
                    return false;

                var tabs = GetProperty(control, "Tabs");
                var drawTab = tabs == null ? null : FindById(tabs, DrawTabId);
                if (drawTab == null)
                    return false;

                var panels = GetProperty(drawTab, "Panels");
                if (!(panels is IEnumerable panelEnumerable))
                    return false;

                var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var panel in panelEnumerable)
                {
                    if (panel == null)
                        continue;
                    var source = GetProperty(panel, "Source");
                    var items = source == null ? null : GetProperty(source, "Items");
                    if (items != null)
                        ApplyIcons(items, found);
                }

                return IconByButtonId.Keys.All(found.Contains);
            }
            catch
            {
                return false;
            }
        }

        private static void ApplyIcons(object collection, ISet<string> found)
        {
            if (!(collection is IEnumerable enumerable))
                return;

            foreach (var item in enumerable)
            {
                if (item == null)
                    continue;

                var id = GetProperty(item, "Id") as string;
                if (id != null && IconByButtonId.TryGetValue(id, out var kind))
                {
                    var image = CreateIcon(kind);
                    SetProperty(item, "ShowImage", true);
                    SetProperty(item, "Image", image);
                    SetProperty(item, "LargeImage", image);
                    found.Add(id);
                }

                var childItems = GetProperty(item, "Items");
                if (childItems != null)
                    ApplyIcons(childItems, found);
            }
        }

        private static ImageSource CreateIcon(IconKind kind)
        {
            // Keep every semantic mark inside the same 32x32 coordinate box so BricsCAD does
            // not scale similar icons differently simply because their geometry bounds differ.
            var transparent = FrozenBrush(Color.FromArgb(0, 0, 0, 0));
            var blue = FrozenBrush(Color.FromRgb(29, 120, 213));
            var blueDark = FrozenBrush(Color.FromRgb(28, 54, 95));
            var blueLight = FrozenBrush(Color.FromRgb(207, 232, 255));
            var text = FrozenBrush(Color.FromRgb(243, 243, 243));
            var amber = FrozenBrush(Color.FromRgb(231, 179, 62));
            var red = FrozenBrush(Color.FromRgb(232, 74, 74));
            var green = FrozenBrush(Color.FromRgb(59, 184, 109));

            var group = new DrawingGroup();
            group.Children.Add(Fill(transparent, new RectangleGeometry(new Rect(0, 0, 32, 32))));

            switch (kind)
            {
                case IconKind.Point:
                    group.Children.Add(Stroke(blueLight, 1.7, new EllipseGeometry(new Point(16, 16), 8.5, 8.5)));
                    group.Children.Add(Stroke(blue, 1.8, Geometry.Parse("M16,3 L16,10 M16,22 L16,29 M3,16 L10,16 M22,16 L29,16")));
                    group.Children.Add(Fill(blue, new EllipseGeometry(new Point(16, 16), 3.2, 3.2)));
                    break;

                case IconKind.Arc:
                    group.Children.Add(Stroke(blue, 2.7, Geometry.Parse("M5,24 C8,8 21,4 27,18")));
                    group.Children.Add(Fill(blueDark, new EllipseGeometry(new Point(5, 24), 2.1, 2.1)));
                    group.Children.Add(Fill(blueDark, new EllipseGeometry(new Point(27, 18), 2.1, 2.1)));
                    break;

                case IconKind.Line:
                    group.Children.Add(Stroke(blue, 2.8, new LineGeometry(new Point(5, 25), new Point(27, 7))));
                    group.Children.Add(Fill(blueDark, new EllipseGeometry(new Point(5, 25), 2.2, 2.2)));
                    group.Children.Add(Fill(blueDark, new EllipseGeometry(new Point(27, 7), 2.2, 2.2)));
                    break;

                case IconKind.Rectangle:
                    group.Children.Add(Fill(blueLight, new RectangleGeometry(new Rect(6, 8, 20, 16), 1.2, 1.2)));
                    group.Children.Add(Stroke(blue, 2.3, new RectangleGeometry(new Rect(5, 7, 22, 18), 1.2, 1.2)));
                    group.Children.Add(Fill(blueDark, new RectangleGeometry(new Rect(4, 6, 3, 3))));
                    group.Children.Add(Fill(blueDark, new RectangleGeometry(new Rect(25, 23, 3, 3))));
                    break;

                case IconKind.Polyline:
                    group.Children.Add(Stroke(blueLight, 1.2, Geometry.Parse("M4,25 L10,9 18,19 28,6")));
                    group.Children.Add(Stroke(blue, 2.7, Geometry.Parse("M4,25 L10,9 18,19 28,6")));
                    foreach (var point in new[] { new Point(4, 25), new Point(10, 9), new Point(18, 19), new Point(28, 6) })
                        group.Children.Add(Fill(blueDark, new EllipseGeometry(point, 1.8, 1.8)));
                    break;

                case IconKind.Circle:
                    group.Children.Add(Fill(blueLight, new EllipseGeometry(new Point(16, 16), 9, 9)));
                    group.Children.Add(Stroke(blue, 2.4, new EllipseGeometry(new Point(16, 16), 10.5, 10.5)));
                    group.Children.Add(Fill(blueDark, new EllipseGeometry(new Point(16, 16), 2, 2)));
                    break;

                case IconKind.Boundary:
                    group.Children.Add(Fill(blueLight, Geometry.Parse("M6,9 L14,5 27,10 24,25 11,28 4,18 Z")));
                    group.Children.Add(Stroke(blue, 2.3, Geometry.Parse("M6,9 L14,5 27,10 24,25 11,28 4,18 Z")));
                    group.Children.Add(Stroke(blueDark, 1.3, Geometry.Parse("M10,12 L15,9 22,12 21,21 13,24 9,18 Z")));
                    break;

                case IconKind.Slope:
                    group.Children.Add(Fill(blueLight, Geometry.Parse("M5,25 L27,25 27,10 Z")));
                    group.Children.Add(Stroke(blue, 2.5, new LineGeometry(new Point(6, 24), new Point(27, 10))));
                    group.Children.Add(Stroke(amber, 2, Geometry.Parse("M7,20 L7,26 13,26")));
                    break;

                case IconKind.Cut:
                    group.Children.Add(Fill(blueLight, new RectangleGeometry(new Rect(5, 8, 22, 16), 1, 1)));
                    group.Children.Add(Stroke(blue, 2, new RectangleGeometry(new Rect(5, 8, 22, 16), 1, 1)));
                    group.Children.Add(Stroke(red, 2.8, new LineGeometry(new Point(8, 27), new Point(24, 5))));
                    group.Children.Add(Fill(red, new EllipseGeometry(new Point(9, 25), 2.1, 2.1)));
                    group.Children.Add(Fill(red, new EllipseGeometry(new Point(23, 7), 2.1, 2.1)));
                    break;

                case IconKind.Move:
                    group.Children.Add(Stroke(blue, 2.3, new LineGeometry(new Point(5, 16), new Point(27, 16))));
                    group.Children.Add(Stroke(blue, 2.3, new LineGeometry(new Point(16, 5), new Point(16, 27))));
                    group.Children.Add(Fill(blue, Geometry.Parse("M3,16 L9,12 9,20 Z M29,16 L23,12 23,20 Z M16,3 L12,9 20,9 Z M16,29 L12,23 20,23 Z")));
                    group.Children.Add(Fill(text, new EllipseGeometry(new Point(16, 16), 2, 2)));
                    break;

                case IconKind.Rotate:
                    group.Children.Add(Stroke(blue, 2.6, Geometry.Parse("M8,10 C14,4 25,6 27,15 C29,23 22,28 14,27")));
                    group.Children.Add(Fill(blue, Geometry.Parse("M5,8 L13,8 8,15 Z")));
                    group.Children.Add(Fill(blueDark, new EllipseGeometry(new Point(16, 16), 2, 2)));
                    break;

                case IconKind.Mirror:
                    group.Children.Add(Stroke(amber, 1.7, new LineGeometry(new Point(16, 4), new Point(16, 28))));
                    group.Children.Add(Fill(blue, Geometry.Parse("M5,24 L13,8 13,24 Z")));
                    group.Children.Add(Stroke(blueLight, 2, Geometry.Parse("M27,24 L19,8 19,24 Z")));
                    break;

                case IconKind.Copy:
                    group.Children.Add(Fill(blueLight, new RectangleGeometry(new Rect(10, 6, 16, 16), 1, 1)));
                    group.Children.Add(Stroke(blueDark, 1.7, new RectangleGeometry(new Rect(10, 6, 16, 16), 1, 1)));
                    group.Children.Add(Fill(blue, new RectangleGeometry(new Rect(5, 11, 16, 16), 1, 1)));
                    group.Children.Add(Stroke(text, 1.2, new RectangleGeometry(new Rect(7, 13, 12, 12), 0.5, 0.5)));
                    break;

                case IconKind.Break:
                    group.Children.Add(Stroke(blue, 2.8, new LineGeometry(new Point(4, 16), new Point(12, 16))));
                    group.Children.Add(Stroke(blue, 2.8, new LineGeometry(new Point(20, 16), new Point(28, 16))));
                    group.Children.Add(Stroke(red, 2.1, Geometry.Parse("M13,8 L18,13 14,18 19,24")));
                    break;

                case IconKind.Join:
                    group.Children.Add(Stroke(blue, 2.7, Geometry.Parse("M4,9 L13,16 4,23 M28,9 L19,16 28,23")));
                    group.Children.Add(Fill(green, new EllipseGeometry(new Point(16, 16), 3.2, 3.2)));
                    break;

                case IconKind.Measure:
                    group.Children.Add(Fill(blue, Geometry.Parse("M5,23 L23,5 28,10 10,28 Z")));
                    group.Children.Add(Stroke(text, 1.2, Geometry.Parse("M10,22 L13,25 M14,18 L17,21 M18,14 L21,17 M22,10 L25,13")));
                    break;

                case IconKind.Corner:
                    group.Children.Add(Stroke(blue, 2.8, Geometry.Parse("M5,27 L5,13 C5,8 8,5 13,5 L28,5")));
                    group.Children.Add(Stroke(amber, 1.6, Geometry.Parse("M8,19 C8,12 12,8 19,8")));
                    break;

                case IconKind.Tee:
                    group.Children.Add(Stroke(blue, 2.8, new LineGeometry(new Point(5, 9), new Point(27, 9))));
                    group.Children.Add(Stroke(blue, 2.8, new LineGeometry(new Point(16, 9), new Point(16, 27))));
                    group.Children.Add(Fill(green, new EllipseGeometry(new Point(16, 9), 2.5, 2.5)));
                    break;

                case IconKind.Import:
                    DrawIfcBox(group, blue, blueLight, blueDark);
                    group.Children.Add(Stroke(green, 2.4, new LineGeometry(new Point(16, 3), new Point(16, 16))));
                    group.Children.Add(Fill(green, Geometry.Parse("M10,11 L16,17 22,11 Z")));
                    break;

                case IconKind.ImportLight:
                    DrawIfcBox(group, blue, blueLight, blueDark);
                    group.Children.Add(Stroke(green, 2.2, new LineGeometry(new Point(16, 3), new Point(16, 15))));
                    group.Children.Add(Fill(green, Geometry.Parse("M11,10 L16,16 21,10 Z")));
                    group.Children.Add(Stroke(amber, 1.8, Geometry.Parse("M22,4 L26,8 M26,4 L22,8")));
                    break;

                case IconKind.Delete:
                    group.Children.Add(Fill(red, new RectangleGeometry(new Rect(9, 10, 14, 17), 2, 2)));
                    group.Children.Add(Fill(red, new RectangleGeometry(new Rect(7, 7, 18, 4), 1, 1)));
                    group.Children.Add(Stroke(text, 1.6, new LineGeometry(new Point(13, 14), new Point(13, 23))));
                    group.Children.Add(Stroke(text, 1.6, new LineGeometry(new Point(19, 14), new Point(19, 23))));
                    break;

                case IconKind.Export:
                    DrawIfcBox(group, blue, blueLight, blueDark);
                    group.Children.Add(Stroke(amber, 2.4, new LineGeometry(new Point(16, 16), new Point(16, 29))));
                    group.Children.Add(Fill(amber, Geometry.Parse("M10,21 L16,15 22,21 Z")));
                    break;
            }

            group.Freeze();
            var image = new DrawingImage(group);
            image.Freeze();
            return image;
        }

        private static void DrawIfcBox(DrawingGroup group, Brush blue, Brush light, Brush ink)
        {
            group.Children.Add(Fill(blue, Geometry.Parse("M6,10 L16,5 26,10 16,15 Z")));
            group.Children.Add(Fill(light, Geometry.Parse("M6,10 L16,15 16,25 6,20 Z")));
            group.Children.Add(Fill(blue, Geometry.Parse("M26,10 L16,15 16,25 26,20 Z")));
            group.Children.Add(Stroke(ink, 1.1, Geometry.Parse("M6,10 L16,15 26,10 M16,15 L16,25")));
        }

        private static SolidColorBrush FrozenBrush(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        private static GeometryDrawing Fill(Brush brush, Geometry geometry) =>
            new GeometryDrawing(brush, null, geometry);

        private static GeometryDrawing Stroke(Brush brush, double thickness, Geometry geometry)
        {
            var pen = new Pen(brush, thickness)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round,
                LineJoin = PenLineJoin.Round
            };
            pen.Freeze();
            return new GeometryDrawing(null, pen, geometry);
        }

        private static object? FindById(object collection, string id)
        {
            if (!(collection is IEnumerable enumerable))
                return null;

            foreach (var item in enumerable)
            {
                if (item == null)
                    continue;
                if (string.Equals(GetProperty(item, "Id") as string, id, StringComparison.OrdinalIgnoreCase))
                    return item;
            }

            return null;
        }

        private static object? FindRibbonControl()
        {
            var servicesType = Type.GetType("Bricscad.Ribbon.RibbonServices, " + AssemblyName, false);
            if (servicesType == null)
                return null;

            var paletteProperty = servicesType.GetProperty("RibbonPaletteSet", BindingFlags.Public | BindingFlags.Static);
            var palette = paletteProperty?.GetValue(null, null);
            if (palette == null)
            {
                servicesType.GetMethod("CreateRibbonPaletteSet", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, null);
                palette = paletteProperty?.GetValue(null, null);
            }

            if (palette == null)
                return null;
            if (palette.GetType().Name == "RibbonControl")
                return palette;

            var direct = GetProperty(palette, "RibbonControl");
            if (direct != null)
                return direct;

            foreach (var property in palette.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (property.PropertyType.Name != "RibbonControl" || property.GetIndexParameters().Length != 0)
                    continue;
                var value = property.GetValue(palette, null);
                if (value != null)
                    return value;
            }

            return null;
        }

        private static object? GetProperty(object target, string name) =>
            target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public)?.GetValue(target, null);

        private static void SetProperty(object target, string name, object value)
        {
            var property = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            if (property == null || !property.CanWrite)
                return;
            if (property.PropertyType.IsInstanceOfType(value) || property.PropertyType == value.GetType())
                property.SetValue(target, value, null);
        }
    }
}
