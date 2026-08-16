using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Media;

namespace QS3D.BricsCAD.V25.Ribbon
{
    /// <summary>
    /// Gives the QS3D-owned XEM tab a deterministic, icon-forward presentation while preserving
    /// the command routing created by RibbonBootstrapper. The visible icon language is recreated
    /// with local vector drawings; no BLT3D bitmap/resource is copied into QS3D.
    /// </summary>
    internal static class BltViewRibbonAugmenter
    {
        private const string AssemblyName = "BrxMgd";
        private const string ViewTabId = "QS3D_VIEW";

        private static readonly PanelSpec[] PanelSpecs =
        {
            new PanelSpec(
                "QS3D_VIEW_ORIENTATION_PANEL_SOURCE",
                "Góc nhìn",
                new ButtonSpec("QS3D_VIEW_ORIENTATION_3D", "3D", ViewIconKind.View3d),
                new ButtonSpec("QS3D_VIEW_ORIENTATION_TOP", "Top", ViewIconKind.Top),
                new ButtonSpec("QS3D_VIEW_ORIENTATION_ORBIT", "Orbit", ViewIconKind.Orbit)),
            new PanelSpec(
                "QS3D_VIEW_FOCUS_PANEL_SOURCE",
                "Tập trung",
                new ButtonSpec("QS3D_VIEW_FOCUS_FOCUS", "Focus", ViewIconKind.Focus),
                new ButtonSpec("QS3D_VIEW_FOCUS_CÔLẬP", "Cô lập", ViewIconKind.Isolate),
                new ButtonSpec("QS3D_VIEW_FOCUS_KHÔIPHỤC", "Khôi phục", ViewIconKind.Restore)),
            new PanelSpec(
                "QS3D_VIEW_SECTION_PANEL_SOURCE",
                "Mặt cắt",
                new ButtonSpec("QS3D_VIEW_SECTION_SECTIONBOX", "Section Box", ViewIconKind.SectionBox),
                new ButtonSpec("QS3D_VIEW_SECTION_SECTIONPLANE", "Section Plane", ViewIconKind.SectionPlane),
                new ButtonSpec("QS3D_VIEW_SECTION_CLIPDISPLAY", "Clip Display", ViewIconKind.ClipDisplay)),
            new PanelSpec(
                "QS3D_VIEW_ZOOM_PANEL_SOURCE",
                "Điều hướng",
                new ButtonSpec("QS3D_VIEW_ZOOM_ZOOMCHỌN", "Zoom chọn", ViewIconKind.ZoomSelected),
                new ButtonSpec("QS3D_VIEW_ZOOM_ZOOMALL", "Zoom all", ViewIconKind.ZoomAll)),
            new PanelSpec(
                "QS3D_VIEW_WORKSPACE_PANEL_SOURCE",
                "Workspace",
                new ButtonSpec("QS3D_VIEW_WORKSPACE_WORKSPACE", "Workspace", ViewIconKind.Workspace),
                new ButtonSpec("QS3D_VIEW_WORKSPACE_BQ", "BQ", ViewIconKind.Quantity),
                new ButtonSpec("QS3D_VIEW_WORKSPACE_REFRESH", "Refresh", ViewIconKind.Refresh))
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
                if (panels == null) return false;

                foreach (var panelSpec in PanelSpecs)
                {
                    var source = FindPanelSource(panels, panelSpec.SourceId);
                    if (source == null) return false;

                    SetProperty(source, "Name", panelSpec.Title);
                    SetProperty(source, "Title", panelSpec.Title);

                    var items = GetProperty(source, "Items");
                    if (items == null) return false;

                    foreach (var buttonSpec in panelSpec.Buttons)
                    {
                        var button = FindById(items, buttonSpec.Id);
                        if (button == null) return false;
                        DecorateButton(button, buttonSpec);
                    }
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

        private static void DecorateButton(object button, ButtonSpec spec)
        {
            // Keep CommandParameter/CommandHandler exactly as RibbonBootstrapper wired them.
            SetProperty(button, "Name", spec.Text);
            SetProperty(button, "Text", spec.Text);
            SetProperty(button, "ShowText", true);
            SetProperty(button, "ShowImage", true);
            SetEnumProperty(button, "Size", "Standard");
            SetProperty(button, "Image", CreateIcon(spec.Icon));
            SetProperty(button, "LargeImage", CreateIcon(spec.Icon));
        }

        private static ImageSource CreateIcon(ViewIconKind kind)
        {
            var blue = FrozenBrush(35, 137, 242);
            var blueDark = FrozenBrush(16, 78, 168);
            var blueSoft = FrozenBrush(126, 193, 255);
            var pale = FrozenBrush(218, 237, 255);
            var ink = FrozenBrush(39, 51, 68);
            var muted = FrozenBrush(125, 148, 175);
            var green = FrozenBrush(47, 188, 113);

            var group = new DrawingGroup();
            group.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(0, 0, 32, 32))));

