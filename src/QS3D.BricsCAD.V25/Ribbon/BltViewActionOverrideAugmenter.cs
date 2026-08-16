using System;
using System.Collections;
using System.Reflection;
using System.Windows;
using System.Windows.Media;

namespace QS3D.BricsCAD.V25.Ribbon
{
    /// <summary>
    /// Applies the owner-reference XEM "Hiển thị" action contract after the base XEM
    /// ribbon has been created. Stable bootstrap item ids are deliberately retained so
    /// existing host reconciliation remains safe, while visible labels, commands, sizing
    /// and locally-generated vector icons match the reference workflow more closely.
    /// </summary>
    internal static class BltViewActionOverrideAugmenter
    {
        private const string AssemblyName = "BrxMgd";
        private const string ViewTabId = "QS3D_VIEW";
        private const string DisplayPanelSourceId = "QS3D_VIEW_SECTION_PANEL_SOURCE";

        private static readonly ActionSpec[] Actions =
        {
            // Preserve the three existing item slots so their visual order becomes:
            // Tối ưu đồ họa -> Section Box -> Cắt theo đối tượng.
            new ActionSpec(
                "QS3D_VIEW_SECTION_SECTIONBOX",
                "Tối ưu đồ họa",
                "QS3DOPTIMIZEGRAPHICS",
                ActionIconKind.OptimizeGraphics),
            new ActionSpec(
                "QS3D_VIEW_SECTION_SECTIONPLANE",
                "Section Box",
                "QS3DSECTIONBOX",
                ActionIconKind.SectionBox),
            new ActionSpec(
                "QS3D_VIEW_SECTION_CLIPDISPLAY",
                "Cắt theo đối tượng",
                "QS3DCUTBYOBJECT",
                ActionIconKind.CutByObject),
        };

        private static bool _initialized;

        public static bool TryInitialize()
        {
            if (_initialized) return true;

            try
            {
                var control = FindRibbonControl();
                if (control == null) return false;

                var tabs = GetProperty(control, "Tabs");
                var viewTab = tabs == null ? null : FindById(tabs, ViewTabId);
                if (viewTab == null) return false;

                var panels = GetProperty(viewTab, "Panels");
                var source = panels == null ? null : FindPanelSource(panels, DisplayPanelSourceId);
                if (source == null) return false;

                SetProperty(source, "Name", "Hiển thị");
                SetProperty(source, "Title", "Hiển thị");

                var items = GetProperty(source, "Items");
                if (items == null) return false;

                foreach (var spec in Actions)
                {
                    var button = FindById(items, spec.Id);
                    if (button == null) return false;
                    ApplyAction(button, spec);
                }

                _initialized = true;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void Reset() => _initialized = false;

        private static void ApplyAction(object button, ActionSpec spec)
        {
            SetProperty(button, "Name", spec.Text);
            SetProperty(button, "Text", spec.Text);
            SetProperty(button, "ShowText", true);
            SetProperty(button, "ShowImage", true);
            SetEnumProperty(button, "Size", "Large");

            // RibbonCommandParameterFallback runs later and captures this exact route, so
            // BricsCAD builds that omit ICommand parameters still execute the intended action.
            SetProperty(button, "CommandParameter", spec.Command);

            var icon = CreateIcon(spec.Icon);
            SetProperty(button, "Image", icon);
            SetProperty(button, "LargeImage", icon);
        }

        private static ImageSource CreateIcon(ActionIconKind kind)
        {
            var blue = FrozenBrush(36, 132, 230);
            var dark = FrozenBrush(31, 58, 92);
            var pale = FrozenBrush(219, 238, 255);
            var cyan = FrozenBrush(77, 185, 232);
            var green = FrozenBrush(51, 176, 113);
            var orange = FrozenBrush(232, 147, 53);

            var group = new DrawingGroup();
            group.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(0, 0, 32, 32))));

