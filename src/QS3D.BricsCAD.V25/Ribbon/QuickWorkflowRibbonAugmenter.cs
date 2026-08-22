using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Windows.Input;
using Bricscad.ApplicationServices;

namespace QS3D.BricsCAD.V25.Ribbon
{
    /// <summary>
    /// Keeps the existing quick-authoring panel and reconciles the clean-room BLT-reference
    /// VẼ/IFC entry points onto the canonical QS3D Ribbon. This class augments existing tabs;
    /// it never creates a duplicate top-level tab or changes Ribbon startup scheduling.
    /// </summary>
    internal static class QuickWorkflowRibbonAugmenter
    {
        private const string AssemblyName = "BrxMgd";
        private const string AuthorTabId = "QS3D_AUTHOR";
        private const string DrawTabId = "QS3D_DRAW";
        private const string AuthorPanelSourceId = "QS3D_AUTHOR_QUICK_PANEL_SOURCE";
        private const string DrawPrimitivesPanelSourceId = "QS3D_DRAW_PRIMITIVES_PANEL_SOURCE";
        private const string DrawTransformPanelSourceId = "QS3D_DRAW_TRANSFORM_PANEL_SOURCE";
        private const string DrawEditPanelSourceId = "QS3D_DRAW_EDIT_PANEL_SOURCE";
        private const string DrawIfcPanelSourceId = "QS3D_DRAW_IFC_PANEL_SOURCE";
        private static bool _initialized;

        private sealed class ButtonSpec
        {
            public ButtonSpec(string id, string text, string command)
            {
                Id = id;
                Text = text;
                Command = command;
            }

            public string Id { get; }
            public string Text { get; }
            public string Command { get; }
        }

        private static readonly ButtonSpec[] AuthorButtons =
        {
            new ButtonSpec("QS3D_AUTHOR_DRAW_ACTIVE", "Vẽ Nhanh", "QS3DDRAWACTIVE"),
            new ButtonSpec("QS3D_AUTHOR_CREATE_SIMILAR", "Vẽ Tương Tự", "QS3DCREATESIMILAR"),
            new ButtonSpec("QS3D_AUTHOR_PLAN2WALLS", "2D → Tường 3D", "QS3DCONVERT2D"),
            new ButtonSpec("QS3D_AUTHOR_WINDOW", "Vẽ Cửa Sổ", "QS3DDRAWWINDOW"),
            new ButtonSpec("QS3D_AUTHOR_MATERIALS", "Vật liệu", "QS3DMATERIALS")
        };

        private static readonly ButtonSpec[] DrawPrimitiveButtons =
        {
            new ButtonSpec("QS3D_DRAW_REF_LINE", "Đường thẳng", "QS3DDRAWLINE"),
            new ButtonSpec("QS3D_DRAW_REF_BY_CAD", "Theo nét CAD", "QS3DDRAWBYCAD"),
            new ButtonSpec("QS3D_DRAW_REF_RECT", "Chữ nhật", "QS3DDRAWRECT"),
            new ButtonSpec("QS3D_DRAW_REF_CIRCLE", "Đường tròn", "QS3DDRAWCIRCLE"),
            new ButtonSpec("QS3D_DRAW_REF_PROFILE", "Biên dạng", "QS3DDRAWPROFILE")
        };

        private static readonly ButtonSpec[] DrawTransformButtons =
        {
            new ButtonSpec("QS3D_DRAW_REF_SLAB_SLOPE", "Dốc sàn", "QS3DFLOORSLOPE"),
            new ButtonSpec("QS3D_DRAW_REF_SLAB_CUT", "Cắt sàn", "QS3DSLABCUT")
        };

        private static readonly ButtonSpec[] DrawEditButtons =
        {
            new ButtonSpec("QS3D_DRAW_REF_CORNER", "Nối góc", "QS3DJOINCORNER"),
            new ButtonSpec("QS3D_DRAW_REF_TEE", "Nối chữ T", "QS3DJOINTEE")
        };

        private static readonly ButtonSpec[] IfcButtons =
        {
            new ButtonSpec("QS3D_DRAW_IFC_IMPORT", "Nhập IFC", "QS3DIFCIMPORT"),
            new ButtonSpec("QS3D_DRAW_IFC_IMPORT_LIGHT", "Nhập IFC (nhẹ)", "QS3DIFCIMPORTLIGHT"),
            new ButtonSpec("QS3D_DRAW_IFC_REMOVE", "Xóa IFC", "QS3DIFCREMOVE"),
            new ButtonSpec("QS3D_DRAW_IFC_EXPORT", "Xuất IFC", "QS3DIFCEXPORT")
        };

