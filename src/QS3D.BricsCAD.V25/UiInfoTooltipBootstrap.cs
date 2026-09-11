using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace QS3D.BricsCAD.V25
{
    internal static class UiInfoTooltipBootstrap
    {
        private const string AgentCenterWindowName = "McpAgentControlCenterWindow";
        private const string UpdateCenterWindowName = "UpdateCenterWindow";
        private const string NextStepMarker = "Tiếp theo: ";
        private static int _registered;

        private static readonly DependencyProperty HookedProperty = DependencyProperty.RegisterAttached(
            "Hooked",
            typeof(bool),
            typeof(UiInfoTooltipBootstrap),
            new PropertyMetadata(false));

        private static readonly DependencyProperty DecoratedProperty = DependencyProperty.RegisterAttached(
            "Decorated",
            typeof(bool),
            typeof(UiInfoTooltipBootstrap),
            new PropertyMetadata(false));

        internal static void EnsureRegistered()
        {
            if (Interlocked.CompareExchange(ref _registered, 1, 0) != 0)
                return;

            try
            {
                EventManager.RegisterClassHandler(
                    typeof(Window),
                    FrameworkElement.LoadedEvent,
                    new RoutedEventHandler(OnWindowLoaded));
            }
            catch
            {
                // Optional presentation polish must not prevent host initialization.
                // Permit another explicit initialization attempt if WPF was not ready.
                Interlocked.Exchange(ref _registered, 0);
            }
        }

        private static void OnWindowLoaded(object sender, RoutedEventArgs args)
        {
            var window = sender as Window;
            if (window == null || !IsTargetWindow(window)) return;

            try
            {
                if (!(bool)window.GetValue(HookedProperty))
                {
                    window.SetValue(HookedProperty, true);
                    window.AddHandler(
                        ButtonBase.ClickEvent,
                        new RoutedEventHandler(OnTargetWindowButtonClick),
                        true);
                }

                Apply(window);
            }
            catch
            {
                // Keep the existing UI usable even if a future visual-tree change makes a matcher stale.
            }
        }

        private static bool IsTargetWindow(Window window)
        {
            var name = window.GetType().Name;
            return string.Equals(name, AgentCenterWindowName, StringComparison.Ordinal)
                || string.Equals(name, UpdateCenterWindowName, StringComparison.Ordinal);
        }

        private static void OnTargetWindowButtonClick(object sender, RoutedEventArgs args)
        {
            var window = sender as Window;
            if (window == null
                || !string.Equals(window.GetType().Name, AgentCenterWindowName, StringComparison.Ordinal))
                return;

            window.Dispatcher.BeginInvoke(
                DispatcherPriority.Loaded,
                new Action(() =>
                {
                    if (!window.IsLoaded) return;
                    try { Apply(window); }
                    catch { }
                }));
        }

        private static void Apply(Window window)
        {
            DecorateVisualTree(window, window.GetType().Name);
        }

        private static void DecorateVisualTree(DependencyObject root, string windowName)
        {
            var count = VisualTreeHelper.GetChildrenCount(root);
            if (count <= 0) return;

            var children = new List<DependencyObject>(count);
            for (var i = 0; i < count; i++)
                children.Add(VisualTreeHelper.GetChild(root, i));

            foreach (var child in children)
            {
                var textBlock = child as TextBlock;
                if (textBlock != null)
                    TryDecorateTextBlock(textBlock, windowName);
                else
                {
                    var textBox = child as TextBox;
                    if (textBox != null)
                        TryDecorateTextBox(textBox, windowName);
                }

                DecorateVisualTree(child, windowName);
            }
        }

        private static void TryDecorateTextBlock(TextBlock source, string windowName)
        {
            if ((bool)source.GetValue(DecoratedProperty)) return;
            var text = source.Text ?? string.Empty;
            if (text.Length == 0) return;

            string compactLabel;
            string accessibleName;
            if (string.Equals(windowName, AgentCenterWindowName, StringComparison.Ordinal))
            {
                if (!TryMatchAgentCenterText(text, out compactLabel, out accessibleName)) return;
            }
            else if (string.Equals(windowName, UpdateCenterWindowName, StringComparison.Ordinal))
            {
                if (!TryMatchUpdateCenterText(source, text, out compactLabel, out accessibleName)) return;
            }
            else
            {
                return;
            }

            ReplaceWithInfo(source, compactLabel, accessibleName);
        }

        private static void TryDecorateTextBox(TextBox source, string windowName)
        {
            if (!string.Equals(windowName, UpdateCenterWindowName, StringComparison.Ordinal)
                || (bool)source.GetValue(DecoratedProperty)
                || !source.IsReadOnly
                || source.TextWrapping != TextWrapping.Wrap
                || source.FontFamily == null
                || !string.Equals(source.FontFamily.Source, "Consolas", StringComparison.OrdinalIgnoreCase))
                return;

            ReplaceWithInfo(source, string.Empty, "Xem ghi chú phát hành");

            var parentGrid = source.Parent as Grid;
            if (parentGrid == null) return;
            var row = Grid.GetRow(source);
            if (row >= 0 && row < parentGrid.RowDefinitions.Count)
                parentGrid.RowDefinitions[row].Height = GridLength.Auto;
            parentGrid.VerticalAlignment = VerticalAlignment.Top;
        }

        private static bool TryMatchAgentCenterText(string text, out string compactLabel, out string accessibleName)
        {
            if (text.StartsWith("Kết nối, Agent desktop, backup/recovery", StringComparison.Ordinal))
            {
                compactLabel = "Thông tin";
                accessibleName = "Thông tin Agent Center";
                return true;
            }

            if (text.StartsWith("Chọn một transport.", StringComparison.Ordinal))
            {
                compactLabel = "Thông tin transport";
                accessibleName = "Giải thích lựa chọn transport";
                return true;
            }

            if (text.StartsWith("Chỉ hiển thị trạng thái thuộc transport", StringComparison.Ordinal))
            {
                compactLabel = "Giải thích trạng thái";
                accessibleName = "Giải thích trạng thái kết nối";
                return true;
            }

            if (text.StartsWith("Runtime API key ·", StringComparison.Ordinal))
            {
                compactLabel = "Runtime API key";
                accessibleName = "Thông tin lưu và dùng Runtime API key";
                return true;
            }

            if (text.StartsWith("Secure Tunnel: ChatGPT chọn Connection = Tunnel", StringComparison.Ordinal))
            {
                compactLabel = "Bảo mật Secure Tunnel";
                accessibleName = "Thông tin bảo mật Secure Tunnel";
                return true;
            }

            if (text.StartsWith("Quick Tunnel có hostname thay đổi", StringComparison.Ordinal))
            {
                compactLabel = "Lưu ý Quick Tunnel";
                accessibleName = "Lưu ý về Quick Tunnel";
                return true;
            }

            var nextStepIndex = text.IndexOf(NextStepMarker, StringComparison.Ordinal);
            if (nextStepIndex >= 0)
            {
                compactLabel = CreateNextStepLabel(text, nextStepIndex);
                accessibleName = "Hướng dẫn bước tiếp theo";
                return true;
            }

            compactLabel = string.Empty;
            accessibleName = string.Empty;
            return false;
        }

        private static bool TryMatchUpdateCenterText(
            TextBlock source,
            string text,
            out string compactLabel,
            out string accessibleName)
        {
            if (text.IndexOf("DLL đang chạy:", StringComparison.Ordinal) >= 0)
            {
                compactLabel = "Build / DLL";
                accessibleName = "Thông tin build và DLL đang chạy";
                return true;
            }

            if (IsUpdateDetail(source))
            {
                compactLabel = "Chi tiết";
                accessibleName = "Chi tiết trạng thái cập nhật";
                return true;
            }

            if (text.StartsWith("Mặc định tắt:", StringComparison.Ordinal)
                || text.StartsWith("Bật: bản tải cài đặt", StringComparison.Ordinal))
            {
                compactLabel = "Cách hoạt động";
                accessibleName = "Cách cập nhật khi đóng BricsCAD hoạt động";
                return true;
            }

            compactLabel = string.Empty;
            accessibleName = string.Empty;
            return false;
        }

        private static bool IsUpdateDetail(TextBlock source)
        {
            return !double.IsNaN(source.LineHeight)
                && Math.Abs(source.LineHeight - 19d) < 0.01d
                && source.TextWrapping == TextWrapping.Wrap
                && source.Margin.Top >= 6d
                && source.Margin.Top <= 8d;
        }

        private static string CreateNextStepLabel(string text, int markerIndex)
        {
            var next = text.Substring(markerIndex).Trim();
            const int maxLength = 76;
            if (next.Length <= maxLength) return next;
            return next.Substring(0, maxLength - 1).TrimEnd() + "…";
        }

        private static void ReplaceWithInfo(FrameworkElement source, string compactLabel, string accessibleName)
        {
            var parent = source.Parent as Panel;
            if (parent == null) return;

            var index = parent.Children.IndexOf(source);
            if (index < 0) return;

            var tone = ResolveTone(source);
            var replacement = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = source.Margin
            };

            if (!string.IsNullOrWhiteSpace(compactLabel))
            {
                replacement.Children.Add(new TextBlock
                {
                    Text = compactLabel,
                    Foreground = tone,
                    FontSize = 11.5,
                    FontWeight = FontWeights.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    MaxWidth = 360,
                    Margin = new Thickness(0, 0, 7, 0)
                });
            }

            replacement.Children.Add(CreateInfoButton(source, accessibleName, tone));
            CopyGridPosition(source, replacement);
            Panel.SetZIndex(replacement, Panel.GetZIndex(source));

            source.SetValue(DecoratedProperty, true);
            source.Visibility = Visibility.Collapsed;
            parent.Children.Insert(index + 1, replacement);
        }

        private static Button CreateInfoButton(FrameworkElement source, string accessibleName, Brush tone)
        {
            var button = new Button
            {
                Content = "i",
                Width = 20,
                Height = 20,
                MinWidth = 20,
                MinHeight = 20,
                Padding = new Thickness(0),
                Background = Brushes.Transparent,
                Foreground = tone,
                BorderBrush = tone,
                BorderThickness = new Thickness(1),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                FontStyle = FontStyles.Italic,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = Cursors.Hand,
                Focusable = true,
                IsTabStop = true,
                Template = CreateInfoButtonTemplate()
            };

            button.SetValue(AutomationProperties.NameProperty, accessibleName);
            button.SetBinding(
                AutomationProperties.HelpTextProperty,
                new Binding("Text") { Source = source, Mode = BindingMode.OneWay });

            var tooltipText = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 460,
                LineHeight = 18,
                Margin = new Thickness(2)
            };
            tooltipText.SetBinding(
                TextBlock.TextProperty,
                new Binding("Text") { Source = source, Mode = BindingMode.OneWay });

            var tooltip = new ToolTip
            {
                Content = tooltipText,
                MaxWidth = 500,
                Padding = new Thickness(9, 7, 9, 7),
                Placement = PlacementMode.Bottom,
                PlacementTarget = button
            };

            button.ToolTip = tooltip;
            button.SetValue(ToolTipService.InitialShowDelayProperty, 180);
            button.SetValue(ToolTipService.ShowDurationProperty, 30000);

            button.MouseEnter += (_, __) => button.Opacity = 0.78d;
            button.MouseLeave += (_, __) => button.Opacity = 1d;
            button.GotKeyboardFocus += (_, __) =>
            {
                button.BorderThickness = new Thickness(2);
                tooltip.IsOpen = true;
            };
            button.LostKeyboardFocus += (_, __) =>
            {
                button.BorderThickness = new Thickness(1);
                tooltip.IsOpen = false;
            };
            button.Unloaded += (_, __) => tooltip.IsOpen = false;
            return button;
        }

        private static ControlTemplate CreateInfoButtonTemplate()
        {
            var border = new FrameworkElementFactory(typeof(Border), "InfoBorder");
            border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
            border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
            border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(10));

            var content = new FrameworkElementFactory(typeof(ContentPresenter));
            content.SetValue(ContentPresenter.ContentProperty, new TemplateBindingExtension(ContentControl.ContentProperty));
            content.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            content.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(content);

            return new ControlTemplate(typeof(Button)) { VisualTree = border };
        }

        private static Brush ResolveTone(FrameworkElement source)
        {
            var textBlock = source as TextBlock;
            if (textBlock != null && textBlock.Foreground != null) return textBlock.Foreground;

            var control = source as Control;
            if (control != null && control.Foreground != null) return control.Foreground;

            return SystemColors.ControlTextBrush;
        }

        private static void CopyGridPosition(FrameworkElement source, FrameworkElement replacement)
        {
            Grid.SetRow(replacement, Grid.GetRow(source));
            Grid.SetColumn(replacement, Grid.GetColumn(source));
            Grid.SetRowSpan(replacement, Grid.GetRowSpan(source));
            Grid.SetColumnSpan(replacement, Grid.GetColumnSpan(source));
        }
    }
}