            switch (kind)
            {
                case ViewIconKind.View3d:
                    AddCube(group, blue, blueDark, pale, 5, 5, 22);
                    break;

                case ViewIconKind.Top:
                    group.Children.Add(Stroke(blue, 2.4, new RectangleGeometry(new Rect(6, 6, 20, 20), 1, 1)));
                    group.Children.Add(Stroke(blueSoft, 1.7, new LineGeometry(new Point(16, 8), new Point(16, 24))));
                    group.Children.Add(Stroke(blueSoft, 1.7, new LineGeometry(new Point(8, 16), new Point(24, 16))));
                    group.Children.Add(Fill(blueDark, new EllipseGeometry(new Point(16, 16), 2.6, 2.6)));
                    break;

                case ViewIconKind.Orbit:
                    group.Children.Add(Stroke(blue, 2.4, new EllipseGeometry(new Point(16, 16), 10, 7)));
                    group.Children.Add(Stroke(blueSoft, 2.1, new EllipseGeometry(new Point(16, 16), 6, 11)));
                    group.Children.Add(Fill(blueDark, Triangle(new Point(27, 15), new Point(22, 12), new Point(22, 18))));
                    group.Children.Add(Fill(blue, new EllipseGeometry(new Point(16, 16), 2.4, 2.4)));
                    break;

                case ViewIconKind.Focus:
                    AddFocusCorners(group, blue, 5, 5, 22, 7);
                    group.Children.Add(Fill(blue, new EllipseGeometry(new Point(16, 16), 5, 5)));
                    group.Children.Add(Fill(pale, new EllipseGeometry(new Point(16, 16), 2, 2)));
                    break;

                case ViewIconKind.Isolate:
                    group.Children.Add(Fill(muted, new RectangleGeometry(new Rect(4, 8, 8, 8), 1, 1)));
                    group.Children.Add(Fill(blue, new RectangleGeometry(new Rect(12, 11, 9, 10), 1, 1)));
                    group.Children.Add(Stroke(blueDark, 1.5, new RectangleGeometry(new Rect(11, 10, 11, 12), 1, 1)));
                    group.Children.Add(Fill(muted, new RectangleGeometry(new Rect(22, 16, 6, 7), 1, 1)));
                    group.Children.Add(Stroke(blueSoft, 1.6, new EllipseGeometry(new Point(16.5, 16), 10.5, 10.5)));
                    break;

                case ViewIconKind.Restore:
                    group.Children.Add(Fill(blueSoft, new RectangleGeometry(new Rect(5, 8, 7, 7), 1, 1)));
                    group.Children.Add(Fill(blue, new RectangleGeometry(new Rect(13, 12, 8, 9), 1, 1)));
                    group.Children.Add(Fill(blueSoft, new RectangleGeometry(new Rect(22, 17, 5, 6), 1, 1)));
                    group.Children.Add(Stroke(green, 2.4, Arc(new Point(16, 16), 11, 200, 330)));
                    group.Children.Add(Fill(green, Triangle(new Point(7, 8), new Point(12, 7), new Point(9, 12))));
                    break;

                case ViewIconKind.SectionBox:
                    AddCube(group, blueSoft, blueDark, pale, 5, 5, 22);
                    group.Children.Add(Stroke(blue, 2.4, new RectangleGeometry(new Rect(10, 7, 12, 18), 1, 1)));
                    break;

                case ViewIconKind.SectionPlane:
                    AddCube(group, muted, blueDark, pale, 5, 5, 22);
                    group.Children.Add(Fill(blue, Parallelogram(new Point(3, 18), new Point(25, 10), new Point(29, 14), new Point(7, 22))));
                    group.Children.Add(Stroke(Brushes.White, 1.1, new LineGeometry(new Point(8, 19), new Point(25, 13))));
                    break;

                case ViewIconKind.ClipDisplay:
                    group.Children.Add(Stroke(blueDark, 2, new RectangleGeometry(new Rect(7, 6, 18, 20), 1, 1)));
                    AddFocusCorners(group, blue, 3, 3, 26, 6);
                    group.Children.Add(Fill(pale, new RectangleGeometry(new Rect(11, 10, 10, 12), 1, 1)));
                    group.Children.Add(Stroke(blueSoft, 1.4, new LineGeometry(new Point(11, 16), new Point(21, 16))));
                    break;

                case ViewIconKind.ZoomSelected:
                    AddMagnifier(group, blueDark, blue);
                    AddFocusCorners(group, blueSoft, 5, 5, 13, 4);
                    group.Children.Add(Fill(blue, new RectangleGeometry(new Rect(9, 9, 5, 5), 1, 1)));
                    break;

                case ViewIconKind.ZoomAll:
                    AddMagnifier(group, blueDark, blue);
                    group.Children.Add(Stroke(blueSoft, 1.5, new RectangleGeometry(new Rect(7, 7, 11, 11), 1, 1)));
                    group.Children.Add(Fill(blue, Triangle(new Point(6, 6), new Point(11, 7), new Point(7, 11))));
                    group.Children.Add(Fill(blue, Triangle(new Point(19, 19), new Point(14, 18), new Point(18, 14))));
                    break;

                case ViewIconKind.Workspace:
                    group.Children.Add(Stroke(blueDark, 1.8, new RectangleGeometry(new Rect(4, 5, 24, 22), 2, 2)));
                    group.Children.Add(Fill(blue, new RectangleGeometry(new Rect(7, 8, 8, 7), 1, 1)));
                    group.Children.Add(Fill(pale, new RectangleGeometry(new Rect(17, 8, 8, 7), 1, 1)));
                    group.Children.Add(Fill(pale, new RectangleGeometry(new Rect(7, 17, 8, 7), 1, 1)));
                    group.Children.Add(Fill(blueSoft, new RectangleGeometry(new Rect(17, 17, 8, 7), 1, 1)));
                    break;

                case ViewIconKind.Quantity:
                    group.Children.Add(Fill(pale, new RectangleGeometry(new Rect(6, 4, 20, 24), 2, 2)));
                    group.Children.Add(Stroke(blueDark, 1.8, new RectangleGeometry(new Rect(6, 4, 20, 24), 2, 2)));
                    group.Children.Add(Fill(blue, new RectangleGeometry(new Rect(9, 8, 14, 4), 1, 1)));
                    group.Children.Add(Stroke(muted, 1.2, new LineGeometry(new Point(10, 16), new Point(22, 16))));
                    group.Children.Add(Stroke(muted, 1.2, new LineGeometry(new Point(10, 20), new Point(22, 20))));
                    group.Children.Add(Fill(green, new EllipseGeometry(new Point(23, 24), 4, 4)));
                    break;

                case ViewIconKind.Refresh:
                    group.Children.Add(Stroke(blue, 2.8, Arc(new Point(16, 16), 10, 205, 350)));
                    group.Children.Add(Stroke(blueDark, 2.8, Arc(new Point(16, 16), 10, 25, 170)));
                    group.Children.Add(Fill(blue, Triangle(new Point(27, 10), new Point(21, 9), new Point(25, 15))));
                    group.Children.Add(Fill(blueDark, Triangle(new Point(5, 22), new Point(11, 23), new Point(7, 17))));
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

            group.Children.Add(Fill(fill, Parallelogram(
                new Point(left + dx, y),
                new Point(right, top),
                new Point(right - dx, top + dy),
                new Point(left, y + dy))));
            group.Children.Add(Stroke(edge, 1.8, PolygonGeometry(
                new Point(left, y + dy),
                new Point(left + dx, y),
                new Point(right, top),
                new Point(right, bottom),
                new Point(right - dx, y + size),
                new Point(left, bottom))));
            group.Children.Add(Stroke(dark, 1.5, new LineGeometry(new Point(left, y + dy), new Point(right - dx, top + dy))));
            group.Children.Add(Stroke(dark, 1.5, new LineGeometry(new Point(right - dx, top + dy), new Point(right, top))));
            group.Children.Add(Stroke(dark, 1.5, new LineGeometry(new Point(right - dx, top + dy), new Point(right - dx, y + size))));
        }

