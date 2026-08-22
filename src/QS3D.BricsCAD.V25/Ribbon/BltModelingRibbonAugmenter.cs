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
    /// Reconciles the QS3D-owned MODELING tab to the owner-provided BLT3D reference.
    /// The layout intentionally uses large lead actions plus compact three-row columns so the
    /// panel rhythm, labels and command order stay familiar while all behavior remains routed
    /// through existing BricsCAD/QS3D commands.
    /// </summary>
    internal static class BltModelingRibbonAugmenter
    {
        private const string AssemblyName = "BrxMgd";
        private const string ModelingTabId = "QS3D_MODELING";
        private const string OwnedPrefix = "QS3D_MODELING_";
        private const string ReferencePrefix = "QS3D_MODELING_BLT_";

        private static bool _initialized;

        private enum IconKind
        {
            Material,
            SteelProfile,
            Detail,
            Plane,
            Line,
            Polyline,
            Rectangle,
            Circle,
            Arc,
            JoinPolyline,
            Offset,
            Move,
            Copy,
            MoveZ,
            Extrude,
            Sweep,
            Loft,
            Family,
            Union,
            Subtract,
            Intersect
        }

        private sealed class ButtonSpec
        {
            public ButtonSpec(
                string id,
                string text,
                string command,
                IconKind icon,
                bool large = false,
                string description = "")
            {
                Id = id;
                Text = text;
                Command = command;
                Icon = icon;
                Large = large;
                Description = description;
            }

            public string Id { get; }
            public string Text { get; }
            public string Command { get; }
            public IconKind Icon { get; }
            public bool Large { get; }
            public string Description { get; }
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
                var modelingTab = tabs == null ? null : FindById(tabs, ModelingTabId);
                if (modelingTab == null)
                    return false;

                var panels = GetProperty(modelingTab, "Panels");
                if (panels == null)
                    return false;

                // Construct every replacement panel before mutating the live Ribbon. If any host
                // API shape differs, the bootstrap MODELING fallback remains untouched.
                var replacementPanels = BuildReferencePanels();
                var fallbackPanels = CaptureOwnedPanels(panels);

                try
                {
                    RemovePanels(panels, fallbackPanels);
                    foreach (var panel in replacementPanels)
                        Add(panels, panel);
                }
                catch
                {
                    // Do not strand the user with a half-built tab. Restore the exact prior QS3D
                    // panels and let RibbonInitializationCoordinator retry through its normal path.
                    RemoveReferencePanelsBestEffort(panels);
                    RestorePanelsBestEffort(panels, fallbackPanels);
                    throw;
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

        private static IReadOnlyList<object> BuildReferencePanels()
        {
            return new[]
            {
                BuildLargePanel(
                    ReferencePrefix + "MATERIAL_PANEL_SOURCE",
                    "Vật liệu",
                    Large("MATERIAL", "Vật\nliệu", "_.MATERIALS", IconKind.Material,
                        "Mở Drawing Explorer tại Materials để xem, tạo và chỉnh vật liệu.")),

                BuildLargePanel(
                    ReferencePrefix + "STEEL_PANEL_SOURCE",
                    "Kết cấu thép",
                    Large("STEEL_PROFILE", "Mặt cắt\nthép", "_.BIMPROFILES", IconKind.SteelProfile,
                        "Mở BIM Profiles để chọn, tạo hoặc chỉnh profile kết cấu thép."),
                    Large("CREATE_DETAIL", "Tạo chi\ntiết", "_.BIMCREATEDETAIL", IconKind.Detail,
                        "Tạo BIM 3D detail bằng BIMCREATEDETAIL.")),

                BuildLargePanel(
                    ReferencePrefix + "PLANE_PANEL_SOURCE",
                    "Mặt phẳng",
                    Large("PLANE_XY", "Mặt\nXY", "_.UCS _World", IconKind.Plane,
                        "Đưa UCS về World để làm việc trên mặt XY chuẩn.")),

                BuildStackPanel(
                    ReferencePrefix + "SKETCH_PANEL_SOURCE",
                    "Vẽ phác",
                    Column(
                        Standard("LINE", "Đường", "_.LINE", IconKind.Line),
                        Standard("POLYLINE", "Polyline", "_.PLINE", IconKind.Polyline),
                        Standard("RECTANGLE", "Chữ nhật", "_.RECTANG", IconKind.Rectangle)),
                    Column(
                        Standard("CIRCLE", "Tròn", "_.CIRCLE", IconKind.Circle),
                        Standard("ARC", "Cung", "_.ARC", IconKind.Arc))),

                BuildStackPanel(
                    ReferencePrefix + "EDIT_PANEL_SOURCE",
                    "Chỉnh sửa",
                    Column(
                        Standard("JOIN_POLYLINE", "Nối polyline", "_.JOIN", IconKind.JoinPolyline,
                            "JOIN nối các đoạn/polyline tương thích thành một chuỗi liên tục."),
                        Standard("OFFSET", "Offset", "_.OFFSET", IconKind.Offset),
                        Standard("MOVE", "Di chuyển", "_.MOVE", IconKind.Move)),
                    Column(
                        Standard("COPY", "Sao chép", "_.COPY", IconKind.Copy),
                        Standard("MOVE_Z", "Theo phương Z", "_.MOVE", IconKind.MoveZ,
                            "MOVE theo trục Z; nhập displacement dạng @0,0,<ΔZ> để khóa đúng phương Z."))),

                BuildStackPanel(
                    ReferencePrefix + "BUILD3D_PANEL_SOURCE",
                    "Dựng 3D",
                    Column(
                        Standard("EXTRUDE", "Extrude", "_.EXTRUDE", IconKind.Extrude),
                        Standard("SWEEP", "Sweep", "_.SWEEP", IconKind.Sweep),
                        Standard("LOFT", "Loft", "_.LOFT", IconKind.Loft))),

                BuildStackPanel(
                    ReferencePrefix + "COMPONENT_PANEL_SOURCE",
                    "Cấu kiện",
                    Column(
                        Standard("ATTACH_FAMILY", "Gắn vào Family", "QS3DFAMILIES", IconKind.Family,
                            "Mở Family Manager để gắn/chỉnh Family, Type và semantic assignment."))),

                BuildStackPanel(
                    ReferencePrefix + "BOOLEAN_PANEL_SOURCE",
                    "Cắt khối",
                    Column(
                        Standard("UNION", "Union", "_.UNION", IconKind.Union),
                        Standard("SUBTRACT", "Subtract", "_.SUBTRACT", IconKind.Subtract),
                        Standard("INTERSECT", "Intersect", "_.INTERSECT", IconKind.Intersect)))
            };
        }

        private static ButtonSpec Large(
            string id,
            string text,
            string command,
            IconKind icon,
            string description = "") =>
            new ButtonSpec(ReferencePrefix + id, text, command, icon, large: true, description: description);

        private static ButtonSpec Standard(
            string id,
            string text,
            string command,
            IconKind icon,
            string description = "") =>
            new ButtonSpec(ReferencePrefix + id, text, command, icon, large: false, description: description);

        private static ButtonSpec[] Column(params ButtonSpec[] buttons) => buttons;

        private static object BuildLargePanel(string sourceId, string title, params ButtonSpec[] specs)
        {
            var source = CreatePanelSource(sourceId, title);
            var items = GetProperty(source, "Items")
                        ?? throw new InvalidOperationException("RibbonPanelSource.Items was not available.");

            foreach (var spec in specs)
                Add(items, CreateButton(spec));

            return CreatePanel(source);
        }

        private static object BuildStackPanel(string sourceId, string title, params ButtonSpec[][] columns)
        {
            var source = CreatePanelSource(sourceId, title);
            var items = GetProperty(source, "Items")
                        ?? throw new InvalidOperationException("RibbonPanelSource.Items was not available.");

            foreach (var column in columns)
                Add(items, CreateStackColumn(column));

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

        private static object CreateStackColumn(IReadOnlyList<ButtonSpec> specs)
        {
            var rowPanel = Create("Bricscad.Windows.RibbonRowPanel");
            var items = GetProperty(rowPanel, "Items")
                        ?? throw new InvalidOperationException("RibbonRowPanel.Items was not available.");

            for (var i = 0; i < specs.Count; i++)
            {
                Add(items, CreateButton(specs[i]));
                if (i + 1 < specs.Count)
                    Add(items, Create("Bricscad.Windows.RibbonRowBreak"));
            }

            return rowPanel;
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
            SetProperty(button, "CommandHandler", new ModelingRibbonCommandHandler());
            SetEnumProperty(button, "Size", spec.Large ? "Large" : "Standard");

            if (!string.IsNullOrWhiteSpace(spec.Description))
            {
                SetProperty(button, "Description", spec.Description);
                SetProperty(button, "ToolTip", spec.Description);
            }

            var icon = CreateIcon(spec.Icon);
            SetProperty(button, "Image", icon);
            SetProperty(button, "LargeImage", icon);
            return button;
        }

        private static ImageSource CreateIcon(IconKind kind)
        {
            var blue = FrozenBrush(Color.FromRgb(31, 126, 235));
            var blueDark = FrozenBrush(Color.FromRgb(14, 78, 170));
            var blueSoft = FrozenBrush(Color.FromRgb(123, 188, 255));
            var pale = FrozenBrush(Color.FromRgb(218, 236, 255));
            var ink = FrozenBrush(Color.FromRgb(54, 65, 80));
            var danger = FrozenBrush(Color.FromRgb(224, 69, 69));
            var group = new DrawingGroup();

            switch (kind)
            {
                case IconKind.Material:
                    group.Children.Add(Fill(blueDark, Geometry.Parse("M4,9 L16,3 28,9 16,15 Z")));
                    group.Children.Add(Fill(blue, Geometry.Parse("M4,15 L16,9 28,15 16,21 Z")));
                    group.Children.Add(Fill(blueSoft, Geometry.Parse("M4,21 L16,15 28,21 16,28 Z")));
                    break;

                case IconKind.SteelProfile:
                    group.Children.Add(Stroke(blue, 2.8, Geometry.Parse("M4,25 L10,9 L18,20 L27,5")));
                    group.Children.Add(Fill(blueDark, new EllipseGeometry(new Point(10, 9), 2.2, 2.2)));
                    group.Children.Add(Fill(blueDark, new EllipseGeometry(new Point(18, 20), 2.2, 2.2)));
                    group.Children.Add(Stroke(ink, 1.5, new LineGeometry(new Point(4, 28), new Point(28, 28))));
                    break;

                case IconKind.Detail:
                    group.Children.Add(Stroke(blue, 2.6, new RectangleGeometry(new Rect(5, 7, 22, 18), 1, 1)));
                    group.Children.Add(Stroke(blueDark, 2.1, new LineGeometry(new Point(5, 13), new Point(27, 13))));
                    group.Children.Add(Fill(pale, new RectangleGeometry(new Rect(9, 16, 14, 6), 1, 1)));
                    break;

                case IconKind.Plane:
                    group.Children.Add(Fill(blueSoft, Geometry.Parse("M4,21 L12,8 29,12 21,26 Z")));
                    group.Children.Add(Stroke(blueDark, 2.2, Geometry.Parse("M4,21 L12,8 29,12 21,26 Z")));
                    group.Children.Add(Stroke(pale, 1.3, new LineGeometry(new Point(10, 18), new Point(24, 21))));
                    break;

                case IconKind.Line:
                    DrawLineIcon(group, blue, blueDark);
                    break;

                case IconKind.Polyline:
                    group.Children.Add(Stroke(blue, 2.6, Geometry.Parse("M4,25 L10,9 18,19 28,6")));
                    group.Children.Add(Fill(blueDark, new EllipseGeometry(new Point(10, 9), 2.2, 2.2)));
                    group.Children.Add(Fill(blueDark, new EllipseGeometry(new Point(18, 19), 2.2, 2.2)));
                    break;

                case IconKind.Rectangle:
                    group.Children.Add(Stroke(blue, 2.5, new RectangleGeometry(new Rect(5, 7, 22, 18), 1, 1)));
                    break;

                case IconKind.Circle:
                    group.Children.Add(Stroke(blue, 2.5, new EllipseGeometry(new Point(16, 16), 10, 10)));
                    break;

                case IconKind.Arc:
                    group.Children.Add(Stroke(blue, 2.7, Geometry.Parse("M5,24 C8,8 24,5 28,20")));
                    break;

                case IconKind.JoinPolyline:
                    group.Children.Add(Stroke(blue, 2.7, Geometry.Parse("M4,9 L13,16 M28,9 L19,16")));
                    group.Children.Add(Stroke(blueDark, 2.7, Geometry.Parse("M13,16 L19,16")));
                    group.Children.Add(Fill(blueDark, new EllipseGeometry(new Point(16, 16), 2.5, 2.5)));
                    break;

                case IconKind.Offset:
                    group.Children.Add(Stroke(blue, 2.4, Geometry.Parse("M5,24 C8,12 17,7 27,8")));
                    group.Children.Add(Stroke(blueSoft, 2.4, Geometry.Parse("M4,17 C8,7 17,3 27,4")));
                    break;

                case IconKind.Move:
                    DrawMoveIcon(group, blue);
                    break;

                case IconKind.Copy:
                    group.Children.Add(Fill(pale, new RectangleGeometry(new Rect(10, 5, 17, 17), 1, 1)));
                    group.Children.Add(Stroke(blueDark, 1.8, new RectangleGeometry(new Rect(10, 5, 17, 17), 1, 1)));
                    group.Children.Add(Fill(blue, new RectangleGeometry(new Rect(5, 10, 17, 17), 1, 1)));
                    break;

                case IconKind.MoveZ:
                    group.Children.Add(Stroke(blue, 2.7, new LineGeometry(new Point(16, 27), new Point(16, 5))));
                    group.Children.Add(Fill(blue, Geometry.Parse("M16,3 L11,10 21,10 Z")));
                    group.Children.Add(Stroke(blueDark, 2.0, Geometry.Parse("M8,25 L24,25")));
                    group.Children.Add(Stroke(blueDark, 2.0, Geometry.Parse("M22,12 L27,12 22,20 27,20")));
                    break;

                case IconKind.Extrude:
                    group.Children.Add(Stroke(blue, 2.1, new RectangleGeometry(new Rect(5, 17, 12, 10), 1, 1)));
                    group.Children.Add(Stroke(blueSoft, 2.1, new RectangleGeometry(new Rect(15, 6, 12, 10), 1, 1)));
                    group.Children.Add(Stroke(blueDark, 1.6, Geometry.Parse("M5,17 L15,6 M17,17 L27,6 M17,27 L27,16")));
                    break;

                case IconKind.Sweep:
                    group.Children.Add(Stroke(blue, 2.4, Geometry.Parse("M6,25 C8,9 19,7 28,12")));
                    group.Children.Add(Stroke(blueDark, 2.2, new EllipseGeometry(new Point(7, 24), 4, 4)));
                    group.Children.Add(Fill(pale, new EllipseGeometry(new Point(27, 12), 4, 4)));
                    break;

                case IconKind.Loft:
                    group.Children.Add(Stroke(blueDark, 2.1, new EllipseGeometry(new Point(16, 7), 7, 3)));
                    group.Children.Add(Stroke(blue, 2.1, new EllipseGeometry(new Point(16, 25), 11, 4)));
                    group.Children.Add(Stroke(blueSoft, 1.8, Geometry.Parse("M9,7 C8,14 6,19 5,25 M23,7 C24,14 26,19 27,25")));
                    break;

                case IconKind.Family:
                    group.Children.Add(Fill(pale, Geometry.Parse("M5,10 L15,5 25,10 15,15 Z")));
                    group.Children.Add(Fill(blue, Geometry.Parse("M5,10 L15,15 15,27 5,22 Z")));
                    group.Children.Add(Fill(blueDark, Geometry.Parse("M25,10 L15,15 15,27 25,22 Z")));
                    group.Children.Add(Stroke(blueSoft, 2.1, Geometry.Parse("M23,5 L29,5 M26,2 L26,8")));
                    break;

                case IconKind.Union:
                    DrawBooleanIcon(group, blue, blueSoft, blueDark, 0);
                    break;

                case IconKind.Subtract:
                    DrawBooleanIcon(group, blue, pale, danger, 1);
                    break;

                case IconKind.Intersect:
                    DrawBooleanIcon(group, pale, pale, blueDark, 2);
                    break;
            }

            group.Freeze();
            var image = new DrawingImage(group);
            image.Freeze();
            return image;
        }

        private static void DrawLineIcon(DrawingGroup group, Brush blue, Brush blueDark)
        {
            group.Children.Add(Stroke(blue, 2.7, new LineGeometry(new Point(5, 25), new Point(27, 7))));
            group.Children.Add(Fill(blueDark, new EllipseGeometry(new Point(5, 25), 2.2, 2.2)));
            group.Children.Add(Fill(blueDark, new EllipseGeometry(new Point(27, 7), 2.2, 2.2)));
        }

        private static void DrawMoveIcon(DrawingGroup group, Brush blue)
        {
            group.Children.Add(Stroke(blue, 2.4, new LineGeometry(new Point(5, 16), new Point(27, 16))));
            group.Children.Add(Stroke(blue, 2.4, new LineGeometry(new Point(16, 5), new Point(16, 27))));
            group.Children.Add(Fill(blue, Geometry.Parse(
                "M3,16 L9,12 9,20 Z M29,16 L23,12 23,20 Z M16,3 L12,9 20,9 Z M16,29 L12,23 20,23 Z")));
        }

        private static void DrawBooleanIcon(
            DrawingGroup group,
            Brush left,
            Brush right,
            Brush accent,
            int mode)
        {
            group.Children.Add(Fill(left, new EllipseGeometry(new Point(12, 16), 8, 8)));
            group.Children.Add(Fill(right, new EllipseGeometry(new Point(20, 16), 8, 8)));
            group.Children.Add(Stroke(accent, 1.8, new EllipseGeometry(new Point(12, 16), 8, 8)));
            group.Children.Add(Stroke(accent, 1.8, new EllipseGeometry(new Point(20, 16), 8, 8)));

            if (mode == 1)
                group.Children.Add(Stroke(accent, 2.1, new LineGeometry(new Point(17, 12), new Point(23, 20))));
            else if (mode == 2)
                group.Children.Add(Fill(accent, Geometry.Parse("M16,9 C12,12 12,20 16,23 C20,20 20,12 16,9 Z")));
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

        private static List<object> CaptureOwnedPanels(object panels)
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
                if (sourceId != null && sourceId.StartsWith(OwnedPrefix, StringComparison.OrdinalIgnoreCase))
                    captured.Add(panel);
            }

            return captured;
        }

        private static void RemovePanels(object panels, IEnumerable<object> toRemove)
        {
            foreach (var panel in toRemove)
                Remove(panels, panel);
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
                // Rollback is best-effort; initialization remains false and the coordinator retries.
            }
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
                // Host version may expose a different size enum; image/text remain usable.
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

        private sealed class ModelingRibbonCommandHandler : ICommand
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