        public static bool TryInitialize()
        {
            if (_initialized) return true;

            QS3D.BricsCAD.V25.UI.ReferenceWorkspaceTreeAugmenter.EnsureRegistered();

            try
            {
                var control = FindRibbonControl();
                if (control == null) return false;
                var tabs = GetProperty(control, "Tabs");
                if (!(tabs is IEnumerable tabItems)) return false;

                var authorTab = FindById(tabItems, AuthorTabId);
                var drawTab = FindById(tabItems, DrawTabId);
                if (authorTab == null || drawTab == null) return false;

                var authorPanels = GetProperty(authorTab, "Panels");
                var drawPanels = GetProperty(drawTab, "Panels");
                if (!(authorPanels is IEnumerable authorPanelItems) || !(drawPanels is IEnumerable drawPanelItems))
                    return false;

                var authorSource = FindPanelSource(authorPanelItems, AuthorPanelSourceId)
                                   ?? CreatePanel(authorPanels, AuthorPanelSourceId, "Tác vụ nhanh");
                EnsureButtons(authorSource, AuthorButtons);

                var primitives = FindPanelSource(drawPanelItems, DrawPrimitivesPanelSourceId);
                var transform = FindPanelSource(drawPanelItems, DrawTransformPanelSourceId);
                var edit = FindPanelSource(drawPanelItems, DrawEditPanelSourceId);
                if (primitives == null || transform == null || edit == null) return false;

                SetProperty(primitives, "Name", "Vẽ");
                SetProperty(primitives, "Title", "Vẽ");
                EnsureButtons(primitives, DrawPrimitiveButtons);

                SetProperty(transform, "Name", "Công cụ");
                SetProperty(transform, "Title", "Công cụ");
                EnsureButtons(transform, DrawTransformButtons);

                SetProperty(edit, "Name", "Kết nối & đo");
                SetProperty(edit, "Title", "Kết nối & đo");
                EnsureButtons(edit, DrawEditButtons);

                var ifcSource = FindPanelSource(drawPanelItems, DrawIfcPanelSourceId)
                                ?? CreatePanel(drawPanels, DrawIfcPanelSourceId, "IFC");
                EnsureButtons(ifcSource, IfcButtons);

                _initialized = true;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void Reset() => _initialized = false;

        private static void EnsureButtons(object source, ButtonSpec[] specs)
        {
            var items = GetProperty(source, "Items")
                        ?? throw new InvalidOperationException("RibbonPanelSource.Items was not available.");

            foreach (var spec in specs)
            {
                var button = FindById(items as IEnumerable, spec.Id) ?? FindByText(items, spec.Text);
                if (button == null)
                {
                    button = Create("Bricscad.Windows.RibbonButton");
                    SetProperty(button, "Id", spec.Id);
                    Add(items, button);
                }

                SetProperty(button, "Name", spec.Text);
                SetProperty(button, "Text", spec.Text);
                SetProperty(button, "ShowText", true);
                SetProperty(button, "ShowImage", false);
                SetProperty(button, "CommandParameter", spec.Command);
                SetProperty(button, "CommandHandler", new CommandHandler());
            }
        }

        private static object? FindPanelSource(IEnumerable panels, string sourceId)
        {
            foreach (var panel in panels)
            {
                if (panel == null) continue;
                var source = GetProperty(panel, "Source");
                if (source == null) continue;
                if (string.Equals(GetProperty(source, "Id") as string, sourceId, StringComparison.OrdinalIgnoreCase))
                    return source;
            }
            return null;
        }

        private static object CreatePanel(object panels, string sourceId, string title)
        {
            var source = Create("Bricscad.Windows.RibbonPanelSource");
            SetProperty(source, "Id", sourceId);
            SetProperty(source, "Name", title);
            SetProperty(source, "Title", title);

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

        private static void Add(object collection, object item)
        {
            var method = collection.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(x => x.Name == "Add" && x.GetParameters().Length == 1 && x.GetParameters()[0].ParameterType.IsAssignableFrom(item.GetType()));
            if (method == null) throw new InvalidOperationException("Ribbon collection does not expose a compatible Add method.");
            method.Invoke(collection, new[] { item });
        }

        private static object? FindById(IEnumerable? collection, string id)
        {
            if (collection == null) return null;
            foreach (var item in collection)
            {
                if (item == null) continue;
                if (string.Equals(GetProperty(item, "Id") as string, id, StringComparison.OrdinalIgnoreCase))
                    return item;
            }
            return null;
        }

        private static object? FindByText(object collection, string text)
        {
            if (!(collection is IEnumerable enumerable)) return null;
            foreach (var item in enumerable)
            {
                if (item == null) continue;
                if (string.Equals(GetProperty(item, "Text") as string, text, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(GetProperty(item, "Name") as string, text, StringComparison.OrdinalIgnoreCase))
                    return item;
            }
            return null;
        }

        private sealed class CommandHandler : ICommand
        {
            public bool CanExecute(object? parameter) => parameter is string command && !string.IsNullOrWhiteSpace(command);

            public void Execute(object? parameter)
            {
                if (!(parameter is string command) || string.IsNullOrWhiteSpace(command)) return;
                Application.DocumentManager.MdiActiveDocument?.SendStringToExecute(command + " ", true, false, false);
            }

            public event EventHandler? CanExecuteChanged { add { } remove { } }
        }
    }
}