        private static void AddFocusCorners(DrawingGroup group, Brush brush, double x, double y, double size, double arm)
        {
            var right = x + size;
            var bottom = y + size;
            group.Children.Add(Stroke(brush, 2, new LineGeometry(new Point(x, y), new Point(x + arm, y))));
            group.Children.Add(Stroke(brush, 2, new LineGeometry(new Point(x, y), new Point(x, y + arm))));
            group.Children.Add(Stroke(brush, 2, new LineGeometry(new Point(right, y), new Point(right - arm, y))));
            group.Children.Add(Stroke(brush, 2, new LineGeometry(new Point(right, y), new Point(right, y + arm))));
            group.Children.Add(Stroke(brush, 2, new LineGeometry(new Point(x, bottom), new Point(x + arm, bottom))));
            group.Children.Add(Stroke(brush, 2, new LineGeometry(new Point(x, bottom), new Point(x, bottom - arm))));
            group.Children.Add(Stroke(brush, 2, new LineGeometry(new Point(right, bottom), new Point(right - arm, bottom))));
            group.Children.Add(Stroke(brush, 2, new LineGeometry(new Point(right, bottom), new Point(right, bottom - arm))));
        }

        private static void AddMagnifier(DrawingGroup group, Brush dark, Brush accent)
        {
            group.Children.Add(Stroke(accent, 2.4, new EllipseGeometry(new Point(12.5, 12.5), 8, 8)));
            group.Children.Add(Stroke(dark, 3.2, new LineGeometry(new Point(18, 18), new Point(27, 27))));
        }

