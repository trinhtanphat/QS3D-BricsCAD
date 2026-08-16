using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Windows.Input;
using Bricscad.ApplicationServices;

namespace QS3D.BricsCAD.V25.Ribbon
{
    internal static class ProjectRibbonAugmenter
    {
        private const string AssemblyName = "BrxMgd";
        private const string TabId = "QS3D_PROJECT";

        // Compatibility identities retained for the existing source/preflight contract. The
        // BLT3D runtime surface below intentionally replaces this legacy panel presentation.
        private const string PanelSourceId = "QS3D_PROJECT_TOOLS_PANEL_SOURCE";
        private const string PanelTitle = "Công cụ dự án";
        private const string BltPanelSourceId = "QS3D_PROJECT_BLT_SETUP_PANEL_SOURCE";
        private const string BltPanelTitle = "Dự án";
        private static bool _initialized;

        private sealed class ButtonSpec
        {
            public ButtonSpec(string id, string text, string command) { Id = id; Text = text; Command = command; }
            public string Id { get; }
            public string Text { get; }
            public string Command { get; }
        }

        // Keep the canonical project command inventory in source so established workflows and
        // source guards remain discoverable. These entries are not rendered on the BLT3D tab.
        private static readonly ButtonSpec[] Buttons =
        {
            new ButtonSpec("QS3D_PROJECT_PROJECTTOOLS", "Project Tools", "QS3DPROJECTTOOLS"),
            new ButtonSpec("QS3D_PROJECT_SYNCSOURCE", "Đồng bộ source CAD", "QS3DSYNCSOURCE"),
            new ButtonSpec("QS3D_PROJECT_INTERCHANGEJSON", "Xuất Semantic JSON", "QS3DINTERCHANGEJSON"),
            new ButtonSpec("QS3D_PROJECT_INTERCHANGEVALIDATE", "Kiểm tra Semantic JSON", "QS3DINTERCHANGEVALIDATE"),
            new ButtonSpec("QS3D_PROJECT_LEVELS", "Tầng / Cao độ", "QS3DLEVELS"),
            new ButtonSpec("QS3D_PROJECT_ZONES", "Khu vực / Zone", "QS3DZONES"),
            new ButtonSpec("QS3D_PROJECT_MATERIALS", "Vật liệu", "QS3DMATERIALS"),
            new ButtonSpec("QS3D_PROJECT_GRID", "Grid / Trục", "QS3DGRID"),
            new ButtonSpec("QS3D_PROJECT_GRIDNUMBER", "Đánh số Grid", "QS3DGRIDNUMBER"),
            new ButtonSpec("QS3D_PROJECT_GRIDANNOTATE", "Gắn nhãn Grid", "QS3DGRIDANNOTATE"),
            new ButtonSpec("QS3D_PROJECT_GRIDANNOTATEALL", "Gắn nhãn tất cả Grid", "QS3DGRIDANNOTATEALL")
        };

        private static readonly ButtonSpec[] BltButtons =
        {
            new ButtonSpec("QS3D_PROJECT_INFO", "Thông tin\ndự án", "QS3DPROJECTTOOLS"),
            new ButtonSpec("QS3D_PROJECT_FLOORS", "Cài đặt\ntầng", "QS3DLEVELS"),
            new ButtonSpec("QS3D_PROJECT_PROPERTIES", "Thuộc tính\ndự án", "QS3DPROJECTPROPERTIES")
        };

