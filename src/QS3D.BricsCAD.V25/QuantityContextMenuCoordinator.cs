using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Adds a small QS3D action to the native BricsCAD right-click menu without taking over
    /// selection or mutating the drawing from a menu callback. Reflection keeps the adapter
    /// tolerant of minor BrxMgd context-menu surface differences while still using the public
    /// BricsCAD Application context-menu API.
    /// </summary>
    internal static class QuantityContextMenuCoordinator
    {
        private const string ExtensionTypeName = "Bricscad.Windows.ContextMenuExtension, BrxMgd";
        private const string MenuItemTypeName = "System.Windows.Forms.MenuItem, System.Windows.Forms";
        private const string QuantityCommand = "QS3DQUANTITYINSIGHT";

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

                var addMethod = typeof(Application).GetMethod(
                    "AddDefaultContextMenuExtension",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { extensionType },
                    null)
                    ?? throw new InvalidOperationException("BricsCAD AddDefaultContextMenuExtension API is unavailable.");
                addMethod.Invoke(null, new[] { extension });

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
            var extension = _extension;
            var item = _menuItem;
            var clickHandler = _clickHandler;
            var popupHandler = _popupHandler;
            _extension = null;
            _menuItem = null;
            _clickHandler = null;
            _popupHandler = null;

            if (extension != null)
            {
                try
                {
                    var extensionType = extension.GetType();
                    var removeMethod = typeof(Application).GetMethod(
                        "RemoveDefaultContextMenuExtension",
                        BindingFlags.Public | BindingFlags.Static,
                        null,
                        new[] { extensionType },
                        null);
                    removeMethod?.Invoke(null, new[] { extension });
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