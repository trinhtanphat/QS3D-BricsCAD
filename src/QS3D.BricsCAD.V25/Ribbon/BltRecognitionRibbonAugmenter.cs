using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Windows.Input;
using System.Windows.Media;
using Application = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.Ribbon
{
    /// <summary>
    /// Reconciles only the QS3D-owned NHẬN DẠNG tab into the compact BLT3D-familiar
    /// topbar layout. The workspace/palette below the Ribbon is intentionally untouched.
    /// Recognition actions without a matching production workflow remain visibly present
    /// but disabled so a familiar label can never dispatch an unrelated QS3D command.
    /// </summary>
    internal static class BltRecognitionRibbonAugmenter
    {
        private const string AssemblyName = "BrxMgd";
        private const string RecognitionTabId = "QS3D_RECOGNIZE";
        private const string LegacyRecognitionPanelSourceId = "QS3D_RECOGNIZE_RECOGNIZE_PANEL_SOURCE";
        private const string LegacyReviewPanelSourceId = "QS3D_RECOGNIZE_REVIEW_PANEL_SOURCE";
        private const string RecognitionPanelSourceId = "QS3D_RECOGNIZE_BLT_RECOGNITION_PANEL_SOURCE";
        private const string BeamPanelSourceId = "QS3D_RECOGNIZE_BLT_BEAM_PANEL_SOURCE";
        private static bool _initialized;

        private enum IconKind { Restore, Text, Options, Table, Boundary, Label, Auto, Validate }

        private sealed class RecognitionButtonSpec
        {
            public RecognitionButtonSpec(string id, string text, string command, IconKind icon, bool enabled = false, double width = 136d)
            { Id = id; Text = text; Command = command; Icon = icon; Enabled = enabled; Width = width; }
            public string Id { get; }
            public string Text { get; }
            public string Command { get; }
            public IconKind Icon { get; }
            public bool Enabled { get; }
            public double Width { get; }
        }

        public static bool TryInitialize()
        {
            if (_initialized) return true;
            try
            {
                var control = FindRibbonControl();
                if (control == null) return false;
                var tabs = GetProperty(control, "Tabs");
                var tab = tabs == null ? null : FindById(tabs, RecognitionTabId);
                if (tab == null) return false;
                var panels = GetProperty(tab, "Panels");
                if (panels == null) return false;

                RemoveOwnedPanel(panels, LegacyRecognitionPanelSourceId);
                RemoveOwnedPanel(panels, LegacyReviewPanelSourceId);
                RemoveOwnedPanel(panels, RecognitionPanelSourceId);
                RemoveOwnedPanel(panels, BeamPanelSourceId);

                AddPanel(panels, RecognitionPanelSourceId, "Nhận dạng",
                    new[]
                    {
                        new RecognitionButtonSpec("QS3D_RECOGNIZE_BLT_RESTORE", "Khôi phục đã chọn", "QS3DRECOGNITIONRESTORE", IconKind.Restore),
                        new RecognitionButtonSpec("QS3D_RECOGNIZE_BLT_TEXT", "Nhận dạng chữ", string.Empty, IconKind.Text, enabled: false),
                        new RecognitionButtonSpec("QS3D_RECOGNIZE_BLT_OPTIONS", "Tùy chọn nhận dạng", "QS3DRECOGNITIONOPTIONS", IconKind.Options)
                    },
                    new[]
                    {
                        new RecognitionButtonSpec("QS3D_RECOGNIZE_BLT_TABLE", "Bảng biểu phần tử", string.Empty, IconKind.Table, enabled: false, width: 132d)
                    });

                AddPanel(panels, BeamPanelSourceId, "Dầm",
                    new[]
                    {
                        new RecognitionButtonSpec("QS3D_RECOGNIZE_BLT_BOUNDARY", "Chọn đường biên", "QS3DRECOGNITIONBOUNDARY", IconKind.Boundary),
                        new RecognitionButtonSpec("QS3D_RECOGNIZE_BLT_LABEL", "Chọn nhãn", "QS3DRECOGNITIONLABEL", IconKind.Label),
                        new RecognitionButtonSpec("QS3D_RECOGNIZE_BLT_AUTO", "Tự động nhận dạng", "QS3DRECOGNITIONAUTO", IconKind.Auto)
                    },
                    new[]
                    {
                        new RecognitionButtonSpec("QS3D_RECOGNIZE_BLT_VALIDATE", "Xác định Kiểm tra", string.Empty, IconKind.Validate, enabled: false, width: 136d)
                    });

                _initialized = true;
                return true;
            }
            catch { return false; }
        }

        public static void Reset() => _initialized = false;

        private static void AddPanel(object panels, string sourceId, string title, RecognitionButtonSpec[] leftColumn, RecognitionButtonSpec[] rightColumn)
        {
            var source = Create("Bricscad.Windows.RibbonPanelSource");
            SetProperty(source, "Id", sourceId); SetProperty(source, "Name", title); SetProperty(source, "Title", title);
            var items = GetProperty(source, "Items") ?? throw new InvalidOperationException("RibbonPanelSource.Items was not available.");
            Add(items, CreateColumn(sourceId + "_LEFT", leftColumn));
            Add(items, CreateColumn(sourceId + "_RIGHT", rightColumn));
            var panel = Create("Bricscad.Windows.RibbonPanel"); SetProperty(panel, "Source", source); Add(panels, panel);
        }

        private static object CreateColumn(string id, RecognitionButtonSpec[] specs)
        {
            var column = Create("Bricscad.Windows.RibbonRowPanel"); SetProperty(column, "Id", id);
            var items = GetProperty(column, "Items") ?? throw new InvalidOperationException("RibbonRowPanel.Items was not available.");
            for (var i = 0; i < specs.Length; i++) { if (i > 0) Add(items, Create("Bricscad.Windows.RibbonRowBreak")); Add(items, CreateButton(specs[i])); }
            return column;
        }

        private static object CreateButton(RecognitionButtonSpec spec)
        {
            var button = Create("Bricscad.Windows.RibbonButton");
            SetProperty(button, "Id", spec.Id); SetProperty(button, "Name", spec.Text); SetProperty(button, "Text", spec.Text);
            SetProperty(button, "ShowText", true); SetProperty(button, "ShowImage", true); SetProperty(button, "IsEnabled", spec.Enabled); SetProperty(button, "Width", spec.Width);
            SetEnumProperty(button, "Size", "Standard");
            var image = CreateIcon(spec.Icon); SetProperty(button, "Image", image); SetProperty(button, "LargeImage", image);
            if (spec.Enabled && !string.IsNullOrWhiteSpace(spec.Command)) { SetProperty(button, "CommandParameter", spec.Command); SetProperty(button, "CommandHandler", new RecognitionRibbonCommandHandler()); }
            return button;
        }

        private static ImageSource CreateIcon(IconKind kind)
        {
            var accent = FrozenBrush(Color.FromRgb(32, 137, 245));
            var accentDark = FrozenBrush(Color.FromRgb(14, 79, 170));
            var light = FrozenBrush(Color.FromRgb(218, 236, 255));
            var ink = FrozenBrush(Color.FromRgb(42, 51, 63));
            var amber = FrozenBrush(Color.FromRgb(239, 177, 45));
            var warning = FrozenBrush(Color.FromRgb(224, 72, 72));
            var group = new DrawingGroup();
            switch (kind)
            {
                case IconKind.Restore:
                    group.Children.Add(Stroke(accent, 3.0, Geometry.Parse("M25,10 C21,5 12,5 7,11 C3,16 5,24 11,27")));
                    group.Children.Add(Fill(accent, Geometry.Parse("M5,7 L13,8 8,15 Z")));
                    group.Children.Add(Stroke(light, 2.0, Geometry.Parse("M12,27 C18,30 26,26 27,19"))); break;
                case IconKind.Text:
                    group.Children.Add(Stroke(accentDark, 2.4, Geometry.Parse("M8,26 L15,6 17,6 24,26 M11,18 L21,18")));
                    group.Children.Add(Fill(light, new RectangleGeometry(new System.Windows.Rect(4, 4, 5, 5), 1, 1))); break;
                case IconKind.Options:
                    group.Children.Add(Stroke(accent, 2.2, new RectangleGeometry(new System.Windows.Rect(5, 6, 22, 20), 2, 2)));
                    group.Children.Add(Stroke(accentDark, 1.8, Geometry.Parse("M9,12 L12,15 17,9 M9,21 L12,24 17,18")));
                    group.Children.Add(Stroke(light, 1.8, new LineGeometry(new System.Windows.Point(19, 12), new System.Windows.Point(24, 12))));
                    group.Children.Add(Stroke(light, 1.8, new LineGeometry(new System.Windows.Point(19, 21), new System.Windows.Point(24, 21)))); break;
                case IconKind.Table:
                    group.Children.Add(Stroke(accentDark, 2.0, new RectangleGeometry(new System.Windows.Rect(5, 7, 22, 18), 1, 1)));
                    group.Children.Add(Stroke(accent, 1.5, new LineGeometry(new System.Windows.Point(5, 13), new System.Windows.Point(27, 13))));
                    group.Children.Add(Stroke(accent, 1.5, new LineGeometry(new System.Windows.Point(5, 19), new System.Windows.Point(27, 19))));
                    group.Children.Add(Stroke(accent, 1.5, new LineGeometry(new System.Windows.Point(13, 7), new System.Windows.Point(13, 25))));
                    group.Children.Add(Stroke(accent, 1.5, new LineGeometry(new System.Windows.Point(21, 7), new System.Windows.Point(21, 25)))); break;
                case IconKind.Boundary:
                    group.Children.Add(Stroke(accent, 2.5, Geometry.Parse("M5,9 L14,5 27,11 24,25 10,27 4,18 Z")));
                    group.Children.Add(Fill(accentDark, new EllipseGeometry(new System.Windows.Point(5, 9), 2.2, 2.2)));
                    group.Children.Add(Fill(warning, new EllipseGeometry(new System.Windows.Point(24, 25), 2.2, 2.2))); break;
                case IconKind.Label:
                    group.Children.Add(Fill(accent, Geometry.Parse("M5,8 L18,8 27,16 18,24 5,24 Z")));
                    group.Children.Add(Fill(ink, new EllipseGeometry(new System.Windows.Point(10, 16), 2.2, 2.2)));
                    group.Children.Add(Stroke(light, 1.6, new LineGeometry(new System.Windows.Point(15, 13), new System.Windows.Point(21, 16)))); break;
                case IconKind.Auto:
                    group.Children.Add(Stroke(accent, 2.5, new LineGeometry(new System.Windows.Point(7, 25), new System.Windows.Point(23, 9))));
                    group.Children.Add(Fill(accentDark, Geometry.Parse("M5,22 L10,27 27,10 22,5 Z")));
                    group.Children.Add(Fill(amber, Geometry.Parse("M25,3 L27,8 31,10 27,12 25,17 23,12 19,10 23,8 Z"))); break;
                case IconKind.Validate:
                    group.Children.Add(Stroke(accentDark, 2.2, new RectangleGeometry(new System.Windows.Rect(5, 6, 21, 20), 2, 2)));
                    group.Children.Add(Stroke(accent, 2.4, Geometry.Parse("M9,16 L14,21 23,11")));
                    group.Children.Add(Fill(warning, new EllipseGeometry(new System.Windows.Point(26, 7), 3.0, 3.0))); break;
            }
            group.Freeze(); var image = new DrawingImage(group); image.Freeze(); return image;
        }

        private static SolidColorBrush FrozenBrush(Color color) { var brush = new SolidColorBrush(color); brush.Freeze(); return brush; }
        private static GeometryDrawing Fill(Brush brush, Geometry geometry) => new GeometryDrawing(brush, null, geometry);
        private static GeometryDrawing Stroke(Brush brush, double thickness, Geometry geometry) { var pen = new Pen(brush, thickness); pen.Freeze(); return new GeometryDrawing(null, pen, geometry); }

        private static void RemoveOwnedPanel(object panels, string sourceId)
        {
            if (!(panels is IEnumerable enumerable)) return;
            object? match = null;
            foreach (var panel in enumerable)
            {
                if (panel == null) continue; var source = GetProperty(panel, "Source"); if (source == null) continue;
                if (!string.Equals(GetProperty(source, "Id") as string, sourceId, StringComparison.OrdinalIgnoreCase)) continue;
                match = panel; break;
            }
            if (match != null) Remove(panels, match);
        }

        private static object? FindById(object collection, string id)
        {
            if (!(collection is IEnumerable enumerable)) return null;
            foreach (var item in enumerable) { if (item != null && string.Equals(GetProperty(item, "Id") as string, id, StringComparison.OrdinalIgnoreCase)) return item; }
            return null;
        }

        private static object? FindRibbonControl()
        {
            var servicesType = Type.GetType("Bricscad.Ribbon.RibbonServices, " + AssemblyName, false); if (servicesType == null) return null;
            var paletteProperty = servicesType.GetProperty("RibbonPaletteSet", BindingFlags.Public | BindingFlags.Static); var palette = paletteProperty?.GetValue(null, null);
            if (palette == null) { servicesType.GetMethod("CreateRibbonPaletteSet", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, null); palette = paletteProperty?.GetValue(null, null); }
            if (palette == null) return null; if (palette.GetType().Name == "RibbonControl") return palette;
            var direct = GetProperty(palette, "RibbonControl"); if (direct != null) return direct;
            foreach (var property in palette.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            { if (property.PropertyType.Name != "RibbonControl" || property.GetIndexParameters().Length != 0) continue; var value = property.GetValue(palette, null); if (value != null) return value; }
            return null;
        }

        private static object Create(string fullName) => Activator.CreateInstance(Type.GetType(fullName + ", " + AssemblyName, true)!) ?? throw new InvalidOperationException("Cannot create " + fullName);
        private static object? GetProperty(object target, string name) => target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public)?.GetValue(target, null);
        private static void SetProperty(object target, string name, object value)
        {
            var property = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            if (property == null || !property.CanWrite) return;
            if (property.PropertyType.IsInstanceOfType(value) || property.PropertyType == value.GetType()) property.SetValue(target, value, null);
        }
        private static void SetEnumProperty(object target, string name, string enumValue)
        {
            var property = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            if (property == null || !property.CanWrite || !property.PropertyType.IsEnum) return;
            try { property.SetValue(target, Enum.Parse(property.PropertyType, enumValue, true), null); } catch { }
        }
        private static void Add(object collection, object item)
        {
            var method = collection.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public).FirstOrDefault(candidate => candidate.Name == "Add" && candidate.GetParameters().Length == 1 && candidate.GetParameters()[0].ParameterType.IsAssignableFrom(item.GetType()));
            if (method == null) throw new InvalidOperationException("Collection does not expose a compatible Add method."); method.Invoke(collection, new[] { item });
        }
        private static void Remove(object collection, object item)
        {
            var method = collection.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public).FirstOrDefault(candidate => candidate.Name == "Remove" && candidate.GetParameters().Length == 1 && candidate.GetParameters()[0].ParameterType.IsAssignableFrom(item.GetType()));
            if (method == null) throw new InvalidOperationException("Collection does not expose a compatible Remove method."); method.Invoke(collection, new[] { item });
        }

        private sealed class RecognitionRibbonCommandHandler : ICommand
        {
            public bool CanExecute(object? parameter) => parameter is string command && !string.IsNullOrWhiteSpace(command);
            public void Execute(object? parameter)
            {
                if (!(parameter is string command) || string.IsNullOrWhiteSpace(command)) return;
                var document = Application.DocumentManager.MdiActiveDocument; if (document == null) return;
                try { document.SendStringToExecute(command.Trim() + " ", true, false, false); } catch { }
            }
            public event EventHandler? CanExecuteChanged { add { } remove { } }
        }
    }
}
