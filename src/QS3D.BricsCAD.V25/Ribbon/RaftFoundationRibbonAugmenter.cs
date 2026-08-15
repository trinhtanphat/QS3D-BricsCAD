using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Windows.Input;
using Bricscad.ApplicationServices;

namespace QS3D.BricsCAD.V25.Ribbon
{
    internal static class RaftFoundationRibbonAugmenter
    {
        private const string AssemblyName = "BrxMgd";
        private const string AuthorTabId = "QS3D_AUTHOR";
        private const string StructurePanelSourceId = "QS3D_AUTHOR_STRUCTURE_PANEL_SOURCE";
        private const string ButtonId = "QS3D_AUTHOR_STRUCTURE_MONG_BE_QUICK";
        private const string ButtonText = "Móng Bè";
        private const string Command = "QS3DDRAWRAFTFOUNDATION";
        private static bool _initialized;

        public static bool TryInitialize()
        {
            if (_initialized) return true;
            try
            {
                var control = FindRibbonControl();
                if (control == null) return false;
                var tabs = GetProperty(control, "Tabs") as IEnumerable;
                if (tabs == null) return false;
                var authorTab = FindById(tabs, AuthorTabId);
                if (authorTab == null) return false;
                var panels = GetProperty(authorTab, "Panels") as IEnumerable;
                if (panels == null) return false;
                var source = FindPanelSource(panels, StructurePanelSourceId);
                if (source == null) return false;
                var items = GetProperty(source, "Items")
                    ?? throw new InvalidOperationException("RibbonPanelSource.Items was not available.");

                var button = FindById(items as IEnumerable, ButtonId) ?? FindByText(items, ButtonText);
                if (button == null)
                {
                    button = Create("Bricscad.Windows.RibbonButton");
                    SetProperty(button, "Id", ButtonId);
                    Add(items, button);
                }

                SetProperty(button, "Name", ButtonText);
                SetProperty(button, "Text", ButtonText);
                SetProperty(button, "ShowText", true);
                SetProperty(button, "ShowImage", false);
                SetProperty(button, "CommandParameter", Command);
                SetProperty(button, "CommandHandler", new CommandHandler());

                _initialized = true;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void Reset() => _initialized = false;

        private static object? FindRibbonControl()
        {
            var servicesType = Type.GetType("Bricscad.Ribbon.RibbonServices, " + AssemblyName, false);
            if (servicesType == null) return null;
            var palette = servicesType.GetProperty("RibbonPaletteSet", BindingFlags.Public | BindingFlags.Static)?.GetValue(null, null);
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

        private static object? FindById(IEnumerable? items, string id)
        {
            if (items == null) return null;
            foreach (var item in items)
            {
                if (item == null) continue;
                if (string.Equals(GetProperty(item, "Id") as string, id, StringComparison.OrdinalIgnoreCase))
                    return item;
            }
            return null;
        }

        private static object? FindByText(object items, string text)
        {
            if (!(items is IEnumerable enumerable)) return null;
            foreach (var item in enumerable)
            {
                if (item == null) continue;
                if (string.Equals(GetProperty(item, "Text") as string, text, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(GetProperty(item, "Name") as string, text, StringComparison.OrdinalIgnoreCase))
                    return item;
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