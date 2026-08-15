using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace QS3D.BricsCAD.V25.Ribbon
{
    /// <summary>
    /// Keeps QS3D's Ribbon navigation as an additive group on the host Ribbon tab row.
    /// Native/third-party tabs are never renamed, removed permanently, or reordered relative
    /// to one another; only tabs owned by QS3D are moved to the end of RibbonControl.Tabs.
    /// </summary>
    internal static class Qs3dRibbonTabGroupCoordinator
    {
        private const string AssemblyName = "BrxMgd";
        private const string OwnedTabPrefix = "QS3D_";

        // Match the BLT3D-familiar top navigation requested for QS3D. Existing QS3D-only
        // extension tabs that are not in this list are kept, but follow this primary group.
        private static readonly string[] PrimaryOwnedTabOrder =
        {
            "QS3D_HOME",
            "QS3D_PROJECT",
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

                var snapshot = Snapshot(tabs);
                var ownedTabs = snapshot.Where(IsOwnedTab).ToList();
                if (ownedTabs.Count == 0)
                    return false;

                // RibbonBootstrapper creates these primary tabs synchronously. If the host is
                // still reconstructing its Ribbon, retry instead of publishing a partial order.
                if (PrimaryOwnedTabOrder.Any(id => ownedTabs.All(tab => !HasId(tab, id))))
                    return false;

                var orderedOwnedTabs = OrderOwnedTabs(ownedTabs);
                if (!AlreadyGroupedAtEnd(snapshot, orderedOwnedTabs))
                {
                    // Remove/re-add QS3D-owned objects only. This preserves the relative order
                    // and every property/panel/button of all native and third-party host tabs.
                    foreach (var tab in ownedTabs)
                        Remove(tabs, tab);
                    foreach (var tab in orderedOwnedTabs)
                        Add(tabs, tab);
                }

                if (!AlreadyGroupedAtEnd(Snapshot(tabs), orderedOwnedTabs))
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

        private static List<object> OrderOwnedTabs(IReadOnlyList<object> ownedTabs)
        {
            var ordered = new List<object>(ownedTabs.Count);
            foreach (var id in PrimaryOwnedTabOrder)
            {
                var tab = ownedTabs.FirstOrDefault(candidate => HasId(candidate, id));
                if (tab != null)
                    ordered.Add(tab);
            }

            // Preserve the original relative order of any future/extra QS3D-owned tabs.
            foreach (var tab in ownedTabs)
            {
                if (!ordered.Any(candidate => ReferenceEquals(candidate, tab)))
                    ordered.Add(tab);
            }

            return ordered;
        }

        private static bool AlreadyGroupedAtEnd(IReadOnlyList<object> snapshot, IReadOnlyList<object> orderedOwnedTabs)
        {
            if (orderedOwnedTabs.Count == 0 || snapshot.Count < orderedOwnedTabs.Count)
                return false;

            var start = snapshot.Count - orderedOwnedTabs.Count;
            for (var i = 0; i < orderedOwnedTabs.Count; i++)
            {
                if (!ReferenceEquals(snapshot[start + i], orderedOwnedTabs[i]))
                    return false;
            }

            // The suffix comparison above also proves that no QS3D-owned tab remains mixed
            // into the native side when the owned-tab counts match.
            return snapshot.Count(IsOwnedTab) == orderedOwnedTabs.Count;
        }

        private static bool IsOwnedTab(object tab)
        {
            var id = GetProperty(tab, "Id") as string;
            return !string.IsNullOrWhiteSpace(id)
                   && id.StartsWith(OwnedTabPrefix, StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasId(object tab, string id) =>
            string.Equals(GetProperty(tab, "Id") as string, id, StringComparison.OrdinalIgnoreCase);

        private static List<object> Snapshot(object collection)
        {
            if (!(collection is IEnumerable enumerable))
                throw new InvalidOperationException("Ribbon tabs collection is not enumerable.");

            var result = new List<object>();
            foreach (var item in enumerable)
            {
                if (item != null)
                    result.Add(item);
            }

            return result;
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
                .FirstOrDefault(candidate =>
                    candidate.Name == "Add"
                    && candidate.GetParameters().Length == 1
                    && candidate.GetParameters()[0].ParameterType.IsAssignableFrom(item.GetType()));

            if (method == null)
                throw new InvalidOperationException("Ribbon tabs collection does not expose a compatible Add method.");

            method.Invoke(collection, new[] { item });
        }

        private static void Remove(object collection, object item)
        {
            var method = collection.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(candidate =>
                    candidate.Name == "Remove"
                    && candidate.GetParameters().Length == 1
                    && candidate.GetParameters()[0].ParameterType.IsAssignableFrom(item.GetType()));

            if (method == null)
                throw new InvalidOperationException("Ribbon tabs collection does not expose a compatible Remove method.");

            method.Invoke(collection, new[] { item });
        }
    }
}
