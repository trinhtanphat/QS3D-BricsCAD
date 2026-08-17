using System;
using System.Collections;
using System.Reflection;
using System.Windows.Threading;
using Bricscad.Windows;
using QS3D.BricsCAD.V25.UI;
using DrawingSize = System.Drawing.Size;
using WpfSize = System.Windows.Size;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Owns the compact owner-reference Project Information work surface inside BricsCAD.
    /// The surface is intentionally non-mutating and automatically releases itself when the
    /// user leaves the QS3D Project tab, preventing stale coverage of HOME/BIM/native workspaces.
    /// </summary>
    internal static class ProjectSetupPaletteCoordinator
    {
        private const string AssemblyName = "BrxMgd";
        private const string ProjectTabId = "QS3D_PROJECT";
        private static readonly Guid ProjectSetupGuid =
            new Guid("D9F85CA8-837A-4C40-A60B-3A89B7E1477B");
        private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(250);

        private static PaletteSet? _palette;
        private static BltProjectSetupPanel? _panel;
        private static DispatcherTimer? _tabWatcher;

        public static bool IsVisible => _palette != null && _palette.Visible;

        public static void ShowProjectInformation()
        {
            Show(panel => panel.ShowProjectInformation());
        }

        public static void Hide()
        {
            StopTabWatcher();
            if (_palette != null)
                _palette.Visible = false;
        }

        public static void Dispose()
        {
            StopTabWatcher();
            var palette = _palette;
            _palette = null;
            _panel = null;
            if (palette == null) return;
            try { palette.Dispose(); }
            catch
            {
                // BricsCAD may already be tearing down native UI during plugin unload.
            }
        }

        private static void Show(Action<BltProjectSetupPanel> selectSurface)
        {
            EnsureCreated();

            // Project Setup owns the large embedded canvas. Release other QS3D docked surfaces
            // first so the reference view does not stack over HOME/BIM palettes.
            StartCenterPaletteCoordinator.Hide();
            PaletteCoordinator.Hide();

            if (_panel != null)
                selectSurface(_panel);
            if (_palette != null)
                _palette.Visible = true;

            StartTabWatcher();
        }

        private static void EnsureCreated()
        {
            if (_palette != null && _panel != null) return;

            Dispose();
            try
            {
                _panel = new BltProjectSetupPanel();
                _palette = new PaletteSet("QS3D — Thiết lập dự án", ProjectSetupGuid)
                {
                    DockEnabled = DockSides.Left | DockSides.Right,
                    Dock = DockSides.Left,
                    Visible = false,
                    KeepFocus = false,
                    MinimumSize = new DrawingSize(720, 480)
                };
                _palette.DeviceIndependentSize = new WpfSize(1040, 680);
                _palette.AddVisual("Dự án", _panel, true);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private static void StartTabWatcher()
        {
            if (_tabWatcher != null) return;

            var timer = new DispatcherTimer(DispatcherPriority.ContextIdle)
            {
                Interval = PollInterval
            };
            timer.Tick += OnTabWatcherTick;
            _tabWatcher = timer;
            timer.Start();
        }

        private static void StopTabWatcher()
        {
            var timer = _tabWatcher;
            _tabWatcher = null;
            if (timer == null) return;
            try { timer.Stop(); } catch { }
            try { timer.Tick -= OnTabWatcherTick; } catch { }
        }

        private static void OnTabWatcherTick(object? sender, EventArgs e)
        {
            try
            {
                var control = FindRibbonControl();
                if (control == null) return;

                var selectedId = TryGetSelectedTabId(control);
                if (string.IsNullOrWhiteSpace(selectedId)) return;
                if (string.Equals(selectedId, ProjectTabId, StringComparison.OrdinalIgnoreCase)) return;

                Hide();
            }
            catch
            {
                // Presentation polling is best-effort. A transient Ribbon rebuild must not affect
                // project/CAD state; the next timer tick retries while this surface remains visible.
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
            if (tabs == null) return null;

            foreach (var propertyName in new[] { "SelectedTabIndex", "SelectedIndex", "CurrentTabIndex" })
            {
                var rawIndex = GetProperty(control, propertyName);
                if (!(rawIndex is int index) || index < 0) continue;
                var tab = ItemAt(tabs, index);
                var id = TabId(tab);
                if (!string.IsNullOrWhiteSpace(id))
                    return id;
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
                    if (!string.IsNullOrWhiteSpace(id))
                        return id;
                }
            }

            return null;
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

        private static string? TabId(object? tab)
        {
            if (tab == null) return null;
            return GetProperty(tab, "Id") as string ?? GetProperty(tab, "Name") as string;
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
    }
}
