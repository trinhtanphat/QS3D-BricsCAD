using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using Teigha.Runtime;
using Application = Bricscad.ApplicationServices.Application;
using WpfApplication = System.Windows.Application;

namespace QS3D.BricsCAD.V25
{
    internal enum Qs3dThemeMode
    {
        System,
        Dark,
        Light
    }

    /// <summary>
    /// Canonical host-wide QS3D theme owner. The MCP Agent Center selector, QS3D WPF
    /// surfaces and BricsCAD COLORTHEME all converge here. System mode follows the
    /// Windows app-theme preference and resolves it to BricsCAD's dark/light host mode.
    /// </summary>
    internal static class Qs3dThemeCoordinator
    {
        private const int MaxTrackedElements = 2048;
        private const int MaxVisualNodesPerApply = 12000;
        private const string ThemeFileName = "theme-mode.txt";
        private static readonly object Gate = new object();
        private static readonly List<WeakReference> TrackedElements = new List<WeakReference>();
        private static bool _started;
        private static bool _classHandlersRegistered;
        private static Qs3dThemeMode _mode = Qs3dThemeMode.System;

        internal static string ThemeFilePath
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QS3D", ThemeFileName); }
        }

        internal static void Start()
        {
            lock (Gate)
            {
                if (_started) return;
                _mode = LoadMode();
                if (!_classHandlersRegistered)
                {
                    EventManager.RegisterClassHandler(
                        typeof(FrameworkElement),
                        FrameworkElement.LoadedEvent,
                        new RoutedEventHandler(OnFrameworkElementLoaded),
                        true);
                    EventManager.RegisterClassHandler(
                        typeof(Button),
                        Button.ClickEvent,
                        new RoutedEventHandler(OnAnyButtonClick),
                        true);
                    _classHandlersRegistered = true;
                }
                _started = true;
            }

            SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
            ApplyCurrentTheme("startup");
        }

        internal static void Stop()
        {
            lock (Gate)
            {
                if (!_started) return;
                _started = false;
                TrackedElements.Clear();
            }
            try { SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged; } catch { }
        }

        internal static Qs3dThemeMode CurrentMode
        {
            get { lock (Gate) return _mode; }
        }

        internal static bool EffectiveDark
        {
            get { return ResolveEffectiveDark(CurrentMode); }
        }

        internal static string Describe()
        {
            var mode = CurrentMode;
            var effectiveDark = ResolveEffectiveDark(mode);
            var colorTheme = "unknown";
            try { colorTheme = Convert.ToString(Application.GetSystemVariable("COLORTHEME"), CultureInfo.InvariantCulture) ?? "unknown"; }
            catch { }
            return "mode=" + ModeText(mode)
                   + "; effective=" + (effectiveDark ? "dark" : "light")
                   + "; bricscad.COLORTHEME=" + colorTheme;
        }

        internal static void SetMode(Qs3dThemeMode mode, string source)
        {
            lock (Gate) _mode = mode;
            PersistMode(mode);
            ApplyCurrentTheme(source ?? "theme-change");
        }

        private static void ApplyCurrentTheme(string reason)
        {
            var mode = CurrentMode;
            var dark = ResolveEffectiveDark(mode);
            QueueBricsCadTheme(dark);
            ApplyTrackedWpfTheme(dark);
            McpDiagnosticHub.Record(
                "theme",
                "info",
                "theme-applied",
                "mode=" + ModeText(mode) + "; effective=" + (dark ? "dark" : "light") + "; reason=" + (reason ?? string.Empty));
        }

        private static void QueueBricsCadTheme(bool dark)
        {
            try { Application.DocumentManager.ExecuteInApplicationContext(ApplyBricsCadThemeInContext, dark); }
            catch (Exception ex) { McpDiagnosticHub.Record("theme", "warning", "bricscad-theme-queue-failed", ex.Message); }
        }

