using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace QS3D.BricsCAD.V25.Ribbon
{
    /// <summary>
    /// Mirrors the already-qualified BLT3D drawing/tool/IFC ribbon contract into MÔ HÌNH BIM.
    /// The source panels remain on VẼ; BIM receives independent panel/button objects that reuse
    /// the exact same command handlers, command parameters, icons and sizing. This keeps the
    /// owner-reference behavior in one place and avoids duplicating geometry/business logic.
    /// </summary>
    internal static class BltBimRibbonMirrorAugmenter
    {
        private const string AssemblyName = "BrxMgd";
        private const string DrawTabId = "QS3D_DRAW";
        private const string BimTabId = "QS3D_BIM";
        private const string DrawOwnedPrefix = "QS3D_DRAW_BLT_";
        private const string BimOwnedPrefix = "QS3D_BIM_BLT_";

        private static readonly PanelMirrorSpec[] PanelSpecs =
        {
            new PanelMirrorSpec("QS3D_DRAW_BLT_DRAW_PANEL_SOURCE", "QS3D_BIM_BLT_DRAW_PANEL_SOURCE"),
            new PanelMirrorSpec("QS3D_DRAW_BLT_TOOLS_PANEL_SOURCE", "QS3D_BIM_BLT_TOOLS_PANEL_SOURCE"),
            new PanelMirrorSpec("QS3D_DRAW_BLT_IFC_PANEL_SOURCE", "QS3D_BIM_BLT_IFC_PANEL_SOURCE")
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

                var drawTab = FindById(tabs, DrawTabId);
                var bimTab = FindById(tabs, BimTabId);
                if (drawTab == null || bimTab == null) return false;

                var drawPanels = GetProperty(drawTab, "Panels");
                var bimPanels = GetProperty(bimTab, "Panels");
                if (drawPanels == null || bimPanels == null) return false;

                var sources = new List<object>(PanelSpecs.Length);
                foreach (var spec in PanelSpecs)
                {
                    var panel = FindPanelBySourceId(drawPanels, spec.SourceId);
                    if (panel == null) return false;
                    sources.Add(panel);
                }

                // MÔ HÌNH BIM is an exact owner-reference contract. Remove only QS3D-owned BIM
                // panels; native/third-party tabs and panels are never touched.
                RemoveQs3dOwnedBimPanels(bimPanels);

                for (var index = 0; index < PanelSpecs.Length; index++)
                    AddMirroredPanel(bimPanels, sources[index], PanelSpecs[index]);

                _initialized = true;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void Reset() => _initialized = false;

        private static void RemoveQs3dOwnedBimPanels(object panels)
        {
            if (!(panels is IEnumerable enumerable)) return;

            var remove = new List<object>();
            foreach (var panel in enumerable)
            {
                if (panel == null) continue;
                var source = GetProperty(panel, "Source");
                var sourceId = source == null ? null : GetProperty(source, "Id") as string;
                if (!string.IsNullOrWhiteSpace(sourceId)
                    && sourceId.StartsWith("QS3D_BIM_", StringComparison.OrdinalIgnoreCase))
                    remove.Add(panel);
            }

            foreach (var panel in remove)
                Remove(panels, panel);
        }

        private static void AddMirroredPanel(object targetPanels, object sourcePanel, PanelMirrorSpec spec)
        {
            var source = GetProperty(sourcePanel, "Source")
                         ?? throw new InvalidOperationException("Source RibbonPanel did not expose Source.");
            var sourceItems = GetProperty(source, "Items") as IEnumerable
                              ?? throw new InvalidOperationException("Source RibbonPanelSource did not expose Items.");

            var mirroredSource = Create("Bricscad.Windows.RibbonPanelSource");
            SetProperty(mirroredSource, "Id", spec.TargetId);
            CopyProperty(source, mirroredSource, "Name");
            CopyProperty(source, mirroredSource, "Title");

            var mirroredItems = GetProperty(mirroredSource, "Items")
                                ?? throw new InvalidOperationException("Target RibbonPanelSource.Items was not available.");

            var buttonCount = 0;
            foreach (var sourceItem in sourceItems)
            {
                if (sourceItem == null) continue;
                var mirrored = CloneRibbonItem(sourceItem, ref buttonCount);
                if (mirrored == null) continue;
                Add(mirroredItems, mirrored);
            }

            if (buttonCount == 0)
                throw new InvalidOperationException("BLT source panel contained no mirrorable ribbon buttons.");

            var panel = Create("Bricscad.Windows.RibbonPanel");
            SetProperty(panel, "Source", mirroredSource);
            Add(targetPanels, panel);
        }

        private static object? CloneRibbonItem(object source, ref int buttonCount)
        {
            var typeName = source.GetType().Name;
            if (string.Equals(typeName, "RibbonButton", StringComparison.Ordinal))
            {
                var target = Activator.CreateInstance(source.GetType())
                             ?? throw new InvalidOperationException("Cannot clone " + source.GetType().FullName + ".");

                var id = GetProperty(source, "Id") as string;
                if (!string.IsNullOrWhiteSpace(id))
                {
                    var mirroredId = id.StartsWith(DrawOwnedPrefix, StringComparison.OrdinalIgnoreCase)
                        ? BimOwnedPrefix + id.Substring(DrawOwnedPrefix.Length)
                        : BimOwnedPrefix + id;
                    SetProperty(target, "Id", mirroredId);
                }

                // Copy presentation and routing from the already-qualified BLT panel. ImageSource
                // instances are frozen by the source augmenter and are safe to share.
                foreach (var property in new[]
                {
                    "Name", "Text", "ShowText", "ShowImage", "CommandParameter", "CommandHandler",
                    "Description", "ToolTip", "Size", "Image", "LargeImage", "IsEnabled", "IsVisible"
                })
                    CopyProperty(source, target, property);

                buttonCount++;
                return target;
            }

            if (string.Equals(typeName, "RibbonRowBreak", StringComparison.Ordinal))
            {
                return Activator.CreateInstance(source.GetType())
                       ?? throw new InvalidOperationException("Cannot clone " + source.GetType().FullName + ".");
            }

            if (string.Equals(typeName, "RibbonRowPanel", StringComparison.Ordinal))
            {
                var target = Activator.CreateInstance(source.GetType())
                             ?? throw new InvalidOperationException("Cannot clone " + source.GetType().FullName + ".");
                var sourceItems = GetProperty(source, "Items") as IEnumerable
                                  ?? throw new InvalidOperationException("Source RibbonRowPanel did not expose Items.");
                var targetItems = GetProperty(target, "Items")
                                  ?? throw new InvalidOperationException("Target RibbonRowPanel did not expose Items.");

                foreach (var sourceItem in sourceItems)
                {
                    if (sourceItem == null) continue;
                    var mirrored = CloneRibbonItem(sourceItem, ref buttonCount);
                    if (mirrored != null)
                        Add(targetItems, mirrored);
                }

                return target;
            }

            // Unknown QS3D-owned ribbon item shapes are never guessed. Fail the mirror rather than
            // silently emitting a different BIM surface.
            return null;
        }

        private static object? FindPanelBySourceId(object panels, string sourceId)
        {
            if (!(panels is IEnumerable enumerable)) return null;
            foreach (var panel in enumerable)
            {
                if (panel == null) continue;
                var source = GetProperty(panel, "Source");
                if (source == null) continue;
                if (string.Equals(GetProperty(source, "Id") as string, sourceId, StringComparison.OrdinalIgnoreCase))
                    return panel;
            }
            return null;
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

        private static object Create(string fullName) =>
            Activator.CreateInstance(Type.GetType(fullName + ", " + AssemblyName, true)!)
            ?? throw new InvalidOperationException("Cannot create " + fullName);

        private static object? GetProperty(object target, string name) =>
            target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public)?.GetValue(target, null);

        private static void CopyProperty(object source, object target, string name)
        {
            var sourceProperty = source.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            var targetProperty = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            if (sourceProperty == null || targetProperty == null || !targetProperty.CanWrite) return;

            var value = sourceProperty.GetValue(source, null);
            if (value == null)
            {
                if (!targetProperty.PropertyType.IsValueType || Nullable.GetUnderlyingType(targetProperty.PropertyType) != null)
                    targetProperty.SetValue(target, null, null);
                return;
            }

            if (targetProperty.PropertyType.IsInstanceOfType(value) || targetProperty.PropertyType == value.GetType())
                targetProperty.SetValue(target, value, null);
        }

        private static void SetProperty(object target, string name, object value)
        {
            var property = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            if (property == null || !property.CanWrite) return;
            if (property.PropertyType.IsInstanceOfType(value) || property.PropertyType == value.GetType())
                property.SetValue(target, value, null);
        }

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

        private sealed class PanelMirrorSpec
        {
            public PanelMirrorSpec(string sourceId, string targetId)
            {
                SourceId = sourceId;
                TargetId = targetId;
            }

            public string SourceId { get; }
            public string TargetId { get; }
        }
    }
}
