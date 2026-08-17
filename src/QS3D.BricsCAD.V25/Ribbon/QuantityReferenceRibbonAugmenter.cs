using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Windows.Input;
using Bricscad.ApplicationServices;

namespace QS3D.BricsCAD.V25.Ribbon
{
    /// <summary>
    /// Reconciles the QS3D quantity tab into the compact BLT3D-style quantity workspace.
    /// The baseline bootstrapper intentionally remains broad; this final presentation pass
    /// removes QS3D-owned legacy quantity panels and recreates the two reference panels in
    /// deterministic order with large, icon-first commands.
    /// </summary>
    internal static class QuantityReferenceRibbonAugmenter
    {
        private const string AssemblyName = "BrxMgd";
        private const string TabId = "QS3D_QTY";
        private const string SettingsPanelSourceId = "QS3D_QTY_SETTINGS_PANEL_SOURCE";
        private const string QuantityPanelSourceId = "QS3D_QTY_QUANTITY_PANEL_SOURCE";

        private static readonly string[] OwnedPanelSourceIds =
        {
            // Current BLT3D-reference panels.
            SettingsPanelSourceId,
            QuantityPanelSourceId,

            // Older grouped bootstrap panels that must not survive beside the reference UI.
            "QS3D_QTY_EXCEL_PANEL_SOURCE",
            "QS3D_QTY_OPENINGS_PANEL_SOURCE",
            "QS3D_QTY_REBAR_SCHEDULE_PANEL_SOURCE",
            "QS3D_QTY_REBAR_3D_PANEL_SOURCE",
            "QS3D_QTY_REBAR_HEALTH_PANEL_SOURCE",

            // Previous quantity reference/fallback layouts.
            "QS3D_QTY_REFERENCE_PANEL_SOURCE",
            "QS3D_QTY_PANEL_SOURCE"
        };

        private static bool _initialized;

        private sealed class ButtonSpec
        {
            public ButtonSpec(string id, string text, string command, RibbonIconKind icon)
            {
                Id = id;
                Text = text;
                Command = command;
                Icon = icon;
            }

            public string Id { get; }
            public string Text { get; }
            public string Command { get; }
            public RibbonIconKind Icon { get; }
        }

        private static readonly ButtonSpec[] SettingsButtons =
        {
            new ButtonSpec(
                "QS3D_QTY_BLT_SETTINGS",
                "Cài đặt\ntính toán",
                "QS3DQUANTITYSETTINGS",
                RibbonIconKind.QuantitySettings)
        };

        private static readonly ButtonSpec[] QuantityButtons =
        {
            new ButtonSpec(
                "QS3D_QTY_BLT_CALCULATE",
                "Tính khối lượng\n(Engine2)",
                "QS3DREGEN",
                RibbonIconKind.QuantityCalculate),
            new ButtonSpec(
                "QS3D_QTY_BLT_EXPORT",
                "Xuất\n.blte2",
                "QS3DED2",
                RibbonIconKind.QuantityExport),
            new ButtonSpec(
                "QS3D_QTY_BLT_VIEW",
                "Xem khối\nlượng",
                "QS3DBQ",
                RibbonIconKind.QuantityView),
            new ButtonSpec(
                "QS3D_QTY_BLT_EXPLAIN",
                "Diễn\ngiải",
                "QS3DQUANTITYINSIGHT",
                RibbonIconKind.QuantityExplain),
            new ButtonSpec(
                "QS3D_QTY_BLT_COMPARE",
                "Đối chiếu\nCũ/Mới",
                "QS3DREVDIFF",
                RibbonIconKind.QuantityCompare)
        };