        private static void ApplyBricsCadThemeInContext(object state)
        {
            var dark = state is bool && (bool)state;
            try
            {
                var desired = dark ? 0 : 1;
                var current = Convert.ToInt32(Application.GetSystemVariable("COLORTHEME"), CultureInfo.InvariantCulture);
                if (current != desired)
                    Application.SetSystemVariable("COLORTHEME", (short)desired);
            }
            catch (Exception ex)
            {
                McpDiagnosticHub.Record("theme", "warning", "bricscad-theme-apply-failed", ex.Message);
            }
        }

        private static void ApplyTrackedWpfTheme(bool dark)
        {
            List<FrameworkElement> elements;
            lock (Gate)
            {
                elements = new List<FrameworkElement>();
                for (var i = TrackedElements.Count - 1; i >= 0; i--)
                {
                    var target = TrackedElements[i].Target as FrameworkElement;
                    if (target == null)
                    {
                        TrackedElements.RemoveAt(i);
                        continue;
                    }
                    elements.Add(target);
                }
            }

            try
            {
                var app = WpfApplication.Current;
                if (app != null)
                {
                    foreach (Window window in app.Windows)
                        if (window != null) elements.Add(window);
                    ApplyDictionary(app.Resources, dark);
                }
            }
            catch { }

            foreach (var element in elements)
                ApplyElementOnDispatcher(element, dark);
        }

        private static void ApplyElementOnDispatcher(FrameworkElement element, bool dark)
        {
            try
            {
                if (element.Dispatcher == null || element.Dispatcher.CheckAccess())
                    ApplyElement(element, dark);
                else
                    element.Dispatcher.BeginInvoke(new Action(() => ApplyElement(element, dark)));
            }
            catch { }
        }

        private static void ApplyElement(FrameworkElement element, bool dark)
        {
            try
            {
                ApplyDictionary(element.Resources, dark);
                var nodes = 0;
                ApplyVisualTree(element, dark, ref nodes);
            }
            catch { }
        }

