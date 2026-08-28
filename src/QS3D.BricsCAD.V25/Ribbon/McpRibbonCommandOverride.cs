using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace QS3D.BricsCAD.V25.Ribbon
{
    /// <summary>
    /// Final TOOL/MCP command routing for the embedded single-repository MCP service.
    /// The legacy binder remains the presentation/bootstrap contract; this override owns
    /// only the four MCP buttons and runs before the generic command-parameter fallback.
    /// </summary>
    internal static class McpRibbonCommandOverride
    {
        private const string AssemblyName = "BrxMgd";
        private const string ToolTabId = "QS3D_TOOL";
        private const string Prefix = "QS3D_TOOL_BLT_";

        private static readonly IReadOnlyDictionary<string, string> CommandByButtonId =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [Prefix + "MCP_SETTINGS"] = "QS3DMCPAGENTCENTER",
                [Prefix + "MCP_DOCS"] = "QS3DMCPDOCSHTTP",
                [Prefix + "AI_DASHBOARD"] = "QS3DMCPAGENTCENTER",
                [Prefix + "MCP_CONNECTION"] = "QS3DMCPCHECKHTTP"
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
                var tab = tabs == null ? null : FindById(tabs, ToolTabId);
                if (tab == null) return false;
                var panels = GetProperty(tab, "Panels");
                if (!(panels is IEnumerable panelEnumerable)) return false;

                var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var visited = new HashSet<object>();
                foreach (var panel in panelEnumerable)
                {
                    if (panel == null) continue;
                    var source = GetProperty(panel, "Source");
                    var items = source == null ? null : GetProperty(source, "Items");
                    if (items != null) OverrideCollection(items, visited, found);
                }

                if (found.Count != CommandByButtonId.Count) return false;
                _initialized = true;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void Reset() => _initialized = false;

        private static void OverrideCollection(
            object collection,
            HashSet<object> visited,
            HashSet<string> found)
        {
            if (!(collection is IEnumerable enumerable)) return;
            foreach (var item in enumerable)
            {
                if (item == null || !visited.Add(item)) continue;

                var id = GetProperty(item, "Id") as string;
                if (!string.IsNullOrWhiteSpace(id)
                    && CommandByButtonId.TryGetValue(id!, out var command))
                {
                    SetProperty(item, "CommandParameter", command);
                    found.Add(id!);
                }

                var nested = GetProperty(item, "Items");
                if (nested != null) OverrideCollection(nested, visited, found);
            }
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
            var paletteProperty = servicesType.GetProperty(
                "RibbonPaletteSet",
                BindingFlags.Public | BindingFlags.Static);
            var palette = paletteProperty?.GetValue(null, null);
            if (palette == null) return null;
            if (palette.GetType().Name == "RibbonControl") return palette;

            var direct = GetProperty(palette, "RibbonControl");
            if (direct != null) return direct;

            foreach (var property in palette.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (property.PropertyType.Name != "RibbonControl"
                    || property.GetIndexParameters().Length != 0)
                    continue;
                var value = property.GetValue(palette, null);
                if (value != null) return value;
            }

            return null;
        }

        private static object? GetProperty(object target, string name) =>
            target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public)
                ?.GetValue(target, null);

        private static void SetProperty(object target, string name, object value)
        {
            var property = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            if (property == null || !property.CanWrite) return;
            if (property.PropertyType.IsInstanceOfType(value) || property.PropertyType == value.GetType())
                property.SetValue(target, value, null);
        }
    }
}
