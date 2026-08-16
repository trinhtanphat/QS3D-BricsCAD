using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Application = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.Ribbon
{
    /// <summary>
    /// Reconciles only the QS3D-owned VẼ tab into the compact, icon-forward BLT3D-familiar
    /// layout requested by the product owner. Native and third-party Ribbon content is untouched.
    /// Commands deliberately reuse BricsCAD/QS3D operations instead of duplicating geometry logic.
    /// </summary>
    internal static class BltDrawRibbonAugmenter
    {
        private const string AssemblyName = "BrxMgd";
        private const string DrawTabId = "QS3D_DRAW";

        private const string LegacyPrimitivesPanelSourceId = "QS3D_DRAW_PRIMITIVES_PANEL_SOURCE";
        private const string LegacyTransformPanelSourceId = "QS3D_DRAW_TRANSFORM_PANEL_SOURCE";
        private const string LegacyEditPanelSourceId = "QS3D_DRAW_EDIT_PANEL_SOURCE";

        private const string DrawPanelSourceId = "QS3D_DRAW_BLT_DRAW_PANEL_SOURCE";
        private const string ToolsPanelSourceId = "QS3D_DRAW_BLT_TOOLS_PANEL_SOURCE";
        private const string IfcPanelSourceId = "QS3D_DRAW_BLT_IFC_PANEL_SOURCE";

        private static bool _initialized;

        private enum IconKind
        {
            Point,
            Arc,
            Line,
            Rectangle,
            Polyline,
            Circle,
            Boundary,
            Slope,
            Cut,
            Move,
            Rotate,
            Mirror,
            Copy,
            Break,
            Join,
            Measure,
            Corner,
            Tee,
            Import,
            ImportLight,
            Delete,
            Export
        }

        private sealed class DrawButtonSpec
        {
            public DrawButtonSpec(
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
                var drawTab = tabs == null ? null : FindById(tabs, DrawTabId);
                if (drawTab == null)
                    return false;

                var panels = GetProperty(drawTab, "Panels");
                if (panels == null)
                    return false;

                // RibbonBootstrapper owns the fallback text-only panels. Replace only those three
                // plus our own previous reconciliation, leaving unrelated augmenters untouched.
                RemoveOwnedPanel(panels, LegacyPrimitivesPanelSourceId);
                RemoveOwnedPanel(panels, LegacyTransformPanelSourceId);
                RemoveOwnedPanel(panels, LegacyEditPanelSourceId);
                RemoveOwnedPanel(panels, DrawPanelSourceId);
                RemoveOwnedPanel(panels, ToolsPanelSourceId);
                RemoveOwnedPanel(panels, IfcPanelSourceId);

                AddPanel(
                    panels,
                    DrawPanelSourceId,
                    "Vẽ",
                    new DrawButtonSpec("QS3D_DRAW_BLT_POINT", "Điểm", "_.POINT", IconKind.Point),
                    new DrawButtonSpec("QS3D_DRAW_BLT_ARC", "Cung", "_.ARC", IconKind.Arc),
                    new DrawButtonSpec("QS3D_DRAW_BLT_LINE", "Đường thẳng", "_.LINE", IconKind.Line),
                    new DrawButtonSpec("QS3D_DRAW_BLT_RECTANGLE", "Chữ nhật", "_.RECTANG", IconKind.Rectangle),
                    new DrawButtonSpec(
                        "QS3D_DRAW_BLT_TRACE",
                        "Theo nét CAD",
                        "_.PLINE",
                        IconKind.Polyline,
                        description: "Vẽ polyline liên tục bám theo hình học CAD hiện có."),
                    new DrawButtonSpec("QS3D_DRAW_BLT_CIRCLE", "Đường tròn", "_.CIRCLE", IconKind.Circle));

                AddPanel(
                    panels,
                    ToolsPanelSourceId,
                    "Công cụ",
                    new DrawButtonSpec("QS3D_DRAW_BLT_BOUNDARY", "Biên dạng", "_.BOUNDARY", IconKind.Boundary),
                    new DrawButtonSpec(
                        "QS3D_DRAW_BLT_SLAB_SLOPE",
                        "Dốc sàn",
                        "_.ROTATE3D",
                        IconKind.Slope,
                        description: "Nghiêng hình học sàn/solid theo trục và góc bằng ROTATE3D."),
                    new DrawButtonSpec(
                        "QS3D_DRAW_BLT_SLAB_CUT",
                        "Cắt sàn",
                        "QS3DDRAWSLABOPEN",
                        IconKind.Cut,
                        description: "Tạo slabOpen QS3D trên đúng một semantic Slab đang chọn."),
                    new DrawButtonSpec("QS3D_DRAW_BLT_MOVE", "Di chuyển", "_.MOVE", IconKind.Move),
                    new DrawButtonSpec("QS3D_DRAW_BLT_ROTATE", "Xoay", "_.ROTATE", IconKind.Rotate),
                    new DrawButtonSpec("QS3D_DRAW_BLT_MIRROR", "Đối xứng", "_.MIRROR", IconKind.Mirror),
                    new DrawButtonSpec("QS3D_DRAW_BLT_COPY", "Sao chép", "_.COPY", IconKind.Copy),
                    new DrawButtonSpec("QS3D_DRAW_BLT_BREAK", "Chia cấu kiện", "_.BREAK", IconKind.Break),
                    new DrawButtonSpec("QS3D_DRAW_BLT_JOIN", "Nối liền", "_.JOIN", IconKind.Join),
                    new DrawButtonSpec("QS3D_DRAW_BLT_DISTANCE", "Đo khoảng cách", "_.DIST", IconKind.Measure),
                    new DrawButtonSpec(
                        "QS3D_DRAW_BLT_CORNER",
                        "Nối góc",
                        "_.FILLET",
                        IconKind.Corner,
                        description: "Nối/trim hai cạnh tại góc; giữ Shift khi chọn cạnh thứ hai để ép bán kính 0."),
                    new DrawButtonSpec(
                        "QS3D_DRAW_BLT_TEE",
                        "Nối chữ T",
                        "_.EXTEND",
                        IconKind.Tee,
                        description: "Kéo dài cạnh tới cạnh biên để tạo giao chữ T."));

                AddPanel(
                    panels,
                    IfcPanelSourceId,
                    "IFC",
                    new DrawButtonSpec(
                        "QS3D_DRAW_BLT_IFC_IMPORT",
                        "Nhập IFC",
                        "_.IMPORT",
                        IconKind.Import,
                        large: true,
                        description: "Mở Import và chọn IFC/IFCZIP; BricsCAD hiển thị IFC Import Settings trước khi nhập."),
                    new DrawButtonSpec(
                        "QS3D_DRAW_BLT_IFC_IMPORT_LIGHT",
                        "Nhập IFC\n(nhẹ)",
                        "_.IMPORT",
                        IconKind.ImportLight,
                        large: true,
                        description: "Mở IFC Import Settings; chọn profile Optimized for referencing để ưu tiên mesh và giảm dữ liệu không cần thiết."),
                    new DrawButtonSpec(
                        "QS3D_DRAW_BLT_IFC_DELETE",
                        "Xóa IFC",
                        "_.ERASE",
                        IconKind.Delete,
                        large: true,
                        description: "Xóa có chọn lọc các entity IFC; QS3D không tự quét/xóa toàn bộ bản vẽ."),
                    new DrawButtonSpec(
                        "QS3D_DRAW_BLT_IFC_EXPORT",
                        "Xuất IFC",
                        "_.IFCEXPORT",
                        IconKind.Export,
                        large: true,
                        description: "Xuất toàn bộ hoặc selection hiện hành bằng IFCEXPORT của BricsCAD BIM."));

                _initialized = true;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void Reset() => _initialized = false;

        private static void AddPanel(object panels, string sourceId, string title, params DrawButtonSpec[] buttonSpecs)
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

        private static object CreateButton(DrawButtonSpec spec)
        {
            var button = Create("Bricscad.Windows.RibbonButton");
            SetProperty(button, "Id", spec.Id);
            SetProperty(button, "Name", spec.Text.Replace("\n", " "));
            SetProperty(button, "Text", spec.Text);
            SetProperty(button, "ShowText", true);
            SetProperty(button, "ShowImage", true);
            SetProperty(button, "CommandParameter", spec.Command);
            SetProperty(button, "CommandHandler", new DrawRibbonCommandHandler());
            if (!string.IsNullOrWhiteSpace(spec.Description))
            {
                SetProperty(button, "Description", spec.Description);
                SetProperty(button, "ToolTip", spec.Description);
            }
            SetEnumProperty(button, "Size", spec.Large ? "Large" : "Standard");

            var image = CreateIcon(spec.Icon);
            SetProperty(button, "Image", image);
            SetProperty(button, "LargeImage", image);
            return button;
        }

        private static ImageSource CreateIcon(IconKind kind)
        {
            var accent = FrozenBrush(Color.FromRgb(32, 137, 245));
            var accentDark = FrozenBrush(Color.FromRgb(17, 79, 170));
            var light = FrozenBrush(Color.FromRgb(219, 237, 255));
            var ink = FrozenBrush(Color.FromRgb(37, 48, 65));
            var warning = FrozenBrush(Color.FromRgb(230, 65, 65));
            var amber = FrozenBrush(Color.FromRgb(238, 181, 55));
            var group = new DrawingGroup();

            switch (kind)
            {
                case IconKind.Point:
                    group.Children.Add(Fill(accent, new EllipseGeometry(new Point(16, 16), 5, 5)));
                    group.Children.Add(Stroke(light, 1.8, new EllipseGeometry(new Point(16, 16), 10, 10)));
                    break;
                case IconKind.Arc:
                    group.Children.Add(Stroke(accent, 3, Geometry.Parse("M5,24 C8,7 24,5 28,20")));
                    group.Children.Add(Fill(accentDark, new EllipseGeometry(new Point(5, 24), 2.5, 2.5)));
                    group.Children.Add(Fill(accentDark, new EllipseGeometry(new Point(28, 20), 2.5, 2.5)));
                    break;
                case IconKind.Line:
                    group.Children.Add(Stroke(accent, 3, new LineGeometry(new Point(5, 25), new Point(27, 7))));
                    group.Children.Add(Fill(accentDark, new EllipseGeometry(new Point(5, 25), 2.4, 2.4)));
                    group.Children.Add(Fill(accentDark, new EllipseGeometry(new Point(27, 7), 2.4, 2.4)));
                    break;
                case IconKind.Rectangle:
                    group.Children.Add(Stroke(accent, 2.8, new RectangleGeometry(new Rect(5, 7, 22, 18), 1, 1)));
                    group.Children.Add(Fill(light, new RectangleGeometry(new Rect(9, 11, 14, 10), 1, 1)));
                    break;
                case IconKind.Polyline:
                    group.Children.Add(Stroke(accent, 2.8, Geometry.Parse("M4,24 L10,10 18,19 28,6")));
                    group.Children.Add(Fill(accentDark, new EllipseGeometry(new Point(10, 10), 2.2, 2.2)));
                    group.Children.Add(Fill(accentDark, new EllipseGeometry(new Point(18, 19), 2.2, 2.2)));
                    break;
                case IconKind.Circle:
                    group.Children.Add(Stroke(accent, 2.8, new EllipseGeometry(new Point(16, 16), 10, 10)));
                    group.Children.Add(Fill(accentDark, new EllipseGeometry(new Point(16, 16), 2.2, 2.2)));
                    break;
                case IconKind.Boundary:
                    group.Children.Add(Stroke(accent, 2.6, Geometry.Parse("M5,8 L14,4 27,10 24,25 10,28 4,18 Z")));
                    group.Children.Add(Stroke(light, 1.4, Geometry.Parse("M9,11 L15,8 23,12 21,21 12,24 8,18 Z")));
                    break;
                case IconKind.Slope:
                    group.Children.Add(Fill(light, Geometry.Parse("M4,25 L28,25 28,10 Z")));
                    group.Children.Add(Stroke(accent, 2.8, new LineGeometry(new Point(5, 24), new Point(27, 10))));
                    group.Children.Add(Stroke(amber, 2.2, Geometry.Parse("M8,20 L8,26 14,26")));
                    break;
                case IconKind.Cut:
                    group.Children.Add(Stroke(accent, 2.4, new RectangleGeometry(new Rect(5, 7, 22, 18), 1, 1)));
                    group.Children.Add(Stroke(warning, 2.8, new LineGeometry(new Point(9, 26), new Point(24, 5))));
                    group.Children.Add(Fill(warning, new EllipseGeometry(new Point(10, 23), 2.4, 2.4)));
                    group.Children.Add(Fill(warning, new EllipseGeometry(new Point(23, 8), 2.4, 2.4)));
                    break;
                case IconKind.Move:
                    group.Children.Add(Stroke(accent, 2.6, new LineGeometry(new Point(5, 16), new Point(27, 16))));
                    group.Children.Add(Stroke(accent, 2.6, new LineGeometry(new Point(16, 5), new Point(16, 27))));
                    group.Children.Add(Fill(accent, Geometry.Parse("M3,16 L9,12 9,20 Z M29,16 L23,12 23,20 Z M16,3 L12,9 20,9 Z M16,29 L12,23 20,23 Z")));
                    break;
                case IconKind.Rotate:
                    group.Children.Add(Stroke(accent, 2.8, Geometry.Parse("M8,10 C15,3 27,8 27,18 C27,25 20,29 13,27")));
                    group.Children.Add(Fill(accent, Geometry.Parse("M5,8 L13,8 8,15 Z")));
                    break;
                case IconKind.Mirror:
                    group.Children.Add(Stroke(accentDark, 1.8, new LineGeometry(new Point(16, 4), new Point(16, 28))));
                    group.Children.Add(Fill(accent, Geometry.Parse("M5,24 L13,8 13,24 Z")));
                    group.Children.Add(Stroke(light, 2, Geometry.Parse("M27,24 L19,8 19,24 Z")));
                    break;
                case IconKind.Copy:
                    group.Children.Add(Fill(light, new RectangleGeometry(new Rect(10, 6, 16, 16), 1, 1)));
                    group.Children.Add(Stroke(accentDark, 1.8, new RectangleGeometry(new Rect(10, 6, 16, 16), 1, 1)));
                    group.Children.Add(Fill(accent, new RectangleGeometry(new Rect(5, 11, 16, 16), 1, 1)));
                    break;
                case IconKind.Break:
                    group.Children.Add(Stroke(accent, 3, new LineGeometry(new Point(4, 16), new Point(12, 16))));
                    group.Children.Add(Stroke(accent, 3, new LineGeometry(new Point(20, 16), new Point(28, 16))));
                    group.Children.Add(Stroke(warning, 2, Geometry.Parse("M13,9 L18,14 14,18 19,23")));
                    break;
                case IconKind.Join:
                    group.Children.Add(Stroke(accent, 3, Geometry.Parse("M4,10 L13,16 4,22 M28,10 L19,16 28,22")));
                    group.Children.Add(Fill(accentDark, new EllipseGeometry(new Point(16, 16), 3, 3)));
                    break;
                case IconKind.Measure:
                    group.Children.Add(Fill(accent, Geometry.Parse("M5,23 L23,5 28,10 10,28 Z")));
                    group.Children.Add(Stroke(light, 1.2, Geometry.Parse("M10,22 L13,25 M14,18 L17,21 M18,14 L21,17 M22,10 L25,13")));
                    break;
                case IconKind.Corner:
                    group.Children.Add(Stroke(accent, 3, Geometry.Parse("M5,26 L5,12 C5,8 8,5 12,5 L27,5")));
                    group.Children.Add(Fill(accentDark, new EllipseGeometry(new Point(6, 6), 2.2, 2.2)));
                    break;
                case IconKind.Tee:
                    group.Children.Add(Stroke(accent, 3, new LineGeometry(new Point(5, 9), new Point(27, 9))));
                    group.Children.Add(Stroke(accent, 3, new LineGeometry(new Point(16, 9), new Point(16, 27))));
                    group.Children.Add(Fill(accentDark, new EllipseGeometry(new Point(16, 9), 2.4, 2.4)));
                    break;
                case IconKind.Import:
                    DrawIfcBox(group, accent, light, ink);
                    group.Children.Add(Stroke(accentDark, 2.6, new LineGeometry(new Point(16, 3), new Point(16, 16))));
                    group.Children.Add(Fill(accentDark, Geometry.Parse("M10,11 L16,17 22,11 Z")));
                    break;
                case IconKind.ImportLight:
                    DrawIfcBox(group, accent, light, ink);
                    group.Children.Add(Stroke(accentDark, 2.4, new LineGeometry(new Point(16, 3), new Point(16, 15))));
                    group.Children.Add(Fill(accentDark, Geometry.Parse("M11,10 L16,16 21,10 Z")));
                    group.Children.Add(Stroke(amber, 2, Geometry.Parse("M22,4 L25,7 M25,4 L22,7")));
                    break;
                case IconKind.Delete:
                    group.Children.Add(Fill(warning, new RectangleGeometry(new Rect(9, 10, 14, 17), 2, 2)));
                    group.Children.Add(Fill(warning, new RectangleGeometry(new Rect(7, 7, 18, 4), 1, 1)));
                    group.Children.Add(Stroke(light, 1.8, new LineGeometry(new Point(13, 14), new Point(13, 23))));
                    group.Children.Add(Stroke(light, 1.8, new LineGeometry(new Point(19, 14), new Point(19, 23))));
                    break;
                case IconKind.Export:
                    DrawIfcBox(group, accent, light, ink);
                    group.Children.Add(Stroke(accentDark, 2.6, new LineGeometry(new Point(16, 16), new Point(16, 29))));
                    group.Children.Add(Fill(accentDark, Geometry.Parse("M10,21 L16,15 22,21 Z")));
                    break;
            }

            group.Freeze();
            var image = new DrawingImage(group);
            image.Freeze();
            return image;
        }

        private static void DrawIfcBox(DrawingGroup group, Brush accent, Brush light, Brush ink)
        {
            group.Children.Add(Fill(accent, Geometry.Parse("M6,10 L16,5 26,10 16,15 Z")));
            group.Children.Add(Fill(light, Geometry.Parse("M6,10 L16,15 16,25 6,20 Z")));
            group.Children.Add(Fill(accent, Geometry.Parse("M26,10 L16,15 16,25 26,20 Z")));
            group.Children.Add(Stroke(ink, 1.2, Geometry.Parse("M6,10 L16,15 26,10 M16,15 L16,25")));
        }

        private static SolidColorBrush FrozenBrush(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        private static GeometryDrawing Fill(Brush brush, Geometry geometry) => new GeometryDrawing(brush, null, geometry);

        private static GeometryDrawing Stroke(Brush brush, double thickness, Geometry geometry)
        {
            var pen = new Pen(brush, thickness);
            pen.Freeze();
            return new GeometryDrawing(null, pen, geometry);
        }

        private static void RemoveOwnedPanel(object panels, string sourceId)
        {
            if (!(panels is IEnumerable enumerable))
                return;

            object? match = null;
            foreach (var panel in enumerable)
            {
                if (panel == null)
                    continue;
                var source = GetProperty(panel, "Source");
                if (source == null)
                    continue;
                if (string.Equals(GetProperty(source, "Id") as string, sourceId, StringComparison.OrdinalIgnoreCase))
                {
                    match = panel;
                    break;
                }
            }

            if (match != null)
                Remove(panels, match);
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
                // Host version may expose a different size enum; image/text still render safely.
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

        private sealed class DrawRibbonCommandHandler : ICommand
        {
            public bool CanExecute(object? parameter) => parameter is string text && !string.IsNullOrWhiteSpace(text);

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
