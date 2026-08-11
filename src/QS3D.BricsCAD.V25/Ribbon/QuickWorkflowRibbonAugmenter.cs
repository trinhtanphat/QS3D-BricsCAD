using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Windows.Input;
using Bricscad.ApplicationServices;

namespace QS3D.BricsCAD.V25.Ribbon
{
    /// <summary>
    /// Adds BLT-style quick workflow entry points to the existing TẠO MỚI tab without
    /// duplicating the tab itself. Quick actions live in one exact dedicated panel so later
    /// Ribbon information-architecture regrouping cannot silently append them to an unrelated panel.
    /// </summary>
    internal static class QuickWorkflowRibbonAugmenter
    {
        private const string AssemblyName = "BrxMgd";
        private const string TabId = "QS3D_AUTHOR";
        private const string PanelSourceId = "QS3D_AUTHOR_QUICK_PANEL_SOURCE";
        private const string PanelTitle = "Tác vụ nhanh";
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

        private static readonly ButtonSpec[] Buttons =
        {
            new ButtonSpec("QS3D_AUTHOR_DRAW_ACTIVE", "Vẽ Nhanh", "QS3DDRAWACTIVE"),
            new ButtonSpec("QS3D_AUTHOR_CREATE_SIMILAR", "Vẽ Tương Tự", "QS3DCREATESIMILAR"),
            new ButtonSpec("QS3D_AUTHOR_PLAN2WALLS", "2D → Tường 3D", "QS3DCONVERT2D"),
            new ButtonSpec("QS3D_AUTHOR_WINDOW", "Vẽ Cửa Sổ", "QS3DDRAWWINDOW"),
            new ButtonSpec("QS3D_AUTHOR_MATERIALS", "Vật liệu", "QS3DMATERIALS")
        };

        public static bool TryInitialize()
        {
            if (_initialized) return true;
            try
            {
                var control = FindRibbonControl();
                if (control == null) return false;
                var tabs = GetProperty(control, "Tabs");
                if (!(tabs is IEnumerable tabItems)) return false;

                object? authorTab = null;
                foreach (var item in tabItems)
                {
                    if (item == null) continue;
                    if (string.Equals(GetProperty(item, "Id") as string, TabId, StringComparison.OrdinalIgnoreCase))
                    {
                        authorTab = item;
                        break;
                    }
                }
                if (authorTab == null) return false;

                var panels = GetProperty(authorTab, "Panels");
                if (!(panels is IEnumerable panelItems)) return false;
                var source = FindPanelSource(panelItems, PanelSourceId) ?? CreateQuickPanel(panels);

                var items = GetProperty(source, "Items");
                if (items == null) return false;
                foreach (var spec in Buttons)
                {
                    if (CollectionContainsId(items, spec.Id)) continue;
                    var button = Create("Bricscad.Windows.RibbonButton");
                    SetProperty(button, "Id", spec.Id);
                    SetProperty(button, "Name", spec.Text);
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

        private static object CreateQuickPanel(object panels)
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

        private static bool CollectionContainsId(object collection, string id)
        {
            if (!(collection is IEnumerable enumerable)) return false;
            foreach (var item in enumerable)
            {
                if (item == null) continue;
                if (string.Equals(GetProperty(item, "Id") as string, id, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
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