        private static void ApplyDictionary(ResourceDictionary dictionary, bool dark)
        {
            if (dictionary == null) return;
            foreach (var merged in dictionary.MergedDictionaries)
                ApplyDictionary(merged, dark);

            ApplyColorKey(dictionary, "BgCanvas", dark ? Rgb(14, 17, 22) : Rgb(244, 246, 248));
            ApplyColorKey(dictionary, "BgPanel", dark ? Rgb(21, 26, 33) : Rgb(255, 255, 255));
            ApplyColorKey(dictionary, "BgElevated", dark ? Rgb(28, 34, 43) : Rgb(247, 249, 251));
            ApplyColorKey(dictionary, "BgRaised", dark ? Rgb(34, 42, 52) : Rgb(238, 242, 246));
            ApplyColorKey(dictionary, "BgInput", dark ? Rgb(16, 21, 28) : Rgb(255, 255, 255));
            ApplyColorKey(dictionary, "BgHover", dark ? Rgb(37, 46, 57) : Rgb(234, 242, 250));
            ApplyColorKey(dictionary, "BgSelected", dark ? Rgb(18, 58, 98) : Rgb(215, 234, 254));
            ApplyColorKey(dictionary, "BgPressed", dark ? Rgb(15, 78, 130) : Rgb(197, 223, 249));
            ApplyColorKey(dictionary, "BorderWeak", dark ? Rgb(41, 49, 59) : Rgb(216, 222, 230));
            ApplyColorKey(dictionary, "BorderStrong", dark ? Rgb(61, 73, 86) : Rgb(183, 193, 204));
            ApplyColorKey(dictionary, "BorderFocus", dark ? Rgb(99, 178, 255) : Rgb(22, 119, 210));
            ApplyColorKey(dictionary, "BorderLuxury", dark ? Rgb(107, 92, 62) : Rgb(169, 139, 85));
            ApplyColorKey(dictionary, "TextPrimary", dark ? Rgb(244, 247, 250) : Rgb(24, 33, 43));
            ApplyColorKey(dictionary, "TextSecondary", dark ? Rgb(183, 193, 204) : Rgb(80, 93, 107));
            ApplyColorKey(dictionary, "TextMuted", dark ? Rgb(140, 152, 166) : Rgb(111, 124, 137));
            ApplyColorKey(dictionary, "TextDisabled", dark ? Rgb(102, 113, 126) : Rgb(152, 162, 173));
            ApplyColorKey(dictionary, "Accent", dark ? Rgb(47, 141, 255) : Rgb(22, 119, 210));
            ApplyColorKey(dictionary, "AccentHover", dark ? Rgb(99, 176, 255) : Rgb(45, 137, 220));
            ApplyColorKey(dictionary, "AccentPressed", dark ? Rgb(30, 115, 205) : Rgb(15, 96, 175));
            ApplyColorKey(dictionary, "AccentSoft", dark ? Rgb(21, 58, 91) : Rgb(220, 237, 252));
            ApplyColorKey(dictionary, "Luxury", dark ? Rgb(211, 185, 127) : Rgb(139, 108, 51));
            ApplyColorKey(dictionary, "LuxuryMuted", dark ? Rgb(165, 139, 91) : Rgb(151, 121, 67));
            ApplyColorKey(dictionary, "LuxurySoft", dark ? Rgb(51, 45, 34) : Rgb(247, 240, 226));
            ApplyColorKey(dictionary, "Success", dark ? Rgb(71, 194, 138) : Rgb(31, 137, 91));
            ApplyColorKey(dictionary, "Warning", dark ? Rgb(224, 174, 85) : Rgb(160, 107, 14));
            ApplyColorKey(dictionary, "Danger", dark ? Rgb(240, 106, 98) : Rgb(190, 55, 48));
            ApplyColorKey(dictionary, "DangerHover", dark ? Rgb(255, 129, 120) : Rgb(207, 69, 61));
            ApplyColorKey(dictionary, "DangerSurface", dark ? Rgb(67, 40, 37) : Rgb(253, 234, 232));
            ApplyColorKey(dictionary, "DangerPressed", dark ? Rgb(100, 47, 42) : Rgb(172, 43, 37));

            ApplyBrushKey(dictionary, "Bg0Brush", dark ? Rgb(14, 17, 22) : Rgb(244, 246, 248));
            ApplyBrushKey(dictionary, "Bg1Brush", dark ? Rgb(21, 26, 33) : Rgb(255, 255, 255));
            ApplyBrushKey(dictionary, "Bg2Brush", dark ? Rgb(28, 34, 43) : Rgb(247, 249, 251));
            ApplyBrushKey(dictionary, "BgRaisedBrush", dark ? Rgb(34, 42, 52) : Rgb(238, 242, 246));
            ApplyBrushKey(dictionary, "BgInputBrush", dark ? Rgb(16, 21, 28) : Rgb(255, 255, 255));
            ApplyBrushKey(dictionary, "BgHoverBrush", dark ? Rgb(37, 46, 57) : Rgb(234, 242, 250));
            ApplyBrushKey(dictionary, "BgSelectedBrush", dark ? Rgb(18, 58, 98) : Rgb(215, 234, 254));
            ApplyBrushKey(dictionary, "BgPressedBrush", dark ? Rgb(15, 78, 130) : Rgb(197, 223, 249));
            ApplyBrushKey(dictionary, "BorderBrush", dark ? Rgb(41, 49, 59) : Rgb(216, 222, 230));
            ApplyBrushKey(dictionary, "BorderStrongBrush", dark ? Rgb(61, 73, 86) : Rgb(183, 193, 204));
            ApplyBrushKey(dictionary, "BorderFocusBrush", dark ? Rgb(99, 178, 255) : Rgb(22, 119, 210));
            ApplyBrushKey(dictionary, "BorderLuxuryBrush", dark ? Rgb(107, 92, 62) : Rgb(169, 139, 85));
            ApplyBrushKey(dictionary, "TextBrush", dark ? Rgb(244, 247, 250) : Rgb(24, 33, 43));
            ApplyBrushKey(dictionary, "MutedBrush", dark ? Rgb(183, 193, 204) : Rgb(80, 93, 107));
            ApplyBrushKey(dictionary, "SubtleTextBrush", dark ? Rgb(140, 152, 166) : Rgb(111, 124, 137));
            ApplyBrushKey(dictionary, "DisabledTextBrush", dark ? Rgb(102, 113, 126) : Rgb(152, 162, 173));
            ApplyBrushKey(dictionary, "AccentBrush", dark ? Rgb(47, 141, 255) : Rgb(22, 119, 210));
            ApplyBrushKey(dictionary, "AccentHoverBrush", dark ? Rgb(99, 176, 255) : Rgb(45, 137, 220));
            ApplyBrushKey(dictionary, "AccentPressedBrush", dark ? Rgb(30, 115, 205) : Rgb(15, 96, 175));
            ApplyBrushKey(dictionary, "AccentSoftBrush", dark ? Rgb(21, 58, 91) : Rgb(220, 237, 252));
            ApplyBrushKey(dictionary, "LuxuryBrush", dark ? Rgb(211, 185, 127) : Rgb(139, 108, 51));
            ApplyBrushKey(dictionary, "LuxuryMutedBrush", dark ? Rgb(165, 139, 91) : Rgb(151, 121, 67));
            ApplyBrushKey(dictionary, "LuxurySoftBrush", dark ? Rgb(51, 45, 34) : Rgb(247, 240, 226));
            ApplyBrushKey(dictionary, "SuccessBrush", dark ? Rgb(71, 194, 138) : Rgb(31, 137, 91));
            ApplyBrushKey(dictionary, "WarningBrush", dark ? Rgb(224, 174, 85) : Rgb(160, 107, 14));
            ApplyBrushKey(dictionary, "DangerBrush", dark ? Rgb(240, 106, 98) : Rgb(190, 55, 48));
            ApplyBrushKey(dictionary, "DangerHoverBrush", dark ? Rgb(255, 129, 120) : Rgb(207, 69, 61));
            ApplyBrushKey(dictionary, "DangerSurfaceBrush", dark ? Rgb(67, 40, 37) : Rgb(253, 234, 232));
            ApplyBrushKey(dictionary, "DangerPressedBrush", dark ? Rgb(100, 47, 42) : Rgb(172, 43, 37));

            var selection = new SolidColorBrush(dark ? Rgb(18, 58, 98) : Rgb(215, 234, 254));
            var selectionText = new SolidColorBrush(dark ? Rgb(244, 247, 250) : Rgb(24, 33, 43));
            dictionary[SystemColors.HighlightBrushKey] = selection;
            dictionary[SystemColors.InactiveSelectionHighlightBrushKey] = selection;
            dictionary[SystemColors.HighlightTextBrushKey] = selectionText;
            dictionary[SystemColors.InactiveSelectionHighlightTextBrushKey] = selectionText;
        }

