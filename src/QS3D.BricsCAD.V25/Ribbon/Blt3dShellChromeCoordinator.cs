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
    /// Applies the compact QS3D shell-level chrome used by the BLT3D-familiar workflow: the
    /// QS3D-owned red-X / green-V app mark plus a clean tab row without BricsCAD application/QAT/
    /// search chrome. The reference product is layout inspiration only; the mark below is original
    /// QS3D artwork generated in-repository and does not contain copied BLT3D pixels or assets.
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

        // Original QS3D clean-room mark. It contains a dark rounded tile with a red X and green V,
        // authored specifically for QS3D. The ICO carries 16px and 32px PNG frames so the same
        // deterministic mark works for WPF Window.Icon and native WM_SETICON on net48/net8.
        private const string Qs3dBrandIconIcoBase64 =
            "AAABAAIAEBAAAAAAIAAdAwAAJgAAACAgAAAAACAAcwEAAEMDAACJUE5HDQoaCgAAAA1JSERSAAAAEAAAABAIBgAAAB/z/2EAAALkSURBVHicpZM/TJx1GMc/z/O+995xtPR4j/aEtLTDlaKR1GiMMV10lCjRoTgYk6YORpYOhKRTDzZJ49CFqANxbhutJFITBzUOVuOfEGUgnI22chxQjzsK9O7e9/09DpTYQSef6Vm++T7J8/kIQKlU0qmpKXd2bOxAd9zv7aQfGP8yna0O2fTvJNdmZrb3M7K/jJ6bGI7j9mwURZ6JqRriHk0LJibOS6WSwA/OX/3o8nypVFLBTM6+fXG4vlqZ+2lxQdMiALTMOOgpsT1yjIGoUBwccj29fSNXP3h3Xs5PTB/cuPvHn1/c+qbrxUCS5ShRM2MwE/B5Y5eelAICZogKUTNymY6s9/RzZ7YO9584qpXmrqhZMl7othun+vXVsEteC7vkk4FjMt6bF189ERFR3xMXmeQGQk0XMuZ8l3Q2M+K/FIZcX63QF/giZjZ2OAcYAvQFPvUkIed5OAFLjBOvF8k/U5DW9YjlT8touVYjqz5X1jYptyIOeUqXKuVWmyuVGunIcM7R3mrTfbqHnucLbC02iG9HnHyjiNaADhEq7Zi5+jaBCmlPmdvcporj+AtHeXz8KdRXjo0cB+DOx78RxwlhGKIh8MCMvsBnJHeAtjPaBi9nswycCul/5wmOnOnl9KVnyQ3luffdGpu/1Ag6A8rlMloMQ3ZdwoUjOYrpFI3E0YhjBnIdvLma8OPln6GVkHsyRFMed2/8Ds5A9z6rN2s1Qs+nEiWYwMx6nZn1Os4ZGxkh+n6d6q01Ul0p/vphndrCBn7Wx5I9Pvw80DDjvWrNFrbus9SKAFhqtrm5tUM+8Fh+f5GNb6vsruygnmL8Q5efzmRNFO+xwJcv6/eTrJ9Sw/i6HVFQJRaImhHVr1bwAsXUHIYngpfOZE3MTEbfujh8b31l7vbSr+rMAEEAx36RIMper8HJwSHX07eHsj85OSnXZqc/Gz038cqhXH42eSjTQ5b+W6YPp+cnezMq/1fnvwGhv2gJjH87wgAAAABJRU5ErkJggolQTkcNChoKAAAADUlIRFIAAAAgAAAAIAgGAAAAc3p69AAAATpJREFUeJztl7ENwjAQRT+IQdKnSYcUUYUBGICKAZiGGqViAAaACiHRpUmfRRBUBse+O18UGwnBL23r/rvL2bGBX9eEm1itt4+YRsfDjvSafsJciulRmYW3yykqwHyxBOBXoleBVOZ2TLcSL4CU5hIE2QNGTZ6pxsaIBTBGtiE1Fh2gyTOVgXYdAJR1xc7N3IGi7bzAnFHRdiqAPsQD9/17I4g9EENu9rY5C6DJbEj2kpJWwM3+ujnrAaQMqTmp0SRFqYAxtyE02YsA0hYzc2VdeUZDKzGqAlxW2uxZAO1BFAquUZQekCBCgORR7KpoO7LzY/wTPADOLLSOylTzeYLngG1CjVGGQ/pC7AHKKFSdoRDJf0bfA2Buq+b2mkLUzbhXgZQQ3LWcfK2keJhQ5gDTA9wzKrb5XwDwBGcEk1KL9Yk7AAAAAElFTkSuQmCC";

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
        private static IntPtr _qs3dSmallIcon;
        private static IntPtr _qs3dBigIcon;

        public static bool TryInitialize()
        {
            if (_initialized)
                return Reassert();

            try
            {
                var control = FindRibbonControl();
                if (!(control is DependencyObject ribbonRoot))
                    return false;

                if (!ApplyChrome(control, ribbonRoot))
                    return false;

                _initialized = true;
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Idempotently reapplies QS3D-owned shell chrome after a host Ribbon/workspace transition.
        /// BricsCAD can recreate top-level visual/native shell state after initial Ribbon startup;
        /// keeping this operation explicit lets the tab lifecycle restore the X/V mark without
        /// rerunning feature augmenters or taking ownership of native Ribbon content.
        /// </summary>
        public static bool Reassert()
        {
            try
            {
                var control = FindRibbonControl();
                if (!(control is DependencyObject ribbonRoot))
                    return false;

                if (!ApplyChrome(control, ribbonRoot))
                    return false;

                _initialized = true;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool ApplyChrome(object control, DependencyObject ribbonRoot)
        {
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
            return true;
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

            DestroyOwnedIcon(ref _qs3dSmallIcon);
            DestroyOwnedIcon(ref _qs3dBigIcon);
            _hostWindow = null;
            _originalWpfIcon = null;
            _hostHandle = IntPtr.Zero;
            _originalSmallIcon = IntPtr.Zero;
            _originalBigIcon = IntPtr.Zero;
        }

        private static void ApplyWpfWindowIcon(Window window)
        {
            if (!ReferenceEquals(_hostWindow, window))
            {
                _hostWindow = window;
                _originalWpfIcon = window.Icon;
            }

            var iconBytes = ExtractLargestEmbeddedPngFromIco();
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
            if (_hostHandle != handle)
            {
                _hostHandle = handle;
                _originalSmallIcon = SendMessage(handle, WmGetIcon, new IntPtr(IconSmall), IntPtr.Zero);
                _originalBigIcon = SendMessage(handle, WmGetIcon, new IntPtr(IconBig), IntPtr.Zero);
            }

            if (_qs3dSmallIcon == IntPtr.Zero)
                _qs3dSmallIcon = LoadEmbeddedIcon(16, 16);
            if (_qs3dBigIcon == IntPtr.Zero)
                _qs3dBigIcon = LoadEmbeddedIcon(32, 32);

            if (_qs3dSmallIcon != IntPtr.Zero)
                SendMessage(handle, WmSetIcon, new IntPtr(IconSmall), _qs3dSmallIcon);
            if (_qs3dBigIcon != IntPtr.Zero)
                SendMessage(handle, WmSetIcon, new IntPtr(IconBig), _qs3dBigIcon);
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
                "qs3d-brand-icon-" + Guid.NewGuid().ToString("N") + ".ico");
            try
            {
                File.WriteAllBytes(path, Convert.FromBase64String(Qs3dBrandIconIcoBase64));
                return LoadImage(IntPtr.Zero, path, ImageIcon, width, height, LoadFromFile | LoadDefaultColor);
            }
            finally
            {
                try { File.Delete(path); } catch { }
            }
        }

        private static byte[] ExtractLargestEmbeddedPngFromIco()
        {
            var ico = Convert.FromBase64String(Qs3dBrandIconIcoBase64);
            if (ico.Length < 22)
                throw new InvalidDataException("QS3D brand icon payload is incomplete.");

            var count = BitConverter.ToUInt16(ico, 4);
            if (count == 0 || ico.Length < 6 + count * 16)
                throw new InvalidDataException("QS3D brand icon directory is incomplete.");

            var bestArea = -1;
            var bestLength = 0;
            var bestOffset = 0;
            for (var index = 0; index < count; index++)
            {
                var entry = 6 + index * 16;
                var width = ico[entry] == 0 ? 256 : ico[entry];
                var height = ico[entry + 1] == 0 ? 256 : ico[entry + 1];
                var length = BitConverter.ToInt32(ico, entry + 8);
                var offset = BitConverter.ToInt32(ico, entry + 12);
                if (length <= 0 || offset < 0 || offset + length > ico.Length)
                    continue;

                var area = width * height;
                if (area <= bestArea)
                    continue;

                bestArea = area;
                bestLength = length;
                bestOffset = offset;
            }

            if (bestLength <= 0)
                throw new InvalidDataException("QS3D brand icon directory has no valid image entry.");

            var png = new byte[bestLength];
            Buffer.BlockCopy(ico, bestOffset, png, 0, bestLength);
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
