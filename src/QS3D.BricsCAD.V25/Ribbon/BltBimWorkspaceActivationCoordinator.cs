using System;
using System.Collections;
using System.Reflection;
using System.Windows.Threading;

namespace QS3D.BricsCAD.V25.Ribbon
{
    /// <summary>
    /// Keeps the top-level QS3D work surface coupled to the selected Ribbon tab without relying on
    /// a BricsCAD-version-specific Ribbon event signature. BIM retains its bounded docking settle
    /// ticks, while HOME and PROJECT are routed to their dedicated large surfaces so a stale BIM
    /// palette can never remain on top after those tab transitions.
    /// </summary>
    internal static class BltBimWorkspaceActivationCoordinator
    {
        private const string AssemblyName = "BrxMgd";
        private const string HomeTabId = "QS3D_HOME";
        private const string ProjectTabId = "QS3D_PROJECT";
        private const string BimTabId = "QS3D_BIM";
        private const int BimSettleTicks = 2;
        private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(250);

        private static DispatcherTimer? _timer;
        private static string _lastTabId = string.Empty;
        private static int _bimSettleTicksRemaining;

        public static void Start()
        {
            if (_timer != null) return;

            _lastTabId = string.Empty;
            _bimSettleTicksRemaining = 0;
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
            _bimSettleTicksRemaining = 0;
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
                if (string.IsNullOrWhiteSpace(currentId)) return;

                var changed = !string.Equals(currentId, _lastTabId, StringComparison.OrdinalIgnoreCase);
                if (!changed)
                {
                    if (string.Equals(currentId, BimTabId, StringComparison.OrdinalIgnoreCase) &&
                        _bimSettleTicksRemaining > 0)
                    {
                        if (ReassertBimWorkspace())
                        {
                            _bimSettleTicksRemaining--;
                        }
                    }
                    return;
                }

                // BricsCAD may reconstruct its top-level Ribbon/window chrome on workspace or tab
                // transitions after the one-time Ribbon initializer has stopped. Reassert only
                // QS3D-owned shell presentation here; the operation is idempotent and does not
                // rebuild feature panels or take ownership of native tabs. Keep the observed tab
                // unpublished until the complete route succeeds so a transient host/WPF exception
                // leaves the transition pending for the next bounded polling tick.
                Blt3dShellChromeCoordinator.Reassert();

                if (string.Equals(currentId, HomeTabId, StringComparison.OrdinalIgnoreCase))
                {
                    _bimSettleTicksRemaining = 0;
                    RouteHomeSurface();
                    _lastTabId = currentId;
                    return;
                }

                if (string.Equals(currentId, ProjectTabId, StringComparison.OrdinalIgnoreCase))
                {
                    _bimSettleTicksRemaining = 0;
                    RouteProjectSurface();
                    _lastTabId = currentId;
                    return;
                }

                if (!string.Equals(currentId, BimTabId, StringComparison.OrdinalIgnoreCase))
                {
                    _bimSettleTicksRemaining = 0;
                    // Normal CAD/Ribbon tabs do not own either large QS3D special surface.
                    // Preserve the established native/workspace behavior for those tabs while
                    // ensuring a prior HOME/PROJECT surface cannot cover the editor.
                    StartCenterPaletteCoordinator.Hide();
                    ProjectSetupPaletteCoordinator.Hide();
                    _lastTabId = currentId;
                    return;
                }

                _bimSettleTicksRemaining = BimSettleTicks;
                ReassertBimWorkspace();
                _lastTabId = currentId;
            }
            catch
            {
                // Ribbon polling is presentation-only. A host/Ribbon transient must never break
                // CAD commands or initialization; because a failed route is not published as the
                // last tab, the next polling tick retries that transition naturally.
            }
        }

        private static void RouteHomeSurface()
        {
            // HOME owns the Start Center. Release both the Project canvas and BIM palettes first
            // so the previous work surface cannot remain visible behind/on top of it.
            ProjectSetupPaletteCoordinator.Hide();
            PaletteCoordinator.Hide();
            StartCenterPaletteCoordinator.Show();
        }

        private static void RouteProjectSurface()
        {
            // PROJECT owns the dedicated Project Information canvas. Keep this route independent
            // from the BIM Workspace and HOME Start Center; no document/model command is executed.
            StartCenterPaletteCoordinator.Hide();
            PaletteCoordinator.Hide();
            ProjectSetupPaletteCoordinator.ShowProjectInformation();
        }

        private static bool ReassertBimWorkspace()
        {
            Blt3dShellChromeCoordinator.Reassert();
            StartCenterPaletteCoordinator.Hide();
            ProjectSetupPaletteCoordinator.Hide();
            return PaletteCoordinator.ShowBimWorkspace();
        }

        private static string ResolveCurrentTabId(object control)
        {
            var tabs = GetProperty(control, "Tabs");
            if (tabs != null)
            {
                // Prefer explicit selection state exposed by the Ribbon collection. Some BricsCAD
                // builds keep CurrentTab populated while another tab is becoming active, which can
                // otherwise leave the prior QS3D surface visible for the whole transition.
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
            }

            // Direct host properties remain a compatibility fallback for versions that do not
            // expose an index/active marker. Prefer Selected/Active before the more ambiguous
            // CurrentTab property.
            foreach (var propertyName in new[] { "SelectedTab", "ActiveTab", "CurrentTab" })
            {
                var tab = GetProperty(control, propertyName);
                var id = TabId(tab);
                if (!string.IsNullOrWhiteSpace(id)) return id;
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