        public static bool TryInitialize()
        {
            if (_initialized) return true;

            try
            {
                var control = FindRibbonControl();
                if (control == null) return false;

                var tabs = GetProperty(control, "Tabs");
                var quantityTab = tabs == null ? null : FindById(tabs, TabId);
                if (quantityTab == null) return false;

                var panels = GetProperty(quantityTab, "Panels");
                if (panels == null) return false;

                // Remove only QS3D-owned quantity panel IDs. Native/third-party ribbon content
                // remains untouched. Rebuilding both panels also guarantees the order shown in
                // the BLT3D reference regardless of which older QS3D version was loaded first.
                foreach (var sourceId in OwnedPanelSourceIds)
                    RemoveOwnedPanel(panels, sourceId);

                AddPanel(panels, SettingsPanelSourceId, "Cài đặt", SettingsButtons);
                AddPanel(panels, QuantityPanelSourceId, "Khối lượng", QuantityButtons);

                // Do not accept source-level Image/LargeImage assignment as proof that BricsCAD
                // retained a visible native icon. Reapply the six clean-room reference glyphs
                // through the dedicated host-tree polisher and require successful read-back.
                if (!BltQuantityIconPolisher.TryInitialize())
                    return false;

                _initialized = true;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void Reset()
        {
            _initialized = false;
            BltQuantityIconPolisher.Reset();
        }

        private static void AddPanel(object panels, string sourceId, string title, ButtonSpec[] buttonSpecs)
        {
            var source = Create("Bricscad.Windows.RibbonPanelSource");
            SetProperty(source, "Id", sourceId);
            SetProperty(source, "Name", title);
            SetProperty(source, "Title", title);

            var items = GetProperty(source, "Items")
                        ?? throw new InvalidOperationException("RibbonPanelSource.Items was not available.");

            foreach (var spec in buttonSpecs)
                Add(items, CreateButton(spec));

            var panel = Create("Bricscad.Windows.RibbonPanel");
            SetProperty(panel, "Source", source);
            Add(panels, panel);
        }

        private static object CreateButton(ButtonSpec spec)
        {
            var button = Create("Bricscad.Windows.RibbonButton");
            SetProperty(button, "Id", spec.Id);
            SetProperty(button, "Name", spec.Text.Replace("\n", " "));
            SetProperty(button, "Text", spec.Text);
            SetProperty(button, "ShowText", true);
            SetProperty(button, "ShowImage", true);
            SetProperty(button, "CommandParameter", spec.Command);
            SetProperty(button, "CommandHandler", new CommandHandler());
            SetEnumProperty(button, "Size", "Large");
            SetProperty(button, "Image", RibbonIconFactory.Create(spec.Icon, 16));
            SetProperty(button, "LargeImage", RibbonIconFactory.Create(spec.Icon, 32));
            return button;
        }

        private static void RemoveOwnedPanel(object panels, string sourceId)
        {
            while (true)
            {
                var match = FindPanelBySourceId(panels, sourceId);
                if (match == null) return;
                Remove(panels, match);
            }
        }

        private static object? FindPanelBySourceId(object panels, string sourceId)
        {
            if (!(panels is IEnumerable enumerable)) return null;
            foreach (var panel in enumerable)
            {
                if (panel == null) continue;
                var source = GetProperty(panel, "Source");
                if (source == null) continue;
                if (string.Equals(GetProperty(source, "Id") as string, sourceId, StringComparison.OrdinalIgnoreCase))
                    return panel;
            }
            return null;
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
            if (palette == null)
            {
                servicesType.GetMethod("CreateRibbonPaletteSet", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, null);
                palette = paletteProperty?.GetValue(null, null);
            }

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

        private static void SetEnumProperty(object target, string name, string enumValue)
        {
            var property = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            if (property == null || !property.CanWrite || !property.PropertyType.IsEnum) return;
            try
            {
                property.SetValue(target, Enum.Parse(property.PropertyType, enumValue, true), null);
            }
            catch
            {
                // Host versions may expose a different size enum. Images/text still render.
            }
        }

        private static void Add(object collection, object item)
        {
            var method = collection.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(candidate => candidate.Name == "Add"
                    && candidate.GetParameters().Length == 1
                    && candidate.GetParameters()[0].ParameterType.IsAssignableFrom(item.GetType()));
            if (method == null) throw new InvalidOperationException("Ribbon collection does not expose a compatible Add method.");
            method.Invoke(collection, new[] { item });
        }

        private static void Remove(object collection, object item)
        {
            var method = collection.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(candidate => candidate.Name == "Remove"
                    && candidate.GetParameters().Length == 1
                    && candidate.GetParameters()[0].ParameterType.IsAssignableFrom(item.GetType()));
            if (method == null) throw new InvalidOperationException("Ribbon collection does not expose a compatible Remove method.");
            method.Invoke(collection, new[] { item });
        }

        private sealed class CommandHandler : ICommand
        {
            public bool CanExecute(object? parameter) => parameter is string command && !string.IsNullOrWhiteSpace(command);

            public void Execute(object? parameter)
            {
                if (!(parameter is string command) || string.IsNullOrWhiteSpace(command)) return;
                var normalized = command.Trim();
                if (normalized.Length == 0) return;
                Application.DocumentManager.MdiActiveDocument?.SendStringToExecute(normalized + " ", true, false, false);
            }

            public event EventHandler? CanExecuteChanged { add { } remove { } }
        }
    }
}
