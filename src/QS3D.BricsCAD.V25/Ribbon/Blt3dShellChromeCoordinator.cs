using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Media;

namespace QS3D.BricsCAD.V25.Ribbon
{
    /// <summary>
    /// Applies the compact QS3D shell-level chrome used by the BLT3D-familiar workflow without
    /// taking ownership of the BricsCAD host/application icon. Reference-product chrome remains
    /// layout inspiration only; QS3D never embeds screenshot-derived BLT3D pixels or status-marker
    /// artwork as the application icon.
    /// </summary>
    internal static class Blt3dShellChromeCoordinator
    {
        private const string AssemblyName = "BrxMgd";

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

        public static bool TryInitialize()
        {
            if (_initialized)
                return Reassert();

            try
            {
                var control = FindRibbonControl();
                if (!(control is DependencyObject ribbonRoot))
                    return false;

                ApplyChrome(control, ribbonRoot);
                _initialized = true;
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Idempotently reapplies only the QS3D shell visibility contract after a host
        /// Ribbon/workspace transition. Application-icon ownership remains with BricsCAD.
        /// </summary>
        public static bool Reassert()
        {
            try
            {
                var control = FindRibbonControl();
                if (!(control is DependencyObject ribbonRoot))
                    return false;

                ApplyChrome(control, ribbonRoot);
                _initialized = true;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void ApplyChrome(object control, DependencyObject ribbonRoot)
        {
            CollapseKnownChromeProperty(control, "QuickAccessToolBar");
            CollapseKnownChromeProperty(control, "QuickAccessToolbar");
            CollapseKnownChromeProperty(control, "ApplicationButton");
            CollapseKnownChromeProperty(control, "ApplicationMenuButton");
            CollapseKnownChromeProperty(control, "SearchBox");
            CollapseKnownChromeProperty(control, "SearchControl");
            CollapseNonReferenceChrome(ribbonRoot);
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
    }
}
