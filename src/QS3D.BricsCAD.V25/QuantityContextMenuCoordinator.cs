using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using Teigha.DatabaseServices;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Adds a QS3D action to the native BricsCAD object right-click menu. The callback stays
    /// read-only and dispatches a normal QS3D command, so database/project mutation never occurs
    /// while BricsCAD owns the native context-menu callback stack.
    /// </summary>
    internal static class QuantityContextMenuCoordinator
    {
        private const string ExtensionTypeName = "Bricscad.Windows.ContextMenuExtension, BrxMgd";
        private const string MenuItemTypeName = "System.Windows.Forms.MenuItem, System.Windows.Forms";
        private const string QuantityCommand = "QS3DQUANTITYINSIGHT";

        private static RXClass? _entityRuntimeClass;
        private static object? _extension;
        private static object? _menuItem;
        private static Delegate? _clickHandler;
        private static Delegate? _popupHandler;

        public static void Start()
        {
            if (_extension != null) return;

            var extensionType = Type.GetType(ExtensionTypeName, false)
                ?? throw new InvalidOperationException("BricsCAD ContextMenuExtension type is unavailable.");
            var menuItemType = Type.GetType(MenuItemTypeName, false)
                ?? throw new InvalidOperationException("Windows Forms MenuItem type is unavailable.");
            var runtimeClass = RXObject.GetClass(typeof(Entity))
                ?? throw new InvalidOperationException("BricsCAD Entity RXClass is unavailable.");

            object? extension = null;
            object? item = null;
            Delegate? clickHandler = null;
            Delegate? popupHandler = null;
            try
            {
                extension = Activator.CreateInstance(extensionType)
                    ?? throw new InvalidOperationException("Cannot create BricsCAD ContextMenuExtension.");
                SetProperty(extension, "Title", "QS3D");

                item = CreateMenuItem(menuItemType, "Diễn giải khối lượng");
                clickHandler = AttachEvent(item, "Click", nameof(OnMenuClick), required: true);

                var menuItems = GetProperty(extension, "MenuItems")
                    ?? throw new InvalidOperationException("ContextMenuExtension.MenuItems is unavailable.");
                Add(menuItems, item);

                popupHandler = AttachEvent(extension, "Popup", nameof(OnMenuPopup), required: false);

                var addMethod = FindApplicationMethod(
                    "AddObjectContextMenuExtension",
                    runtimeClass.GetType(),
                    extensionType)
                    ?? throw new InvalidOperationException("BricsCAD AddObjectContextMenuExtension API is unavailable.");
                addMethod.Invoke(null, new object[] { runtimeClass, extension });

                _entityRuntimeClass = runtimeClass;
                _extension = extension;
                _menuItem = item;
                _clickHandler = clickHandler;
                _popupHandler = popupHandler;
                RefreshMenuItemState();
            }
            catch
            {
                TryDetachEvent(extension, "Popup", popupHandler);
                TryDetachEvent(item, "Click", clickHandler);
                throw;
            }
        }

        public static void Stop()
        {
            var runtimeClass = _entityRuntimeClass;
            var extension = _extension;
            var item = _menuItem;
            var clickHandler = _clickHandler;
            var popupHandler = _popupHandler;
            _entityRuntimeClass = null;
            _extension = null;
            _menuItem = null;
            _clickHandler = null;
            _popupHandler = null;

            if (runtimeClass != null && extension != null)
            {
                try
                {
                    var removeMethod = FindApplicationMethod(
                        "RemoveObjectContextMenuExtension",
                        runtimeClass.GetType(),
                        extension.GetType());
                    removeMethod?.Invoke(null, new object[] { runtimeClass, extension });
                }
                catch
                {
                    // BricsCAD may already be tearing down its native menus.
                }
            }

            TryDetachEvent(extension, "Popup", popupHandler);
            TryDetachEvent(item, "Click", clickHandler);
        }

        private static void OnMenuPopup(object? sender, EventArgs e) => RefreshMenuItemState();

        private static void OnMenuClick(object? sender, EventArgs e)
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null || !HasImpliedSelection(document)) return;

            // Keep the native menu callback read-only. The normal command surface owns snapshot
            // reading, error handling and palette display after the context menu has closed.
            document.SendStringToExecute(QuantityCommand + " ", true, false, false);
        }

        private static void RefreshMenuItemState()
        {
            var item = _menuItem;
            if (item == null) return;
            var document = Application.DocumentManager.MdiActiveDocument;
            var enabled = document != null && HasImpliedSelection(document);
            SetProperty(item, "Enabled", enabled);
            SetProperty(item, "Visible", enabled);
        }

        private static bool HasImpliedSelection(Document document)
        {
            try
            {
                var result = document.Editor.SelectImplied();
                return result.Status == PromptStatus.OK && result.Value != null && result.Value.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        private static object CreateMenuItem(Type menuItemType, string text)
        {
            var stringConstructor = menuItemType.GetConstructor(new[] { typeof(string) });
            var item = stringConstructor != null
                ? stringConstructor.Invoke(new object[] { text })
                : Activator.CreateInstance(menuItemType)
                  ?? throw new InvalidOperationException("Cannot create native menu item.");
            SetProperty(item, "Text", text);
            return item;
        }

        private static Delegate? AttachEvent(object target, string eventName, string methodName, bool required)
        {
            var eventInfo = target.GetType().GetEvent(eventName, BindingFlags.Instance | BindingFlags.Public);
            if (eventInfo == null)
            {
                if (required) throw new InvalidOperationException(target.GetType().Name + "." + eventName + " event is unavailable.");
                return null;
            }

            var method = typeof(QuantityContextMenuCoordinator).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new InvalidOperationException("Context menu handler " + methodName + " is unavailable.");
            var handler = Delegate.CreateDelegate(eventInfo.EventHandlerType!, method, true);
            eventInfo.AddEventHandler(target, handler);
            return handler;
        }

        private static void TryDetachEvent(object? target, string eventName, Delegate? handler)
        {
            if (target == null || handler == null) return;
            try
            {
                target.GetType().GetEvent(eventName, BindingFlags.Instance | BindingFlags.Public)?.RemoveEventHandler(target, handler);
            }
            catch { }
        }

        private static MethodInfo? FindApplicationMethod(string name, Type firstType, Type secondType)
        {
            return typeof(Application).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(method =>
                {
                    if (!string.Equals(method.Name, name, StringComparison.Ordinal)) return false;
                    var parameters = method.GetParameters();
                    return parameters.Length == 2 &&
                           parameters[0].ParameterType.IsAssignableFrom(firstType) &&
                           parameters[1].ParameterType.IsAssignableFrom(secondType);
                });
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

        private static void Add(object collection, object item)
        {
            var addMethod = collection.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(x =>
                {
                    if (x.Name != "Add") return false;
                    var parameters = x.GetParameters();
                    return parameters.Length == 1 && parameters[0].ParameterType.IsAssignableFrom(item.GetType());
                });
            if (addMethod == null)
                throw new InvalidOperationException("Native context-menu collection does not expose a compatible Add method.");
            addMethod.Invoke(collection, new[] { item });
        }
    }
}
