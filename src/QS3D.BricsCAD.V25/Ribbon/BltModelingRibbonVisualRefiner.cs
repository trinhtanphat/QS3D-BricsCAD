using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace QS3D.BricsCAD.V25.Ribbon
{
    /// <summary>
    /// Final presentation pass for the owner-reference MODELING tab.
    ///
    /// BltModelingRibbonAugmenter owns labels, grouping and command routing. This refiner owns only
    /// deterministic dark-ribbon icon artwork and the final image/size contract. Keeping that split
    /// lets the command surface remain stable while making the visible topbar closer to the supplied
    /// BLT3D reference and preventing a text-only fallback from being accepted as initialized.
    /// </summary>
    internal static class BltModelingRibbonVisualRefiner
    {
        private const string AssemblyName = "BrxMgd";
        private const string ModelingTabId = "QS3D_MODELING";
        private const string ButtonPrefix = "QS3D_MODELING_BLT_";

        private static readonly IReadOnlyDictionary<string, IconKind> ExpectedIcons =
            new Dictionary<string, IconKind>(StringComparer.OrdinalIgnoreCase)
            {
                [ButtonPrefix + "MATERIAL"] = IconKind.Material,
                [ButtonPrefix + "STEEL_PROFILE"] = IconKind.SteelProfile,
                [ButtonPrefix + "CREATE_DETAIL"] = IconKind.Detail,
                [ButtonPrefix + "PLANE_XY"] = IconKind.Plane,
                [ButtonPrefix + "LINE"] = IconKind.Line,
                [ButtonPrefix + "POLYLINE"] = IconKind.Polyline,
                [ButtonPrefix + "RECTANGLE"] = IconKind.Rectangle,
                [ButtonPrefix + "CIRCLE"] = IconKind.Circle,
                [ButtonPrefix + "ARC"] = IconKind.Arc,
                [ButtonPrefix + "JOIN_POLYLINE"] = IconKind.JoinPolyline,
                [ButtonPrefix + "OFFSET"] = IconKind.Offset,
                [ButtonPrefix + "MOVE"] = IconKind.Move,
                [ButtonPrefix + "COPY"] = IconKind.Copy,
                [ButtonPrefix + "MOVE_Z"] = IconKind.MoveZ,
                [ButtonPrefix + "EXTRUDE"] = IconKind.Extrude,
                [ButtonPrefix + "SWEEP"] = IconKind.Sweep,
                [ButtonPrefix + "LOFT"] = IconKind.Loft,
                [ButtonPrefix + "ATTACH_FAMILY"] = IconKind.Family,
                [ButtonPrefix + "UNION"] = IconKind.Union,
                [ButtonPrefix + "SUBTRACT"] = IconKind.Subtract,
                [ButtonPrefix + "INTERSECT"] = IconKind.Intersect,
            };

        private static readonly HashSet<string> LargeButtons = new HashSet<string>(
            new[]
            {
                ButtonPrefix + "MATERIAL",
                ButtonPrefix + "STEEL_PROFILE",
                ButtonPrefix + "CREATE_DETAIL",
                ButtonPrefix + "PLANE_XY",
            },
            StringComparer.OrdinalIgnoreCase);

        private static bool _initialized;

        private enum IconKind
        {
            Material,
            SteelProfile,
            Detail,
            Plane,
            Line,
            Polyline,
            Rectangle,
            Circle,
            Arc,
            JoinPolyline,
            Offset,
            Move,
            Copy,
            MoveZ,
            Extrude,
            Sweep,
            Loft,
            Family,
            Union,
            Subtract,
            Intersect,
        }

        public static bool TryInitialize()
        {
            if (_initialized)
                return true;

            try
            {
                var ribbon = FindRibbonControl();
                if (ribbon == null)
                    return false;

                var tabs = GetProperty(ribbon, "Tabs");
                var modeling = tabs == null ? null : FindById(tabs, ModelingTabId);
                if (modeling == null)
                    return false;

                var panels = GetProperty(modeling, "Panels");
                if (panels == null)
                    return false;

                var buttons = FindOwnedButtons(panels);
                if (buttons.Count != ExpectedIcons.Count)
                    return false;

                foreach (var expected in ExpectedIcons)
                {
                    if (!buttons.TryGetValue(expected.Key, out var button))
                        return false;

                    SetProperty(button, "ShowText", true);
                    SetProperty(button, "ShowImage", true);
                    SetEnumProperty(button, "Size", LargeButtons.Contains(expected.Key) ? "Large" : "Standard");

                    // BricsCAD reliably consumes exact-size frozen bitmaps for Ribbon images. Keep
                    // the clean-room 32x32 vector geometry as the artwork source, but rasterize it
                    // separately for the host's compact and large slots instead of handing the host
                    // one raw DrawingImage for both properties.
                    var icon = CreateReferenceIcon(expected.Value, 16);
                    var largeIcon = CreateReferenceIcon(expected.Value, 32);
                    SetProperty(button, "Image", icon);
                    SetProperty(button, "LargeImage", largeIcon);

                    if (!(GetProperty(button, "Image") is RenderTargetBitmap)
                        || !(GetProperty(button, "LargeImage") is RenderTargetBitmap))
                        return false;
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

        private static Dictionary<string, object> FindOwnedButtons(object panels)
        {
            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in EnumerateRibbonItems(panels))
            {
                if (!string.Equals(item.GetType().Name, "RibbonButton", StringComparison.Ordinal))
                    continue;

                var id = GetProperty(item, "Id") as string;
                if (id == null || id.Length == 0 || !id.StartsWith(ButtonPrefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                // Duplicate IDs are an invalid final presentation because only one button can own
                // the deterministic reference icon/route. Make the retry coordinator fail closed.
                if (result.ContainsKey(id))
                    return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

                result[id] = item;
            }

            return result;
        }

        private static IEnumerable<object> EnumerateRibbonItems(object root)
        {
            if (!(root is IEnumerable enumerable))
                yield break;

            foreach (var item in enumerable)
            {
                if (item == null)
                    continue;

                yield return item;

                var source = GetProperty(item, "Source");
                var sourceItems = source == null ? null : GetProperty(source, "Items");
                if (sourceItems != null)
                {
                    foreach (var nested in EnumerateRibbonItems(sourceItems))
                        yield return nested;
                }

                var childItems = GetProperty(item, "Items");
                if (childItems != null)
                {
                    foreach (var nested in EnumerateRibbonItems(childItems))
                        yield return nested;
                }
            }
        }

        private static ImageSource CreateReferenceIcon(IconKind kind, int pixelSize)
        {
            if (pixelSize != 16 && pixelSize != 32)
                throw new ArgumentOutOfRangeException(nameof(pixelSize));

            var blue = FrozenBrush(Color.FromRgb(35, 132, 242));
            var blueDark = FrozenBrush(Color.FromRgb(15, 82, 178));
            var blueSoft = FrozenBrush(Color.FromRgb(111, 184, 255));
            var pale = FrozenBrush(Color.FromRgb(203, 226, 250));
            var light = FrozenBrush(Color.FromRgb(205, 214, 225));
            var group = new DrawingGroup();

            // Pin every glyph to the same 32x32 logical canvas. Without this invisible frame WPF
            // derives bounds from the visible geometry, which makes otherwise similar Ribbon icons
            // appear at inconsistent scales after BricsCAD fits them into 16px/32px slots.
            group.Children.Add(Fill(Brushes.Transparent, new RectangleGeometry(new Rect(0, 0, 32, 32))));

            switch (kind)
            {
                case IconKind.Material:
                    group.Children.Add(Fill(blueDark, Geometry.Parse("M4,8 L16,2.5 28,8 16,13.5 Z")));
                    group.Children.Add(Fill(blue, Geometry.Parse("M4,14 L16,8.5 28,14 16,19.5 Z")));
                    group.Children.Add(Fill(blueSoft, Geometry.Parse("M4,20 L16,14.5 28,20 16,26 Z")));
                    break;

                case IconKind.SteelProfile:
                    group.Children.Add(Stroke(blue, 2.6, Geometry.Parse("M4,25 L10,8 L18,19 L28,5")));
                    group.Children.Add(Fill(blueDark, new EllipseGeometry(new Point(10, 8), 2.1, 2.1)));
                    group.Children.Add(Fill(blueDark, new EllipseGeometry(new Point(18, 19), 2.1, 2.1)));
                    group.Children.Add(Stroke(light, 1.5, new LineGeometry(new Point(4, 28), new Point(28, 28))));
                    break;

                case IconKind.Detail:
                    group.Children.Add(Stroke(blue, 2.4, new RectangleGeometry(new Rect(5, 6, 22, 19), 1, 1)));
                    group.Children.Add(Stroke(light, 1.5, new LineGeometry(new Point(5, 12), new Point(27, 12))));
                    group.Children.Add(Fill(pale, new RectangleGeometry(new Rect(9, 15, 14, 7), 1, 1)));
                    group.Children.Add(Stroke(blueDark, 1.4, new RectangleGeometry(new Rect(9, 15, 14, 7), 1, 1)));
                    break;

                case IconKind.Plane:
                    group.Children.Add(Fill(blueSoft, Geometry.Parse("M4,21 L12,7 29,11 21,26 Z")));
                    group.Children.Add(Stroke(blueDark, 2.0, Geometry.Parse("M4,21 L12,7 29,11 21,26 Z")));
                    group.Children.Add(Stroke(light, 1.3, new LineGeometry(new Point(10, 18), new Point(24, 21))));
                    break;

                case IconKind.Line:
                    group.Children.Add(Stroke(blue, 2.5, new LineGeometry(new Point(5, 25), new Point(27, 7))));
                    group.Children.Add(Fill(light, new EllipseGeometry(new Point(5, 25), 2, 2)));
                    group.Children.Add(Fill(blueDark, new EllipseGeometry(new Point(27, 7), 2, 2)));
                    break;

                case IconKind.Polyline:
                    group.Children.Add(Stroke(blue, 2.5, Geometry.Parse("M4,25 L10,8 L18,19 L28,6")));
                    group.Children.Add(Fill(blueDark, new EllipseGeometry(new Point(10, 8), 2, 2)));
                    group.Children.Add(Fill(light, new EllipseGeometry(new Point(18, 19), 2, 2)));
                    break;

                case IconKind.Rectangle:
                    group.Children.Add(Stroke(blue, 2.4, new RectangleGeometry(new Rect(5, 7, 22, 18), 1, 1)));
                    group.Children.Add(Fill(blueDark, new EllipseGeometry(new Point(5, 7), 1.6, 1.6)));
                    group.Children.Add(Fill(light, new EllipseGeometry(new Point(27, 25), 1.6, 1.6)));
                    break;

                case IconKind.Circle:
                    group.Children.Add(Stroke(blue, 2.4, new EllipseGeometry(new Point(16, 16), 10, 10)));
                    group.Children.Add(Fill(blueDark, new EllipseGeometry(new Point(16, 16), 1.7, 1.7)));
                    break;

                case IconKind.Arc:
                    group.Children.Add(Stroke(blue, 2.5, Geometry.Parse("M5,24 C8,8 24,5 28,20")));
                    group.Children.Add(Fill(light, new EllipseGeometry(new Point(5, 24), 1.8, 1.8)));
                    group.Children.Add(Fill(blueDark, new EllipseGeometry(new Point(28, 20), 1.8, 1.8)));
                    break;

                case IconKind.JoinPolyline:
                    group.Children.Add(Stroke(blue, 2.5, Geometry.Parse("M4,9 L13,16 M28,9 L19,16")));
                    group.Children.Add(Stroke(blueDark, 2.5, new LineGeometry(new Point(13, 16), new Point(19, 16))));
                    group.Children.Add(Fill(light, new EllipseGeometry(new Point(16, 16), 2.2, 2.2)));
                    break;

                case IconKind.Offset:
                    group.Children.Add(Stroke(blue, 2.3, Geometry.Parse("M5,24 C8,12 17,7 27,8")));
                    group.Children.Add(Stroke(blueSoft, 2.3, Geometry.Parse("M4,17 C8,7 17,3 27,4")));
                    break;

                case IconKind.Move:
                    DrawMoveIcon(group, blue);
                    break;

                case IconKind.Copy:
                    group.Children.Add(Fill(pale, new RectangleGeometry(new Rect(10, 5, 17, 17), 1, 1)));
                    group.Children.Add(Stroke(light, 1.6, new RectangleGeometry(new Rect(10, 5, 17, 17), 1, 1)));
                    group.Children.Add(Fill(blue, new RectangleGeometry(new Rect(5, 10, 17, 17), 1, 1)));
                    group.Children.Add(Stroke(blueDark, 1.4, new RectangleGeometry(new Rect(5, 10, 17, 17), 1, 1)));
                    break;

                case IconKind.MoveZ:
                    group.Children.Add(Stroke(blue, 2.6, new LineGeometry(new Point(16, 27), new Point(16, 5))));
                    group.Children.Add(Fill(blue, Geometry.Parse("M16,3 L11,10 21,10 Z")));
                    group.Children.Add(Stroke(light, 1.7, new LineGeometry(new Point(8, 26), new Point(24, 26))));
                    group.Children.Add(Stroke(blueDark, 1.8, Geometry.Parse("M22,12 L27,12 22,20 27,20")));
                    break;

                case IconKind.Extrude:
                    group.Children.Add(Stroke(blue, 2.0, new RectangleGeometry(new Rect(5, 17, 12, 10), 1, 1)));
                    group.Children.Add(Stroke(blueSoft, 2.0, new RectangleGeometry(new Rect(15, 6, 12, 10), 1, 1)));
                    group.Children.Add(Stroke(light, 1.5, Geometry.Parse("M5,17 L15,6 M17,17 L27,6 M17,27 L27,16")));
                    break;

                case IconKind.Sweep:
                    group.Children.Add(Stroke(blue, 2.3, Geometry.Parse("M6,25 C8,9 19,7 28,12")));
                    group.Children.Add(Stroke(blueDark, 2.0, new EllipseGeometry(new Point(7, 24), 4, 4)));
                    group.Children.Add(Fill(pale, new EllipseGeometry(new Point(27, 12), 4, 4)));
                    group.Children.Add(Stroke(light, 1.2, new EllipseGeometry(new Point(27, 12), 4, 4)));
                    break;

                case IconKind.Loft:
                    group.Children.Add(Stroke(light, 1.7, new EllipseGeometry(new Point(16, 7), 7, 3)));
                    group.Children.Add(Stroke(blue, 2.0, new EllipseGeometry(new Point(16, 25), 11, 4)));
                    group.Children.Add(Stroke(blueSoft, 1.7, Geometry.Parse("M9,7 C8,14 6,19 5,25 M23,7 C24,14 26,19 27,25")));
                    break;

                case IconKind.Family:
                    group.Children.Add(Fill(pale, Geometry.Parse("M5,10 L15,5 25,10 15,15 Z")));
                    group.Children.Add(Fill(blue, Geometry.Parse("M5,10 L15,15 15,27 5,22 Z")));
                    group.Children.Add(Fill(blueDark, Geometry.Parse("M25,10 L15,15 15,27 25,22 Z")));
                    group.Children.Add(Stroke(light, 1.7, Geometry.Parse("M23,5 L29,5 M26,2 L26,8")));
                    break;

                case IconKind.Union:
                    DrawBooleanIcon(group, blue, blueSoft, blueDark, BooleanMode.Union);
                    break;

                case IconKind.Subtract:
                    DrawBooleanIcon(group, blue, pale, blueDark, BooleanMode.Subtract);
                    break;

                case IconKind.Intersect:
                    DrawBooleanIcon(group, pale, pale, blueDark, BooleanMode.Intersect);
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

            var image = new RenderTargetBitmap(pixelSize, pixelSize, 96, 96, PixelFormats.Pbgra32);
            image.Render(visual);
            image.Freeze();
            return image;
        }

        private enum BooleanMode
        {
            Union,
            Subtract,
            Intersect,
        }

        private static void DrawMoveIcon(DrawingGroup group, Brush blue)
        {
            group.Children.Add(Stroke(blue, 2.3, new LineGeometry(new Point(5, 16), new Point(27, 16))));
            group.Children.Add(Stroke(blue, 2.3, new LineGeometry(new Point(16, 5), new Point(16, 27))));
            group.Children.Add(Fill(blue, Geometry.Parse(
                "M3,16 L9,12 9,20 Z M29,16 L23,12 23,20 Z M16,3 L12,9 20,9 Z M16,29 L12,23 20,23 Z")));
        }

        private static void DrawBooleanIcon(
            DrawingGroup group,
            Brush left,
            Brush right,
            Brush accent,
            BooleanMode mode)
        {
            group.Children.Add(Fill(left, new EllipseGeometry(new Point(12, 16), 8, 8)));
            group.Children.Add(Fill(right, new EllipseGeometry(new Point(20, 16), 8, 8)));
            group.Children.Add(Stroke(accent, 1.7, new EllipseGeometry(new Point(12, 16), 8, 8)));
            group.Children.Add(Stroke(accent, 1.7, new EllipseGeometry(new Point(20, 16), 8, 8)));

            if (mode == BooleanMode.Subtract)
                group.Children.Add(Stroke(accent, 2.0, new LineGeometry(new Point(18, 16), new Point(25, 16))));
            else if (mode == BooleanMode.Intersect)
                group.Children.Add(Fill(accent, Geometry.Parse("M16,9 C12,12 12,20 16,23 C20,20 20,12 16,9 Z")));
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
            var pen = new Pen(brush, thickness);
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

        private static void SetEnumProperty(object target, string name, string enumValue)
        {
            var property = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            if (property == null || !property.CanWrite || !property.PropertyType.IsEnum)
                return;

            try
            {
                property.SetValue(target, Enum.Parse(property.PropertyType, enumValue, true), null);
            }
            catch
            {
                // Host version may expose a different size enum; image/text remain usable.
            }
        }
    }
}