        private static void ApplyColorKey(ResourceDictionary dictionary, string key, Color color)
        {
            try
            {
                if (dictionary.Contains(key)) dictionary[key] = color;
            }
            catch { }
        }

        private static void ApplyBrushKey(ResourceDictionary dictionary, string key, Color color)
        {
            try
            {
                if (!dictionary.Contains(key)) return;
                var brush = dictionary[key] as SolidColorBrush;
                if (brush != null && !brush.IsFrozen)
                {
                    brush.Color = color;
                    return;
                }
                dictionary[key] = new SolidColorBrush(color);
            }
            catch { }
        }

        private static void ApplyVisualTree(DependencyObject node, bool dark, ref int visited)
        {
            if (node == null || visited++ >= MaxVisualNodesPerApply) return;

            var control = node as Control;
            if (control != null)
            {
                control.Background = MapBackgroundBrush(control.Background, dark);
                control.BorderBrush = MapBorderBrush(control.BorderBrush, dark);
                control.Foreground = MapForegroundBrush(control.Foreground, dark, HasAccentAncestor(control));
            }

            var panel = node as Panel;
            if (panel != null) panel.Background = MapBackgroundBrush(panel.Background, dark);

            var border = node as Border;
            if (border != null)
            {
                border.Background = MapBackgroundBrush(border.Background, dark);
                border.BorderBrush = MapBorderBrush(border.BorderBrush, dark);
            }

            var text = node as TextBlock;
            if (text != null) text.Foreground = MapForegroundBrush(text.Foreground, dark, HasAccentAncestor(text));

            var count = 0;
            try { count = VisualTreeHelper.GetChildrenCount(node); } catch { }
            for (var i = 0; i < count && visited < MaxVisualNodesPerApply; i++)
            {
                DependencyObject child;
                try { child = VisualTreeHelper.GetChild(node, i); }
                catch { continue; }
                ApplyVisualTree(child, dark, ref visited);
            }
        }

