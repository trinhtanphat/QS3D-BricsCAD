using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace QS3D.BricsCAD.V25.Ribbon
{
    /// <summary>
    /// Preserves the bootstrap Draw fallback when the richer BLT reconciliation fails.
    /// The original fallback panel objects are reused so recovery cannot drift command semantics.
    /// </summary>
    internal static class BltDrawRibbonFailSafe
    {
        private const string AssemblyName = "BrxMgd";
        private const string DrawTabId = "QS3D_DRAW";

        private static readonly string[] LegacyPanelSourceIds =
        {
            "QS3D_DRAW_PRIMITIVES_PANEL_SOURCE",
            "QS3D_DRAW_TRANSFORM_PANEL_SOURCE",
            "QS3D_DRAW_EDIT_PANEL_SOURCE"
        };

        private static readonly string[] RichPanelSourceIds =
        {
            "QS3D_DRAW_BLT_DRAW_PANEL_SOURCE",
            "QS3D_DRAW_BLT_TOOLS_PANEL_SOURCE",
            "QS3D_DRAW_BLT_IFC_PANEL_SOURCE"
        };

        private sealed class CapturedPanel
        {
            public CapturedPanel(string sourceId, object panel)
            {
                SourceId = sourceId;
                Panel = panel;
            }

            public string SourceId { get; }
            public object Panel { get; }
        }

        private sealed class FallbackSnapshot
        {
            public FallbackSnapshot(object panels, IReadOnlyList<CapturedPanel> legacyPanels)
            {
                Panels = panels;
                LegacyPanels = legacyPanels;
            }

            public object Panels { get; }
            public IReadOnlyList<CapturedPanel> LegacyPanels { get; }
        }

        public static bool TryInitialize()
        {
            var fallback = CaptureFallback();
            if (BltDrawRibbonAugmenter.TryInitialize())
                return true;

            RestoreFallback(fallback);
            return false;
        }

        private static FallbackSnapshot? CaptureFallback()
        {
            try
            {
                var panels = FindDrawPanels();
                if (panels == null)
                    return null;

                var captured = new List<CapturedPanel>();
                foreach (var sourceId in LegacyPanelSourceIds)
                {
                    var panel = FindPanelBySourceId(panels, sourceId);
                    if (panel != null)
                        captured.Add(new CapturedPanel(sourceId, panel));
                }

                return new FallbackSnapshot(panels, captured);
            }
            catch
            {
                return null;
            }
        }

        private static void RestoreFallback(FallbackSnapshot? fallback)
        {
            if (fallback == null)
                return;

            foreach (var sourceId in RichPanelSourceIds)
                TryRemoveOwnedPanel(fallback.Panels, sourceId);

            foreach (var captured in fallback.LegacyPanels)
            {
                try
                {
                    if (FindPanelBySourceId(fallback.Panels, captured.SourceId) == null)
                        Add(fallback.Panels, captured.Panel);
                }
                catch
                {
                    // Best-effort host recovery. The failed rich initialization still propagates
                    // as false so the coordinator keeps its normal bounded retry behavior.
                }
            }
        }

        private static object? FindDrawPanels()
        {
            var control = FindRibbonControl();
            if (control == null)
                return null;

            var tabs = GetProperty(control, "Tabs");
            var drawTab = tabs == null ? null : FindById(tabs, DrawTabId);
            return drawTab == null ? null : GetProperty(drawTab, "Panels");
        }

        private static void TryRemoveOwnedPanel(object panels, string sourceId)
        {
            try
            {
                var panel = FindPanelBySourceId(panels, sourceId);
                if (panel != null)
                    Remove(panels, panel);
            }
            catch
            {
                // Recovery must never replace the original BLT initialization failure.
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

        private static void Add(object collection, object item)
        {
            var method = collection.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(candidate => candidate.Name == "Add"
                    && candidate.GetParameters().Length == 1
                    && candidate.GetParameters()[0].ParameterType.IsAssignableFrom(item.GetType()));
            if (method == null)
                throw new InvalidOperationException("Collection does not expose a compatible Add method.");
            method.Invoke(collection, new[] { item });
        }

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