            switch (kind)
            {
                case ActionIconKind.OptimizeGraphics:
                    group.Children.Add(Stroke(dark, 1.8, new RectangleGeometry(new Rect(4.5, 5, 23, 16), 2, 2)));
                    group.Children.Add(Fill(pale, new RectangleGeometry(new Rect(7, 7.5, 18, 11), 1, 1)));
                    group.Children.Add(Stroke(blue, 2.2, Arc(new Point(16, 15), 6.5, 205, 335)));
                    group.Children.Add(Fill(orange, Triangle(new Point(18, 9), new Point(13, 16), new Point(17, 16))));
                    group.Children.Add(Fill(orange, Triangle(new Point(14, 22), new Point(19, 15), new Point(15, 15))));
                    group.Children.Add(Stroke(dark, 1.8, new LineGeometry(new Point(16, 21), new Point(16, 25))));
                    group.Children.Add(Stroke(dark, 1.8, new LineGeometry(new Point(11, 26), new Point(21, 26))));
                    break;

                case ActionIconKind.SectionBox:
                    AddCube(group, cyan, dark, pale, 5, 5, 22);
                    group.Children.Add(Fill(FrozenBrush(198, 228, 255), new RectangleGeometry(new Rect(9.5, 7, 12.5, 18), 1, 1)));
                    group.Children.Add(Stroke(blue, 2.2, new RectangleGeometry(new Rect(9.5, 7, 12.5, 18), 1, 1)));
                    group.Children.Add(Stroke(blue, 1.4, new LineGeometry(new Point(5, 16), new Point(27, 16))));
                    break;

                case ActionIconKind.CutByObject:
                    AddCube(group, cyan, dark, pale, 5, 5, 22);
                    group.Children.Add(Stroke(orange, 2.8, new LineGeometry(new Point(4, 23), new Point(27, 9))));
                    group.Children.Add(Fill(orange, Triangle(new Point(24, 7), new Point(29, 7), new Point(27, 12))));
                    group.Children.Add(Stroke(green, 2.0, new RectangleGeometry(new Rect(10, 10, 12, 12), 1, 1)));
                    group.Children.Add(Fill(green, new EllipseGeometry(new Point(10, 22), 2.4, 2.4)));
                    break;
            }

            var image = new DrawingImage(group);
            if (image.CanFreeze) image.Freeze();
            return image;
        }

        private static void AddCube(DrawingGroup group, Brush edge, Brush dark, Brush fill, double x, double y, double size)
        {
            var left = x;
            var top = y + size * 0.22;
            var right = x + size;
            var bottom = y + size * 0.82;
            var dx = size * 0.22;
            var dy = size * 0.18;

            group.Children.Add(Fill(fill, Polygon(
                new Point(left + dx, y),
                new Point(right, top),
                new Point(right - dx, top + dy),
                new Point(left, y + dy))));
            group.Children.Add(Stroke(edge, 1.8, Polygon(
                new Point(left, y + dy),
                new Point(left + dx, y),
                new Point(right, top),
                new Point(right, bottom),
                new Point(right - dx, y + size),
                new Point(left, bottom))));
            group.Children.Add(Stroke(dark, 1.4, new LineGeometry(new Point(left, y + dy), new Point(right - dx, top + dy))));
            group.Children.Add(Stroke(dark, 1.4, new LineGeometry(new Point(right - dx, top + dy), new Point(right, top))));
            group.Children.Add(Stroke(dark, 1.4, new LineGeometry(new Point(right - dx, top + dy), new Point(right - dx, y + size))));
        }

        private static Geometry Arc(Point center, double radius, double startDegrees, double endDegrees)
        {
            var start = Polar(center, radius, startDegrees);
            var end = Polar(center, radius, endDegrees);
            var geometry = new StreamGeometry();
            using (var context = geometry.Open())
            {
                context.BeginFigure(start, false, false);
                var span = (endDegrees - startDegrees) % 360;
                if (span < 0) span += 360;
                context.ArcTo(end, new Size(radius, radius), 0, span > 180, SweepDirection.Clockwise, true, false);
            }
            if (geometry.CanFreeze) geometry.Freeze();
            return geometry;
        }