        private static Brush? MapBackgroundBrush(Brush? brush, bool dark)
        {
            var solid = brush as SolidColorBrush;
            if (solid == null) return brush;
            var mapped = MapBackgroundColor(solid.Color, dark);
            return mapped == solid.Color ? brush : CloneBrush(solid, mapped);
        }

        private static Brush? MapBorderBrush(Brush? brush, bool dark)
        {
            var solid = brush as SolidColorBrush;
            if (solid == null) return brush;
            var mapped = MapBorderColor(solid.Color, dark);
            return mapped == solid.Color ? brush : CloneBrush(solid, mapped);
        }

        private static Brush? MapForegroundBrush(Brush? brush, bool dark, bool keepLightOnAccent)
        {
            var solid = brush as SolidColorBrush;
            if (solid == null) return brush;
            var mapped = MapForegroundColor(solid.Color, dark, keepLightOnAccent);
            return mapped == solid.Color ? brush : CloneBrush(solid, mapped);
        }

        private static SolidColorBrush CloneBrush(SolidColorBrush source, Color color)
        {
            return new SolidColorBrush(color) { Opacity = source.Opacity };
        }

        private static Color MapBackgroundColor(Color color, bool dark)
        {
            if (dark)
            {
                if (Same(color, 244, 246, 248)) return Rgb(14, 17, 22);
                if (Same(color, 255, 255, 255)) return Rgb(21, 26, 33);
                if (Same(color, 247, 249, 251)) return Rgb(28, 34, 43);
                if (Same(color, 238, 242, 246) || Same(color, 240, 243, 247)) return Rgb(34, 42, 52);
                if (Same(color, 234, 242, 250)) return Rgb(37, 46, 57);
                if (Same(color, 215, 234, 254)) return Rgb(18, 58, 98);
                if (Same(color, 232, 237, 243)) return Rgb(34, 34, 34);
                return color;
            }

            if (Same(color, 14, 17, 22) || Same(color, 29, 29, 29)) return Rgb(244, 246, 248);
            if (Same(color, 21, 26, 33) || Same(color, 39, 39, 39)) return Rgb(255, 255, 255);
            if (Same(color, 28, 34, 43)) return Rgb(247, 249, 251);
            if (Same(color, 34, 42, 52) || Same(color, 35, 35, 35)) return Rgb(238, 242, 246);
            if (Same(color, 16, 21, 28)) return Rgb(255, 255, 255);
            if (Same(color, 37, 46, 57) || Same(color, 47, 47, 47)) return Rgb(234, 242, 250);
            if (Same(color, 18, 58, 98)) return Rgb(215, 234, 254);
            if (Same(color, 15, 78, 130)) return Rgb(197, 223, 249);
            if (Same(color, 34, 34, 34)) return Rgb(232, 237, 243);
            return color;
        }

