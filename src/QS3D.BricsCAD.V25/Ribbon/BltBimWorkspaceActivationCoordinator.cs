using System;
using System.Collections;
using System.Reflection;
using System.Windows.Threading;

namespace QS3D.BricsCAD.V25.Ribbon
{
    /// <summary>
    /// Keeps the BLT3D-style BIM palettes coupled to the QS3D MÔ HÌNH BIM tab without relying on
    /// a BricsCAD-version-specific Ribbon event signature. We sample the selected tab only while
    /// the plugin is loaded and react once per tab transition; manually closing palettes while
    /// staying on BIM is therefore respected.
    /// </summary>
    internal static class BltBimWorkspaceActivationCoordinator
    {
        private const string AssemblyName = "BrxMgd";
        private const string BimTabId = "QS3D_BIM";
        private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(400);

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
            if (timer == null) return;
            try { timer.Stop(); } catch { }
            try { timer.Tick -= OnTick; } catch { }
        }

        private static void OnTick(object? sender, EventArgs e)
        {
            try
            {
                var control = FindRibbonControl();
                if (control == null) return;
                var currentId = ResolveCurrentTabId(control);
                if (string.IsNullOrWhiteSpace(currentId) ||
                    string.Equals(currentId, _lastTabId, StringComparison.OrdinalIgnoreCase))
                    return;

                _lastTabId = currentId;
                if (string.Equals(currentId, BimTabId, StringComparison.OrdinalIgnoreCase))
                    PaletteCoordinator.ShowBimWorkspace();
            }
            catch
            {
                // Ribbon polling is presentation-only. A host/Ribbon transient must never break
                // CAD commands or initialization; the next tick retries naturally.
            }
        }

        private static string ResolveCurrentTabId(object control)
        {
            foreach (var propertyName in new[] { "CurrentTab", "ActiveTab", "SelectedTab" })
            {
                var tab = GetProperty(control, propertyName);
                var id = tab == null ? null : GetProperty(tab, "Id") as string;
                if (!string.IsNullOrWhiteSpace(id)) return id;
            }

            var tabs = GetProperty(control, "Tabs") as IEnumerable;
            if (tabs == null) return string.Empty;
            foreach (var tab in tabs)
            {
                if (tab == null) continue;
                var selected = ReadBool(tab, "IsActive") || ReadBool(tab, "IsSelected");
                if (!selected) continue;
                return GetProperty(tab, "Id") as string ?? string.Empty;
            }
            return string.Empty;
        }

        private static bool ReadBool(object target, string propertyName)
        {
            var value = GetProperty(target, propertyName);
            return value is bool flag && flag;
        }

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
