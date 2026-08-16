using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Application = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.Ribbon
{
    /// <summary>
    /// Reconciles only the QS3D-owned TOOL tab into the compact owner-reference BLT3D layout.
    /// The model/workspace below the Ribbon is intentionally untouched.
    /// </summary>
    internal static class BltToolRibbonAugmenter
    {
        private const string AssemblyName = "BrxMgd";
        private const string ToolTabId = "QS3D_TOOL";
        private const string ReferencePrefix = "QS3D_TOOL_BLT_";

        private static readonly string[] LegacyPanelSourceIds =
        {
            "QS3D_TOOL_INSPECT_PANEL_SOURCE",
            "QS3D_TOOL_FOCUS_PANEL_SOURCE",
            "QS3D_TOOL_VIEW_PANEL_SOURCE",
            "QS3D_TOOL_QUALITY_PANEL_SOURCE"
        };

        private static bool _initialized;

        private enum IconKind
        {
            PileDown,
            LeanConcrete,
            Excavation,
            SlabOpening,
            McpSettings,
            McpDocs,
            AiDashboard,
            Connection,
            CadToBlt
        }

        private sealed class ButtonSpec
        {
            public ButtonSpec(string id, string text, string command, IconKind icon)
            {
                Id = ReferencePrefix + id;
                Text = text;
                Command = command;
                Icon = icon;
            }

            public string Id { get; }
            public string Text { get; }
            public string Command { get; }
            public IconKind Icon { get; }
        }

        public static bool TryInitialize()
        {
            if (_initialized)
                return true;

            try
            {
                var control = FindRibbonControl();
                if (control == null)
                    return false;

                var tabs = GetProperty(control, "Tabs");
                var toolTab = tabs == null ? null : FindById(tabs, ToolTabId);
                if (toolTab == null)
                    return false;

                var panels = GetProperty(toolTab, "Panels");
                if (panels == null)
                    return false;

                // Build the complete replacement before touching the working bootstrap surface.
                // If BricsCAD does not expose one of the expected Ribbon controls yet, the
                // coordinator can retry without leaving TOOL partially rebuilt.
                var replacement = new[]
                {
                    BuildPilePanel(),
                    BuildFoundationPanel(),
                    BuildSlabPanel(),
                    BuildMcpPanel(),
                    BuildAutocadPanel()
                };

                var fallbackPanels = CaptureLegacyPanels(panels);
                RemoveReferencePanelsBestEffort(panels);
                RemovePanels(panels, fallbackPanels);

                try
                {
                    foreach (var panel in replacement)
                        Add(panels, panel);
                }
                catch
                {
                    RemoveReferencePanelsBestEffort(panels);
                    RestorePanelsBestEffort(panels, fallbackPanels);
                    return false;
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

        private static object BuildPilePanel()
        {
            var source = CreatePanelSource(ReferencePrefix + "PILE_PANEL_SOURCE", "Cọc");
            var items = GetProperty(source, "Items")
                        ?? throw new InvalidOperationException("RibbonPanelSource.Items was not available.");

            var column = Create("Bricscad.Windows.RibbonRowPanel");
            var columnItems = GetProperty(column, "Items")
                              ?? throw new InvalidOperationException("RibbonRowPanel.Items was not available.");

            var embed = Create("Bricscad.Windows.RibbonTextBox");
            SetProperty(embed, "Id", ReferencePrefix + "PILE_EMBED_MM");
            SetProperty(embed, "Name", "Ngàm vào đài a (mm)");
            SetProperty(embed, "Text", "Ngàm vào đài a (mm)");
            SetProperty(embed, "TextValue", "1000");
            SetProperty(embed, "ShowText", true);
            SetProperty(embed, "ShowImage", false);
            SetProperty(embed, "Width", 176d);
            SetEnumProperty(embed, "Size", "Standard");
            Add(columnItems, embed);
            Add(columnItems, Create("Bricscad.Windows.RibbonRowBreak"));
            Add(columnItems, CreateButton(new ButtonSpec(
                "PILE_LOWER",
                "Hạ cọc xuống đáy đài",
                "QS3D",
                IconKind.PileDown)));

            Add(items, column);
            return CreatePanel(source);
        }

        private static object BuildFoundationPanel()
        {
            var source = CreatePanelSource(ReferencePrefix + "FOUNDATION_PANEL_SOURCE", "Móng");
            var items = GetProperty(source, "Items")
                        ?? throw new InvalidOperationException("RibbonPanelSource.Items was not available.");

            Add(items, CreateStackColumn(
                new ButtonSpec("LEAN_CONCRETE", "Bê tông lót", "QS3D", IconKind.LeanConcrete),
                new ButtonSpec("EXCAVATE_FOUNDATION", "Đào hố móng", "QS3DEARTHWORK", IconKind.Excavation)));
            return CreatePanel(source);
        }

        private static object BuildSlabPanel()
        {
            var source = CreatePanelSource(ReferencePrefix + "SLAB_PANEL_SOURCE", "Sàn");
            var items = GetProperty(source, "Items")
                        ?? throw new InvalidOperationException("RibbonPanelSource.Items was not available.");

            Add(items, CreateStackColumn(
                new ButtonSpec("SLAB_OPENING", "Lỗ mở → Sàn", "QS3DDRAWSLABOPEN", IconKind.SlabOpening)));
            return CreatePanel(source);
        }

        private static object BuildMcpPanel()
        {
            var source = CreatePanelSource(ReferencePrefix + "MCP_PANEL_SOURCE", "MCP (AI)");
            var items = GetProperty(source, "Items")
                        ?? throw new InvalidOperationException("RibbonPanelSource.Items was not available.");

            Add(items, CreateStackColumn(
                new ButtonSpec("MCP_SETTINGS", "Cài đặt MCP", "QS3D", IconKind.McpSettings),
                new ButtonSpec("MCP_DOCS", "Tài liệu MCP", "QS3D", IconKind.McpDocs),
                new ButtonSpec("AI_DASHBOARD", "Bảng điều khiển AI", "QS3D", IconKind.AiDashboard)));

            Add(items, CreateStackColumn(
                new ButtonSpec("MCP_CONNECTION", "Kiểm tra kết nối", "QS3D", IconKind.Connection)));
            return CreatePanel(source);
        }

        private static object BuildAutocadPanel()
        {
            var source = CreatePanelSource(ReferencePrefix + "AUTOCAD_PANEL_SOURCE", "AutoCAD");
            var items = GetProperty(source, "Items")
                        ?? throw new InvalidOperationException("RibbonPanelSource.Items was not available.");

            Add(items, CreateStackColumn(
                new ButtonSpec("CAD_TO_BLT", "CAD → BLT", "QS3DRECOGNIZE", IconKind.CadToBlt)));
            return CreatePanel(source);
        }

        private static object CreatePanelSource(string sourceId, string title)
        {
            var source = Create("Bricscad.Windows.RibbonPanelSource");
            SetProperty(source, "Id", sourceId);
            SetProperty(source, "Name", title);
            SetProperty(source, "Title", title);
            return source;
        }

        private static object CreatePanel(object source)
        {
            var panel = Create("Bricscad.Windows.RibbonPanel");
            SetProperty(panel, "Source", source);
            return panel;
        }

        private static object CreateStackColumn(params ButtonSpec[] specs)
        {
            var rowPanel = Create("Bricscad.Windows.RibbonRowPanel");
            var items = GetProperty(rowPanel, "Items")
                        ?? throw new InvalidOperationException("RibbonRowPanel.Items was not available.");

            for (var index = 0; index < specs.Length; index++)
            {
                Add(items, CreateButton(specs[index]));
                if (index + 1 < specs.Length)
                    Add(items, Create("Bricscad.Windows.RibbonRowBreak"));
            }

            return rowPanel;
        }

        private static object CreateButton(ButtonSpec spec)
        {
            var button = Create("Bricscad.Windows.RibbonButton");
            SetProperty(button, "Id", spec.Id);
            SetProperty(button, "Name", spec.Text);
            SetProperty(button, "Text", spec.Text);
            SetProperty(button, "ShowText", true);
            SetProperty(button, "ShowImage", true);
            SetProperty(button, "CommandParameter", spec.Command);
            SetProperty(button, "CommandHandler", new ToolRibbonCommandHandler());
            SetEnumProperty(button, "Size", "Standard");

            var icon = CreateIcon(spec.Icon);
            SetProperty(button, "Image", icon);
            SetProperty(button, "LargeImage", icon);
            return button;
        }

        private static ImageSource CreateIcon(IconKind kind)
        {
            // The owner screenshot uses compact blue/white 16 px glyphs. Draw vector artwork
            // in a 32-unit viewport so BricsCAD can scale it cleanly at different DPI values.
            var blue = FrozenBrush(Color.FromRgb(31, 133, 239));
            var blueDark = FrozenBrush(Color.FromRgb(10, 73, 161));
            var blueSoft = FrozenBrush(Color.FromRgb(105, 183, 255));
            var pale = FrozenBrush(Color.FromRgb(224, 240, 255));
            var ink = FrozenBrush(Color.FromRgb(46, 59, 76));
            var green = FrozenBrush(Color.FromRgb(53, 190, 118));
            var group = new DrawingGroup();

            switch (kind)
            {
                case IconKind.PileDown:
                    group.Children.Add(Fill(blueSoft, new RectangleGeometry(new Rect(7, 5, 18, 4), 1, 1)));
                    group.Children.Add(Fill(blue, new RectangleGeometry(new Rect(14, 8, 4, 14), 1, 1)));
                    group.Children.Add(Fill(blueDark, Geometry.Parse("M9,20 L16,29 23,20 19,20 19,16 13,16 13,20 Z")));
                    group.Children.Add(Stroke(pale, 1.2, new LineGeometry(new Point(9, 10), new Point(23, 10))));
                    break;

                case IconKind.LeanConcrete:
                    group.Children.Add(Fill(pale, Geometry.Parse("M5,17 L12,12 28,12 21,17 Z")));
                    group.Children.Add(Fill(blue, Geometry.Parse("M5,17 L21,17 21,24 5,24 Z")));
                    group.Children.Add(Fill(blueDark, Geometry.Parse("M21,17 L28,12 28,19 21,24 Z")));
                    group.Children.Add(Stroke(ink, 1.3, new LineGeometry(new Point(4, 26), new Point(29, 26))));
                    break;

                case IconKind.Excavation:
                    group.Children.Add(Stroke(blueDark, 2.4, Geometry.Parse("M5,24 C9,16 12,12 16,8")));
                    group.Children.Add(Fill(blue, Geometry.Parse("M14,7 L21,5 27,13 21,19 16,16 20,12 Z")));
                    group.Children.Add(Stroke(ink, 1.4, Geometry.Parse("M4,27 L11,23 17,27 24,23 29,26")));
                    break;

                case IconKind.SlabOpening:
                    group.Children.Add(Fill(blueSoft, Geometry.Parse("M4,11 L16,5 28,11 16,17 Z")));
                    group.Children.Add(Fill(blue, Geometry.Parse("M4,11 L16,17 16,27 4,21 Z")));
                    group.Children.Add(Fill(blueDark, Geometry.Parse("M28,11 L16,17 16,27 28,21 Z")));
                    group.Children.Add(Fill(ink, Geometry.Parse("M11,12 L16,9 21,12 16,15 Z")));
                    group.Children.Add(Stroke(pale, 1.8, new LineGeometry(new Point(21, 6), new Point(28, 6))));
                    group.Children.Add(Fill(pale, Geometry.Parse("M29,6 L25,3 25,9 Z")));
                    break;

                case IconKind.McpSettings:
                    group.Children.Add(Stroke(blue, 2.2, Geometry.Parse("M5,18 C7,12 11,9 16,9 C21,9 25,12 27,18")));
                    group.Children.Add(Fill(blueSoft, new EllipseGeometry(new Point(7, 18), 3, 3)));
                    group.Children.Add(Fill(blueSoft, new EllipseGeometry(new Point(25, 18), 3, 3)));
                    group.Children.Add(Fill(blueDark, new EllipseGeometry(new Point(16, 18), 5, 5)));
                    group.Children.Add(Fill(pale, new EllipseGeometry(new Point(16, 18), 2, 2)));
                    break;

                case IconKind.McpDocs:
                    group.Children.Add(Fill(pale, new RectangleGeometry(new Rect(7, 4, 18, 24), 1.5, 1.5)));
                    group.Children.Add(Stroke(blueDark, 1.5, new RectangleGeometry(new Rect(7, 4, 18, 24), 1.5, 1.5)));
                    group.Children.Add(Fill(blue, new RectangleGeometry(new Rect(10, 8, 12, 4), 0.7, 0.7)));
                    group.Children.Add(Stroke(blue, 1.4, new LineGeometry(new Point(10, 16), new Point(22, 16))));
                    group.Children.Add(Stroke(blue, 1.4, new LineGeometry(new Point(10, 20), new Point(22, 20))));
                    group.Children.Add(Stroke(blue, 1.4, new LineGeometry(new Point(10, 24), new Point(19, 24))));
                    break;

                case IconKind.AiDashboard:
                    group.Children.Add(Fill(blue, new RectangleGeometry(new Rect(4, 6, 24, 19), 3, 3)));
                    group.Children.Add(Fill(pale, new EllipseGeometry(new Point(11, 14), 2, 2)));
                    group.Children.Add(Fill(pale, new EllipseGeometry(new Point(21, 14), 2, 2)));
                    group.Children.Add(Stroke(pale, 1.8, Geometry.Parse("M10,19 C13,22 19,22 22,19")));
                    group.Children.Add(Fill(blueDark, Geometry.Parse("M11,25 L8,30 16,25 Z")));
                    break;

                case IconKind.Connection:
                    group.Children.Add(Stroke(blueSoft, 2.0, Geometry.Parse("M5,13 C11,7 21,7 27,13")));
                    group.Children.Add(Stroke(blue, 2.3, Geometry.Parse("M9,17 C13,13 19,13 23,17")));
                    group.Children.Add(Stroke(blueDark, 2.6, Geometry.Parse("M13,21 C15,19 17,19 19,21")));
                    group.Children.Add(Fill(green, new EllipseGeometry(new Point(16, 25), 3, 3)));
                    break;

                case IconKind.CadToBlt:
                    group.Children.Add(Stroke(blueDark, 2.0, new LineGeometry(new Point(4, 25), new Point(4, 7))));
                    group.Children.Add(Stroke(blue, 2.2, Geometry.Parse("M7,22 L13,9 19,19")));
                    group.Children.Add(Stroke(blueSoft, 1.8, new RectangleGeometry(new Rect(20, 6, 8, 14), 1, 1)));
                    group.Children.Add(Stroke(pale, 2.0, new LineGeometry(new Point(10, 26), new Point(25, 26))));
                    group.Children.Add(Fill(blue, Geometry.Parse("M28,26 L23,22 23,30 Z")));
                    break;
            }

            group.Freeze();
            var image = new DrawingImage(group);
            image.Freeze();
            return image;
        }

        private static SolidColorBrush FrozenBrush(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        private static GeometryDrawing Fill(Brush brush, Geometry geometry) =>
            new GeometryDrawing(brush, null, geometry);

        private static GeometryDrawing Stroke(Brush brush, double thickness, Geometry geometry)
        {
            var pen = new Pen(brush, thickness);
            pen.Freeze();
            return new GeometryDrawing(null, pen, geometry);
        }

        private static List<object> CaptureLegacyPanels(object panels)
        {
            var captured = new List<object>();
            if (!(panels is IEnumerable enumerable))
                return captured;

            foreach (var panel in enumerable)
            {
                if (panel == null)
                    continue;
                var source = GetProperty(panel, "Source");
                var sourceId = source == null ? null : GetProperty(source, "Id") as string;
                if (sourceId != null && LegacyPanelSourceIds.Any(id =>
                        string.Equals(sourceId, id, StringComparison.OrdinalIgnoreCase)))
                    captured.Add(panel);
            }

            return captured;
        }

        private static void RemoveReferencePanelsBestEffort(object panels)
        {
            try
            {
                var matches = new List<object>();
                if (panels is IEnumerable enumerable)
                {
                    foreach (var panel in enumerable)
                    {
                        if (panel == null)
                            continue;
                        var source = GetProperty(panel, "Source");
                        var sourceId = source == null ? null : GetProperty(source, "Id") as string;
                        if (sourceId != null && sourceId.StartsWith(ReferencePrefix, StringComparison.OrdinalIgnoreCase))
                            matches.Add(panel);
                    }
                }

                foreach (var panel in matches)
                {
                    try { Remove(panels, panel); } catch { }
                }
            }
            catch
            {
                // Best-effort cleanup. The initialization coordinator will retry if needed.
            }
        }

        private static void RemovePanels(object panels, IEnumerable<object> toRemove)
        {
            foreach (var panel in toRemove)
                Remove(panels, panel);
        }

        private static void RestorePanelsBestEffort(object panels, IEnumerable<object> fallbackPanels)
        {
            foreach (var panel in fallbackPanels)
            {
                try { Add(panels, panel); } catch { }
            }
        }

        private static object? FindById(object collection, string id)
        {
            if (!(collection is IEnumerable enumerable))
                return null;

            foreach (var item in enumerable)
            {
                if (item == null)
                    continue;
                if (string.Equals(GetProperty(item, "Id") as string, id, StringComparison.OrdinalIgnoreCase))
                    return item;
            }

            return null;
        }

        private static object? FindRibbonControl()
        {
            var servicesType = Type.GetType("Bricscad.Ribbon.RibbonServices, " + AssemblyName, false);
            if (servicesType == null)
                return null;

            var paletteProperty = servicesType.GetProperty("RibbonPaletteSet", BindingFlags.Public | BindingFlags.Static);
            var palette = paletteProperty?.GetValue(null, null);
            if (palette == null)
            {
                servicesType.GetMethod("CreateRibbonPaletteSet", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, null);
                palette = paletteProperty?.GetValue(null, null);
            }

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

        private static object Create(string fullName) =>
            Activator.CreateInstance(Type.GetType(fullName + ", " + AssemblyName, true)!)
            ?? throw new InvalidOperationException("Cannot create " + fullName);

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

        private static void SetEnumProperty(object target, string name, string enumValue)
        {
            var property = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            if (property == null || !property.CanWrite || !property.PropertyType.IsEnum)
                return;

            try
            {
                property.SetValue(target, Enum.Parse(property.PropertyType, enumValue, true), null);
            }
            catch
            {
                // Host version may expose a different enum; text/image still remain usable.
            }
        }

        private static void Add(object collection, object item)
        {
            var method = collection.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(candidate => candidate.Name == "Add"
                    && candidate.GetParameters().Length == 1
                    && candidate.GetParameters()[0].ParameterType.IsAssignableFrom(item.GetType()));

            if (method == null)
                throw new InvalidOperationException("Collection does not expose a compatible Add method.");

            method.Invoke(collection, new[] { item });
        }

        private static void Remove(object collection, object item)
        {
            var method = collection.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(candidate => candidate.Name == "Remove"
                    && candidate.GetParameters().Length == 1
                    && candidate.GetParameters()[0].ParameterType.IsAssignableFrom(item.GetType()));

            if (method == null)
                throw new InvalidOperationException("Collection does not expose a compatible Remove method.");

            method.Invoke(collection, new[] { item });
        }

        private sealed class ToolRibbonCommandHandler : ICommand
        {
            public bool CanExecute(object? parameter) =>
                parameter is string command && !string.IsNullOrWhiteSpace(command);

            public void Execute(object? parameter)
            {
                if (!(parameter is string command) || string.IsNullOrWhiteSpace(command))
                    return;

                var document = Application.DocumentManager.MdiActiveDocument;
                if (document == null)
                    return;

                try { document.SendStringToExecute(command.Trim() + " ", true, false, false); }
                catch { }
            }

            public event EventHandler? CanExecuteChanged
            {
                add { }
                remove { }
            }
        }
    }
}
