using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Windows.Input;

namespace QS3D.BricsCAD.V25.Ribbon
{
    /// <summary>
    /// Reconciles the QS3D-owned Home panels into a mouse-first layout.
    /// File actions invoke C# project services directly instead of dispatching host commands.
    /// </summary>
    internal static class BltHomeRibbonAugmenter
    {
        private const string AssemblyName = "BrxMgd";
        private const string HomeTabId = "QS3D_HOME";
        private const string FilePanelSourceId = "QS3D_HOME_FILE_PANEL_SOURCE";
        private const string LegacyProjectPanelSourceId = "QS3D_HOME_PROJECT_PANEL_SOURCE";
        private const string CoordinationPanelSourceId = "QS3D_HOME_COORDINATION_PANEL_SOURCE";
        private const string QualityPanelSourceId = "QS3D_HOME_QUALITY_PANEL_SOURCE";
        private const string UpdatePanelSourceId = "QS3D_HOME_UPDATE_PANEL_SOURCE";
        private const string ConfigPanelSourceId = "QS3D_HOME_CONFIG_PANEL_SOURCE";
        private static bool _initialized;

        private sealed class HomeButtonSpec
        {
            public HomeButtonSpec(string id, string text, Action action, RibbonIconKind icon)
            {
                Id = id;
                Text = text;
                Action = action ?? throw new ArgumentNullException(nameof(action));
                Icon = icon;
            }

            public string Id { get; }
            public string Text { get; }
            public Action Action { get; }
            public RibbonIconKind Icon { get; }
        }

        public static bool TryInitialize()
        {
            if (_initialized) return true;

            try
            {
                var control = FindRibbonControl();
                if (control == null) return false;

                var tabs = GetProperty(control, "Tabs");
                var homeTab = tabs == null ? null : FindById(tabs, HomeTabId);
                if (homeTab == null) return false;

                var panels = GetProperty(homeTab, "Panels");
                if (panels == null) return false;

                RemoveOwnedPanel(panels, FilePanelSourceId);
                RemoveOwnedPanel(panels, LegacyProjectPanelSourceId);
                RemoveOwnedPanel(panels, CoordinationPanelSourceId);
                RemoveOwnedPanel(panels, QualityPanelSourceId);
                RemoveOwnedPanel(panels, ConfigPanelSourceId);

                // UpdateRibbonAugmenter runs immediately before this augmenter. Temporarily
                // detach its panel so the final order is TỆP → HỆ THỐNG → CẤU HÌNH.
                var updatePanel = FindPanelBySourceId(panels, UpdatePanelSourceId);
                if (updatePanel != null) Remove(panels, updatePanel);

                AddPanel(
                    panels,
                    FilePanelSourceId,
                    "Tệp",
                    new HomeButtonSpec("QS3D_HOME_OPEN_PROJECT", "Mở dự án...", ProjectFileUiService.OpenProjectFromPicker, RibbonIconKind.OpenProject),
                    new HomeButtonSpec("QS3D_HOME_SAVE_PROJECT", "Lưu", ProjectFileUiService.SaveCurrentProject, RibbonIconKind.Save),
                    new HomeButtonSpec("QS3D_HOME_SAVE_PROJECT_AS", "Lưu thành...", ProjectFileUiService.SaveCurrentProjectAs, RibbonIconKind.SaveAs));

                if (updatePanel != null) Add(panels, updatePanel);

                AddPanel(
                    panels,
                    ConfigPanelSourceId,
                    "Cấu hình",
                    new HomeButtonSpec("QS3D_HOME_SETTINGS", "Cài đặt", () => new ProjectToolsCommands().ShowProjectTools(), RibbonIconKind.Settings),
                    new HomeButtonSpec("QS3D_HOME_SYSTEM_OBJECTS", "Đối tượng\nhệ thống", () => new FamilyManagerCommands().ShowFamilyManager(), RibbonIconKind.Objects));

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
            SetProperty(button, "CommandParameter", spec.Id);
            SetProperty(button, "CommandHandler", new DirectActionHandler(spec.Action));
            SetEnumProperty(button, "Size", "Large");
            SetProperty(button, "Image", RibbonIconFactory.Create(spec.Icon, 16));
            SetProperty(button, "LargeImage", RibbonIconFactory.Create(spec.Icon, 32));
            return button;
        }

        private static void RemoveOwnedPanel(object panels, string sourceId)
        {
            var match = FindPanelBySourceId(panels, sourceId);
            if (match != null) Remove(panels, match);
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
                // Host version may expose a different size enum; image/text still render safely.
            }
        }

        private static void Add(object collection, object item)
        {
            var method = collection.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(candidate => candidate.Name == "Add"
                    && candidate.GetParameters().Length == 1
                    && candidate.GetParameters()[0].ParameterType.IsAssignableFrom(item.GetType()));
            if (method == null) throw new InvalidOperationException("Collection does not expose a compatible Add method.");
            method.Invoke(collection, new[] { item });
        }

        private static void Remove(object collection, object item)
        {
            var method = collection.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(candidate => candidate.Name == "Remove"
                    && candidate.GetParameters().Length == 1
                    && candidate.GetParameters()[0].ParameterType.IsAssignableFrom(item.GetType()));
            if (method == null) throw new InvalidOperationException("Collection does not expose a compatible Remove method.");
            method.Invoke(collection, new[] { item });
        }

        private sealed class DirectActionHandler : ICommand
        {
            private readonly Action _action;

            public DirectActionHandler(Action action)
            {
                _action = action ?? throw new ArgumentNullException(nameof(action));
            }

            public bool CanExecute(object? parameter) => true;

            public void Execute(object? parameter)
            {
                try
                {
                    _action();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(ex.Message, "QS3D", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }

            public event EventHandler? CanExecuteChanged
            {
                add { }
                remove { }
            }
        }
    }
}
