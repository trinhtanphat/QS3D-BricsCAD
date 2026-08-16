using System;
using System.Collections;
using System.Reflection;

namespace QS3D.BricsCAD.V25.Ribbon
{
    /// <summary>
    /// Fills missing images on the canonical QS3D Ribbon after every richer feature augmenter
    /// has reconciled. Existing Home/Draw/custom images are preserved; text-only buttons get
    /// deterministic semantic QS3D icons based on their command intent.
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
            foreach (var panel in panelEnumerable)
            {
                if (panel == null) continue;
                var source = GetProperty(panel, "Source");
                if (source == null) continue;
                var items = GetProperty(source, "Items");
                if (!(items is IEnumerable itemEnumerable)) continue;

                foreach (var item in itemEnumerable)
                    DecorateItem(item, ref commandButtons);
            }

            return commandButtons > 0;
        }

        private static void DecorateItem(object? item, ref int commandButtons)
        {
            if (item == null) return;

            if (GetProperty(item, "CommandParameter") is string command
                && !string.IsNullOrWhiteSpace(command))
            {
                commandButtons++;
                if (!HasCompleteVisibleIcon(item))
                {
                    var text = (GetProperty(item, "Text") as string)
                               ?? (GetProperty(item, "Name") as string)
                               ?? string.Empty;
                    var icon = ResolveIcon(command, text);
                    SetProperty(item, "ShowImage", true);
                    SetProperty(item, "Image", RibbonIconFactory.Create(icon, 16));
                    SetProperty(item, "LargeImage", RibbonIconFactory.Create(icon, 32));
                }
            }

            // Rich ribbon augmenters may wrap buttons in row/stack containers. Recurse so
            // the same icon policy also covers their fallback layouts without replacing
            // images already supplied by those augmenters.
            var nested = GetProperty(item, "Items");
            if (!(nested is IEnumerable nestedEnumerable)) return;
            foreach (var child in nestedEnumerable)
                DecorateItem(child, ref commandButtons);
        }

        private static bool HasCompleteVisibleIcon(object item) =>
            GetProperty(item, "ShowImage") is bool showImage
            && showImage
            && GetProperty(item, "Image") != null
            && GetProperty(item, "LargeImage") != null;

        private static RibbonIconKind ResolveIcon(string command, string text)
        {
            var normalized = (command + " " + text).Trim().ToUpperInvariant();

            // Recognition / inspection.
            if (normalized.Contains("RECOGNIZE_AUTO") || normalized.Contains("AUTO CHẮC"))
                return RibbonIconKind.RecognitionAuto;
            if (normalized.Contains("RECOGNIZE_INSPECT") || normalized.Contains("INSPECT"))
                return RibbonIconKind.Inspect;
            if (normalized.Contains("RECOGNIZE") || normalized.Contains("NHẬN DẠNG"))
                return RibbonIconKind.Recognition;
            if (normalized.Contains("MEP_TAKEOFF") || normalized.Contains("TAKEOFF"))
                return RibbonIconKind.Takeoff;

            // Draw / edit / measure.
            if (ContainsAny(normalized, "_POINT", "_LINE", "_ARC", "_RECTANGLE", "DRAW"))
                return RibbonIconKind.Draw;
            if (ContainsAny(normalized, "_MOVE", "_ROTATE", "_MIRROR", "_COPY", "_BREAK", "_JOIN"))
                return RibbonIconKind.Transform;
            if (normalized.Contains("MEASURE"))
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

            // BIM authoring / modeling.
            if (ContainsAny(normalized, "BUILD3D", "BUILD 3D", "SINH MÔ HÌNH"))
                return RibbonIconKind.Model3d;
            if (ContainsAny(normalized, "GLASS_WALL", "_WALL", "TƯỜNG", "VÁCH"))
                return RibbonIconKind.Wall;
            if (ContainsAny(normalized, "CURTAIN", "PIER", "JUNCTION", "BEAM", "SLAB", "COLUMN", "FOUNDATION", "KẾT CẤU"))
                return RibbonIconKind.Structure;
            if (ContainsAny(normalized, "AUTO_HOST", "CUT_OPENINGS", "OPENING", "LỖ MỞ"))
                return RibbonIconKind.Opening;
            if (normalized.Contains("DOOR") || normalized.Contains("CỬA"))
                return RibbonIconKind.Door;
            if (normalized.Contains("ROOM") || normalized.Contains("PHÒNG"))
                return RibbonIconKind.Room;

            // Quantity / schedules / data exchange.
            if (ContainsAny(normalized, "REBAR", "BBS", "MESH"))
                return RibbonIconKind.Rebar;
            if (normalized.Contains("SCHEDULE"))
                return RibbonIconKind.Schedule;
            if (ContainsAny(normalized, "EXCEL", "XLSX"))
                return RibbonIconKind.Excel;
            if (ContainsAny(normalized, "_BQ", "QUANTITY", "QTY", "BÓC TÁCH"))
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
    }
}