        private static Color MapBorderColor(Color color, bool dark)
        {
            if (dark)
            {
                if (Same(color, 216, 222, 230)) return Rgb(41, 49, 59);
                if (Same(color, 183, 193, 204)) return Rgb(61, 73, 86);
                if (Same(color, 200, 208, 218)) return Rgb(67, 67, 67);
                if (Same(color, 168, 179, 192)) return Rgb(86, 91, 99);
                return color;
            }

            if (Same(color, 41, 49, 59) || Same(color, 48, 48, 48)) return Rgb(216, 222, 230);
            if (Same(color, 61, 73, 86) || Same(color, 67, 67, 67)) return Rgb(183, 193, 204);
            if (Same(color, 86, 91, 99)) return Rgb(168, 179, 192);
            return color;
        }

        private static Color MapForegroundColor(Color color, bool dark, bool keepLightOnAccent)
        {
            if (dark)
            {
                if (Same(color, 24, 33, 43)) return Rgb(244, 247, 250);
                if (Same(color, 80, 93, 107)) return Rgb(183, 193, 204);
                if (Same(color, 111, 124, 137)) return Rgb(140, 152, 166);
                if (Same(color, 91, 102, 115)) return Rgb(174, 179, 188);
                if (Same(color, 152, 162, 173)) return Rgb(102, 113, 126);
                return color;
            }

            if (Same(color, 244, 247, 250)) return Rgb(24, 33, 43);
            if (Same(color, 183, 193, 204)) return Rgb(80, 93, 107);
            if (Same(color, 140, 152, 166)) return Rgb(111, 124, 137);
            if (Same(color, 174, 179, 188)) return Rgb(91, 102, 115);
            if (Same(color, 102, 113, 126)) return Rgb(152, 162, 173);
            if (Same(color, 255, 255, 255) && !keepLightOnAccent) return Rgb(24, 33, 43);
            return color;
        }

        private static bool HasAccentAncestor(DependencyObject node)
        {
            var current = node;
            for (var i = 0; i < 8 && current != null; i++)
            {
                var control = current as Control;
                if (control != null && IsAccentBrush(control.Background)) return true;
                var border = current as Border;
                if (border != null && IsAccentBrush(border.Background)) return true;
                try { current = VisualTreeHelper.GetParent(current); }
                catch { current = null; }
            }
            return false;
        }

        private static bool IsAccentBrush(Brush? brush)
        {
            var solid = brush as SolidColorBrush;
            if (solid == null) return false;
            var c = solid.Color;
            return Same(c, 20, 113, 236)
                   || Same(c, 47, 141, 255)
                   || Same(c, 22, 119, 210)
                   || Same(c, 240, 106, 98)
                   || Same(c, 190, 55, 48);
        }

        private static void OnFrameworkElementLoaded(object sender, RoutedEventArgs e)
        {
            var element = sender as FrameworkElement;
            if (element == null) return;
            bool active;
            bool dark;
            lock (Gate)
            {
                active = _started;
                dark = ResolveEffectiveDark(_mode);
                if (!active) return;
                TrackElementLocked(element);
            }
            ApplyElement(element, dark);
        }

        private static void TrackElementLocked(FrameworkElement element)
        {
            for (var i = TrackedElements.Count - 1; i >= 0; i--)
            {
                var existing = TrackedElements[i].Target;
                if (existing == null) TrackedElements.RemoveAt(i);
                else if (ReferenceEquals(existing, element)) return;
            }
            if (TrackedElements.Count >= MaxTrackedElements) TrackedElements.RemoveAt(0);
            TrackedElements.Add(new WeakReference(element));
        }

