using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Media;

namespace QS3D.BricsCAD.V25.Ribbon
{
    /// <summary>
    /// Applies a clean-room, BLT3D-familiar icon set to every visible button in the VẼ and
    /// Công cụ panels. The rich Draw augmenter already owns command routing; this decorator is
    /// presentation-only and runs after compact layout refinement so both VẼ and the later BIM
    /// mirror receive the same crisp semantic glyphs.
    /// </summary>
    internal static class BltDrawRibbonReferenceIconDecorator
    {
        private const string AssemblyName = "BrxMgd";
        private const string DrawTabId = "QS3D_DRAW";
        private const string DrawPanelSourceId = "QS3D_DRAW_BLT_DRAW_PANEL_SOURCE";
        private const string ToolsPanelSourceId = "QS3D_DRAW_BLT_TOOLS_PANEL_SOURCE";

        private enum IconKind
        {
            Point,
            Line,
            Polyline,
            Arc,
            Rectangle,
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
            Tee
        }

        private static readonly KeyValuePair<string, IconKind>[] Icons =
        {
            new KeyValuePair<string, IconKind>("QS3D_DRAW_BLT_POINT", IconKind.Point),
            new KeyValuePair<string, IconKind>("QS3D_DRAW_BLT_LINE", IconKind.Line),
            new KeyValuePair<string, IconKind>("QS3D_DRAW_BLT_TRACE", IconKind.Polyline),
            new KeyValuePair<string, IconKind>("QS3D_DRAW_BLT_ARC", IconKind.Arc),
            new KeyValuePair<string, IconKind>("QS3D_DRAW_BLT_RECTANGLE", IconKind.Rectangle),
            new KeyValuePair<string, IconKind>("QS3D_DRAW_BLT_CIRCLE", IconKind.Circle),
            new KeyValuePair<string, IconKind>("QS3D_DRAW_BLT_BOUNDARY", IconKind.Boundary),
            new KeyValuePair<string, IconKind>("QS3D_DRAW_BLT_SLAB_SLOPE", IconKind.Slope),
            new KeyValuePair<string, IconKind>("QS3D_DRAW_BLT_SLAB_CUT", IconKind.Cut),
            new KeyValuePair<string, IconKind>("QS3D_DRAW_BLT_MOVE", IconKind.Move),
            new KeyValuePair<string, IconKind>("QS3D_DRAW_BLT_ROTATE", IconKind.Rotate),
            new KeyValuePair<string, IconKind>("QS3D_DRAW_BLT_MIRROR", IconKind.Mirror),
            new KeyValuePair<string, IconKind>("QS3D_DRAW_BLT_COPY", IconKind.Copy),
            new KeyValuePair<string, IconKind>("QS3D_DRAW_BLT_BREAK", IconKind.Break),
            new KeyValuePair<string, IconKind>("QS3D_DRAW_BLT_JOIN", IconKind.Join),
            new KeyValuePair<string, IconKind>("QS3D_DRAW_BLT_DISTANCE", IconKind.Measure),
            new KeyValuePair<string, IconKind>("QS3D_DRAW_BLT_CORNER", IconKind.Corner),
            new KeyValuePair<string, IconKind>("QS3D_DRAW_BLT_TEE", IconKind.Tee)
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
                if (panels == null)
                    return false;

                var drawSource = FindPanelSourceById(panels, DrawPanelSourceId);
                var toolsSource = FindPanelSourceById(panels, ToolsPanelSourceId);
                if (drawSource == null || toolsSource == null)
                    return false;

                var buttons = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                CollectButtons(GetProperty(drawSource, "Items"), buttons);
                CollectButtons(GetProperty(toolsSource, "Items"), buttons);

                foreach (var spec in Icons)
                {
                    object button;
                    if (!buttons.TryGetValue(spec.Key, out button))
                        return false;

                    SetProperty(button, "ShowImage", true);
                    SetProperty(button, "Image", CreateIcon(spec.Value));
                    SetProperty(button, "LargeImage", CreateIcon(spec.Value));
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void CollectButtons(object? items, IDictionary<string, object> buttons)
        {
            if (!(items is IEnumerable enumerable))
                return;

            foreach (var item in enumerable)
            {
                if (item == null)
                    continue;

                var typeName = item.GetType().Name;
                if (string.Equals(typeName, "RibbonButton", StringComparison.Ordinal))
                {
                    var id = GetProperty(item, "Id") as string;
                    if (!string.IsNullOrWhiteSpace(id))
                        buttons[id] = item;
                    continue;
                }

                if (string.Equals(typeName, "RibbonRowPanel", StringComparison.Ordinal))
                    CollectButtons(GetProperty(item, "Items"), buttons);
            }
        }

        private static ImageSource CreateIcon(IconKind kind)
        {
            var blue = FrozenBrush(Color.FromRgb(30, 132, 235));
            var blueDark = FrozenBrush(Color.FromRgb(17, 76, 165));
            var pale = FrozenBrush(Color.FromRgb(206, 230, 253));
            var gray = FrozenBrush(Color.FromRgb(126, 137, 149));
            var grayDark = FrozenBrush(Color.FromRgb(76, 87, 99));
            var red = FrozenBrush(Color.FromRgb(221, 71, 71));
            var group = new DrawingGroup();

            // Stable vector bounds keep the glyph centered when BricsCAD renders a Standard button.
            group.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(0, 0, 32, 32))));

