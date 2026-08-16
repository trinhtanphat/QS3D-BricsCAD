using System;
using System.Collections;
using System.Reflection;
using System.Windows.Threading;

namespace QS3D.BricsCAD.V25.Ribbon
{
    /// <summary>
    /// Watches the selected Ribbon tab and owns Start Center visibility for the QS3D HOME surface.
    /// Leaving HOME releases the large embedded Start Center immediately so the native BricsCAD
    /// viewport and BIM side palettes can reclaim the work area without stale palette overlap.
    /// </summary>
    internal static class HomeTabActivationCoordinator
    {
        private const string AssemblyName = "BrxMgd";
        private const string HomeTabId = "QS3D_HOME";
        private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(250);
        private static DispatcherTimer? _timer;
        private static object? _ribbonControl;
        private static string? _lastSelectedTabId;

        public static bool TryInitialize()
        {
            if (_timer != null)
                return true;

            var control = FindRibbonControl();
            if (control == null)
                return false;

            _ribbonControl = control;
            _lastSelectedTabId = TryGetSelectedTabId(control);

            var timer = new DispatcherTimer(DispatcherPriority.ContextIdle)
            {
                Interval = PollInterval
            };
            timer.Tick += OnTick;
            _timer = timer;
            timer.Start();
            return true;
        }

        public static void Stop()
        {
            var timer = _timer;
            _timer = null;
            if (timer != null)
            {
                try { timer.Stop(); } catch { }
                try { timer.Tick -= OnTick; } catch { }
            }
            _ribbonControl = null;
            _lastSelectedTabId = null;
        }

        private static void OnTick(object? sender, EventArgs e)
        {
            var control = _ribbonControl;
            if (control == null)
                return;

            try
            {
                var selectedId = TryGetSelectedTabId(control);
                if (string.IsNullOrWhiteSpace(selectedId))
                    return;

                var previous = _lastSelectedTabId;
                if (string.Equals(previous, selectedId, StringComparison.OrdinalIgnoreCase))
                    return;

                _lastSelectedTabId = selectedId;
                if (!string.Equals(selectedId, HomeTabId, StringComparison.OrdinalIgnoreCase))
                {
                    try { StartCenterPaletteCoordinator.Hide(); }
                    catch { }
                    return;
                }

                try
                {
                    // HOME owns the large Start Center canvas. Release BIM/quantity side palettes
                    // before opening it so tab transitions never leave competing docked surfaces.
                    PaletteCoordinator.Hide();
                    new StartCenterCommands().ShowStartCenter();
                }
                catch { }
            }
            catch
            {
                // Host Ribbon can rebuild while workspaces change. Re-resolve on the next tick.
                _ribbonControl = FindRibbonControl();
                if (_ribbonControl != null)
                    _lastSelectedTabId = TryGetSelectedTabId(_ribbonControl);
            }
        }

        private static string? TryGetSelectedTabId(object control)
        {
            foreach (var propertyName in new[] { "CurrentTab", "SelectedTab", "ActiveTab" })
            {
                var tab = GetProperty(control, propertyName);
                var id = TabId(tab);
                if (!string.IsNullOrWhiteSpace(id))
                    return id;
            }

            var tabs = GetProperty(control, "Tabs");
            if (tabs == null)
                return null;

            foreach (var propertyName in new[] { "SelectedTabIndex", "SelectedIndex", "CurrentTabIndex" })
            {
                var rawIndex = GetProperty(control, propertyName);
                if (!(rawIndex is int index) || index < 0)
                    continue;
                var tab = ItemAt(tabs, index);
                var id = TabId(tab);
                if (!string.IsNullOrWhiteSpace(id))
                    return id;
            }

            if (tabs is IEnumerable enumerable)
            {
                foreach (var tab in enumerable)
                {
                    if (tab == null)
                        continue;
                    if (IsTrue(tab, "IsActive") || IsTrue(tab, "IsSelected") || IsTrue(tab, "Selected"))
                    {
                        var id = TabId(tab);
                        if (!string.IsNullOrWhiteSpace(id))
                            return id;
                    }
                }
            }

            return null;
        }

        private static bool IsTrue(object target, string propertyName) =>
            GetProperty(target, propertyName) is bool value && value;

        private static object? ItemAt(object collection, int index)
        {
            if (collection is IList list)
                return index < list.Count ? list[index] : null;

            if (!(collection is IEnumerable enumerable))
                return null;

            var current = 0;
            foreach (var item in enumerable)
            {
                if (current == index)
                    return item;
                current++;
            }
            return null;
        }

        private static string? TabId(object? tab)
        {
            if (tab == null)
                return null;
            return GetProperty(tab, "Id") as string ?? GetProperty(tab, "Name") as string;
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
    }
}
