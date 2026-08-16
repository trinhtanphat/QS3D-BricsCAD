using System;
using System.Collections;
using System.Linq;
using System.Reflection;

namespace QS3D.BricsCAD.V25.Ribbon
{
    /// <summary>
    /// Applies the final owner-reference presentation to the QS3D VẼ tab after the BIM mirror
    /// has copied the qualified Vẽ/Công cụ/IFC staging panels. The BLT3D reference for VẼ ends
    /// after Công cụ, so the staging IFC panel is removed only from QS3D_DRAW while MÔ HÌNH BIM
    /// keeps its independently cloned IFC panel.
    /// </summary>
    internal static class BltDrawRibbonReferenceFinalizer
    {
        private const string AssemblyName = "BrxMgd";
        private const string DrawTabId = "QS3D_DRAW";
        private const string IfcPanelSourceId = "QS3D_DRAW_BLT_IFC_PANEL_SOURCE";

        public static bool TryInitialize()
        {
            try
            {
                var control = FindRibbonControl();
                if (control == null)
                    return false;

                var tabs = GetProperty(control, "Tabs");
                var drawTab = tabs == null ? null : FindById(tabs, DrawTabId);
                if (drawTab == null)
                    return false;

                var panels = GetProperty(drawTab, "Panels");
                if (panels == null)
                    return false;

                var ifcPanel = FindPanelBySourceId(panels, IfcPanelSourceId);
                if (ifcPanel == null)
                    return true;

                Remove(panels, ifcPanel);
                return FindPanelBySourceId(panels, IfcPanelSourceId) == null;
            }
            catch
            {
                return false;
            }
        }

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

                if (string.Equals(
                    GetProperty(source, "Id") as string,
                    sourceId,
                    StringComparison.OrdinalIgnoreCase))
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

                if (string.Equals(
                    GetProperty(item, "Id") as string,
                    id,
                    StringComparison.OrdinalIgnoreCase))
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