        private static void OnAnyButtonClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;
            lock (Gate) if (!_started) return;
            var window = Window.GetWindow(button);
            if (!(window is McpAgentControlCenterWindow)) return;
            var label = Convert.ToString(button.Content, CultureInfo.InvariantCulture) ?? string.Empty;
            if (string.Equals(label, "System", StringComparison.OrdinalIgnoreCase)) SetMode(Qs3dThemeMode.System, "mcp-agent-center");
            else if (string.Equals(label, "Dark", StringComparison.OrdinalIgnoreCase)) SetMode(Qs3dThemeMode.Dark, "mcp-agent-center");
            else if (string.Equals(label, "Light", StringComparison.OrdinalIgnoreCase)) SetMode(Qs3dThemeMode.Light, "mcp-agent-center");
        }

        private static void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (CurrentMode != Qs3dThemeMode.System) return;
            ApplyCurrentTheme("windows-user-preference");
        }

        private static Qs3dThemeMode LoadMode()
        {
            try
            {
                if (!File.Exists(ThemeFilePath)) return Qs3dThemeMode.System;
                return ParseMode(File.ReadAllText(ThemeFilePath).Trim());
            }
            catch { return Qs3dThemeMode.System; }
        }

        private static void PersistMode(Qs3dThemeMode mode)
        {
            try
            {
                var directory = Path.GetDirectoryName(ThemeFilePath);
                if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
                File.WriteAllText(ThemeFilePath, ModeText(mode) + Environment.NewLine);
            }
            catch (Exception ex)
            {
                McpDiagnosticHub.Record("theme", "warning", "theme-persist-failed", ex.Message);
            }
        }

        private static Qs3dThemeMode ParseMode(string value)
        {
            if (string.Equals(value, "dark", StringComparison.OrdinalIgnoreCase)) return Qs3dThemeMode.Dark;
            if (string.Equals(value, "light", StringComparison.OrdinalIgnoreCase)) return Qs3dThemeMode.Light;
            return Qs3dThemeMode.System;
        }

        private static string ModeText(Qs3dThemeMode mode)
        {
            if (mode == Qs3dThemeMode.Dark) return "dark";
            if (mode == Qs3dThemeMode.Light) return "light";
            return "system";
        }

        private static bool ResolveEffectiveDark(Qs3dThemeMode mode)
        {
            if (mode == Qs3dThemeMode.Dark) return true;
            if (mode == Qs3dThemeMode.Light) return false;
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    var value = key == null ? null : key.GetValue("AppsUseLightTheme");
                    if (value is int) return (int)value == 0;
                }
            }
            catch { }
            try
            {
                var current = Convert.ToInt32(Application.GetSystemVariable("COLORTHEME"), CultureInfo.InvariantCulture);
                return current == 0;
            }
            catch { return true; }
        }

        private static Color Rgb(byte r, byte g, byte b) { return Color.FromRgb(r, g, b); }
        private static bool Same(Color color, byte r, byte g, byte b)
        {
            return color.R == r && color.G == g && color.B == b;
        }
    }

    public sealed class Qs3dThemeCommands
    {
        [CommandMethod("QS3DTHEMESYSTEM", CommandFlags.Modal)]
        public void SystemTheme() { Qs3dThemeCoordinator.SetMode(Qs3dThemeMode.System, "command"); }

        [CommandMethod("QS3DTHEMEDARK", CommandFlags.Modal)]
        public void DarkTheme() { Qs3dThemeCoordinator.SetMode(Qs3dThemeMode.Dark, "command"); }

        [CommandMethod("QS3DTHEMELIGHT", CommandFlags.Modal)]
        public void LightTheme() { Qs3dThemeCoordinator.SetMode(Qs3dThemeMode.Light, "command"); }

        [CommandMethod("QS3DTHEMESTATUS", CommandFlags.Modal)]
        public void ThemeStatus()
        {
            McpDiagnosticHub.Record("theme", "info", "theme-status", Qs3dThemeCoordinator.Describe(), Application.DocumentManager.MdiActiveDocument);
            try { Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage("\nQS3D theme: " + Qs3dThemeCoordinator.Describe()); }
            catch { }
        }
    }
}
