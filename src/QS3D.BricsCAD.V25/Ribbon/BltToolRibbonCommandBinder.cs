using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Input;
using Bricscad.ApplicationServices;
using Application = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.Ribbon
{
    /// <summary>
    /// Replaces the presentation-only TOOL fallback command strings with verified runtime
    /// commands after the owner-reference panel tree exists. Keeping binding separate from
    /// layout makes the visual surface retry-safe while preventing generic QS3D workspace
    /// placeholders from shipping as apparently functional buttons.
    /// </summary>
    internal static class BltToolRibbonCommandBinder
    {
        private const string AssemblyName = "BrxMgd";
        private const string ToolTabId = "QS3D_TOOL";
        private const string Prefix = "QS3D_TOOL_BLT_";

        private static readonly IReadOnlyDictionary<string, string> CommandByButtonId =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [Prefix + "PILE_LOWER"] = "QS3DBLTPILELOWER",
                [Prefix + "LEAN_CONCRETE"] = "QS3DBLTLEANCONCRETE",
                [Prefix + "EXCAVATE_FOUNDATION"] = "QS3DBLTFOUNDATIONEXCAVATE",
                [Prefix + "SLAB_OPENING"] = "QS3DDRAWSLABOPEN",
                [Prefix + "MCP_SETTINGS"] = "QS3DMCPSETTINGS",
                [Prefix + "MCP_DOCS"] = "QS3DMCPDOCS",
                [Prefix + "AI_DASHBOARD"] = "QS3DAIDASHBOARD",
                [Prefix + "MCP_CONNECTION"] = "QS3DMCPCHECK",
                [Prefix + "CAD_TO_BLT"] = "QS3DRECOGNIZE"
            };

        private static bool _initialized;

        public static bool TryInitialize()
        {
            if (_initialized) return true;

            try
            {
                var control = FindRibbonControl();
                if (control == null) return false;
                var tabs = GetProperty(control, "Tabs");
                var tab = tabs == null ? null : FindById(tabs, ToolTabId);
                if (tab == null) return false;
                var panels = GetProperty(tab, "Panels");
                if (!(panels is IEnumerable panelEnumerable)) return false;

                var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                object? pileEmbedTextBox = null;
                var visited = new HashSet<object>();
                foreach (var panel in panelEnumerable)
                {
                    if (panel == null) continue;
                    var source = GetProperty(panel, "Source");
                    var items = source == null ? null : GetProperty(source, "Items");
                    if (items != null)
                        BindCollection(items, visited, found, ref pileEmbedTextBox);
                }

                if (found.Count != CommandByButtonId.Count || pileEmbedTextBox == null)
                    return false;

                SetProperty(pileEmbedTextBox, "CommandHandler", new PileEmbedTextBoxHandler(pileEmbedTextBox));
                _initialized = true;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void Reset() => _initialized = false;

        private static void BindCollection(
            object collection,
            HashSet<object> visited,
            HashSet<string> found,
            ref object? pileEmbedTextBox)
        {
            if (!(collection is IEnumerable enumerable)) return;
            foreach (var item in enumerable)
            {
                if (item == null || !visited.Add(item)) continue;
                var id = GetProperty(item, "Id") as string;
                if (!string.IsNullOrWhiteSpace(id))
                {
                    if (CommandByButtonId.TryGetValue(id!, out var command))
                    {
                        SetProperty(item, "CommandParameter", command);
                        found.Add(id!);
                    }
                    else if (string.Equals(id, Prefix + "PILE_EMBED_MM", StringComparison.OrdinalIgnoreCase))
                    {
                        pileEmbedTextBox = item;
                    }
                }

                var nested = GetProperty(item, "Items");
                if (nested != null)
                    BindCollection(nested, visited, found, ref pileEmbedTextBox);
            }
        }

        private static object? FindById(object collection, string id)
        {
            if (!(collection is IEnumerable enumerable)) return null;
            foreach (var item in enumerable)
            {
                if (item == null) continue;
                if (string.Equals(GetProperty(item, "Id") as string, id, StringComparison.OrdinalIgnoreCase))
                    return item;
            }
            return null;
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

        private static void SetProperty(object target, string name, object value)
        {
            var property = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            if (property == null || !property.CanWrite) return;
            if (property.PropertyType.IsInstanceOfType(value) || property.PropertyType == value.GetType())
                property.SetValue(target, value, null);
        }

        private sealed class PileEmbedTextBoxHandler : ICommand
        {
            private readonly object _textBox;

            public PileEmbedTextBoxHandler(object textBox)
            {
                _textBox = textBox ?? throw new ArgumentNullException(nameof(textBox));
            }

            public bool CanExecute(object? parameter) => true;

            public void Execute(object? parameter)
            {
                var value = GetProperty(_textBox, "TextValue") as string;
                if (string.IsNullOrWhiteSpace(value)) return;
                var document = Application.DocumentManager.MdiActiveDocument;
                if (BltToolRuntimeState.TrySetPileEmbedMillimeters(value, out var message))
                {
                    if (document != null) document.Editor.WriteMessage("\nQS3D " + message);
                    return;
                }

                if (document != null)
                    document.Editor.WriteMessage("\nQS3D giá trị Ngàm vào đài không hợp lệ: " + message);
            }

            public event EventHandler? CanExecuteChanged
            {
                add { }
                remove { }
            }
        }
    }
}
