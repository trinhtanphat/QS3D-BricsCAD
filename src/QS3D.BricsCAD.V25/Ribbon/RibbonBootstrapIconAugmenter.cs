using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace QS3D.BricsCAD.V25.Ribbon
{
    /// <summary>
    /// Fills missing images on the canonical QS3D Ribbon after every richer feature augmenter
    /// has reconciled. Existing Home/Draw/custom images are preserved except for a small set of
    /// owner-approved semantic overrides where a generic/brand image is known to be incorrect.
    /// </summary>
    internal static class RibbonBootstrapIconAugmenter
    {
        private const string AssemblyName = "BrxMgd";

        private static readonly string[] TargetTabIds =
        {
            "QS3D_HOME",
            "QS3D_PROJECT",
            "QS3D_AUTHOR",
            "QS3D_BIM",
            "QS3D_RECOGNIZE",
            "QS3D_DRAW",
            "QS3D_TOOL",
            "QS3D_MODELING",
            "QS3D_VIEW",
            "QS3D_QTY",
            "QS3D_REV"
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
                if (tabs == null) return false;

                foreach (var tabId in TargetTabIds)
                {
                    if (!ApplyIconsToTab(tabs, tabId))
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

        private static bool ApplyIconsToTab(object tabs, string tabId)
        {
            var tab = FindById(tabs, tabId);
            if (tab == null) return false;

            var panels = GetProperty(tab, "Panels");
            if (!(panels is IEnumerable panelEnumerable)) return false;

            var commandButtons = 0;
            var visited = new HashSet<object>();
            foreach (var panel in panelEnumerable)
            {
                if (panel == null) continue;
                var source = GetProperty(panel, "Source");
                if (source == null) continue;
                var items = GetProperty(source, "Items");
                if (!(items is IEnumerable itemEnumerable)) continue;

                foreach (var item in itemEnumerable)
                    DecorateItem(item, visited, ref commandButtons);
            }

            return commandButtons > 0;
        }

        private static void DecorateItem(object? item, HashSet<object> visited, ref int commandButtons)
        {
            if (item == null || !visited.Add(item)) return;

            if (GetProperty(item, "CommandParameter") is string command
                && !string.IsNullOrWhiteSpace(command))
            {
                commandButtons++;
                var text = (GetProperty(item, "Text") as string)
                           ?? (GetProperty(item, "Name") as string)
                           ?? string.Empty;

                // These three commands are explicit owner-facing fixes. Apply them even when a
                // previous augmenter supplied a complete image, because those complete images can
                // still be semantically wrong (generic Objects or the QS3D product brand).
                if (IsSystemObjects(command))
                {
                    ApplySemanticIcon(item, RibbonIconKind.Model3d, makeLarge: true);
                }
                else if (IsProjectInfo(command))
                {
                    ApplySemanticIcon(item, RibbonIconKind.Inspect, makeLarge: true);
                }
                else if (IsIfcRemove(command, text))
                {
                    SetProperty(item, "ShowImage", true);
                    SetProperty(item, "Image", CreateIfcRemoveIcon(16));
                    SetProperty(item, "LargeImage", CreateIfcRemoveIcon(32));
                    // IFC import/add/remove are primary, full-height actions in the richer ribbon
                    // and the BIM mirror. Keep the fallback path at the same BricsCAD visual weight.
                    SetEnumProperty(item, "Size", "Large");
                }
                else if (!HasCompleteVisibleIcon(item))
                {
                    var icon = ResolveIcon(command, text);
                    SetProperty(item, "ShowImage", true);
                    SetProperty(item, "Image", CreateIcon(icon, 16));
                    SetProperty(item, "LargeImage", CreateIcon(icon, 32));
                }
            }

            // Rich ribbon augmenters may wrap buttons in row/stack containers. Recurse so
            // the same icon policy also covers their fallback layouts without replacing
            // images already supplied by those augmenters. Guard cycles/shared host objects.
            var nested = GetProperty(item, "Items");
            if (!(nested is IEnumerable nestedEnumerable)) return;
            foreach (var child in nestedEnumerable)
                DecorateItem(child, visited, ref commandButtons);
        }

        private static void ApplySemanticIcon(object item, RibbonIconKind icon, bool makeLarge)
        {
            SetProperty(item, "ShowImage", true);
            SetProperty(item, "Image", CreateIcon(icon, 16));
            SetProperty(item, "LargeImage", CreateIcon(icon, 32));
            if (makeLarge)
                SetEnumProperty(item, "Size", "Large");
        }

        private static bool IsSystemObjects(string command) =>
            command.IndexOf("QS3D_HOME_SYSTEM_OBJECTS", StringComparison.OrdinalIgnoreCase) >= 0
            || command.IndexOf("QS3DSYSTEMOBJECTS", StringComparison.OrdinalIgnoreCase) >= 0;

        private static bool IsProjectInfo(string command) =>
            command.IndexOf("QS3DPROJECTINFO", StringComparison.OrdinalIgnoreCase) >= 0
            || command.IndexOf("QS3D_PROJECT_INFO", StringComparison.OrdinalIgnoreCase) >= 0;

        private static bool IsIfcRemove(string command, string text) =>
            command.IndexOf("IFCREMOVE", StringComparison.OrdinalIgnoreCase) >= 0
            || command.IndexOf("IFC_REMOVE", StringComparison.OrdinalIgnoreCase) >= 0
            || text.IndexOf("Xóa IFC", StringComparison.OrdinalIgnoreCase) >= 0
            || text.IndexOf("Xoa IFC", StringComparison.OrdinalIgnoreCase) >= 0
            || text.IndexOf("Remove IFC", StringComparison.OrdinalIgnoreCase) >= 0;

        private static object CreateIcon(RibbonIconKind icon, int pixelSize)
        {
            // The product logo is reserved for explicitly branded product-identity actions.
            // Functional or unclassified command buttons use semantic/neutral icons instead.
            if (icon == RibbonIconKind.Qs3dLogo)
                return Qs3dBrandIconFactory.Create(pixelSize);

            // RibbonIconFactory uses compact path-data strings for a few generated shapes.
            // Keep rendering invariant even on Windows installations that use a comma decimal
            // separator, then restore the host UI thread culture immediately afterward.
            var thread = Thread.CurrentThread;
            var previous = thread.CurrentCulture;
            try
            {
                thread.CurrentCulture = CultureInfo.InvariantCulture;
                return RibbonIconFactory.Create(icon, pixelSize);
            }
            finally
            {
                thread.CurrentCulture = previous;
            }
        }

        private static object CreateIfcRemoveIcon(int pixelSize)
        {
            var blue = new SolidColorBrush(Color.FromRgb(34, 137, 245));
            var blueDark = new SolidColorBrush(Color.FromRgb(13, 77, 172));
            var light = new SolidColorBrush(Color.FromRgb(224, 238, 255));
            var red = new SolidColorBrush(Color.FromRgb(220, 65, 65));
            blue.Freeze();
            blueDark.Freeze();
            light.Freeze();
            red.Freeze();

            var group = new DrawingGroup();
            group.Children.Add(new GeometryDrawing(blueDark, null, new RectangleGeometry(new Rect(4, 4, 19, 24), 2, 2)));
            group.Children.Add(new GeometryDrawing(blue, null, new RectangleGeometry(new Rect(7, 7, 13, 18), 1, 1)));
            group.Children.Add(new GeometryDrawing(light, null, new RectangleGeometry(new Rect(9, 10, 9, 2), 0.6, 0.6)));
            group.Children.Add(new GeometryDrawing(light, null, new RectangleGeometry(new Rect(9, 15, 9, 2), 0.6, 0.6)));

            group.Children.Add(new GeometryDrawing(red, null, new EllipseGeometry(new Point(24, 23), 7, 7)));
            var deletePen = new Pen(Brushes.White, 2.2)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round
            };
            deletePen.Freeze();
            group.Children.Add(new GeometryDrawing(null, deletePen, new LineGeometry(new Point(20.5, 19.5), new Point(27.5, 26.5))));
            group.Children.Add(new GeometryDrawing(null, deletePen, new LineGeometry(new Point(27.5, 19.5), new Point(20.5, 26.5))));
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

        private static bool HasCompleteVisibleIcon(object item) =>
            GetProperty(item, "ShowImage") is bool showImage
            && showImage
            && GetProperty(item, "Image") != null
            && GetProperty(item, "LargeImage") != null;

        private static RibbonIconKind ResolveIcon(string command, string text)
        {
            var normalized = (command + " " + text).Trim().ToUpperInvariant();

            // Brand identity is intentional only for the product start entry itself.
            if (ContainsAny(normalized, "QS3DSTART", "START CENTER"))
                return RibbonIconKind.Qs3dLogo;
            if (ContainsAny(normalized, "FAMIL", "FAMILY / TYPE"))
                return RibbonIconKind.Objects;
            if (ContainsAny(normalized, "LAYER", "XREF"))
                return RibbonIconKind.Workspace;
            if (ContainsAny(normalized, "CAPTURE", "BÓC CHỌN"))
                return RibbonIconKind.Inspect;

            // Recognition / inspection.
            if (normalized.Contains("RECOGNIZE_AUTO") || normalized.Contains("AUTO CHẮC"))
                return RibbonIconKind.RecognitionAuto;
            if (normalized.Contains("RECOGNIZE_INSPECT") || normalized.Contains("INSPECT"))
                return RibbonIconKind.Inspect;
            if (normalized.Contains("RECOGNIZE") || normalized.Contains("NHẬN DẠNG"))
                return RibbonIconKind.Recognition;
            if (normalized.Contains("MEP_TAKEOFF") || normalized.Contains("TAKEOFF"))
                return RibbonIconKind.Takeoff;

            // Edit / measure. Generic DRAW is intentionally deferred until all semantic domains
            // have had a chance to classify commands such as DRAW_WALL, DRAW_DOOR and DRAW_REBAR.
            if (ContainsAny(normalized, "_MOVE", "_ROTATE", "_MIRROR", "_COPY", "_BREAK", "_JOIN"))
                return RibbonIconKind.Transform;
            if (ContainsAny(normalized, "MEASURE", "_DIST", "DISTANCE", "KHOẢNG CÁCH"))
                return RibbonIconKind.Measure;

            // Tool / navigation semantics.
            if (normalized.Contains("LOCATE"))
                return RibbonIconKind.Locate;
            if (normalized.Contains("HIGHLIGHT"))
                return RibbonIconKind.Highlight;
            if (normalized.Contains("FOCUS"))
                return RibbonIconKind.Focus;
            if (normalized.Contains("ISOLATE"))
                return RibbonIconKind.Isolate;
            if (normalized.Contains("RESTORE"))
                return RibbonIconKind.Restore;
            if (normalized.Contains("ORBIT"))
                return RibbonIconKind.Orbit;
            if (ContainsAny(normalized, "VIEW3D", "VIEW_TOP", "3D VIEW", "TOP VIEW"))
                return RibbonIconKind.View3d;
            if (normalized.Contains("WORKSPACE"))
                return RibbonIconKind.Workspace;
            if (normalized.Contains("SECTION"))
                return RibbonIconKind.Section;
            if (normalized.Contains("ZOOM"))
                return RibbonIconKind.Focus;
            if (normalized.Contains("CLIP"))
                return RibbonIconKind.Section;
            if (normalized.Contains("SNAP"))
                return RibbonIconKind.Locate;

            // BIM authoring / modeling.
            if (ContainsAny(normalized, "BUILD3D", "BUILD 3D", "SINH MÔ HÌNH"))
                return RibbonIconKind.Model3d;
            if (ContainsAny(normalized, "AUTO_HOST", "AUTOLINKHOST", "LINKHOST", "CUT_OPENINGS", "OPENING", "LỖ MỞ", "HOST"))
                return RibbonIconKind.Opening;
            if (normalized.Contains("DOOR") || normalized.Contains("CỬA"))
                return RibbonIconKind.Door;
            if (normalized.Contains("ROOM") || normalized.Contains("PHÒNG"))
                return RibbonIconKind.Room;
            if (ContainsAny(normalized, "GLASS_WALL", "_WALL", "TƯỜNG", "VÁCH"))
                return RibbonIconKind.Wall;
            if (ContainsAny(normalized, "CURTAIN", "PIER", "JUNCTION", "BEAM", "SLAB", "COLUMN", "FOUNDATION", "STAIR", "RAILING", "EARTHWORK", "CẦU THANG", "LAN CAN", "ĐÀO ĐẤT", "KẾT CẤU"))
                return RibbonIconKind.Structure;

            // Quantity / schedules / data exchange.
            if (ContainsAny(normalized, "REBAR", "BBS", "MESH"))
                return RibbonIconKind.Rebar;
            if (normalized.Contains("SCHEDULE"))
                return RibbonIconKind.Schedule;
            if (ContainsAny(normalized, "EXCEL", "XLSX"))
                return RibbonIconKind.Excel;
            if (ContainsAny(normalized, "QS3DBQ", "_BQ", " BQ", "QUANTITY", "QTY", "BÓC TÁCH"))
                return RibbonIconKind.Quantity;

            // Review / release.
            if (normalized.Contains("BASELINE"))
                return RibbonIconKind.Compare;
            if (normalized.Contains("DIFF"))
                return RibbonIconKind.Diff;
            if (normalized.Contains("RELEASE"))
                return RibbonIconKind.Release;
            if (ContainsAny(normalized, "HEALTH", "VALIDATE", "CHECK"))
                return RibbonIconKind.UpdateStatus;

            // Common shell commands.
            if (normalized.Contains("SAVEAS") || normalized.Contains("SAVE AS") || normalized.Contains("EXPORT"))
                return RibbonIconKind.SaveAs;
            if (normalized.Contains("SAVE") || normalized.Contains("QSAVE") || normalized.Contains("LƯU"))
                return RibbonIconKind.Save;
            if (ContainsAny(normalized, "IMPORT", "RELOAD", "OPEN", "NẠP"))
                return RibbonIconKind.OpenProject;
            if (ContainsAny(normalized, "SETTINGS", "PROJECTTOOLS", "CONFIG", "LICENSE", "HELP"))
                return RibbonIconKind.Settings;
            if (ContainsAny(normalized, "REFRESH", "REGEN", "SYNC", "UPDATE"))
                return RibbonIconKind.Update;

            // BricsCAD's native rectangle command is _RECTANG, while the longer spelling may
            // appear in aliases/labels. Cover both before the neutral final safety net.
            if (normalized.Contains("_RECTANG"))
                return RibbonIconKind.Draw;

            // Generic drawing is deliberately the last semantic fallback. Otherwise broad DRAW
            // command names shadow richer intents such as DRAW_WALL, DRAW_REBAR or DRAW_DOOR.
            if (ContainsAny(normalized, "_POINT", "_LINE", "_ARC", "_RECTANGLE", "DRAW"))
                return RibbonIconKind.Draw;

            // Do not turn a missing mapping into product branding. Unknown functional commands
            // get a neutral object/catalog glyph until a richer semantic mapping is added.
            return RibbonIconKind.Objects;
        }

        private static bool ContainsAny(string value, params string[] candidates)
        {
            foreach (var candidate in candidates)
            {
                if (value.Contains(candidate))
                    return true;
            }
            return false;
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

        private static void SetEnumProperty(object target, string name, string value)
        {
            var property = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            if (property == null || !property.CanWrite || !property.PropertyType.IsEnum) return;
            try
            {
                property.SetValue(target, Enum.Parse(property.PropertyType, value, true), null);
            }
            catch
            {
                // Keep icon reconciliation best-effort on host versions with a different enum.
            }
        }
    }
}
