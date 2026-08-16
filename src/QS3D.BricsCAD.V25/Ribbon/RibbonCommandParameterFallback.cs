using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Input;
using Application = Bricscad.ApplicationServices.Application;

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
        private const string DrawCommandHandlerTypeName = "DrawRibbonCommandHandler";

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
                    if (tabId == null
                        || string.IsNullOrWhiteSpace(tabId)
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
            if (command == null || string.IsNullOrWhiteSpace(command))
                return;

            var handler = GetProperty(item, "CommandHandler") as ICommand;
            if (handler is CommandParameterFallbackHandler || handler is CapturedCommandHandler)
                return;

            // The BLT Draw handler is deliberately command-string-only. Replace it with a
            // captured dispatcher so VẼ/Công cụ/IFC buttons stay executable even on BricsCAD
            // builds that invoke ICommand with a null/different event parameter. Also repair a
            // command-bearing QS3D button that somehow lost its handler instead of leaving it dead.
            if (handler == null ||
                string.Equals(handler.GetType().Name, DrawCommandHandlerTypeName, StringComparison.Ordinal))
            {
                SetProperty(item, "CommandHandler", new CapturedCommandHandler(command));
                return;
            }

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

        private sealed class CapturedCommandHandler : ICommand
        {
            private readonly string _fallbackCommand;

            public CapturedCommandHandler(string fallbackCommand)
            {
                _fallbackCommand = fallbackCommand ?? throw new ArgumentNullException(nameof(fallbackCommand));
            }

            public bool CanExecute(object? parameter) =>
                Application.DocumentManager.MdiActiveDocument != null &&
                !string.IsNullOrWhiteSpace(ResolveParameter(parameter));

            public void Execute(object? parameter)
            {
                var command = ResolveParameter(parameter);
                if (string.IsNullOrWhiteSpace(command))
                    return;

                var document = Application.DocumentManager.MdiActiveDocument;
                if (document == null)
                    return;

                try
                {
                    document.SendStringToExecute(command.Trim() + " ", true, false, false);
                }
                catch (Exception ex)
                {
                    try
                    {
                        document.Editor.WriteMessage(
                            "\nQS3D Ribbon: không thể chạy lệnh '" + command.Trim() + "': " + ex.Message);
                    }
                    catch
                    {
                        // The host may already be tearing the document down; never turn a UI
                        // command-dispatch error into a second exception from diagnostics.
                    }
                }
            }

            private string ResolveParameter(object? _)
            {
                // The button's captured CommandParameter is authoritative. Some BricsCAD builds
                // pass non-null button metadata (for example an id/text string) as the ICommand
                // event parameter; treating that value as a CAD command can launch the wrong
                // command. Always dispatch the command that belongs to this ribbon button.
                return _fallbackCommand;
            }

            public event EventHandler? CanExecuteChanged
            {
                add { }
                remove { }
            }
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

            private string ResolveParameter(object? _)
            {
                // Keep wrapped QS3D handlers pinned to the owning button's CommandParameter as
                // well. This makes a null, label, id, or other host-supplied event parameter
                // harmless instead of allowing it to change the action routed by the button.
                return _fallbackCommand;
            }

            public event EventHandler? CanExecuteChanged
            {
                add { _inner.CanExecuteChanged += value; }
                remove { _inner.CanExecuteChanged -= value; }
            }
        }
    }
}
