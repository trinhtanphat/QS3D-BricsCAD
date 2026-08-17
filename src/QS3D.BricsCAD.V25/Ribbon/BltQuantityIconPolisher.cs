using System;
using System.Collections;
using System.Reflection;
using System.Windows;
using System.Windows.Media;

namespace QS3D.BricsCAD.V25.Ribbon
{
    /// <summary>
    /// Final presentation pass for the six QS3D-owned ĐỊNH LƯỢNG commands.
    /// QuantityReferenceRibbonAugmenter creates the buttons early, but BricsCAD may rebuild or
    /// normalize Ribbon item presentation later in the same initialization lifecycle. Reapply
    /// clean-room owner-reference artwork after the generic icon pass and verify the host kept
    /// both Image and LargeImage so a text-only quantity surface remains retryable instead of
    /// being accepted as initialized.
    /// </summary>
    internal static class BltQuantityIconPolisher
    {
        private const string AssemblyName = "BrxMgd";
        private const string QuantityTabId = "QS3D_QTY";
        private static bool _initialized;

        private enum IconKind
        {
            Settings,
            Calculate,
            Export,
            View,
            Explain,
            Compare
        }

        public static bool TryInitialize()
        {
            if (_initialized)
                return true;

            try
            {
                var control = FindRibbonControl();
                if (control == null)
                    return false;

                var tabs = GetProperty(control, "Tabs");
                var tab = tabs == null ? null : FindById(tabs, QuantityTabId);
                if (tab == null)
                    return false;

                var panels = GetProperty(tab, "Panels");
                if (!(panels is IEnumerable enumerablePanels))
                    return false;

                var polished = 0;
                foreach (var panel in enumerablePanels)
                {
                    if (panel == null)
                        continue;
                    var source = GetProperty(panel, "Source");
                    var items = source == null ? null : GetProperty(source, "Items");
                    if (items != null)
                        PolishCollection(items, ref polished);
                }

                // The BLT3D-reference quantity surface owns exactly one settings command and
                // five quantity commands. A partial image assignment must not become sticky.
                if (polished != 6)
                    return false;

                _initialized = true;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void Reset() => _initialized = false;

        private static void PolishCollection(object collection, ref int polished)
        {
            if (!(collection is IEnumerable enumerable))
                return;

            foreach (var item in enumerable)
            {
                if (item == null)
                    continue;

                var id = GetProperty(item, "Id") as string;
                if (!string.IsNullOrWhiteSpace(id) && TryGetIconKind(id!, out var kind))
                {
                    var image = CreateIcon(kind);
                    SetProperty(item, "Image", image);
                    SetProperty(item, "LargeImage", image);
                    SetProperty(item, "ShowImage", true);
                    SetEnumProperty(item, "Size", "Large");

                    // Read back the host properties. The old source guard only proved that
                    // assignment statements existed; it could not detect a reflection/type
                    // mismatch that silently left the native Ribbon button text-only.
                    if (HasCompleteVisibleIcon(item))
                        polished++;
                }

                var children = GetProperty(item, "Items");
                if (children != null)
                    PolishCollection(children, ref polished);
            }
        }

        private static bool HasCompleteVisibleIcon(object item) =>
            GetProperty(item, "ShowImage") is bool showImage
            && showImage
            && GetProperty(item, "Image") != null
            && GetProperty(item, "LargeImage") != null;

        private static bool TryGetIconKind(string id, out IconKind kind)
        {
            switch (id)
            {
                case "QS3D_QTY_BLT_SETTINGS":
                    kind = IconKind.Settings;
                    return true;
                case "QS3D_QTY_BLT_CALCULATE":
                    kind = IconKind.Calculate;
                    return true;
                case "QS3D_QTY_BLT_EXPORT":
                    kind = IconKind.Export;
                    return true;
                case "QS3D_QTY_BLT_VIEW":
                    kind = IconKind.View;
                    return true;
                case "QS3D_QTY_BLT_EXPLAIN":
                    kind = IconKind.Explain;
                    return true;
                case "QS3D_QTY_BLT_COMPARE":
                    kind = IconKind.Compare;
                    return true;
                default:
                    kind = default;
                    return false;
            }
        }

        private static ImageSource CreateIcon(IconKind kind)
        {
            // Clean-room vector recreation of the visual language visible in the supplied
            // reference screenshot: bright CAD blue + white bodywork, green export direction,
            // and amber accents. No BLT3D bitmap/resource is embedded or copied.
            var blue = FrozenBrush(Color.FromRgb(41, 139, 229));
            var blueDeep = FrozenBrush(Color.FromRgb(0, 83, 157));
            var blueLight = FrozenBrush(Color.FromRgb(139, 203, 249));
            var bluePale = FrozenBrush(Color.FromRgb(213, 237, 253));
            var paper = FrozenBrush(Color.FromRgb(246, 250, 253));
            var amber = FrozenBrush(Color.FromRgb(241, 161, 43));
            var green = FrozenBrush(Color.FromRgb(66, 190, 101));

            var group = new DrawingGroup
            {
                ClipGeometry = new RectangleGeometry(new Rect(0, 0, 32, 32))
            };

            switch (kind)
            {
                case IconKind.Settings:
                    // Calculator/table body plus blue gear, matching the reference silhouette.
                    group.Children.Add(Fill(paper, new RectangleGeometry(new Rect(4.5, 3.5, 18, 25), 1.5, 1.5)));
                    group.Children.Add(Stroke(blueDeep, 1.7, new RectangleGeometry(new Rect(4.5, 3.5, 18, 25), 1.5, 1.5)));
                    group.Children.Add(Fill(blueLight, new RectangleGeometry(new Rect(7.5, 6.5, 12, 4.5), 0.8, 0.8)));
                    for (var row = 0; row < 3; row++)
                    {
                        for (var column = 0; column < 3; column++)
                        {
                            group.Children.Add(Fill(
                                blue,
                                new RectangleGeometry(new Rect(7.5 + column * 4.2, 14 + row * 4.2, 2.5, 2.5), 0.45, 0.45)));
                        }
                    }
                    AddGear(group, blueDeep, bluePale, new Point(24.5, 23.5), 4.8);
                    break;

                case IconKind.Calculate:
                    AddReferenceCube(group, blue, blueDeep, blueLight, paper);
                    // Amber lightning bolt is the distinctive Engine2 cue in the screenshot.
                    group.Children.Add(Fill(amber, Geometry.Parse(
                        "M25,10 L20.5,18 H24 L21.5,27 L30,16 H26.5 L29,10 Z")));
                    group.Children.Add(Stroke(paper, 0.8, Geometry.Parse(
                        "M25,10 L20.5,18 H24 L21.5,27 L30,16 H26.5 L29,10")));
                    break;

                case IconKind.Export:
                    AddReferenceCube(group, blue, blueDeep, blueLight, paper);
                    // Green upward export arrow rises from the open cube in the reference.
                    group.Children.Add(Stroke(green, 2.7, new LineGeometry(new Point(16, 15), new Point(16, 4.5))));
                    group.Children.Add(Fill(green, Geometry.Parse("M11.5,8.5 L16,3 L20.5,8.5 Z")));
                    break;

                case IconKind.View:
                    // Four-column quantity chart with the same dark/light blue cadence.
                    group.Children.Add(Stroke(blueDeep, 1.6, new LineGeometry(new Point(4, 27), new Point(29, 27))));
                    group.Children.Add(Fill(blueDeep, new RectangleGeometry(new Rect(6, 19, 4.2, 8), 0.6, 0.6)));
                    group.Children.Add(Fill(blue, new RectangleGeometry(new Rect(12, 13, 4.2, 14), 0.6, 0.6)));
                    group.Children.Add(Fill(blueDeep, new RectangleGeometry(new Rect(18, 7, 4.2, 20), 0.6, 0.6)));
                    group.Children.Add(Fill(blueLight, new RectangleGeometry(new Rect(24, 21, 4.2, 6), 0.6, 0.6)));
                    break;

                case IconKind.Explain:
                    // White report/table sheet with blue header and amber row accents.
                    group.Children.Add(Fill(paper, new RectangleGeometry(new Rect(4.5, 4, 23, 24), 1.5, 1.5)));
                    group.Children.Add(Stroke(blueDeep, 1.7, new RectangleGeometry(new Rect(4.5, 4, 23, 24), 1.5, 1.5)));
                    group.Children.Add(Fill(blue, new RectangleGeometry(new Rect(7, 7, 18, 4), 0.7, 0.7)));
                    group.Children.Add(Fill(amber, new RectangleGeometry(new Rect(8, 14, 5, 2.2), 0.4, 0.4)));
                    group.Children.Add(Stroke(blue, 1.45, new LineGeometry(new Point(15, 15.1), new Point(24, 15.1))));
                    group.Children.Add(Fill(blueLight, new RectangleGeometry(new Rect(8, 19, 5, 2.2), 0.4, 0.4)));
                    group.Children.Add(Stroke(blue, 1.45, new LineGeometry(new Point(15, 20.1), new Point(24, 20.1))));
                    group.Children.Add(Fill(blueLight, new RectangleGeometry(new Rect(8, 24, 5, 1.8), 0.4, 0.4)));
                    group.Children.Add(Stroke(blue, 1.35, new LineGeometry(new Point(15, 24.9), new Point(22.5, 24.9))));
                    break;

                case IconKind.Compare:
                    // Blue balance with amber pivot/knob: the most recognizable Cũ/Mới cue.
                    group.Children.Add(Fill(amber, new EllipseGeometry(new Point(16, 4.5), 2.4, 2.4)));
                    group.Children.Add(Stroke(blueDeep, 2.3, new LineGeometry(new Point(16, 7), new Point(16, 26.5))));
                    group.Children.Add(Stroke(blue, 2.0, Geometry.Parse("M5,10 C10,12 22,12 27,10")));
                    group.Children.Add(Fill(amber, new EllipseGeometry(new Point(16, 10.7), 1.8, 1.8)));
                    group.Children.Add(Stroke(blueDeep, 1.5, new LineGeometry(new Point(8, 11), new Point(8, 18))));
                    group.Children.Add(Stroke(blueDeep, 1.5, new LineGeometry(new Point(24, 11), new Point(24, 18))));
                    group.Children.Add(Stroke(blue, 1.8, Geometry.Parse("M3.5,18 C5,22 11,22 12.5,18")));
                    group.Children.Add(Stroke(blue, 1.8, Geometry.Parse("M19.5,18 C21,22 27,22 28.5,18")));
                    group.Children.Add(Stroke(blueDeep, 2.2, new LineGeometry(new Point(10.5, 27), new Point(21.5, 27))));
                    break;
            }

            group.Freeze();
            var image = new DrawingImage(group);
            image.Freeze();
            return image;
        }

        private static void AddReferenceCube(
            DrawingGroup group,
            Brush blue,
            Brush blueDeep,
            Brush blueLight,
            Brush paper)
        {
            group.Children.Add(Fill(blueLight, Geometry.Parse("M5,9 L16,3.5 L27,9 L16,15 Z")));
            group.Children.Add(Fill(paper, Geometry.Parse("M5,9 L16,15 L16,27.5 L5,21.5 Z")));
            group.Children.Add(Fill(blue, Geometry.Parse("M16,15 L27,9 L27,21.5 L16,27.5 Z")));
            group.Children.Add(Stroke(blueDeep, 1.5, Geometry.Parse(
                "M5,9 L16,3.5 L27,9 L27,21.5 L16,27.5 L5,21.5 Z M5,9 L16,15 L27,9 M16,15 V27.5")));
            group.Children.Add(Stroke(paper, 1.4, new LineGeometry(new Point(16, 4.5), new Point(16, 13.5))));
        }

        private static void AddGear(DrawingGroup group, Brush outer, Brush inner, Point center, double radius)
        {
            for (var index = 0; index < 8; index++)
            {
                var angle = index * Math.PI / 4.0;
                var x1 = center.X + Math.Cos(angle) * (radius - 0.7);
                var y1 = center.Y + Math.Sin(angle) * (radius - 0.7);
                var x2 = center.X + Math.Cos(angle) * (radius + 1.6);
                var y2 = center.Y + Math.Sin(angle) * (radius + 1.6);
                group.Children.Add(Stroke(outer, 2.2, new LineGeometry(new Point(x1, y1), new Point(x2, y2))));
            }

            group.Children.Add(Fill(outer, new EllipseGeometry(center, radius, radius)));
            group.Children.Add(Fill(inner, new EllipseGeometry(center, 1.8, 1.8)));
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

        private static void SetEnumProperty(object target, string name, string value)
        {
            var property = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            if (property == null || !property.CanWrite || !property.PropertyType.IsEnum)
                return;

            try
            {
                property.SetValue(target, Enum.Parse(property.PropertyType, value, true), null);
            }
            catch
            {
                // Host variants may expose a different size enum; image read-back still guards
                // the essential icon contract and allows the coordinator to retry if needed.
            }
        }
    }
}
