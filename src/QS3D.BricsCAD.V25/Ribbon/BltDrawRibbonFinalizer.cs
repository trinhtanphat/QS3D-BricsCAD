using System;
using System.Collections;
using System.Linq;
using System.Reflection;

namespace QS3D.BricsCAD.V25.Ribbon
{
    /// <summary>
    /// Final owner-reference pass for the VẼ tab. The Draw augmenter intentionally creates
    /// a qualified IFC panel first so MÔ HÌNH BIM can mirror it. Once that mirror exists,
    /// remove IFC only from the visible VẼ tab so the owner-reference surface ends at Công cụ.
    /// </summary>
    internal static class BltDrawRibbonFinalizer
    {
        private const string AssemblyName = "BrxMgd";
        private const string DrawTabId = "QS3D_DRAW";
        private const string BimTabId = "QS3D_BIM";
        private const string DrawIfcPanelSourceId = "QS3D_DRAW_BLT_IFC_PANEL_SOURCE";
        private const string BimIfcPanelSourceId = "QS3D_BIM_BLT_IFC_PANEL_SOURCE";

        private static bool _initialized;

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
                if (tabs == null)
                    return false;

                var drawTab = FindById(tabs, DrawTabId);
                var bimTab = FindById(tabs, BimTabId);
                if (drawTab == null || bimTab == null)
                    return false;

                var drawPanels = GetProperty(drawTab, "Panels");
                var bimPanels = GetProperty(bimTab, "Panels");
                if (drawPanels == null || bimPanels == null)
                    return false;

                // Do not hide the Draw IFC source until BIM owns its independent clone.
                // This makes retries fail closed instead of silently dropping BIM IFC actions.
                if (FindPanelBySourceId(bimPanels, BimIfcPanelSourceId) == null)
                    return false;

                var drawIfc = FindPanelBySourceId(drawPanels, DrawIfcPanelSourceId);
                if (drawIfc != null)
                    Remove(drawPanels, drawIfc);

                _initialized = true;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void Reset() => _initialized = false;

        private static object? FindPanelBySourceId(object panels, string sourceId)
        {
            if (!(panels is IEnumerable enumerable))
                return null;

            foreach (var panel in enumerable)
            {
                if (panel == null)
                    continue;
                var source = GetProperty(panel, "Source");
                if (source == null)
                    continue;
                if (string.Equals(GetProperty(source, "Id") as string, sourceId, StringComparison.OrdinalIgnoreCase))
                    return panel;
            }

            return null;
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

        private static void Remove(object collection, object item)
        {
            var method = collection.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(candidate => candidate.Name == "Remove"
                    && candidate.GetParameters().Length == 1
                    && candidate.GetParameters()[0].ParameterType.IsAssignableFrom(item.GetType()));
            if (method == null)
                throw new InvalidOperationException("Collection does not expose a compatible Remove method.");
            method.Invoke(collection, new[] { item });
        }
    }
}
