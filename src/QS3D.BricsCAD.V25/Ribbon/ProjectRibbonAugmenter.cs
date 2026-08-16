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
        private const string PanelSourceId = "QS3D_PROJECT_BLT_SETUP_PANEL_SOURCE";
        private const string PanelTitle = "Dự án";
        private static bool _initialized;

        private sealed class ButtonSpec
        {
            public ButtonSpec(string id, string text, string command)
            {
                Id = id;
                Text = text;
                Command = command;
            }

            public string Id { get; }
            public string Text { get; }
            public string Command { get; }
        }

        // Keep the Project Setup ribbon intentionally small and deterministic. The BLT3D
        // reference has exactly these three entry points in this tab; detailed actions live
        // inside the Project Setup surface instead of leaking into extra ribbon groups.
        private static readonly ButtonSpec[] Buttons =
        {
            new ButtonSpec("QS3D_PROJECT_INFO", "Thông tin\ndự án", "QS3DPROJECTTOOLS"),
            new ButtonSpec("QS3D_PROJECT_FLOORS", "Cài đặt\ntầng", "QS3DLEVELS"),
            new ButtonSpec("QS3D_PROJECT_PROPERTIES", "Thuộc tính\ndự án", "QS3DPROJECTTOOLS")
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

                // RibbonBootstrapper creates the generic Status/Template/Scope panels first.
                // The BLT3D Project Setup tab replaces those groups rather than adding another
                // tools panel beside them, so reconcile the whole QS3D-owned tab here.
                Clear(panels);
                var source = CreateProjectSetupPanel(panels);
                var items = GetProperty(source, "Items");
                if (items == null) return false;

                foreach (var spec in Buttons)
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

        private static object CreateProjectSetupPanel(object panels)
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
