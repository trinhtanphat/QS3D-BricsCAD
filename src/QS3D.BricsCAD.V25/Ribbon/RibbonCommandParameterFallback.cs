using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Input;

namespace QS3D.BricsCAD.V25.Ribbon
{
    /// <summary>
    /// Makes QS3D ribbon ICommand handlers resilient to BricsCAD builds that do not
    /// forward RibbonButton.CommandParameter into CanExecute/Execute.
    /// </summary>
    internal static class RibbonCommandParameterFallback
    {
        private const string AssemblyName = "BrxMgd";
        private const string Qs3dTabPrefix = "QS3D_";

        public static bool TryInitialize()
        {
            try
            {
                var control = FindRibbonControl();
                if (control == null)
                    return false;

                var tabs = GetProperty(control, "Tabs");
                if (!(tabs is IEnumerable enumerable))
                    return false;

                foreach (var tab in enumerable)
                {
                    if (tab == null)
                        continue;

                    var tabId = GetProperty(tab, "Id") as string;
                    if (string.IsNullOrWhiteSpace(tabId)
                        || !tabId.StartsWith(Qs3dTabPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    RepairTab(tab);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void RepairTab(object tab)
        {
            var panels = GetProperty(tab, "Panels");
            if (!(panels is IEnumerable panelEnumerable))
                return;

            foreach (var panel in panelEnumerable)
            {
                if (panel == null)
                    continue;

                var source = GetProperty(panel, "Source");
                if (source == null)
                    continue;

                var items = GetProperty(source, "Items");
                if (items == null)
                    continue;

                RepairCollection(items, new HashSet<object>());
            }
        }

        private static void RepairCollection(object collection, HashSet<object> visited)
        {
            if (!(collection is IEnumerable enumerable))
                return;

            foreach (var item in enumerable)
            {
                if (item == null || !visited.Add(item))
                    continue;

                RepairItem(item);

                var nestedItems = GetProperty(item, "Items");
                if (nestedItems != null)
                    RepairCollection(nestedItems, visited);
            }
        }

        private static void RepairItem(object item)
        {
            var command = GetProperty(item, "CommandParameter") as string;
            if (string.IsNullOrWhiteSpace(command))
                return;

            var handler = GetProperty(item, "CommandHandler") as ICommand;
            if (handler == null || handler is CommandParameterFallbackHandler)
                return;

            SetProperty(item, "CommandHandler", new CommandParameterFallbackHandler(handler, command));
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

        private static void SetProperty(object target, string name, object value)
        {
            var property = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            if (property == null || !property.CanWrite)
                return;

            if (property.PropertyType.IsInstanceOfType(value) || property.PropertyType == value.GetType())
                property.SetValue(target, value, null);
        }

        private sealed class CommandParameterFallbackHandler : ICommand
        {
            private readonly ICommand _inner;
            private readonly string _fallbackCommand;

            public CommandParameterFallbackHandler(ICommand inner, string fallbackCommand)
            {
                _inner = inner ?? throw new ArgumentNullException(nameof(inner));
                _fallbackCommand = fallbackCommand ?? throw new ArgumentNullException(nameof(fallbackCommand));
            }

            public bool CanExecute(object? parameter) => _inner.CanExecute(ResolveParameter(parameter));

            public void Execute(object? parameter) => _inner.Execute(ResolveParameter(parameter));

            private string ResolveParameter(object? parameter) =>
                parameter is string command && !string.IsNullOrWhiteSpace(command)
                    ? command
                    : _fallbackCommand;

            public event EventHandler? CanExecuteChanged
            {
                add { _inner.CanExecuteChanged += value; }
                remove { _inner.CanExecuteChanged -= value; }
            }
        }
    }
}