        private static Point Polar(Point center, double radius, double degrees)
        {
            var radians = degrees * Math.PI / 180.0;
            return new Point(center.X + radius * Math.Cos(radians), center.Y + radius * Math.Sin(radians));
        }

        private static Geometry Triangle(Point a, Point b, Point c) => Polygon(a, b, c);

        private static Geometry Polygon(params Point[] points)
        {
            var geometry = new StreamGeometry();
            using (var context = geometry.Open())
            {
                context.BeginFigure(points[0], true, true);
                for (var i = 1; i < points.Length; i++)
                    context.LineTo(points[i], true, false);
            }
            if (geometry.CanFreeze) geometry.Freeze();
            return geometry;
        }

        private static GeometryDrawing Fill(Brush brush, Geometry geometry) => new GeometryDrawing(brush, null, geometry);
        private static GeometryDrawing Stroke(Brush brush, double thickness, Geometry geometry) =>
            new GeometryDrawing(null, new Pen(brush, thickness), geometry);

        private static Brush FrozenBrush(byte r, byte g, byte b)
        {
            var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
            brush.Freeze();
            return brush;
        }

        private static object? FindPanelSource(object panels, string sourceId)
        {
            if (!(panels is IEnumerable enumerable)) return null;
            foreach (var panel in enumerable)
            {
                if (panel == null) continue;
                var source = GetProperty(panel, "Source");
                if (source == null) continue;
                if (string.Equals(GetProperty(source, "Id") as string, sourceId, StringComparison.OrdinalIgnoreCase))
                    return source;
            }
            return null;
        }

        private static object? FindById(object collection, string id)
        {
            if (!(collection is IEnumerable enumerable)) return null;
            foreach (var item in enumerable)
            {
                if (item == null) continue;
                if (string.Equals(GetProperty(item, "Id") as string, id, StringComparison.OrdinalIgnoreCase))
                    return item;
            }
            return null;
        }

        private static object? FindRibbonControl()
        {
            var servicesType = Type.GetType("Bricscad.Ribbon.RibbonServices, " + AssemblyName, false);
            if (servicesType == null) return null;

            var paletteProperty = servicesType.GetProperty("RibbonPaletteSet", BindingFlags.Public | BindingFlags.Static);
            var palette = paletteProperty?.GetValue(null, null);
            if (palette == null) return null;
            if (palette.GetType().Name == "RibbonControl") return palette;

            var direct = GetProperty(palette, "RibbonControl");
            if (direct != null) return direct;

            foreach (var property in palette.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (property.PropertyType.Name != "RibbonControl" || property.GetIndexParameters().Length != 0)
                    continue;
                var value = property.GetValue(palette, null);
                if (value != null) return value;
            }
            return null;
        }

        private static object? GetProperty(object target, string name) =>
            target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public)?.GetValue(target, null);

        private static void SetProperty(object target, string name, object value)
        {
            var property = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            if (property == null || !property.CanWrite) return;
            if (property.PropertyType.IsInstanceOfType(value) || property.PropertyType == value.GetType())
                property.SetValue(target, value, null);
        }

        private static void SetEnumProperty(object target, string name, string enumValue)
        {
            var property = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            if (property == null || !property.CanWrite || !property.PropertyType.IsEnum) return;
            try { property.SetValue(target, Enum.Parse(property.PropertyType, enumValue, true), null); }
            catch { }
        }

        private enum ActionIconKind
        {
            OptimizeGraphics,
            SectionBox,
            CutByObject,
        }

        private sealed class ActionSpec
        {
            public ActionSpec(string id, string text, string command, ActionIconKind icon)
            {
                Id = id;
                Text = text;
                Command = command;
                Icon = icon;
            }

            public string Id { get; }
            public string Text { get; }
            public string Command { get; }
            public ActionIconKind Icon { get; }
        }
    }
}