            switch (kind)
            {
                case IconKind.Point:
                    group.Children.Add(Fill(blue, new RectangleGeometry(new Rect(11, 11, 10, 10), 1, 1)));
                    group.Children.Add(Stroke(pale, 1.4, new EllipseGeometry(new Point(16, 16), 8.5, 8.5)));
                    break;

                case IconKind.Line:
                    group.Children.Add(Stroke(gray, 2.5, new LineGeometry(new Point(6, 24), new Point(26, 8))));
                    group.Children.Add(Fill(blue, new EllipseGeometry(new Point(6, 24), 2.2, 2.2)));
                    group.Children.Add(Fill(blue, new EllipseGeometry(new Point(26, 8), 2.2, 2.2)));
                    break;

                case IconKind.Polyline:
                    group.Children.Add(Stroke(gray, 2.4, Geometry.Parse("M4,24 L10,10 18,20 28,7")));
                    group.Children.Add(Fill(blue, new EllipseGeometry(new Point(10, 10), 2.1, 2.1)));
                    group.Children.Add(Fill(blue, new EllipseGeometry(new Point(18, 20), 2.1, 2.1)));
                    break;

                case IconKind.Arc:
                    group.Children.Add(Stroke(gray, 2.5, Geometry.Parse("M5,24 C9,8 24,6 28,20")));
                    group.Children.Add(Fill(blue, new EllipseGeometry(new Point(5, 24), 2.1, 2.1)));
                    group.Children.Add(Fill(blue, new EllipseGeometry(new Point(28, 20), 2.1, 2.1)));
                    break;

                case IconKind.Rectangle:
                    group.Children.Add(Stroke(gray, 2.2, new RectangleGeometry(new Rect(5, 7, 22, 18), 1, 1)));
                    group.Children.Add(Fill(blue, new RectangleGeometry(new Rect(4, 6, 4, 4), 1, 1)));
                    group.Children.Add(Fill(blue, new RectangleGeometry(new Rect(24, 22, 4, 4), 1, 1)));
                    break;

                case IconKind.Circle:
                    group.Children.Add(Stroke(gray, 2.4, new EllipseGeometry(new Point(16, 16), 10, 10)));
                    group.Children.Add(Fill(blue, new EllipseGeometry(new Point(16, 16), 2.2, 2.2)));
                    break;

                case IconKind.Boundary:
                    group.Children.Add(Stroke(gray, 2.2, Geometry.Parse("M5,9 L14,5 27,10 25,24 11,28 4,18 Z")));
                    group.Children.Add(Stroke(blue, 1.7, Geometry.Parse("M9,12 L15,9 23,12 21,21 12,24 8,18 Z")));
                    break;

                case IconKind.Slope:
                    group.Children.Add(Fill(pale, Geometry.Parse("M5,25 L28,25 28,10 Z")));
                    group.Children.Add(Stroke(blue, 2.5, new LineGeometry(new Point(5, 24), new Point(27, 10))));
                    group.Children.Add(Stroke(grayDark, 1.5, new LineGeometry(new Point(5, 26), new Point(28, 26))));
                    break;

                case IconKind.Cut:
                    group.Children.Add(Stroke(gray, 2, new RectangleGeometry(new Rect(5, 7, 22, 18), 1, 1)));
                    group.Children.Add(Stroke(red, 2.5, Geometry.Parse("M8,26 L24,5")));
                    group.Children.Add(Fill(blue, new EllipseGeometry(new Point(9, 23), 2.2, 2.2)));
                    group.Children.Add(Fill(blue, new EllipseGeometry(new Point(23, 8), 2.2, 2.2)));
                    break;

                case IconKind.Move:
                    group.Children.Add(Stroke(blue, 2.2, new LineGeometry(new Point(5, 16), new Point(27, 16))));
                    group.Children.Add(Stroke(blue, 2.2, new LineGeometry(new Point(16, 5), new Point(16, 27))));
                    group.Children.Add(Fill(blue, Geometry.Parse("M3,16 L9,12 9,20 Z M29,16 L23,12 23,20 Z M16,3 L12,9 20,9 Z M16,29 L12,23 20,23 Z")));
                    break;

                case IconKind.Rotate:
                    group.Children.Add(Stroke(gray, 2.3, Geometry.Parse("M8,10 C15,4 26,8 26,18 C26,25 20,28 13,27")));
                    group.Children.Add(Fill(blue, Geometry.Parse("M5,8 L13,8 8,15 Z")));
                    break;

                case IconKind.Mirror:
                    group.Children.Add(Stroke(grayDark, 1.6, new LineGeometry(new Point(16, 4), new Point(16, 28))));
                    group.Children.Add(Fill(blue, Geometry.Parse("M5,24 L13,8 13,24 Z")));
                    group.Children.Add(Stroke(gray, 1.8, Geometry.Parse("M27,24 L19,8 19,24 Z")));
                    break;

                case IconKind.Copy:
                    group.Children.Add(Fill(pale, new RectangleGeometry(new Rect(10, 5, 16, 18), 1, 1)));
                    group.Children.Add(Stroke(gray, 1.8, new RectangleGeometry(new Rect(10, 5, 16, 18), 1, 1)));
                    group.Children.Add(Fill(Brushes.White, new RectangleGeometry(new Rect(5, 10, 16, 17), 1, 1)));
                    group.Children.Add(Stroke(blue, 2, new RectangleGeometry(new Rect(5, 10, 16, 17), 1, 1)));
                    break;

                case IconKind.Break:
                    group.Children.Add(Stroke(gray, 2.2, new LineGeometry(new Point(4, 16), new Point(12, 16))));
                    group.Children.Add(Stroke(gray, 2.2, new LineGeometry(new Point(20, 16), new Point(28, 16))));
                    group.Children.Add(Stroke(blue, 2, Geometry.Parse("M12,10 L17,14 14,18 20,22")));
                    break;

                case IconKind.Join:
                    group.Children.Add(Stroke(gray, 2.4, Geometry.Parse("M5,21 L12,14")));
                    group.Children.Add(Stroke(gray, 2.4, Geometry.Parse("M20,14 L27,7")));
                    group.Children.Add(Stroke(blue, 2.8, new LineGeometry(new Point(11, 15), new Point(21, 13))));
                    break;

                case IconKind.Measure:
                    group.Children.Add(Stroke(gray, 1.8, new LineGeometry(new Point(5, 23), new Point(27, 9))));
                    group.Children.Add(Stroke(blue, 2.2, new LineGeometry(new Point(8, 25), new Point(28, 12))));
                    group.Children.Add(Fill(blue, Geometry.Parse("M6,26 L11,25 8,21 Z M30,10 L25,11 28,15 Z")));
                    break;

                case IconKind.Corner:
                    group.Children.Add(Stroke(gray, 2.5, Geometry.Parse("M5,26 L5,10 L18,10")));
                    group.Children.Add(Stroke(blue, 2.5, Geometry.Parse("M18,10 Q26,10 26,18 L26,27")));
                    break;

                case IconKind.Tee:
                    group.Children.Add(Stroke(gray, 2.5, new LineGeometry(new Point(5, 9), new Point(27, 9))));
                    group.Children.Add(Stroke(blue, 2.7, new LineGeometry(new Point(16, 9), new Point(16, 27))));
                    break;
            }

            var image = new DrawingImage(group);
            if (image.CanFreeze)
                image.Freeze();
            return image;
        }

        private static GeometryDrawing Stroke(Brush brush, double width, Geometry geometry)
        {
            var pen = new Pen(brush, width)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round,
                LineJoin = PenLineJoin.Round
            };
            if (pen.CanFreeze)
                pen.Freeze();
            return new GeometryDrawing(null, pen, geometry);
        }

        private static GeometryDrawing Fill(Brush brush, Geometry geometry) =>
            new GeometryDrawing(brush, null, geometry);

        private static Brush FrozenBrush(Color color)
        {
            var brush = new SolidColorBrush(color);
            if (brush.CanFreeze)
                brush.Freeze();
            return brush;
        }

        private static object? FindPanelSourceById(object panels, string sourceId)
        {
            if (!(panels is IEnumerable enumerable))
                return null;

            foreach (var panel in enumerable)
            {
                if (panel == null)
                    continue;

                var source = GetProperty(panel, "Source");
                if (source == null)
                    continue;

                if (string.Equals(GetProperty(source, "Id") as string, sourceId, StringComparison.OrdinalIgnoreCase))
                    return source;
            }

            return null;
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
