using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace QS3D.BricsCAD.V25.Ribbon
{
    /// <summary>
    /// Enforces the owner-reference QS3D topbar contract without touching native/third-party tabs.
    /// Legacy QS3D-owned tabs that are not part of the current ten-tab product surface are removed,
    /// and the surviving captions are normalized before the existing group coordinator orders them.
    /// </summary>
    internal static class BltTopbarTabContract
    {
        private const string AssemblyName = "BrxMgd";
        private const string OwnedPrefix = "QS3D_";

        private static readonly TabSpec[] Tabs =
        {
            new TabSpec("QS3D_HOME", "KHỞI ĐẦU"),
            new TabSpec("QS3D_PROJECT", "THIẾT LẬP DỰ ÁN"),
            new TabSpec("QS3D_BIM", "MÔ HÌNH BIM"),
            new TabSpec("QS3D_RECOGNIZE", "NHẬN DẠNG"),
            new TabSpec("QS3D_DRAW", "VẼ"),
            new TabSpec("QS3D_TOOL", "TOOL"),
            new TabSpec("QS3D_MODELING", "MODELING"),
            new TabSpec("QS3D_VIEW", "XEM"),
            new TabSpec("QS3D_QTY", "ĐỊNH LƯỢNG"),
            new TabSpec("QS3D_REV", "BẢN SỬA ĐỔI")
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

                var allowed = new HashSet<string>(Tabs.Select(tab => tab.Id), StringComparer.OrdinalIgnoreCase);
                var remove = new List<object>();
                if (tabs is IEnumerable enumerable)
                {
                    foreach (var tab in enumerable)
                    {
                        if (tab == null) continue;
                        var id = GetProperty(tab, "Id") as string;
                        if (!string.IsNullOrWhiteSpace(id)
                            && id.StartsWith(OwnedPrefix, StringComparison.OrdinalIgnoreCase)
                            && !allowed.Contains(id))
                            remove.Add(tab);
                    }
                }

                foreach (var tab in remove)
                    Remove(tabs, tab);

                foreach (var spec in Tabs)
                {
                    var tab = FindById(tabs, spec.Id);
                    if (tab == null) return false;
                    SetProperty(tab, "Name", spec.Title);
                    SetProperty(tab, "Title", spec.Title);
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

        private sealed class TabSpec
        {
            public TabSpec(string id, string title)
            {
                Id = id;
                Title = title;
            }

            public string Id { get; }
            public string Title { get; }
        }
    }
}
