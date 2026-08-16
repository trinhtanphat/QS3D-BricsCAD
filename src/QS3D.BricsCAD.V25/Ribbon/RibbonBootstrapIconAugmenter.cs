using System;
using System.Collections;
using System.Reflection;

namespace QS3D.BricsCAD.V25.Ribbon
{
    /// <summary>
    /// Fills missing images on the canonical QS3D Ribbon after every richer feature augmenter
    /// has reconciled. Existing Home/Draw/custom images are preserved; only text-only command
    /// buttons receive deterministic QS3D-generated fallback icons.
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
                {
                    if (item == null) continue;
                    if (!(GetProperty(item, "CommandParameter") is string command)
                        || string.IsNullOrWhiteSpace(command))
                        continue;

                    commandButtons++;
                    if (HasCompleteVisibleIcon(item))
                        continue;

                    var text = (GetProperty(item, "Text") as string)
                               ?? (GetProperty(item, "Name") as string)
                               ?? string.Empty;
                    var icon = ResolveIcon(command, text);
                    SetProperty(item, "ShowImage", true);
                    SetProperty(item, "Image", RibbonIconFactory.Create(icon, 16));
                    SetProperty(item, "LargeImage", RibbonIconFactory.Create(icon, 32));
                }
            }

            return commandButtons > 0;
        }

        private static bool HasCompleteVisibleIcon(object item) =>
            GetProperty(item, "ShowImage") is bool showImage
            && showImage
            && GetProperty(item, "Image") != null
            && GetProperty(item, "LargeImage") != null;

        private static RibbonIconKind ResolveIcon(string command, string text)
        {
            var normalized = (command + " " + text).Trim().ToUpperInvariant();

            if (normalized.Contains("HEALTH")
                || normalized.Contains("VALIDATE")
                || normalized.Contains("CHECK"))
                return RibbonIconKind.UpdateStatus;

            if (normalized.Contains("SAVEAS") || normalized.Contains("SAVE AS") || normalized.Contains("EXPORT"))
                return RibbonIconKind.SaveAs;

            if (normalized.Contains("SAVE") || normalized.Contains("QSAVE") || normalized.Contains("LƯU"))
                return RibbonIconKind.Save;

            if (normalized.Contains("IMPORT")
                || normalized.Contains("RELOAD")
                || normalized.Contains("OPEN")
                || normalized.Contains("NẠP"))
                return RibbonIconKind.OpenProject;

            if (normalized.Contains("SETTINGS")
                || normalized.Contains("PROJECTTOOLS")
                || normalized.Contains("CONFIG")
                || normalized.Contains("LICENSE")
                || normalized.Contains("HELP"))
                return RibbonIconKind.Settings;

            if (normalized.Contains("REFRESH")
                || normalized.Contains("REGEN")
                || normalized.Contains("SYNC")
                || normalized.Contains("BUILD")
                || normalized.Contains("CUT")
                || normalized.Contains("UPDATE"))
                return RibbonIconKind.Update;

            return RibbonIconKind.Objects;
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