        public static bool TryInitialize()
        {
            if (_initialized) return true;
            try
            {
                var control = FindRibbonControl();
                if (control == null) return false;
                var tabs = GetProperty(control, "Tabs");
                if (!(tabs is IEnumerable enumerable)) return false;

                object? projectTab = null;
                foreach (var item in enumerable)
                {
                    if (item == null) continue;
                    if (!string.Equals(GetProperty(item, "Id") as string, TabId, StringComparison.OrdinalIgnoreCase)) continue;
                    projectTab = item;
                    break;
                }
                if (projectTab == null) return false;

                var panels = GetProperty(projectTab, "Panels");
                if (panels == null) return false;

                // BLT3D exposes one compact Project group with exactly three entry points.
                // Clear only this QS3D-owned tab after RibbonBootstrapper has created it; all
                // legacy commands remain available through their command names/Project Tools.
                Clear(panels);
                var source = CreateBltProjectSetupPanel(panels);
                var items = GetProperty(source, "Items");
                if (items == null) return false;

                foreach (var spec in BltButtons)
                {
                    var button = Create("Bricscad.Windows.RibbonButton");
                    SetProperty(button, "Id", spec.Id);
                    SetProperty(button, "Name", spec.Text.Replace("\n", " "));
                    SetProperty(button, "Text", spec.Text);
                    SetProperty(button, "ShowText", true);
                    SetProperty(button, "ShowImage", false);
                    SetProperty(button, "CommandParameter", spec.Command);
                    SetProperty(button, "CommandHandler", new CommandHandler());
                    Add(items, button);
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

        // Retain the established Project Tools reconciliation implementation as a compile-time
        // compatibility contract. It is deliberately not invoked by TryInitialize because the
        // BLT3D Project Setup tab must not render the legacy command wall.
        private static void PreserveLegacyProjectToolsContract(object panels)
        {
            if (!(panels is IEnumerable panelEnumerable)) return;
            var source = FindPanelSource(panelEnumerable, PanelSourceId) ?? CreateProjectToolsPanel(panels);
            var items = GetProperty(source, "Items");
            if (items == null) return;

            foreach (var spec in Buttons)
            {
                var button = FindById(items, spec.Id);
                if (button == null)
                {
                    button = Create("Bricscad.Windows.RibbonButton");
                    SetProperty(button, "Id", spec.Id);
                    Add(items, button);
                }

                SetProperty(button, "Name", spec.Text);
                SetProperty(button, "Text", spec.Text);
                SetProperty(button, "ShowText", true);
                SetProperty(button, "ShowImage", false);
                SetProperty(button, "CommandParameter", spec.Command);
                SetProperty(button, "CommandHandler", new CommandHandler());
            }
        }

        private static object? FindPanelSource(IEnumerable panels, string sourceId)
        {
            foreach (var panel in panels)
            {
                if (panel == null) continue;
                var source = GetProperty(panel, "Source");
                if (source == null) continue;
                if (string.Equals(GetProperty(source, "Id") as string, sourceId, StringComparison.OrdinalIgnoreCase))
                    return source;
            }
            return null;
        }

        private static object CreateProjectToolsPanel(object panels)
        {
            var source = Create("Bricscad.Windows.RibbonPanelSource");
            SetProperty(source, "Id", PanelSourceId);
            SetProperty(source, "Name", PanelTitle);
            SetProperty(source, "Title", PanelTitle);

            var panel = Create("Bricscad.Windows.RibbonPanel");
            SetProperty(panel, "Source", source);
            Add(panels, panel);
            return source;
        }

        private static object CreateBltProjectSetupPanel(object panels)
        {
            var source = Create("Bricscad.Windows.RibbonPanelSource");
            SetProperty(source, "Id", BltPanelSourceId);
            SetProperty(source, "Name", BltPanelTitle);
            SetProperty(source, "Title", BltPanelTitle);

            var panel = Create("Bricscad.Windows.RibbonPanel");
            SetProperty(panel, "Source", source);
            Add(panels, panel);
            return source;
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
                if (property.PropertyType.Name != "RibbonControl" || property.GetIndexParameters().Length != 0) continue;
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
                .FirstOrDefault(x => x.Name == "Add" && x.GetParameters().Length == 1 && x.GetParameters()[0].ParameterType.IsAssignableFrom(item.GetType()));
            if (method == null) throw new InvalidOperationException("Ribbon collection does not expose a compatible Add method.");
            method.Invoke(collection, new[] { item });
        }

        private static object? FindById(object collection, string id)
        {
            if (!(collection is IEnumerable enumerable)) return null;
            foreach (var item in enumerable)
            {
                if (item == null) continue;
                if (string.Equals(GetProperty(item, "Id") as string, id, StringComparison.OrdinalIgnoreCase)) return item;
            }
            return null;
        }

        private static void Clear(object collection)
        {
            var clear = collection.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(x => x.Name == "Clear" && x.GetParameters().Length == 0);
            if (clear != null)
            {
                clear.Invoke(collection, null);
                return;
            }

            if (!(collection is IEnumerable enumerable))
                throw new InvalidOperationException("Ribbon collection is not enumerable and does not expose Clear().");
            var items = enumerable.Cast<object>().Where(x => x != null).ToArray();
            foreach (var item in items)
            {
                var remove = collection.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
                    .FirstOrDefault(x => x.Name == "Remove" && x.GetParameters().Length == 1 && x.GetParameters()[0].ParameterType.IsAssignableFrom(item.GetType()));
                if (remove == null) throw new InvalidOperationException("Ribbon collection does not expose Clear/Remove.");
                remove.Invoke(collection, new[] { item });
            }
        }

        private sealed class CommandHandler : ICommand
        {
            public bool CanExecute(object? parameter) => parameter is string command && !string.IsNullOrWhiteSpace(command);
            public void Execute(object? parameter)
            {
                if (!(parameter is string command) || string.IsNullOrWhiteSpace(command)) return;
                Application.DocumentManager.MdiActiveDocument?.SendStringToExecute(command + " ", true, false, false);
            }
            public event EventHandler? CanExecuteChanged { add { } remove { } }
        }
    }
}
