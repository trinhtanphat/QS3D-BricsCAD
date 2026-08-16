using System;
using System.Collections;
using System.Reflection;

namespace QS3D.BricsCAD.V25.Ribbon
{
    /// <summary>
    /// Adds deterministic QS3D-generated icons to the bootstrap-owned Project and Authoring
    /// tabs without changing command IDs, grouping, or the separately-polished Home ribbon.
    /// </summary>
    internal static class RibbonBootstrapIconAugmenter
    {
        private const string AssemblyName = "BrxMgd";
        private static readonly string[] TargetTabIds = { "QS3D_PROJECT", "QS3D_AUTHOR" };
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

            var updatedButtons = 0;
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

                    var icon = ResolveIcon(command);
                    SetProperty(item, "ShowImage", true);
                    SetProperty(item, "Image", RibbonIconFactory.Create(icon, 16));
                    SetProperty(item, "LargeImage", RibbonIconFactory.Create(icon, 32));
                    updatedButtons++;
                }
            }

            return updatedButtons > 0;
        }

        private static RibbonIconKind ResolveIcon(string command)
        {
            var normalized = command.Trim().ToUpperInvariant();

            if (normalized.Contains("HEALTH")
                || normalized.Contains("VALIDATE")
                || normalized.Contains("CHECK"))
                return RibbonIconKind.UpdateStatus;

            if (normalized.Contains("EXPORT") || normalized.Contains("SAVE"))
                return RibbonIconKind.SaveAs;

            if (normalized.Contains("IMPORT")
                || normalized.Contains("RELOAD")
                || normalized.Contains("OPEN"))
                return RibbonIconKind.OpenProject;

            if (normalized.Contains("SETTINGS") || normalized.Contains("PROJECTTOOLS"))
                return RibbonIconKind.Settings;

            if (normalized.Contains("REFRESH")
                || normalized.Contains("REGEN")
                || normalized.Contains("SYNC")
                || normalized.Contains("BUILD")
                || normalized.Contains("CUT"))
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
