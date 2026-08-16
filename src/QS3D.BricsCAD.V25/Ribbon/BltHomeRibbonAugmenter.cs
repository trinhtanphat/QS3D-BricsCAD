using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Bricscad.ApplicationServices;
using Application = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.Ribbon
{
    /// <summary>
    /// Reconciles only the QS3D-owned KHỞI ĐẦU panels into the clean-room BLT3D-familiar
    /// layout requested by the product owner. Native/third-party Ribbon content is untouched.
    /// Separate RibbonPanel objects intentionally provide the visible vertical group divider.
    /// </summary>
    internal static class BltHomeRibbonAugmenter
    {
        private const string AssemblyName = "BrxMgd";
        private const string HomeTabId = "QS3D_HOME";
        private const string ProjectPanelSourceId = "QS3D_HOME_PROJECT_PANEL_SOURCE";
        private const string CoordinationPanelSourceId = "QS3D_HOME_COORDINATION_PANEL_SOURCE";
        private const string QualityPanelSourceId = "QS3D_HOME_QUALITY_PANEL_SOURCE";
        private const string ConfigPanelSourceId = "QS3D_HOME_CONFIG_PANEL_SOURCE";
        private static bool _initialized;

        private enum IconKind
        {
            Open,
            Save,
            SaveAs,
            Settings,
            Objects
        }

        private sealed class HomeButtonSpec
        {
            public HomeButtonSpec(string id, string text, string command, IconKind icon)
            {
                Id = id;
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
                var homeTab = tabs == null ? null : FindById(tabs, HomeTabId);
                if (homeTab == null)
                    return false;

                var panels = GetProperty(homeTab, "Panels");
                if (panels == null)
                    return false;

                RemoveOwnedPanel(panels, ProjectPanelSourceId);
                RemoveOwnedPanel(panels, CoordinationPanelSourceId);
                RemoveOwnedPanel(panels, QualityPanelSourceId);
                RemoveOwnedPanel(panels, ConfigPanelSourceId);

                AddPanel(
                    panels,
                    ProjectPanelSourceId,
                    "Dự án",
                    new HomeButtonSpec("QS3D_HOME_OPEN", "Mở...", "_.OPEN", IconKind.Open),
                    new HomeButtonSpec("QS3D_HOME_SAVE", "Lưu", "_.QSAVE", IconKind.Save),
                    new HomeButtonSpec("QS3D_HOME_SAVEAS", "Lưu thành...", "_.SAVEAS", IconKind.SaveAs));

                AddPanel(
                    panels,
                    ConfigPanelSourceId,
                    "Cấu hình",
                    new HomeButtonSpec("QS3D_HOME_SETTINGS", "Cài đặt", "QS3DPROJECTTOOLS", IconKind.Settings),
                    new HomeButtonSpec("QS3D_HOME_SYSTEM_OBJECTS", "Đối tượng\nhệ thống", "QS3DFAMILIES", IconKind.Objects));

                _initialized = true;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void Reset() => _initialized = false;

        private static void AddPanel(object panels, string sourceId, string title, params HomeButtonSpec[] buttonSpecs)
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

        private static object CreateButton(HomeButtonSpec spec)
        {
            var button = Create("Bricscad.Windows.RibbonButton");
            SetProperty(button, "Id", spec.Id);
            SetProperty(button, "Name", spec.Text.Replace("\n", " "));
            SetProperty(button, "Text", spec.Text);
            SetProperty(button, "ShowText", true);
            SetProperty(button, "ShowImage", true);
            SetProperty(button, "CommandParameter", spec.Command);
            SetProperty(button, "CommandHandler", new HomeRibbonCommandHandler());
            SetEnumProperty(button, "Size", "Large");

            var image = CreateIcon(spec.Icon);
            SetProperty(button, "Image", image);
            SetProperty(button, "LargeImage", image);
            return button;
        }

        private static ImageSource CreateIcon(IconKind kind)
        {
            var accent = new SolidColorBrush(Color.FromRgb(34, 137, 245));
            var accentDark = new SolidColorBrush(Color.FromRgb(13, 77, 172));
            var light = new SolidColorBrush(Color.FromRgb(224, 238, 255));
            var dark = new SolidColorBrush(Color.FromRgb(31, 42, 58));
            accent.Freeze();
            accentDark.Freeze();
            light.Freeze();
            dark.Freeze();

            var group = new DrawingGroup();
            switch (kind)
            {
                case IconKind.Open:
                    group.Children.Add(Fill(accentDark, new RectangleGeometry(new Rect(3, 8, 13, 7), 2, 2)));
                    group.Children.Add(Fill(accent, new RectangleGeometry(new Rect(3, 11, 26, 17), 2, 2)));
                    group.Children.Add(Fill(light, new RectangleGeometry(new Rect(6, 14, 20, 10), 1, 1)));
                    group.Children.Add(Fill(accent, Geometry.Parse("M4,13 L11,13 14,10 29,10 25,27 4,27 Z")));
                    break;

                case IconKind.Save:
                    group.Children.Add(Fill(accent, new RectangleGeometry(new Rect(4, 3, 24, 26), 2, 2)));
                    group.Children.Add(Fill(dark, new RectangleGeometry(new Rect(9, 4, 13, 8), 1, 1)));
                    group.Children.Add(Fill(light, new RectangleGeometry(new Rect(9, 17, 14, 9), 1, 1)));
                    group.Children.Add(Fill(accentDark, new RectangleGeometry(new Rect(18, 5, 3, 5), 0, 0)));
                    break;

                case IconKind.SaveAs:
                    group.Children.Add(Fill(accent, new RectangleGeometry(new Rect(3, 3, 22, 25), 2, 2)));
                    group.Children.Add(Fill(dark, new RectangleGeometry(new Rect(8, 4, 12, 7), 1, 1)));
                    group.Children.Add(Fill(light, new RectangleGeometry(new Rect(8, 16, 12, 9), 1, 1)));
                    group.Children.Add(Fill(accentDark, new EllipseGeometry(new Point(25, 23), 7, 7)));
                    group.Children.Add(Stroke(Brushes.White, 2.2, new LineGeometry(new Point(21, 23), new Point(29, 23))));
                    group.Children.Add(Stroke(Brushes.White, 2.2, new LineGeometry(new Point(25, 19), new Point(25, 27))));
                    break;

                case IconKind.Settings:
                    group.Children.Add(Fill(accent, new EllipseGeometry(new Point(16, 16), 10, 10)));
                    group.Children.Add(Fill(ShellBrush(), new EllipseGeometry(new Point(16, 16), 4, 4)));
                    for (var i = 0; i < 8; i++)
                    {
                        var angle = i * Math.PI / 4.0;
                        var x1 = 16 + Math.Cos(angle) * 10;
                        var y1 = 16 + Math.Sin(angle) * 10;
                        var x2 = 16 + Math.Cos(angle) * 14;
                        var y2 = 16 + Math.Sin(angle) * 14;
                        group.Children.Add(Stroke(accent, 4, new LineGeometry(new Point(x1, y1), new Point(x2, y2))));
                    }
                    break;

                case IconKind.Objects:
                    group.Children.Add(Fill(accent, new EllipseGeometry(new Point(9, 9), 5, 5)));
                    group.Children.Add(Fill(light, new EllipseGeometry(new Point(23, 9), 5, 5)));
                    group.Children.Add(Fill(accentDark, new EllipseGeometry(new Point(9, 23), 5, 5)));
                    group.Children.Add(Fill(accent, new EllipseGeometry(new Point(23, 23), 5, 5)));
                    group.Children.Add(Stroke(accentDark, 1.5, new RectangleGeometry(new Rect(3, 3, 26, 26), 2, 2)));
                    break;
            }

            group.Freeze();
            var image = new DrawingImage(group);
            image.Freeze();
            return image;
        }

        private static Brush ShellBrush()
        {
            var brush = new SolidColorBrush(Color.FromRgb(32, 32, 32));
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

        private sealed class HomeRibbonCommandHandler : ICommand
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
