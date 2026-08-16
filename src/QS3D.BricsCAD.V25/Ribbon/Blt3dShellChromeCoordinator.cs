using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace QS3D.BricsCAD.V25.Ribbon
{
    /// <summary>
    /// Applies the shell-level details visible in the BLT3D reference topbar:
    /// the BLT3D app mark plus a clean tab row without BricsCAD application/QAT/search chrome.
    /// All host-owned visibility/icon values are restored when QS3D unloads.
    /// </summary>
    internal static class Blt3dShellChromeCoordinator
    {
        private const string AssemblyName = "BrxMgd";
        private const uint ImageIcon = 1;
        private const uint LoadFromFile = 0x0010;
        private const uint LoadDefaultColor = 0x0000;
        private const uint WmGetIcon = 0x007F;
        private const uint WmSetIcon = 0x0080;
        private const int IconSmall = 0;
        private const int IconBig = 1;
        private const uint GaRoot = 2;

        // Exact 16x16 BLT3D title-bar mark cropped from the user-provided reference screenshot.
        // The ICO embeds those exact PNG pixels and can be consumed by both net48 and net8 without
        // System.Drawing or a host-version-specific resource-pack URI.
        private const string Blt3dIconIcoBase64 =
            "AAABAAEAEBAAAAAAIAC3AgAAFgAAAIlQTkcNChoKAAAADUlIRFIAAAAQAAAAEAgGAAAAH/P/YQAAAn5JREFUeJxlk81vE1cUxX/3+XkcezxxSk0DobBA7EBCrNuw6A4WoSAikBB7EKwoCPEv8LUCskRVU4Ig4qNSgRXddYGEhAQLJIKC+BQIBU8843jsee+ysGUScpZX75x777nnyWJ7XukjTTPiZgooIKxEr1aLQsKwNKha7z1e4dzFv7l96yFxmqKqqOoKuoggItTCkL37fuPUiUMYAWuM4ez5aS5PzeJddzkDMabX23voCzbjhMtTswCcOXUYkyRtZmbuIXgKhQJiDMZayHPyOCaPY8hzjLWIMb03eK5fv0+StLGLzZRW1sE5hxiDyzLyNKWy4Wc2Hz0OwOtrf7H09h2FaghBgHpPK+sQLyZYRAYja55TXr+OjZMHWLd7D+WxMcAwNrGXD3dv8Gb2Jp2FBvRXExHMsqVR57BbxhkZn6A8uhafLeHSBkM/1hidPMHI7tNot8OgKWC/XUkxgSV+9Yn/T1+ivm0Tm3/fSfDDGB+eNEg+N8njImKld9FVAgBesUNFiIb5/GyeL3Mfqf96GJUitiI441aQVwtI/2TeEURlxA4hxmOK/Vx47Y2/LGOG7yFFEIs6j6rvhSp3iC0h5Qi8h64f+GBZkTiBLy/QQgmCCMEghYBCuIZ84RXtx7NQLEKtCq0MVcUORyGVUkC7lfUGiucheY9Gm5D1O/DJJ9qPrrL09B+0m1C6eAypR4RXHlCLKphqdYiDB3ehGJxzqCmi3sHCc5j7l/jmMdJHf6KdNlL/CR+W0NE6+08eohpVkEbrpXqFsxemuXP7PxpJ0t9KQF3fKQsoknUZ+WUrE0f28cf2rZSCAFn+nZOkzWIzXeVrPygghuFKQHW4Oqh+BR0ZFiqoacnNAAAAAElFTkSuQmCC";

        private sealed class HiddenChrome
        {
            public HiddenChrome(FrameworkElement element, Visibility visibility)
            {
                Element = element;
                Visibility = visibility;
            }

            public FrameworkElement Element { get; }
            public Visibility Visibility { get; }
        }

        private static readonly List<HiddenChrome> HiddenElements = new List<HiddenChrome>();
        private static bool _initialized;
        private static Window? _hostWindow;
        private static ImageSource? _originalWpfIcon;
        private static IntPtr _hostHandle;
        private static IntPtr _originalSmallIcon;
        private static IntPtr _originalBigIcon;
        private static IntPtr _bltSmallIcon;
        private static IntPtr _bltBigIcon;

        public static bool TryInitialize()
        {
            if (_initialized)
                return true;

            try
            {
                var control = FindRibbonControl();
                if (!(control is DependencyObject ribbonRoot))
                    return false;

                var window = Window.GetWindow(ribbonRoot);
                var handle = ResolveHostWindowHandle(ribbonRoot, window);
                if (window == null && handle == IntPtr.Zero)
                    return false;

                if (window != null)
                    ApplyWpfWindowIcon(window);
                if (handle != IntPtr.Zero)
                    ApplyNativeWindowIcon(handle);

                CollapseKnownChromeProperty(control, "QuickAccessToolBar");
                CollapseKnownChromeProperty(control, "QuickAccessToolbar");
                CollapseKnownChromeProperty(control, "ApplicationButton");
                CollapseKnownChromeProperty(control, "ApplicationMenuButton");
                CollapseKnownChromeProperty(control, "SearchBox");
                CollapseKnownChromeProperty(control, "SearchControl");
                CollapseNonReferenceChrome(ribbonRoot);

                _initialized = true;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void Reset()
        {
            _initialized = false;

            foreach (var hidden in HiddenElements)
            {
                try { hidden.Element.Visibility = hidden.Visibility; }
                catch { }
            }
            HiddenElements.Clear();

            try
            {
                if (_hostWindow != null)
                    _hostWindow.Icon = _originalWpfIcon;
            }
            catch
            {
                // Host window may already be closing.
            }

            try
            {
                if (_hostHandle != IntPtr.Zero)
                {
                    SendMessage(_hostHandle, WmSetIcon, new IntPtr(IconSmall), _originalSmallIcon);
                    SendMessage(_hostHandle, WmSetIcon, new IntPtr(IconBig), _originalBigIcon);
                }
            }
            catch
            {
                // Native shell may already be gone during process teardown.
            }

            DestroyOwnedIcon(ref _bltSmallIcon);
            DestroyOwnedIcon(ref _bltBigIcon);
            _hostWindow = null;
            _originalWpfIcon = null;
            _hostHandle = IntPtr.Zero;
            _originalSmallIcon = IntPtr.Zero;
            _originalBigIcon = IntPtr.Zero;
        }

        private static void ApplyWpfWindowIcon(Window window)
        {
            if (_hostWindow == null)
            {
                _hostWindow = window;
                _originalWpfIcon = window.Icon;
            }

            var iconBytes = ExtractEmbeddedPngFromIco();
            using (var stream = new MemoryStream(iconBytes, writable: false))
            {
                var frame = BitmapFrame.Create(
                    stream,
                    BitmapCreateOptions.PreservePixelFormat,
                    BitmapCacheOption.OnLoad);
                if (frame.CanFreeze)
                    frame.Freeze();
                window.Icon = frame;
            }
        }

        private static void ApplyNativeWindowIcon(IntPtr handle)
        {
            if (_hostHandle == IntPtr.Zero)
            {
                _hostHandle = handle;
                _originalSmallIcon = SendMessage(handle, WmGetIcon, new IntPtr(IconSmall), IntPtr.Zero);
                _originalBigIcon = SendMessage(handle, WmGetIcon, new IntPtr(IconBig), IntPtr.Zero);
            }

            if (_bltSmallIcon == IntPtr.Zero)
                _bltSmallIcon = LoadEmbeddedIcon(16, 16);
            if (_bltBigIcon == IntPtr.Zero)
                _bltBigIcon = LoadEmbeddedIcon(32, 32);

            if (_bltSmallIcon != IntPtr.Zero)
                SendMessage(handle, WmSetIcon, new IntPtr(IconSmall), _bltSmallIcon);
            if (_bltBigIcon != IntPtr.Zero)
                SendMessage(handle, WmSetIcon, new IntPtr(IconBig), _bltBigIcon);
        }

        private static IntPtr ResolveHostWindowHandle(DependencyObject ribbonRoot, Window? window)
        {
            try
            {
                if (window != null)
                {
                    var wpfHandle = new WindowInteropHelper(window).Handle;
                    if (wpfHandle != IntPtr.Zero)
                        return wpfHandle;
                }
            }
            catch { }

            try
            {
                if (ribbonRoot is Visual visual)
                {
                    var source = PresentationSource.FromVisual(visual) as HwndSource;
                    if (source != null && source.Handle != IntPtr.Zero)
                    {
                        var root = GetAncestor(source.Handle, GaRoot);
                        return root != IntPtr.Zero ? root : source.Handle;
                    }
                }
            }
            catch { }

            try
            {
                return Process.GetCurrentProcess().MainWindowHandle;
            }
            catch
            {
                return IntPtr.Zero;
            }
        }

        private static IntPtr LoadEmbeddedIcon(int width, int height)
        {
            var path = Path.Combine(
                Path.GetTempPath(),
                "qs3d-blt3d-icon-" + Guid.NewGuid().ToString("N") + ".ico");
            try
            {
                File.WriteAllBytes(path, Convert.FromBase64String(Blt3dIconIcoBase64));
                return LoadImage(IntPtr.Zero, path, ImageIcon, width, height, LoadFromFile | LoadDefaultColor);
            }
            finally
            {
                try { File.Delete(path); } catch { }
            }
        }

        private static byte[] ExtractEmbeddedPngFromIco()
        {
            var ico = Convert.FromBase64String(Blt3dIconIcoBase64);
            if (ico.Length < 22)
                throw new InvalidDataException("BLT3D icon payload is incomplete.");

            var length = BitConverter.ToInt32(ico, 14);
            var offset = BitConverter.ToInt32(ico, 18);
            if (length <= 0 || offset < 0 || offset + length > ico.Length)
                throw new InvalidDataException("BLT3D icon directory entry is invalid.");

            var png = new byte[length];
            Buffer.BlockCopy(ico, offset, png, 0, length);
            return png;
        }

        private static void DestroyOwnedIcon(ref IntPtr icon)
        {
            if (icon == IntPtr.Zero)
                return;

            try { DestroyIcon(icon); } catch { }
            icon = IntPtr.Zero;
        }

        private static void CollapseKnownChromeProperty(object control, string propertyName)
        {
            try
            {
                var value = GetProperty(control, propertyName);
                if (value is FrameworkElement element)
                    Hide(element);
            }
            catch
            {
                // Property names vary by BricsCAD host version; the visual-tree fallback follows.
            }
        }

        private static void CollapseNonReferenceChrome(DependencyObject root)
        {
            var pending = new Stack<DependencyObject>();
            pending.Push(root);

            while (pending.Count > 0)
            {
                var current = pending.Pop();
                var childCount = VisualTreeHelper.GetChildrenCount(current);
                for (var index = childCount - 1; index >= 0; index--)
                {
                    var child = VisualTreeHelper.GetChild(current, index);
                    if (child is FrameworkElement element && IsNonReferenceTopbarChrome(element))
                    {
                        Hide(element);
                        continue;
                    }

                    pending.Push(child);
                }
            }
        }

        private static bool IsNonReferenceTopbarChrome(FrameworkElement element)
        {
            var descriptor = (element.GetType().Name + " " + element.Name).Replace("_", string.Empty);
            return Contains(descriptor, "QuickAccess")
                   || Contains(descriptor, "ApplicationButton")
                   || Contains(descriptor, "ApplicationMenuButton")
                   || Contains(descriptor, "RibbonSearch")
                   || Contains(descriptor, "SearchBox")
                   || Contains(descriptor, "SearchControl")
                   || Contains(descriptor, "InfoCenter");
        }

        private static bool Contains(string value, string token) =>
            value.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;

        private static void Hide(FrameworkElement element)
        {
            if (HiddenElements.Exists(entry => ReferenceEquals(entry.Element, element)))
                return;

            HiddenElements.Add(new HiddenChrome(element, element.Visibility));
            element.Visibility = Visibility.Collapsed;
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

        private static object? GetProperty(object target, string name) =>
            target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public)?.GetValue(target, null);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadImage(
            IntPtr hInstance,
            string name,
            uint type,
            int width,
            int height,
            uint loadFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr GetAncestor(IntPtr hWnd, uint flags);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyIcon(IntPtr hIcon);
    }
}
