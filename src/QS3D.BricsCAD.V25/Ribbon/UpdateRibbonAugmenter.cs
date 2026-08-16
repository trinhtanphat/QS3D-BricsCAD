using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using QS3D.BricsCAD.V25.Updates;

namespace QS3D.BricsCAD.V25.Ribbon
{
    internal static class UpdateRibbonAugmenter
    {
        private const string AssemblyName = "BrxMgd";
        private const string TabId = "QS3D_HOME";
        private const string PanelSourceId = "QS3D_HOME_UPDATE_PANEL_SOURCE";
        private const string PanelTitle = "Hệ thống";
        private static bool _initialized;

        private sealed class ButtonSpec
        {
            public ButtonSpec(string id, string text, Action action, RibbonIconKind icon)
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

        private static readonly ButtonSpec[] Buttons =
        {
            new ButtonSpec("QS3D_HOME_UPDATE_CHECK", "Cập nhật QS3D", () => new UpdateCommands().ShowUpdateCenter(), RibbonIconKind.Update),
            new ButtonSpec("QS3D_HOME_UPDATE_ON_CLOSE", "Update khi đóng", ToggleInstallOnExit, RibbonIconKind.UpdateOnClose),
            new ButtonSpec("QS3D_HOME_UPDATE_STATUS", "Trạng thái Update", ShowUpdateStatus, RibbonIconKind.UpdateStatus)
        };

        public static bool TryInitialize()
        {
            if (_initialized) return true;
            try
            {
                var control = FindRibbonControl();
                if (control == null) return false;
                var tabs = GetProperty(control, "Tabs");
                if (!(tabs is IEnumerable enumerable)) return false;

                object? homeTab = null;
                foreach (var item in enumerable)
                {
                    if (item == null) continue;
                    if (string.Equals(GetProperty(item, "Id") as string, TabId, StringComparison.OrdinalIgnoreCase))
                    {
                        homeTab = item;
                        break;
                    }
                }
                if (homeTab == null) return false;

                var panels = GetProperty(homeTab, "Panels");
                if (panels == null) return false;
                RemoveOwnedPanel(panels);
                var source = CreateUpdatePanel(panels);
                var items = GetProperty(source, "Items");
                if (items == null) return false;

                foreach (var spec in Buttons)
                    Add(items, CreateButton(spec));

                _initialized = true;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void Reset() => _initialized = false;

        private static object CreateButton(ButtonSpec spec)
        {
            var button = Create("Bricscad.Windows.RibbonButton");
            SetProperty(button, "Id", spec.Id);
            SetProperty(button, "Name", spec.Text);
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

        private static void ToggleInstallOnExit()
        {
            var enabled = !UpdatePreferences.InstallOnExit;
            if (!UpdatePreferences.TrySetInstallOnExit(enabled, out var error))
                throw new InvalidOperationException(error);

            MessageBox.Show(
                enabled
                    ? "Đã bật cập nhật khi đóng BricsCAD. Khi có gói cập nhật đã được xác thực, QS3D có thể hoàn tất cài đặt sau khi ứng dụng đóng."
                    : "Đã tắt cập nhật khi đóng BricsCAD.",
                "QS3D — Update khi đóng",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private static void ShowUpdateStatus()
        {
            var assembly = typeof(UpdateCommands).Assembly;
            var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            var version = string.IsNullOrWhiteSpace(informational)
                ? assembly.GetName().Version?.ToString() ?? "unknown"
                : informational!.Split('+')[0];
            var installOnExit = UpdatePreferences.InstallOnExit ? "Bật" : "Tắt";

            MessageBox.Show(
                "Phiên bản đang chạy: " + version + "\nUpdate khi đóng: " + installOnExit + "\n\nNhấn “Cập nhật QS3D” để mở trung tâm cập nhật và kiểm tra release mới.",
                "QS3D — Trạng thái Update",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private static void RemoveOwnedPanel(object panels)
        {
            if (!(panels is IEnumerable enumerable)) return;
            object? match = null;
            foreach (var panel in enumerable)
            {
                if (panel == null) continue;
                var source = GetProperty(panel, "Source");
                if (source == null) continue;
                if (string.Equals(GetProperty(source, "Id") as string, PanelSourceId, StringComparison.OrdinalIgnoreCase))
                {
                    match = panel;
                    break;
                }
            }
            if (match != null) Remove(panels, match);
        }

        private static object CreateUpdatePanel(object panels)
        {
            var source = Create("Bricscad.Windows.RibbonPanelSource");
            SetProperty(source, "Id", PanelSourceId);
            SetProperty(source, "Name", PanelTitle);
            SetProperty(source, "Title", PanelTitle);

            var panel = Create("Bricscad.Windows.RibbonPanel");
            SetProperty(panel, "Source", source);
            Add(panels, panel);
            return source;
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
            try { property.SetValue(target, Enum.Parse(property.PropertyType, enumValue, true), null); }
            catch { }
        }

        private static void Add(object collection, object item)
        {
            var method = collection.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(x => x.Name == "Add" && x.GetParameters().Length == 1 && x.GetParameters()[0].ParameterType.IsAssignableFrom(item.GetType()));
            if (method == null) throw new InvalidOperationException("Ribbon collection does not expose a compatible Add method.");
            method.Invoke(collection, new[] { item });
        }

        private static void Remove(object collection, object item)
        {
            var method = collection.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(x => x.Name == "Remove" && x.GetParameters().Length == 1 && x.GetParameters()[0].ParameterType.IsAssignableFrom(item.GetType()));
            if (method == null) throw new InvalidOperationException("Ribbon collection does not expose a compatible Remove method.");
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
                try { _action(); }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "QS3D", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            public event EventHandler? CanExecuteChanged { add { } remove { } }
        }
    }
}
