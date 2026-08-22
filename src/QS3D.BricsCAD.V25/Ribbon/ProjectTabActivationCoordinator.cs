using System;
using System.Collections;
using System.Reflection;
using System.Windows.Threading;

namespace QS3D.BricsCAD.V25.Ribbon
{
    /// <summary>
    /// Couples the Project Information palette to the QS3D Project ribbon tab.
    /// This coordinator is presentation-only and never creates or mutates project data.
    /// </summary>
    internal static class ProjectTabActivationCoordinator
    {
        private const string AssemblyName = "BrxMgd";
        private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(250);

        private static DispatcherTimer? _timer;
        private static string _lastTabId = string.Empty;

        public static void Start()
        {
            if (_timer != null) return;

            _lastTabId = string.Empty;
            var timer = new DispatcherTimer(DispatcherPriority.ApplicationIdle)
            {
                Interval = PollInterval
            };
            timer.Tick += OnTick;
            _timer = timer;
            timer.Start();
        }

        public static void Stop()
        {
            var timer = _timer;
            _timer = null;
            _lastTabId = string.Empty;
            if (timer != null)
            {
                try { timer.Stop(); } catch { }
                try { timer.Tick -= OnTick; } catch { }
            }

            try { ProjectSetupPaletteCoordinator.Hide(); } catch { }
        }

        internal static void NotifyActiveTabChanged(string tabId)
        {
            if (string.IsNullOrWhiteSpace(tabId) ||
                string.Equals(tabId, _lastTabId, StringComparison.OrdinalIgnoreCase))
                return;

            _lastTabId = tabId;
            if (string.Equals(tabId, ProjectRibbonAugmenter.ProjectTabGroupId, StringComparison.OrdinalIgnoreCase))
            {
                ProjectSetupPaletteCoordinator.ShowProjectInformation();
                return;
            }

            ProjectSetupPaletteCoordinator.Hide();
        }

        private static void OnTick(object? sender, EventArgs e)
        {
            try
            {
                var control = FindRibbonControl();
                if (control == null) return;

                var currentId = ResolveCurrentTabId(control);
                if (!string.IsNullOrWhiteSpace(currentId))
                    NotifyActiveTabChanged(currentId);
            }
            catch
            {
                // Ribbon polling is presentation-only. A transient host rebuild must never affect
                // CAD/project state; the next tick can retry naturally.
            }
        }

        private static string ResolveCurrentTabId(object control)
        {
            foreach (var propertyName in new[] { "CurrentTab", "SelectedTab", "ActiveTab" })
            {
                var tab = GetProperty(control, propertyName);
                var id = TabId(tab);
                if (!string.IsNullOrWhiteSpace(id)) return id;
            }

            var tabs = GetProperty(control, "Tabs");
            if (tabs == null) return string.Empty;

            foreach (var propertyName in new[] { "SelectedTabIndex", "SelectedIndex", "CurrentTabIndex" })
            {
                var rawIndex = GetProperty(control, propertyName);
                if (!(rawIndex is int index) || index < 0) continue;
                var tab = ItemAt(tabs, index);
                var id = TabId(tab);
                if (!string.IsNullOrWhiteSpace(id)) return id;
            }

            if (tabs is IEnumerable enumerable)
            {
                foreach (var tab in enumerable)
                {
                    if (tab == null) continue;
                    if (!ReadBool(tab, "IsActive") &&
                        !ReadBool(tab, "IsSelected") &&
                        !ReadBool(tab, "Selected"))
                        continue;

                    var id = TabId(tab);
                    if (!string.IsNullOrWhiteSpace(id)) return id;
                }
            }

            return string.Empty;
        }

        private static string TabId(object? tab)
        {
            if (tab == null) return string.Empty;
            return GetProperty(tab, "Id") as string ?? GetProperty(tab, "Name") as string ?? string.Empty;
        }

        private static object? ItemAt(object collection, int index)
        {
            if (collection is IList list)
                return index < list.Count ? list[index] : null;

            if (!(collection is IEnumerable enumerable))
                return null;

            var current = 0;
            foreach (var item in enumerable)
            {
                if (current == index) return item;
                current++;
            }
            return null;
        }

        private static bool ReadBool(object target, string propertyName) =>
            GetProperty(target, propertyName) is bool flag && flag;

        private static object? FindRibbonControl()
        {
            var servicesType = Type.GetType("Bricscad.Ribbon.RibbonServices, " + AssemblyName, false);
            if (servicesType == null) return null;

            var palette = servicesType.GetProperty("RibbonPaletteSet", BindingFlags.Public | BindingFlags.Static)
                ?.GetValue(null, null);
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
    }
}