        private static Geometry Arc(Point center, double radius, double startDegrees, double endDegrees)
        {
            var start = Polar(center, radius, startDegrees);
            var end = Polar(center, radius, endDegrees);
            var geometry = new StreamGeometry();
            using (var context = geometry.Open())
            {
                context.BeginFigure(start, false, false);
                var span = NormalizeAngle(endDegrees - startDegrees);
                context.ArcTo(end, new Size(radius, radius), 0, span > 180, SweepDirection.Clockwise, true, false);
            }
            if (geometry.CanFreeze) geometry.Freeze();
            return geometry;
        }

        private static double NormalizeAngle(double value)
        {
            var normalized = value % 360;
            if (normalized < 0) normalized += 360;
            return normalized;
        }

        private static Point Polar(Point center, double radius, double degrees)
        {
            var radians = degrees * Math.PI / 180.0;
            return new Point(center.X + radius * Math.Cos(radians), center.Y + radius * Math.Sin(radians));
        }

        private static Geometry Triangle(Point a, Point b, Point c) => PolygonGeometry(a, b, c);
        private static Geometry Parallelogram(Point a, Point b, Point c, Point d) => PolygonGeometry(a, b, c, d);

        private static Geometry PolygonGeometry(params Point[] points)
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
            try
            {
                property.SetValue(target, Enum.Parse(property.PropertyType, enumValue, true), null);
            }
            catch
            {
                // Host-major differences may expose another size enum. Text/icon still render.
            }
        }

        private enum ViewIconKind
        {
            View3d,
            Top,
            Orbit,
            Focus,
            Isolate,
            Restore,
            SectionBox,
            SectionPlane,
            ClipDisplay,
            ZoomSelected,
            ZoomAll,
            Workspace,
            Quantity,
            Refresh
        }

        private sealed class PanelSpec
        {
            public PanelSpec(string sourceId, string title, params ButtonSpec[] buttons)
            {
                SourceId = sourceId;
                Title = title;
                Buttons = buttons;
            }

            public string SourceId { get; }
            public string Title { get; }
            public IReadOnlyList<ButtonSpec> Buttons { get; }
        }

        private sealed class ButtonSpec
        {
            public ButtonSpec(string id, string text, ViewIconKind icon)
            {
                Id = id;
                Text = text;
                Icon = icon;
            }

            public string Id { get; }
            public string Text { get; }
            public ViewIconKind Icon { get; }
        }
    }
}
