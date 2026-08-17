using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media;

namespace QS3D.BricsCAD.V25.Ribbon
{
    /// <summary>
    /// Applies a final, presentation-only icon pass to the eight QS3D-owned NHẬN DẠNG
    /// buttons after generic Ribbon decoration has completed. The artwork is clean-room,
    /// transparent-background vector geometry designed on a 32x32 grid so BricsCAD can
    /// downsample it cleanly to the compact 16px-style reference density.
    /// </summary>
    internal static class BltRecognitionIconPolisher
    {
        private const string AssemblyName = "BrxMgd";
        private const string RecognitionTabId = "QS3D_RECOGNIZE";
        private static bool _initialized;

        private enum IconKind
        {
            Restore,
            Text,
            Options,
            Table,
            Boundary,
            Label,
            Auto,
            Validate
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
                var tab = tabs == null ? null : FindById(tabs, RecognitionTabId);
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

                // The reference Recognition surface owns exactly eight semantic buttons.
                // Failing closed here keeps a partial host Ribbon tree eligible for retry.
                if (polished != 8)
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
                    polished++;
                }

                var children = GetProperty(item, "Items");
                if (children != null)
                    PolishCollection(children, ref polished);
            }
        }

        private static bool TryGetIconKind(string id, out IconKind kind)
        {
            switch (id)
            {
                case "QS3D_RECOGNIZE_BLT_RESTORE":
                    kind = IconKind.Restore;
                    return true;
                case "QS3D_RECOGNIZE_BLT_TEXT":
                    kind = IconKind.Text;
                    return true;
                case "QS3D_RECOGNIZE_BLT_OPTIONS":
                    kind = IconKind.Options;
                    return true;
                case "QS3D_RECOGNIZE_BLT_TABLE":
                    kind = IconKind.Table;
                    return true;
                case "QS3D_RECOGNIZE_BLT_BOUNDARY":
                    kind = IconKind.Boundary;
                    return true;
                case "QS3D_RECOGNIZE_BLT_LABEL":
                    kind = IconKind.Label;
                    return true;
                case "QS3D_RECOGNIZE_BLT_AUTO":
                    kind = IconKind.Auto;
                    return true;
                case "QS3D_RECOGNIZE_BLT_VALIDATE":
                    kind = IconKind.Validate;
                    return true;
                default:
                    kind = default;
                    return false;
            }
        }

        private static ImageSource CreateIcon(IconKind kind)
        {
            // Palette follows the visible reference language: saturated CAD blue for active
            // recognition actions, amber/orange for auto/selection emphasis, neutral graphite
            // for commands that BricsCAD additionally greys while disabled. The owner-requested
            // validation status pair deliberately keeps saturated red/green source artwork so
            // the final host bitmap still carries an unmistakable X / V semantic cue.
            var blue = FrozenBrush(Color.FromRgb(33, 132, 221));
            var blueDeep = FrozenBrush(Color.FromRgb(0, 86, 164));
            var blueLight = FrozenBrush(Color.FromRgb(152, 211, 249));
            var amber = FrozenBrush(Color.FromRgb(241, 164, 38));
            var orange = FrozenBrush(Color.FromRgb(228, 111, 31));
            var statusRed = FrozenBrush(Color.FromRgb(224, 62, 62));
            var statusGreen = FrozenBrush(Color.FromRgb(55, 176, 90));
            var graphite = FrozenBrush(Color.FromRgb(78, 88, 99));
            var neutral = FrozenBrush(Color.FromRgb(154, 164, 174));
            var neutralLight = FrozenBrush(Color.FromRgb(218, 224, 230));
            var paper = FrozenBrush(Color.FromRgb(242, 247, 251));

            var group = new DrawingGroup
            {
                ClipGeometry = new RectangleGeometry(new Rect(0, 0, 32, 32))
            };

            switch (kind)
            {
                case IconKind.Restore:
                    // Selection-frame corners + two-way restore arrow, matching the visual
                    // meaning of "Khôi phục đã chọn" without copying a proprietary bitmap.
                    group.Children.Add(Stroke(blue, 2.0, Geometry.Parse(
                        "M4,10 V4 H10 M22,4 H28 V10 M28,22 V28 H22 M10,28 H4 V22")));
                    group.Children.Add(Stroke(blueDeep, 2.4, Geometry.Parse(
                        "M9,16 C10,10 14,7 19,7 C23,7 26,10 27,14")));
                    group.Children.Add(Fill(blueDeep, Geometry.Parse("M27,10 L29,16 L23,15 Z")));
                    group.Children.Add(Stroke(blue, 2.4, Geometry.Parse(
                        "M24,20 C22,25 15,27 10,23 C8,21 7,19 7,17")));
                    group.Children.Add(Fill(blue, Geometry.Parse("M7,21 L5,15 L11,16 Z")));
                    break;

                case IconKind.Text:
                    // Neutral OCR/text glyph; this button is reference-disabled and the host
                    // applies its own disabled-state treatment on top of the source artwork.
                    group.Children.Add(Stroke(neutral, 1.8, Geometry.Parse(
                        "M5,10 V5 H10 M22,5 H27 V10 M27,22 V27 H22 M10,27 H5 V22")));
                    group.Children.Add(Stroke(graphite, 2.2, Geometry.Parse(
                        "M9,24 L15,8 H18 L24,24 M12,18 H21")));
                    group.Children.Add(Stroke(neutral, 1.6, Geometry.Parse("M8,27 H25")));
                    break;

                case IconKind.Options:
                    // Three compact option sliders echo the small settings/check controls in
                    // the BLT3D reference while remaining readable at 16px.
                    group.Children.Add(Stroke(blueDeep, 2.0, Geometry.Parse(
                        "M6,9 H26 M6,16 H26 M6,23 H26")));
                    group.Children.Add(Fill(paper, new EllipseGeometry(new Point(11, 9), 3.2, 3.2)));
                    group.Children.Add(Stroke(blue, 1.8, new EllipseGeometry(new Point(11, 9), 3.2, 3.2)));
                    group.Children.Add(Fill(paper, new EllipseGeometry(new Point(21, 16), 3.2, 3.2)));
                    group.Children.Add(Stroke(blue, 1.8, new EllipseGeometry(new Point(21, 16), 3.2, 3.2)));
                    group.Children.Add(Fill(paper, new EllipseGeometry(new Point(14, 23), 3.2, 3.2)));
                    group.Children.Add(Stroke(blue, 1.8, new EllipseGeometry(new Point(14, 23), 3.2, 3.2)));
                    break;

                case IconKind.Table:
                    // Compact neutral element-table glyph for the reference-disabled command.
                    group.Children.Add(Fill(neutralLight, new RectangleGeometry(new Rect(5, 6, 22, 20), 1.5, 1.5)));
                    group.Children.Add(Stroke(graphite, 1.8, new RectangleGeometry(new Rect(5, 6, 22, 20), 1.5, 1.5)));
                    group.Children.Add(Fill(neutral, new RectangleGeometry(new Rect(6, 7, 20, 5), 0.8, 0.8)));
                    group.Children.Add(Stroke(graphite, 1.4, Geometry.Parse(
                        "M5,13 H27 M5,19 H27 M12,12 V26 M20,12 V26")));
                    break;

                case IconKind.Boundary:
                    // Vertex-based polyline with one amber selected node mirrors the visible
                    // CAD-boundary semantics of "Chọn đường biên".
                    group.Children.Add(Stroke(blue, 2.2, Geometry.Parse(
                        "M6,23 L7,10 L16,6 L27,12 L24,25 Z")));
                    group.Children.Add(Stroke(amber, 2.4, Geometry.Parse("M7,10 L16,6")));
                    group.Children.Add(Fill(blueDeep, new EllipseGeometry(new Point(7, 10), 2.2, 2.2)));
                    group.Children.Add(Fill(blueDeep, new EllipseGeometry(new Point(16, 6), 2.2, 2.2)));
                    group.Children.Add(Fill(blue, new EllipseGeometry(new Point(27, 12), 2.2, 2.2)));
                    group.Children.Add(Fill(orange, new EllipseGeometry(new Point(24, 25), 2.5, 2.5)));
                    group.Children.Add(Fill(blue, new EllipseGeometry(new Point(6, 23), 2.2, 2.2)));
                    break;

                case IconKind.Label:
                    // Outlined CAD tag/label with a punch-hole and short annotation strokes.
                    group.Children.Add(Fill(paper, Geometry.Parse(
                        "M5,9 L18,9 L27,16 L18,24 L5,24 Z")));
                    group.Children.Add(Stroke(blue, 2.0, Geometry.Parse(
                        "M5,9 L18,9 L27,16 L18,24 L5,24 Z")));
                    group.Children.Add(Fill(blueDeep, new EllipseGeometry(new Point(10, 16), 2.1, 2.1)));
                    group.Children.Add(Stroke(blueDeep, 1.6, Geometry.Parse("M14,14 H20 M14,19 H21")));
                    break;

                case IconKind.Auto:
                    // Blue recognition wand + amber sparkle cluster, close to the reference's
                    // blue/orange auto-recognition visual cue.
                    group.Children.Add(Fill(blueDeep, Geometry.Parse(
                        "M5,23 L10,28 L24,14 L19,9 Z")));
                    group.Children.Add(Stroke(blueLight, 1.5, Geometry.Parse("M9,23 L20,12")));
                    group.Children.Add(Fill(amber, Geometry.Parse(
                        "M25,3 L27,8 L31,10 L27,12 L25,17 L23,12 L19,10 L23,8 Z")));
                    group.Children.Add(Fill(orange, Geometry.Parse(
                        "M28,18 L29,21 L32,22 L29,23 L28,27 L27,23 L24,22 L27,21 Z")));
                    break;

                case IconKind.Validate:
                    // Compact clean-room status pair requested by the owner: a red X and green
                    // V/check inside the same corner-target language as the Recognition reference.
                    // Keep the unsupported command disabled; this artwork only restores the cue.
                    group.Children.Add(Stroke(neutral, 1.6, Geometry.Parse(
                        "M4,10 V5 H9 M23,5 H28 V10 M28,22 V27 H23 M9,27 H4 V22")));
                    group.Children.Add(Stroke(statusRed, 3.0, Geometry.Parse(
                        "M8,11 L14,17 M14,11 L8,17")));
                    group.Children.Add(Stroke(statusGreen, 3.0, Geometry.Parse(
                        "M17,18 L21,22 L27,12")));
                    break;
            }

            group.Freeze();
            var image = new DrawingImage(group);
            image.Freeze();
            return image;
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
